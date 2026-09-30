using System;
using System.Collections.Generic;
using System.Linq;
using AutoGBT.Core.AI;
using AutoGBT.Core.Models;
using AutoGBT.SolidWorks.Analysis;
using AutoGBT.SolidWorks.Drawings;
using SolidWorks.Interop.sldworks;

namespace AutoGBT.SolidWorks
{
    /// <summary>
    /// SolidWorks oturumuna bağlı AutoGBT servisi.
    /// </summary>
    public sealed class AutoGbtService
    {
        private readonly ISldWorks _swApp;
        private readonly ModelAnalyzer _analyzer;
        private readonly AutoGbtAssistant _assistant;
        private readonly DrawingGenerator _generator;
        private readonly AssemblyScanner _assemblyScanner;

        public AutoGbtService(ISldWorks swApp)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
            _analyzer = new ModelAnalyzer(swApp);
            _assistant = new AutoGbtAssistant(AutoGbtOptions.FromEnvironment());
            _generator = new DrawingGenerator(swApp, _assistant);
            _assemblyScanner = new AssemblyScanner(swApp, _assistant);
        }

        public ModelSummary Analyze() => _analyzer.AnalyzeActiveDocument();

        public DrawingPlan Preview(DrawingKind kind, DrawingRequest? request = null)
        {
            var model = Analyze();
            var req = request ?? new DrawingRequest { Kind = kind };
            req.Kind = kind;
            return _assistant.PlanDrawing(model, req);
        }

        public string Explain(DrawingKind kind, DrawingRequest? request = null)
        {
            var model = Analyze();
            var req = request ?? new DrawingRequest { Kind = kind };
            req.Kind = kind;
            var plan = _assistant.PlanDrawing(model, req);
            return _assistant.Explain(model, plan);
        }

        public DrawingResult CreateBendDrawing(DrawingRequest? request = null)
            => Create(DrawingKind.Bend, request);

        public DrawingResult CreateCutDrawing(DrawingRequest? request = null)
            => Create(DrawingKind.Cut, request);

        public DrawingResult CreateMachiningDrawing(DrawingRequest? request = null)
            => Create(DrawingKind.Machining, request);

        public DrawingResult Create(DrawingKind kind, DrawingRequest? request = null)
        {
            var model = Analyze();
            var req = request ?? new DrawingRequest { Kind = kind };
            req.Kind = kind;

            if (string.IsNullOrWhiteSpace(req.Title))
            {
                req.Title = kind switch
                {
                    DrawingKind.Bend => $"{model.PartName} — Büküm Teknik Resmi",
                    DrawingKind.Cut => $"{model.PartName} — Kesim / Açınım Resmi",
                    DrawingKind.Machining => $"{model.PartName} — İşleme Teknik Resmi",
                    DrawingKind.Weld => $"{model.PartName} — Kaynak Noktaları Resmi",
                    _ => model.PartName
                };
            }

            return _generator.Generate(model, req);
        }

        /// <summary>
        /// Aktif montaj/parçadaki tüm uygun bileşenler için proses bazlı teknik resimler üretir.
        /// </summary>
        public AssemblyBatchResult CreateAllComponentDrawings(DrawingRequest? template = null)
        {
            var jobs = _assemblyScanner.ScanActiveDocument();
            var result = new AssemblyBatchResult
            {
                ComponentCount = jobs.Count
            };

            foreach (var job in jobs)
            {
                result.Lines.Add(
                    $"{job.Summary.PartName} ×{job.Summary.Quantity} → " +
                    string.Join(", ", job.Kinds.Select(AutoGbtAssistant.ToTurkish)));

                if (job.Kinds.Count == 0) continue;

                // Bileşen dosyası kayıtlı değilse çizim görünüşü eklenemez.
                if (string.IsNullOrWhiteSpace(job.Summary.FilePath))
                {
                    result.Warnings.Add($"{job.Summary.PartName}: kayıtlı yol yok, atlandı.");
                    continue;
                }

                foreach (var kind in job.Kinds)
                {
                    var req = template != null
                        ? CloneRequest(template, kind, job.Summary.PartName)
                        : new DrawingRequest { Kind = kind, Title = TitleFor(kind, job.Summary.PartName) };

                    var draw = _generator.Generate(job.Summary, req);
                    result.Results.Add(draw);
                    if (!draw.Success)
                        result.Warnings.Add($"{job.Summary.PartName}/{AutoGbtAssistant.ToTurkish(kind)}: {draw.Message}");
                }
            }

            result.SuccessCount = result.Results.Count(r => r.Success);
            result.Message =
                $"AutoGBT montaj tarandı: {result.ComponentCount} parça, {result.SuccessCount} teknik resim üretildi.";
            return result;
        }

        public IReadOnlyList<ComponentJob> ScanAssembly() => _assemblyScanner.ScanActiveDocument();

        public AutoGbtAssistant Assistant => _assistant;

        private static DrawingRequest CloneRequest(DrawingRequest template, DrawingKind kind, string partName)
        {
            return new DrawingRequest
            {
                Kind = kind,
                SheetFormat = template.SheetFormat,
                IncludeBendTable = template.IncludeBendTable,
                IncludeHoleTable = template.IncludeHoleTable,
                IncludeBillOfMaterials = template.IncludeBillOfMaterials,
                AutoDimension = template.AutoDimension,
                ShowFlatPattern = template.ShowFlatPattern,
                ShowBendNotes = template.ShowBendNotes,
                ShowSurfaceFinish = template.ShowSurfaceFinish,
                ShowToleranceBlock = template.ShowToleranceBlock,
                DrawnBy = template.DrawnBy,
                ExtraInstructions = template.ExtraInstructions,
                Title = TitleFor(kind, partName)
            };
        }

        private static string TitleFor(DrawingKind kind, string partName) => kind switch
        {
            DrawingKind.Bend => $"{partName} — Büküm Teknik Resmi",
            DrawingKind.Cut => $"{partName} — Kesim / Açınım Resmi",
            DrawingKind.Machining => $"{partName} — İşleme Teknik Resmi",
            DrawingKind.Weld => $"{partName} — Kaynak Noktaları Resmi",
            _ => partName
        };
    }

    public sealed class AssemblyBatchResult
    {
        public string Message { get; set; } = string.Empty;
        public int ComponentCount { get; set; }
        public int SuccessCount { get; set; }
        public List<DrawingResult> Results { get; set; } = new List<DrawingResult>();
        public List<string> Warnings { get; set; } = new List<string>();
        public List<string> Lines { get; set; } = new List<string>();
    }
}
