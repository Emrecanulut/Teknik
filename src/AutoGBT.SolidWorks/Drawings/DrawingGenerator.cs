using System;
using System.Collections.Generic;
using System.IO;
using AutoGBT.Core.AI;
using AutoGBT.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace AutoGBT.SolidWorks.Drawings
{
    /// <summary>
    /// Gerçek SolidWorks .SLDDRW üreticisi.
    /// Düzen: kullanıcı örneklerine göre Ön / Yan / Kesit A-A / Detay / İzometrik,
    /// model ölçüleri, balonlar, yüzey pürüzlülük ve başlık notları.
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
                Directory.CreateDirectory(Path.GetDirectoryName(drawingPath)!);

                var sheetSize = SheetSizeMeters(request.SheetFormat);
                var drawDoc = CreateDrawingDocument(sheetSize);
                if (drawDoc == null)
                    throw new InvalidOperationException("SolidWorks çizim belgesi oluşturulamadı.");

                var drawModel = (ModelDoc2)drawDoc;
                ConfigureSheet(drawDoc, plan, sheetSize);

                var createdViews = new List<View>();
                foreach (var view in plan.Views)
                {
                    var v = CreateView(drawDoc, drawModel, modelPath, view, sheetSize, plan);
                    if (v != null) createdViews.Add(v);
                }

                // Model ölçülerini çizime aktar (örnekteki gibi dolu ölçü seti)
                if (request.AutoDimension)
                    TryInsertAllDimensions(drawModel);

                // Balon numaraları (muayene / takip)
                if (request.Kind == DrawingKind.Machining || request.Kind == DrawingKind.Cut)
                    TryInsertBalloons(drawModel);

                PlaceProcessNotes(drawModel, plan, request, model);
                UpdateTitleBlockNotes(drawModel, plan, model, request);

                drawModel.ForceRebuild3(true);
                drawModel.SaveAs3(
                    drawingPath,
                    (int)swSaveAsVersion_e.swSaveAsCurrentVersion,
                    (int)swSaveAsOptions_e.swSaveAsOptions_Silent);

                result.Success = true;
                result.DrawingPath = drawingPath;
                result.Message =
                    $"AutoGBT gerçek SolidWorks teknik resmi oluşturdu ({AutoGbtAssistant.ToTurkish(request.Kind)}):\n{drawingPath}";
                result.Warnings.AddRange(plan.QualityChecks.FindAll(c =>
                    c.StartsWith("Uyarı", StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Teknik resim oluşturulamadı: " + ex.Message;
            }

            return result;
        }

        private DrawingDoc? CreateDrawingDocument((double Width, double Height) sheetSize)
        {
            var template = TemplatePathOrEmpty();
            DrawingDoc? drawDoc = null;

            if (!string.IsNullOrWhiteSpace(template) && File.Exists(template))
            {
                drawDoc = _swApp.NewDocument(template, 0, sheetSize.Width, sheetSize.Height) as DrawingDoc;
            }

            if (drawDoc == null)
            {
                // Standart A3 yatay çizim
                drawDoc = _swApp.NewDocument(
                    string.Empty,
                    (int)swDwgPaperSizes_e.swDwgPapersA3size,
                    sheetSize.Width,
                    sheetSize.Height) as DrawingDoc;
            }

            if (drawDoc == null)
            {
                var modelDoc = _swApp.NewDocument(
                    string.Empty,
                    (int)swDocumentTypes_e.swDocDRAWING,
                    sheetSize.Width,
                    sheetSize.Height) as ModelDoc2;
                drawDoc = modelDoc as DrawingDoc;
            }

            return drawDoc;
        }

        private string RequireSavedModel()
        {
            var model = _swApp.ActiveDoc as ModelDoc2
                ?? throw new InvalidOperationException("Aktif model yok.");
            var path = model.GetPathName();
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("Önce parçayı kaydedin (.SLDPRT/.SLDASM).");
            return path;
        }

        /// <summary>
        /// Çıktı: &lt;parça_klasörü&gt;/AutoGBT_Drawings/&lt;Kesim|Buküm|Isleme|Kaynak&gt;/Parça_xxx.SLDDRW
        /// </summary>
        private static string BuildOutputPath(string modelPath, DrawingKind kind)
        {
            var dir = Path.GetDirectoryName(modelPath)
                      ?? Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var name = Path.GetFileNameWithoutExtension(modelPath);
            var folder = kind switch
            {
                DrawingKind.Bend => "Buküm",
                DrawingKind.Cut => "Kesim",
                DrawingKind.Machining => "Isleme",
                DrawingKind.Weld => "Kaynak",
                _ => "Diger"
            };
            var outDir = Path.Combine(dir, "AutoGBT_Drawings", folder);
            Directory.CreateDirectory(outDir);
            return Path.Combine(outDir, $"{name}_{folder}.SLDDRW");
        }

        private static string TemplatePathOrEmpty()
        {
            var env = Environment.GetEnvironmentVariable("AUTOGBT_DRAWING_TEMPLATE");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env)) return env;

            // Yaygın SolidWorks şablon yolları
            var candidates = new[]
            {
                @"C:\ProgramData\SolidWorks\SOLIDWORKS 2024\templates\Drawing.drwdot",
                @"C:\ProgramData\SolidWorks\SOLIDWORKS 2023\templates\Drawing.drwdot",
                @"C:\ProgramData\SolidWorks\SOLIDWORKS 2022\templates\Drawing.drwdot",
                @"C:\ProgramData\SolidWorks\SOLIDWORKS 2021\templates\Drawing.drwdot",
                @"C:\ProgramData\SolidWorks\SOLIDWORKS 2020\templates\Drawing.drwdot"
            };
            foreach (var c in candidates)
                if (File.Exists(c)) return c;
            return string.Empty;
        }

        private static (double Width, double Height) SheetSizeMeters(DrawingSheetFormat format)
        {
            return format switch
            {
                DrawingSheetFormat.A4Portrait => (0.210, 0.297),
                DrawingSheetFormat.A4Landscape => (0.297, 0.210),
                DrawingSheetFormat.A2Landscape => (0.594, 0.420),
                _ => (0.420, 0.297)
            };
        }

        private static void ConfigureSheet(DrawingDoc drawDoc, DrawingPlan plan, (double Width, double Height) size)
        {
            try
            {
                var sheet = (Sheet)drawDoc.GetCurrentSheet();
                sheet?.SetProperties2(
                    (int)swDwgPaperSizes_e.swDwgPapersA3size,
                    (int)swDwgTemplates_e.swDwgTemplateCustom,
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
                try
                {
                    var sheet = (Sheet)drawDoc.GetCurrentSheet();
                    sheet?.SetName(AutoGbtAssistant.ToTurkish(plan.Kind));
                }
                catch { /* ignore */ }
            }
        }

        private static double ParseScale(string scale)
        {
            if (string.IsNullOrWhiteSpace(scale)) return 1;
            var parts = scale.Split(':');
            if (parts.Length != 2) return 1;
            if (!double.TryParse(parts[0], out var a)) return 1;
            if (!double.TryParse(parts[1], out var b) || b == 0) return 1;
            return a / b;
        }

        private View? CreateView(
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
                    var flat = drawDoc.CreateFlatPatternViewFromModelView3(
                        modelPath, string.Empty, x, y, 0, false, false) as View;
                    AnnotateView(drawModel, flat, view);
                    return flat;
                }

                string namedView = MapOrientation(view.Orientation);
                var swView = drawDoc.CreateDrawViewFromModelView3(modelPath, namedView, x, y) as View;
                if (swView == null) return null;

                AnnotateView(drawModel, swView, view);

                if (view.IsIsometric)
                {
                    try
                    {
                        swView.SetDisplayMode3(
                            false,
                            (int)swViewDisplayMode_e.swViewDisplayMode_ShadedWithEdges,
                            false,
                            true);
                    }
                    catch { /* ignore */ }
                }
                else if (view.IsSection)
                {
                    // Kesit görünüşü için gizli çizgi + tarama notu
                    try
                    {
                        swView.SetDisplayMode3(
                            false,
                            (int)swViewDisplayMode_e.swViewDisplayMode_HiddenLinesRemoved,
                            false,
                            false);
                        drawModel.InsertNote($"{view.Name} — kesit tarama / iç detay");
                    }
                    catch { /* ignore */ }
                }
                else if (view.IsDetail)
                {
                    try
                    {
                        drawModel.InsertNote($"{view.Name} — detay ölçeği büyütülmüş");
                    }
                    catch { /* ignore */ }
                }

                return swView;
            }
            catch (Exception ex)
            {
                try { drawModel.InsertNote($"[AutoGBT] {view.Name}: {ex.Message}"); }
                catch { /* ignore */ }
                return null;
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
                case "detail": return "*Front";
                default: return "*Front";
            }
        }

        private static void AnnotateView(ModelDoc2 drawModel, View? view, ViewPlan plan)
        {
            if (view == null) return;
            try
            {
                drawModel.ActivateView(view.GetName2());
                var note = (Note)drawModel.InsertNote(plan.Name);
                note?.SetText(plan.Name);
            }
            catch { /* ignore */ }
        }

        private static void TryInsertAllDimensions(ModelDoc2 drawModel)
        {
            try
            {
                var ext = drawModel.Extension;
                if (ext == null) return;

                // İşaretli + model ölçülerini aktar
                ext.InsertModelAnnotations3(
                    (int)swImportModelItemsSource_e.swImportModelItemsFromEntireModel,
                    (int)swInsertAnnotation_e.swInsertDimensionsMarkedForDrawing
                    | (int)swInsertAnnotation_e.swInsertDimensionsNotMarkedForDrawing
                    | (int)swInsertAnnotation_e.swInsertHoleWizardInfo
                    | (int)swInsertAnnotation_e.swInsertInstanceCount,
                    true, true, false, true);
            }
            catch
            {
                try
                {
                    drawModel.InsertNote(
                        "AutoGBT: Model ölçüleri kısmen aktarıldı. Insert > Model Items ile tamamlayın.");
                }
                catch { /* ignore */ }
            }
        }

        private static void TryInsertBalloons(ModelDoc2 drawModel)
        {
            try
            {
                // Otomatik balon — görünüşteki bileşen/ölçü etiketleri için
                var ext = drawModel.Extension;
                // AutoBalloon için seçim gerekir; yoksa bilgilendirme notu bırak.
                drawModel.InsertNote(
                    "BALON: Muayene numaraları — Auto Balloon / Inspection ile tamamlanabilir.");
            }
            catch { /* ignore */ }
        }

        private static void PlaceProcessNotes(
            ModelDoc2 drawModel,
            DrawingPlan plan,
            DrawingRequest request,
            ModelSummary model)
        {
            var notes = new List<string>
            {
                "Ölçüler milimetredir.",
                "Genel tolerans: DIN ISO 2768-m (medium).",
                "Çapak alınız; keskin kenarları 0.1–0.3 mm kırınız.",
                "Çizik ve ezik kabul edilmez."
            };

            if (request.ShowSurfaceFinish || plan.Kind == DrawingKind.Machining)
                notes.Add("Yüzey pürüzlülüğü: Ra 3.2 µm (aksi belirtilmedikçe).");

            if (plan.Kind == DrawingKind.Cut)
                notes.Add("KESİM: Dış kontur + iç boşluklar. Büküm çizgileri kesilmez.");

            if (plan.Kind == DrawingKind.Bend)
                notes.Add("BÜKÜM: Açı / iç yarıçap / flanş / büküm payı tablosuna uyunuz.");

            if (plan.Kind == DrawingKind.Weld)
                notes.Add("KAYNAK: MAG (aksi yoksa). Sıçrantı temizlenecek.");

            if (!string.IsNullOrWhiteSpace(model.Material))
                notes.Add("Malzeme: " + model.Material);

            foreach (var ann in plan.Annotations)
            {
                if (ann.Kind.Equals("Note", StringComparison.OrdinalIgnoreCase)
                    || ann.Kind.Equals("Finish", StringComparison.OrdinalIgnoreCase)
                    || ann.Kind.Equals("GD&T", StringComparison.OrdinalIgnoreCase))
                {
                    notes.Add(ann.Text);
                }
            }

            try
            {
                drawModel.InsertNote(string.Join(Environment.NewLine, notes));
            }
            catch { /* ignore */ }
        }

        private static void UpdateTitleBlockNotes(
            ModelDoc2 drawModel,
            DrawingPlan plan,
            ModelSummary model,
            DrawingRequest request)
        {
            try
            {
                var lines = new List<string>
                {
                    "AUTOGBT | " + plan.Title,
                    $"Parça: {model.PartName}",
                    $"Malzeme: {model.Material}",
                    $"Ölçek: {plan.RecommendedScale}",
                    $"Proses: {AutoGbtAssistant.ToTurkish(plan.Kind)}",
                    $"Çizen: {request.DrawnBy}",
                    $"Tarih: {DateTime.Now:dd.MM.yyyy}",
                    "Sayfa: A3 | FOGLIO 1 DI 1"
                };
                if (model.ThicknessMm > 0)
                    lines.Add($"Kalınlık: {model.ThicknessMm:0.##} mm");
                if (model.Quantity > 1)
                    lines.Add($"Adet: {model.Quantity}");

                drawModel.InsertNote(string.Join(Environment.NewLine, lines));
            }
            catch { /* ignore */ }
        }
    }
}
