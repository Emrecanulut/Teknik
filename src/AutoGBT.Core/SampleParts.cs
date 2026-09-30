using System.Collections.Generic;
using AutoGBT.Core.Models;

namespace AutoGBT.Core
{
    /// <summary>
    /// Windows masaüstü uygulaması için örnek parçalar (SolidWorks olmadan UI testi).
    /// </summary>
    public static class SampleParts
    {
        public static IReadOnlyList<ModelSummary> All { get; } = new List<ModelSummary>
        {
            new ModelSummary
            {
                PartName = "L-Bracket-SM-02",
                Material = "DX51D+Z275",
                ThicknessMm = 2,
                IsSheetMetal = true,
                BendCount = 2,
                HoleCount = 4,
                FeatureCount = 11,
                BoundingBox = new BoundingBoxMm { X = 120, Y = 80, Z = 40 },
                Bends =
                {
                    new BendInfo { Name = "Edge-Flange1", AngleDeg = 90, RadiusMm = 2, BendLineId = "BL-01" },
                    new BendInfo { Name = "Edge-Flange2", AngleDeg = 90, RadiusMm = 2, BendLineId = "BL-02" }
                },
                Holes =
                {
                    new HoleInfo { Name = "Delik1", DiameterMm = 6.5, IsThreaded = false },
                    new HoleInfo { Name = "Delik2", DiameterMm = 6.5, IsThreaded = false },
                    new HoleInfo { Name = "Delik3", DiameterMm = 8, IsThreaded = true, ThreadSpec = "M8" },
                    new HoleInfo { Name = "Delik4", DiameterMm = 8, IsThreaded = true, ThreadSpec = "M8" }
                },
                Notes = { "İki bükümlü sac bracket; montaj delikleri ve M8 bağlantı." }
            },
            new ModelSummary
            {
                PartName = "Kapak-Panel-A4",
                Material = "AlMg3",
                ThicknessMm = 1.5,
                IsSheetMetal = true,
                BendCount = 4,
                HoleCount = 8,
                FeatureCount = 18,
                BoundingBox = new BoundingBoxMm { X = 280, Y = 180, Z = 25 },
                Bends =
                {
                    new BendInfo { Name = "Flange-N", AngleDeg = 90, RadiusMm = 1.5, BendLineId = "BL-01" },
                    new BendInfo { Name = "Flange-S", AngleDeg = 90, RadiusMm = 1.5, BendLineId = "BL-02" },
                    new BendInfo { Name = "Flange-E", AngleDeg = 90, RadiusMm = 1.5, BendLineId = "BL-03" },
                    new BendInfo { Name = "Flange-W", AngleDeg = 90, RadiusMm = 1.5, BendLineId = "BL-04" }
                },
                Notes = { "Dört kenarı flanşlı kapak paneli; lazer kesim + büküm." }
            },
            new ModelSummary
            {
                PartName = "Adapter-Block-CNC",
                Material = "Al7075-T6",
                IsSheetMetal = false,
                BendCount = 0,
                HoleCount = 6,
                FeatureCount = 24,
                BoundingBox = new BoundingBoxMm { X = 90, Y = 60, Z = 35 },
                Holes =
                {
                    new HoleInfo { Name = "H7-1", DiameterMm = 10, IsThreaded = false },
                    new HoleInfo { Name = "H7-2", DiameterMm = 10, IsThreaded = false },
                    new HoleInfo { Name = "M6-1", DiameterMm = 6, IsThreaded = true, ThreadSpec = "M6" },
                    new HoleInfo { Name = "M6-2", DiameterMm = 6, IsThreaded = true, ThreadSpec = "M6" },
                    new HoleInfo { Name = "M5-1", DiameterMm = 5, IsThreaded = true, ThreadSpec = "M5" },
                    new HoleInfo { Name = "M5-2", DiameterMm = 5, IsThreaded = true, ThreadSpec = "M5" }
                },
                Notes = { "CNC freze adapter bloğu; kademeli delikler ve cepler." }
            }
        };
    }
}
