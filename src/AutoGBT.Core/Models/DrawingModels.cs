namespace AutoGBT.Core.Models
{
    /// <summary>
    /// Teknik resim türleri: büküm, kesim, kaynak ve işleme.
    /// </summary>
    public enum DrawingKind
    {
        Bend = 0,       // Büküm
        Cut = 1,        // Kesim / Flat pattern
        Machining = 2,  // İşleme
        Weld = 3        // Kaynak noktaları
    }

    public enum DrawingSheetFormat
    {
        A4Portrait,
        A4Landscape,
        A3Landscape,
        A2Landscape
    }

    public sealed class ModelSummary
    {
        public string PartName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string Material { get; set; } = "Belirtilmemiş";
        public double ThicknessMm { get; set; }
        public bool IsSheetMetal { get; set; }
        public bool IsWeldment { get; set; }
        public int BendCount { get; set; }
        public int HoleCount { get; set; }
        public int FeatureCount { get; set; }
        public int Quantity { get; set; } = 1;
        public string ComponentPath { get; set; } = string.Empty;
        public BoundingBoxMm BoundingBox { get; set; } = new BoundingBoxMm();
        public System.Collections.Generic.List<BendInfo> Bends { get; set; }
            = new System.Collections.Generic.List<BendInfo>();
        public System.Collections.Generic.List<HoleInfo> Holes { get; set; }
            = new System.Collections.Generic.List<HoleInfo>();
        public System.Collections.Generic.List<string> Notes { get; set; }
            = new System.Collections.Generic.List<string>();
    }

    public sealed class BoundingBoxMm
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public double MaxFaceAreaApprox =>
            System.Math.Max(X * Y, System.Math.Max(Y * Z, X * Z));
    }

    public sealed class BendInfo
    {
        public string Name { get; set; } = string.Empty;
        public double AngleDeg { get; set; }
        public double RadiusMm { get; set; }
        public double Direction { get; set; }
        public string BendLineId { get; set; } = string.Empty;
    }

    public sealed class HoleInfo
    {
        public string Name { get; set; } = string.Empty;
        public double DiameterMm { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public bool IsThreaded { get; set; }
        public string ThreadSpec { get; set; } = string.Empty;
    }

    public sealed class DrawingRequest
    {
        public DrawingKind Kind { get; set; }
        public DrawingSheetFormat SheetFormat { get; set; } = DrawingSheetFormat.A3Landscape;
        public bool IncludeBendTable { get; set; } = true;
        public bool IncludeHoleTable { get; set; } = true;
        public bool IncludeBillOfMaterials { get; set; }
        public bool AutoDimension { get; set; } = true;
        public bool ShowFlatPattern { get; set; } = true;
        public bool ShowBendNotes { get; set; } = true;
        public bool ShowSurfaceFinish { get; set; } = true;
        public bool ShowToleranceBlock { get; set; } = true;
        public string Title { get; set; } = string.Empty;
        public string DrawnBy { get; set; } = "AutoGBT";
        public string ScaleHint { get; set; } = "Otomatik";
        public string ExtraInstructions { get; set; } = string.Empty;
    }

    public sealed class DrawingPlan
    {
        public DrawingKind Kind { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public DrawingSheetFormat SheetFormat { get; set; }
        public string RecommendedScale { get; set; } = "1:2";
        public System.Collections.Generic.List<ViewPlan> Views { get; set; }
            = new System.Collections.Generic.List<ViewPlan>();
        public System.Collections.Generic.List<AnnotationPlan> Annotations { get; set; }
            = new System.Collections.Generic.List<AnnotationPlan>();
        public System.Collections.Generic.List<string> TitleBlockFields { get; set; }
            = new System.Collections.Generic.List<string>();
        public System.Collections.Generic.List<string> QualityChecks { get; set; }
            = new System.Collections.Generic.List<string>();
        public double Confidence { get; set; }
    }

    public sealed class ViewPlan
    {
        public string Name { get; set; } = string.Empty;
        public string Orientation { get; set; } = "Front";
        public bool IsFlatPattern { get; set; }
        public bool IsIsometric { get; set; }
        public bool IsSection { get; set; }
        public bool IsDetail { get; set; }
        public double RelativeX { get; set; }
        public double RelativeY { get; set; }
        public double RelativeWidth { get; set; } = 0.35;
        public double RelativeHeight { get; set; } = 0.35;
        public string Description { get; set; } = string.Empty;
    }

    public sealed class AnnotationPlan
    {
        public string Kind { get; set; } = "Note"; // Note, Dimension, BendTable, HoleTable, GD&T, Finish
        public string Text { get; set; } = string.Empty;
        public string TargetView { get; set; } = string.Empty;
        public int Priority { get; set; }
    }

    public sealed class DrawingResult
    {
        public bool Success { get; set; }
        public string DrawingPath { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DrawingPlan? Plan { get; set; }
        public System.Collections.Generic.List<string> Warnings { get; set; }
            = new System.Collections.Generic.List<string>();
    }
}
