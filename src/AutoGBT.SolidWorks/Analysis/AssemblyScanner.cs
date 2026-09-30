using System;
using System.Collections.Generic;
using System.Linq;
using AutoGBT.Core.AI;
using AutoGBT.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace AutoGBT.SolidWorks.Analysis
{
    /// <summary>
    /// Montajdaki tüm benzersiz parçaları tarar ve sınıflandırır.
    /// </summary>
    public sealed class AssemblyScanner
    {
        private readonly ISldWorks _swApp;
        private readonly AutoGbtAssistant _assistant;

        public AssemblyScanner(ISldWorks swApp, AutoGbtAssistant? assistant = null)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
            _assistant = assistant ?? new AutoGbtAssistant();
        }

        public IReadOnlyList<ComponentJob> ScanActiveDocument()
        {
            var model = _swApp.ActiveDoc as ModelDoc2
                ?? throw new InvalidOperationException("Açık belge yok.");

            if (model.GetType() == (int)swDocumentTypes_e.swDocPART)
            {
                var analyzer = new ModelAnalyzer(_swApp);
                var summary = analyzer.AnalyzeActiveDocument();
                return new[]
                {
                    ToJob(summary, summary.FilePath, 1)
                };
            }

            if (model.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY)
                throw new InvalidOperationException("Parça veya montaj açın.");

            var asm = (AssemblyDoc)model;
            var map = new Dictionary<string, ComponentJob>(StringComparer.OrdinalIgnoreCase);

            object[]? comps = asm.GetComponents(false) as object[];
            if (comps == null || comps.Length == 0)
                throw new InvalidOperationException("Montajda bileşen bulunamadı.");

            foreach (var obj in comps)
            {
                if (obj is not Component2 comp) continue;
                if (comp.IsSuppressed()) continue;

                string path = comp.GetPathName() ?? string.Empty;
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                if (string.IsNullOrWhiteSpace(name))
                    name = comp.Name2 ?? "Parça";

                // Bağlantı elemanı / toolbox elemesi (basit)
                if (IsLikelyFastener(name, path))
                    continue;

                string key = string.IsNullOrWhiteSpace(path) ? name : path;
                if (map.TryGetValue(key, out var existing))
                {
                    existing.Summary.Quantity++;
                    continue;
                }

                var summary = AnalyzeComponent(comp, name, path);
                map[key] = ToJob(summary, path, 1);
            }

            return map.Values
                .OrderByDescending(j => j.Kinds.Count)
                .ThenBy(j => j.Summary.PartName)
                .ToList();
        }

        private ModelSummary AnalyzeComponent(Component2 comp, string name, string path)
        {
            var summary = new ModelSummary
            {
                PartName = name,
                FilePath = path,
                ComponentPath = path,
                Quantity = 1
            };

            try
            {
                // Bileşeni geçici olarak açmadan feature bilgisi sınırlı olabilir.
                var modelDoc = comp.GetModelDoc2() as ModelDoc2;
                if (modelDoc != null)
                {
                    // ActiveDoc değiştirmeden kaba analiz: FirstFeature üzerinde dolaş.
                    TryFillFromModel(modelDoc, summary);
                }
                else
                {
                    InferFromName(summary);
                }
            }
            catch
            {
                InferFromName(summary);
            }

            if (!summary.IsSheetMetal && !summary.IsWeldment && summary.FeatureCount == 0)
                InferFromName(summary);

            return summary;
        }

        private static void TryFillFromModel(ModelDoc2 model, ModelSummary summary)
        {
            try
            {
                var box = (double[])model.GetPartBox(true);
                if (box != null && box.Length >= 6)
                {
                    summary.BoundingBox = new BoundingBoxMm
                    {
                        X = Math.Abs(box[3] - box[0]) * 1000.0,
                        Y = Math.Abs(box[4] - box[1]) * 1000.0,
                        Z = Math.Abs(box[5] - box[2]) * 1000.0
                    };
                }
            }
            catch { /* ignore */ }

            try
            {
                var feat = (Feature)model.FirstFeature();
                while (feat != null)
                {
                    summary.FeatureCount++;
                    var typeName = feat.GetTypeName2() ?? string.Empty;
                    if (typeName.IndexOf("SheetMetal", StringComparison.OrdinalIgnoreCase) >= 0
                        || typeName.IndexOf("Bend", StringComparison.OrdinalIgnoreCase) >= 0
                        || typeName.IndexOf("EdgeFlange", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        summary.IsSheetMetal = true;
                        if (typeName.IndexOf("Bend", StringComparison.OrdinalIgnoreCase) >= 0
                            || typeName.IndexOf("Flange", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            summary.BendCount++;
                        }
                    }
                    if (typeName.IndexOf("Weld", StringComparison.OrdinalIgnoreCase) >= 0
                        || typeName.IndexOf("Weldment", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        summary.IsWeldment = true;
                    }
                    if (typeName.IndexOf("Hole", StringComparison.OrdinalIgnoreCase) >= 0
                        || typeName.IndexOf("Cut", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        summary.HoleCount++;
                    }
                    feat = (Feature)feat.GetNextFeature();
                }
            }
            catch { /* ignore */ }
        }

        private static void InferFromName(ModelSummary summary)
        {
            var n = (summary.PartName ?? string.Empty).ToLowerInvariant();
            if (n.Contains("sac") || n.Contains("bracket") || n.Contains("panel") || n.Contains("flange") || n.Contains("kapak"))
            {
                summary.IsSheetMetal = true;
                summary.BendCount = Math.Max(summary.BendCount, 1);
                summary.Notes.Add("Dosya adından sac metal olarak sınıflandırıldı.");
            }
            else if (n.Contains("kaynak") || n.Contains("weld") || n.Contains("frame") || n.Contains("sasi") || n.Contains("şasi"))
            {
                summary.IsWeldment = true;
                summary.Notes.Add("Dosya adından kaynaklı konstrüksiyon olarak sınıflandırıldı.");
            }
            else
            {
                summary.Notes.Add("İşleme parçası olarak sınıflandırıldı.");
            }
        }

        private static bool IsLikelyFastener(string name, string path)
        {
            var n = (name + " " + path).ToLowerInvariant();
            return n.Contains("toolbox")
                   || n.Contains("\\bolts\\")
                   || n.Contains("din912")
                   || n.Contains("din933")
                   || n.Contains("washer")
                   || n.Contains("somun")
                   || n.Contains("vida")
                   || System.Text.RegularExpressions.Regex.IsMatch(n, @"\bm\d{1,2}x\d");
        }

        private ComponentJob ToJob(ModelSummary summary, string path, int qty)
        {
            summary.Quantity = qty;
            summary.FilePath = path;
            var kinds = _assistant.RecommendKinds(summary).ToList();
            return new ComponentJob
            {
                Summary = summary,
                Kinds = kinds
            };
        }
    }

    public sealed class ComponentJob
    {
        public ModelSummary Summary { get; set; } = new ModelSummary();
        public List<DrawingKind> Kinds { get; set; } = new List<DrawingKind>();
    }
}
