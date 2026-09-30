using System;
using AutoGBT.Core.AI;
using AutoGBT.Core.Analysis;
using AutoGBT.Core.Models;
using SolidWorks.Interop.sldworks;

namespace AutoGBT.Core
{
    /// <summary>
    /// Eklenti komutlarının kullandığı tek giriş noktası.
    /// </summary>
    public sealed class AutoGbtService
    {
        private readonly ISldWorks _swApp;
        private readonly ModelAnalyzer _analyzer;
        private readonly AutoGbtAssistant _assistant;
        private readonly DrawingGenerator _generator;

        public AutoGbtService(ISldWorks swApp)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
            _analyzer = new ModelAnalyzer(swApp);
            _assistant = new AutoGbtAssistant(AutoGbtOptions.FromEnvironment());
            _generator = new DrawingGenerator(swApp, _assistant);
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
                    _ => model.PartName
                };
            }

            return _generator.Generate(model, req);
        }

        public AutoGbtAssistant Assistant => _assistant;
    }
}
