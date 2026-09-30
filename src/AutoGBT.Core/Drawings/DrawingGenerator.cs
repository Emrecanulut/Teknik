using System;
using System.IO;
using AutoGBT.Core.AI;
using AutoGBT.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace AutoGBT.Core.Drawings
{
    /// <summary>
    /// AutoGBT planını SolidWorks Drawing belgesine dönüştürür.
    /// </summary>
    public sealed class DrawingGenerator
    {
        private readonly ISldWorks _swApp;
        private readonly AutoGbtAssistant _assistant;

        public DrawingGenerator(ISldWorks swApp, AutoGbtAssistant? assistant = null)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
            _assistant = assistant ?? new AutoGbtAssistant();
        }

        public DrawingResult Generate(ModelSummary model, DrawingRequest request)
        {
            var result = new DrawingResult();
            try
            {
                var plan = _assistant.PlanDrawing(model, request);
                result.Plan = plan;

                var modelPath = string.IsNullOrWhiteSpace(model.FilePath)
                    ? RequireSavedModel()
                    : model.FilePath;

                var drawingPath = BuildOutputPath(modelPath, request.Kind);
                var sheetSize = SheetSizeMeters(request.SheetFormat);

                int errors = 0, warnings = 0;
                var drawDoc = (DrawingDoc)_swApp.NewDocument(
                    TemplatePathOrEmpty(),
                    (int)swDwgPaperSizes_e.swDwgPapersUserDefined,
                    sheetSize.Width,
                    sheetSize.Height);

                if (drawDoc == null)
                {
                    // Şablon bulunamazsa boş çizim oluştur.
                    var modelDoc = (ModelDoc2)_swApp.NewDocument("", (int)swDocumentTypes_e.swDocDRAWING, sheetSize.Width, sheetSize.Height);
                    drawDoc = modelDoc as DrawingDoc;
                }

                if (drawDoc == null)
                    throw new InvalidOperationException("SolidWorks çizim belgesi oluşturulamadı.");

                var drawModel = (ModelDoc2)drawDoc;
                ConfigureSheet(drawDoc, plan, sheetSize);

                foreach (var view in plan.Views)
                    CreateView(drawDoc, drawModel, modelPath, view, sheetSize, plan);

                PlaceNotes(drawModel, plan);
                UpdateTitleBlockNotes(drawModel, plan);

                drawModel.ForceRebuild3(true);
                drawModel.SaveAs3(drawingPath, (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                    (int)swSaveAsOptions_e.swSaveAsOptions_Silent);

                result.Success = true;
                result.DrawingPath = drawingPath;
                result.Message = $"AutoGBT {AutoGbtAssistant.ToTurkish(request.Kind)} teknik resmini oluşturdu.";
                result.Warnings.AddRange(plan.QualityChecks.FindAll(c => c.StartsWith("Uyarı", StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Teknik resim oluşturulamadı: " + ex.Message;
            }

            return result;
        }

        private string RequireSavedModel()
        {
            var model = _swApp.ActiveDoc as ModelDoc2
                ?? throw new InvalidOperationException("Aktif model yok.");
            var path = model.GetPathName();
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Önce parçayı kaydedin; çizim görünüşleri kayıtlı dosya yoluna ihtiyaç duyar.");
            return path;
        }

        private static string BuildOutputPath(string modelPath, DrawingKind kind)
        {
            var dir = Path.GetDirectoryName(modelPath) ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var name = Path.GetFileNameWithoutExtension(modelPath);
            var suffix = kind switch
            {
                DrawingKind.Bend => "_Buküm",
                DrawingKind.Cut => "_Kesim",
                DrawingKind.Machining => "_Isleme",
                _ => "_AutoGBT"
            };
            return Path.Combine(dir, name + suffix + ".SLDDRW");
        }

        private static string TemplatePathOrEmpty()
        {
            // Kullanıcı kendi .drwdot şablonunu AUTOGBT_DRAWING_TEMPLATE ile verebilir.
            return Environment.GetEnvironmentVariable("AUTOGBT_DRAWING_TEMPLATE") ?? string.Empty;
        }

        private static (double Width, double Height) SheetSizeMeters(DrawingSheetFormat format)
        {
            // SolidWorks API metre cinsinden bekler.
            return format switch
            {
                DrawingSheetFormat.A4Portrait => (0.210, 0.297),
                DrawingSheetFormat.A4Landscape => (0.297, 0.210),
                DrawingSheetFormat.A2Landscape => (0.594, 0.420),
                _ => (0.420, 0.297) // A3 landscape
            };
        }

        private static void ConfigureSheet(DrawingDoc drawDoc, DrawingPlan plan, (double Width, double Height) size)
        {
            try
            {
                var sheet = (Sheet)drawDoc.GetCurrentSheet();
                sheet?.SetProperties2(
                    (int)swDwgPaperSizes_e.swDwgPapersUserDefined,
                    (int)swDwgTemplates_e.swDwgTemplateNone,
                    ParseScale(plan.RecommendedScale),
                    1,
                    false,
                    size.Width,
                    size.Height,
                    true);
                sheet?.SetName(AutoGbtAssistant.ToTurkish(plan.Kind));
            }
            catch
            {
                // Şablon farklılıklarında sessizce devam.
            }
        }

        private static double ParseScale(string scale)
        {
            // "1:2" -> 0.5, "2:1" -> 2
            if (string.IsNullOrWhiteSpace(scale)) return 1;
            var parts = scale.Split(':');
            if (parts.Length != 2) return 1;
            if (!double.TryParse(parts[0], out var a)) return 1;
            if (!double.TryParse(parts[1], out var b) || b == 0) return 1;
            return a / b;
        }

        private void CreateView(
            DrawingDoc drawDoc,
            ModelDoc2 drawModel,
            string modelPath,
            ViewPlan view,
            (double Width, double Height) sheet,
            DrawingPlan plan)
        {
            try
            {
                double x = view.RelativeX * sheet.Width;
                double y = view.RelativeY * sheet.Height;

                if (view.IsFlatPattern)
                {
                    // Flat pattern görünüşü — sac metal parçalarda.
                    var swView = drawDoc.CreateFlatPatternViewFromModelView3(
                        modelPath, string.Empty, x, y, 0, false, false);
                    AnnotateView(drawModel, swView as View, view);
                    return;
                }

                string namedView = MapOrientation(view.Orientation);
                object swViewObj = drawDoc.CreateDrawViewFromModelView3(modelPath, namedView, x, y);
                AnnotateView(drawModel, swViewObj as View, view);

                if (view.IsIsometric)
                {
                    // İzometrik görünüşlerde gizli çizgi kapalı tutulur.
                    try
                    {
                        if (swViewObj is View v)
                            v.SetDisplayMode3(false, (int)swViewDisplayMode_e.swViewDisplayMode_ShadedWithEdges, false, true);
                    }
                    catch { /* ignore */ }
                }
            }
            catch (Exception ex)
            {
                // Tek görünüş hatası tüm çizimi düşürmesin.
                drawModel.InsertNote($"[AutoGBT] {view.Name} oluşturulamadı: {ex.Message}");
            }
        }

        private static string MapOrientation(string orientation)
        {
            switch ((orientation ?? "").Trim().ToLowerInvariant())
            {
                case "top": return "*Top";
                case "right": return "*Right";
                case "left": return "*Left";
                case "back": return "*Back";
                case "bottom": return "*Bottom";
                case "isometric": return "*Isometric";
                case "dimetric": return "*Dimetric";
                case "trimetric": return "*Trimetric";
                case "flatpattern": return "*FlatPattern";
                case "section": return "*Front";
                default: return "*Front";
            }
        }

        private static void AnnotateView(ModelDoc2 drawModel, View? view, ViewPlan plan)
        {
            if (view == null) return;
            try
            {
                drawModel.ActivateView(view.GetName2());
                // Görünüş adı notu
                var note = (Note)drawModel.InsertNote(plan.Name);
                if (note != null)
                {
                    note.SetText(plan.Name + (string.IsNullOrEmpty(plan.Description) ? "" : " — " + plan.Description));
                }
            }
            catch { /* ignore */ }
        }

        private static void PlaceNotes(ModelDoc2 drawModel, DrawingPlan plan)
        {
            foreach (var ann in plan.Annotations)
            {
                try
                {
                    var prefix = ann.Kind.Equals("Note", StringComparison.OrdinalIgnoreCase)
                        ? ""
                        : $"[{ann.Kind}] ";
                    drawModel.InsertNote(prefix + ann.Text);
                }
                catch { /* ignore */ }
            }
        }

        private static void UpdateTitleBlockNotes(ModelDoc2 drawModel, DrawingPlan plan)
        {
            try
            {
                var block = string.Join("  |  ", plan.TitleBlockFields);
                drawModel.InsertNote("AUTOGBT | " + plan.Title + Environment.NewLine + block);
            }
            catch { /* ignore */ }
        }
    }
}
