using System;
using System.Linq;
using AutoGBT.Core;
using AutoGBT.Core.Models;
using AutoGBT.SolidWorks;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace AutoGBT.Addin
{
    /// <summary>
    /// SolidWorks oturumunu Windows UI paneline bağlar.
    /// </summary>
    internal sealed class SolidWorksAutoGbtHost : IAutoGbtHost
    {
        private readonly ISldWorks _swApp;
        private readonly AutoGbtService _service;

        public SolidWorksAutoGbtHost(ISldWorks swApp, AutoGbtService service)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public string HostName => "SolidWorks";
        public bool CanCreateSolidWorksDrawing => true;
        public bool SupportsAssemblyBatch => true;

        public ModelSummary Analyze() => _service.Analyze();

        public DrawingPlan Preview(DrawingKind kind, DrawingRequest request)
            => _service.Preview(kind, request);

        public string Explain(ModelSummary model, DrawingPlan plan)
            => _service.Assistant.Explain(model, plan);

        public DrawingResult CreateDrawing(DrawingKind kind, DrawingRequest request)
            => _service.Create(kind, request);

        public string RunAssemblyBatch(DrawingRequest request)
        {
            var batch = _service.CreateAllComponentDrawings(request);
            var detail = string.Join("\n", batch.Lines.Take(30));
            if (batch.Warnings.Count > 0)
                detail += "\n\nUyarılar:\n" + string.Join("\n", batch.Warnings.Take(15));
            return batch.Message + "\n\n" + detail;
        }

        public void NotifyUser(string message, bool isError = false)
        {
            _swApp.SendMsgToUser2(
                message,
                (int)(isError ? swMessageBoxIcon_e.swMbWarning : swMessageBoxIcon_e.swMbInformation),
                (int)swMessageBoxBtn_e.swMbOk);
        }
    }
}
