using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AutoGBT.Core.Models;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace AutoGBT.SolidWorks.Analysis
{
    /// <summary>
    /// Aktif katı modeli okuyup AutoGBT için özet üretir.
    /// </summary>
    public sealed class ModelAnalyzer
    {
        private readonly ISldWorks _swApp;

        public ModelAnalyzer(ISldWorks swApp)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
        }

        public ModelSummary AnalyzeActiveDocument()
        {
            var model = _swApp.ActiveDoc as ModelDoc2
                ?? throw new InvalidOperationException("Açık bir SolidWorks belgesi bulunamadı.");

            if (model.GetType() != (int)swDocumentTypes_e.swDocPART
                && model.GetType() != (int)swDocumentTypes_e.swDocASSEMBLY)
            {
                throw new InvalidOperationException("Teknik resim üretimi için bir parça veya montaj açın.");
            }

            var summary = new ModelSummary
            {
                PartName = model.GetTitle(),
                FilePath = model.GetPathName() ?? string.Empty
            };

            TryReadMaterial(model, summary);
            TryReadBoundingBox(model, summary);
            TryReadSheetMetal(model, summary);
            TryReadHoles(model, summary);
            TryReadFeatureCount(model, summary);

            summary.Notes.Add(summary.IsSheetMetal
                ? "Sac metal parça tespit edildi — büküm ve kesim resimleri önerilir."
                : "Katı parça tespit edildi — işleme resmi önceliklidir.");

            return summary;
        }

        private static void TryReadMaterial(ModelDoc2 model, ModelSummary summary)
        {
            try
            {
                var part = model as PartDoc;
                if (part == null) return;

                object materialObj = part.GetMaterialPropertyName2("", out string database);
                if (materialObj is string material && !string.IsNullOrWhiteSpace(material))
                {
                    summary.Material = material;
                    if (!string.IsNullOrWhiteSpace(database))
                        summary.Notes.Add($"Malzeme veritabanı: {database}");
                }
            }
            catch
            {
                summary.Notes.Add("Malzeme bilgisi okunamadı.");
            }
        }

        private static void TryReadBoundingBox(ModelDoc2 model, ModelSummary summary)
        {
            try
            {
                var box = (double[])model.GetPartBox(true);
                if (box == null || box.Length < 6) return;

                summary.BoundingBox = new BoundingBoxMm
                {
                    X = Math.Abs(box[3] - box[0]) * 1000.0,
                    Y = Math.Abs(box[4] - box[1]) * 1000.0,
                    Z = Math.Abs(box[5] - box[2]) * 1000.0
                };
            }
            catch
            {
                summary.Notes.Add("Sınır kutusu hesaplanamadı.");
            }
        }

        private static void TryReadSheetMetal(ModelDoc2 model, ModelSummary summary)
        {
            try
            {
                var feat = (Feature)model.FirstFeature();
                while (feat != null)
                {
                    var typeName = feat.GetTypeName2();
                    if (string.Equals(typeName, "SheetMetal", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "FlattenBends", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "ProcessBends", StringComparison.OrdinalIgnoreCase))
                    {
                        summary.IsSheetMetal = true;
                    }

                    if (string.Equals(typeName, "Weldment", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "WeldBead", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "EndCap", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "Gusset", StringComparison.OrdinalIgnoreCase)
                        || (typeName != null && typeName.IndexOf("Weld", StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        summary.IsWeldment = true;
                    }

                    if (string.Equals(typeName, "Bend", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "OneBend", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "SketchBend", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "EdgeFlange", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "MiterFlange", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(typeName, "Hem", StringComparison.OrdinalIgnoreCase))
                    {
                        summary.IsSheetMetal = true;
                        summary.BendCount++;
                        summary.Bends.Add(new BendInfo
                        {
                            Name = feat.Name,
                            AngleDeg = EstimateBendAngle(feat),
                            RadiusMm = EstimateBendRadius(feat),
                            BendLineId = $"BL-{summary.BendCount:00}"
                        });
                    }

                    if (string.Equals(typeName, "SheetMetal", StringComparison.OrdinalIgnoreCase))
                    {
                        summary.ThicknessMm = EstimateThickness(feat, summary);
                    }

                    feat = (Feature)feat.GetNextFeature();
                }

                if (summary.IsSheetMetal && summary.ThicknessMm <= 0)
                {
                    // Sac kalınlığı için Z ekseninin en küçük boyutu sıkça iyi bir tahmindir.
                    var dims = new[] { summary.BoundingBox.X, summary.BoundingBox.Y, summary.BoundingBox.Z }
                        .Where(v => v > 0.01)
                        .OrderBy(v => v)
                        .ToArray();
                    if (dims.Length > 0)
                        summary.ThicknessMm = Math.Round(dims[0], 2);
                }
            }
            catch
            {
                summary.Notes.Add("Sac metal özellikleri tam okunamadı.");
            }
        }

        private static void TryReadHoles(ModelDoc2 model, ModelSummary summary)
        {
            try
            {
                var feat = (Feature)model.FirstFeature();
                while (feat != null)
                {
                    var typeName = feat.GetTypeName2();
                    if (typeName != null &&
                        (typeName.IndexOf("Hole", StringComparison.OrdinalIgnoreCase) >= 0
                         || typeName.IndexOf("Cut", StringComparison.OrdinalIgnoreCase) >= 0
                         || string.Equals(typeName, "ICE", StringComparison.OrdinalIgnoreCase)))
                    {
                        var hole = new HoleInfo
                        {
                            Name = feat.Name,
                            DiameterMm = EstimateHoleDiameter(feat),
                            IsThreaded = typeName.IndexOf("Thread", StringComparison.OrdinalIgnoreCase) >= 0
                                         || feat.Name.IndexOf("M", StringComparison.OrdinalIgnoreCase) >= 0
                        };

                        if (hole.IsThreaded && hole.DiameterMm > 0)
                            hole.ThreadSpec = $"M{hole.DiameterMm:0.#}";

                        summary.Holes.Add(hole);
                        summary.HoleCount++;
                    }

                    feat = (Feature)feat.GetNextFeature();
                }
            }
            catch
            {
                summary.Notes.Add("Delik/kesim özellikleri kısmen okundu.");
            }
        }

        private static void TryReadFeatureCount(ModelDoc2 model, ModelSummary summary)
        {
            try
            {
                int count = 0;
                var feat = (Feature)model.FirstFeature();
                while (feat != null)
                {
                    count++;
                    feat = (Feature)feat.GetNextFeature();
                }
                summary.FeatureCount = count;
            }
            catch
            {
                // ignore
            }
        }

        private static double EstimateBendAngle(Feature feat)
        {
            try
            {
                // SolidWorks sürümüne göre FeatureData değişir; güvenli varsayılan.
                return 90.0;
            }
            catch
            {
                return 90.0;
            }
        }

        private static double EstimateBendRadius(Feature feat)
        {
            return 1.0;
        }

        private static double EstimateThickness(Feature feat, ModelSummary summary)
        {
            if (summary.ThicknessMm > 0) return summary.ThicknessMm;
            return 2.0;
        }

        private static double EstimateHoleDiameter(Feature feat)
        {
            return 0;
        }

        public static string FormatSummary(ModelSummary s)
        {
            return string.Format(
                CultureInfo.GetCultureInfo("tr-TR"),
                "{0} | Malzeme: {1} | Kalınlık: {2:0.##} mm | Büküm: {3} | Delik: {4} | Sac: {5}",
                s.PartName, s.Material, s.ThicknessMm, s.BendCount, s.HoleCount,
                s.IsSheetMetal ? "Evet" : "Hayır");
        }
    }
}
