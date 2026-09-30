using System;
using System.IO;
using System.Linq;
using AutoGBT.Core.AI;
using AutoGBT.Core.Models;

namespace AutoGBT.Core
{
    /// <summary>
    /// SolidWorks olmadan Windows UI üzerinde çalışan örnek host.
    /// Planlama gerçek AutoGBT motoruyla yapılır; çizim dosyası yerel bir rapor olarak yazılır.
    /// </summary>
    public sealed class DesktopAutoGbtHost : IAutoGbtHost
    {
        private readonly AutoGbtAssistant _assistant = new AutoGbtAssistant();
        private ModelSummary _current;

        public DesktopAutoGbtHost(ModelSummary? initial = null)
        {
            _current = initial ?? SampleParts.All[0];
        }

        public string HostName => "AutoGBT Windows";
        public bool CanCreateSolidWorksDrawing => false;

        public ModelSummary CurrentModel => _current;

        public void SetModel(ModelSummary model)
        {
            _current = model ?? throw new ArgumentNullException(nameof(model));
        }

        public ModelSummary Analyze() => _current;

        public DrawingPlan Preview(DrawingKind kind, DrawingRequest request)
        {
            request.Kind = kind;
            return _assistant.PlanDrawing(_current, request);
        }

        public string Explain(ModelSummary model, DrawingPlan plan)
            => _assistant.Explain(model, plan);

        public DrawingResult CreateDrawing(DrawingKind kind, DrawingRequest request)
        {
            request.Kind = kind;
            if (string.IsNullOrWhiteSpace(request.Title))
            {
                request.Title = kind switch
                {
                    DrawingKind.Bend => $"{_current.PartName} — Büküm Teknik Resmi",
                    DrawingKind.Cut => $"{_current.PartName} — Kesim / Açınım Resmi",
                    DrawingKind.Machining => $"{_current.PartName} — İşleme Teknik Resmi",
                    _ => _current.PartName
                };
            }

            var plan = _assistant.PlanDrawing(_current, request);
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "AutoGBT");
            Directory.CreateDirectory(dir);

            var suffix = kind switch
            {
                DrawingKind.Bend => "_Buküm",
                DrawingKind.Cut => "_Kesim",
                DrawingKind.Machining => "_Isleme",
                _ => "_AutoGBT"
            };
            var path = Path.Combine(dir, _current.PartName + suffix + ".txt");

            var report = _assistant.Explain(_current, plan);
            var body =
                "AutoGBT Teknik Resim Planı (Windows önizleme)\r\n" +
                "SolidWorks eklentisinde bu plan .SLDDRW olarak üretilir.\r\n\r\n" +
                report + "\r\n\r\n" +
                "Görünüşler:\r\n" +
                string.Join("\r\n", plan.Views.Select(v =>
                    $"- {v.Name} [{v.Orientation}] ({v.RelativeX:0.00},{v.RelativeY:0.00})")) +
                "\r\n\r\nBaşlık bloğu:\r\n" +
                string.Join("\r\n", plan.TitleBlockFields.Select(f => "- " + f));

            File.WriteAllText(path, body);

            return new DrawingResult
            {
                Success = true,
                DrawingPath = path,
                Message = $"AutoGBT {AutoGbtAssistant.ToTurkish(kind)} planını Windows'ta oluşturdu. SolidWorks eklentisinde .SLDDRW üretilir.",
                Plan = plan,
                Warnings = plan.QualityChecks.FindAll(c =>
                    c.StartsWith("Uyarı", StringComparison.OrdinalIgnoreCase))
            };
        }

        public void NotifyUser(string message, bool isError = false)
        {
            // UI MessageBox ile gösterir; host sessiz kalabilir.
        }
    }
}
