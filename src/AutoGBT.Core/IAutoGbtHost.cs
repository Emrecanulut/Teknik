using System;
using AutoGBT.Core.Models;

namespace AutoGBT.Core
{
    /// <summary>
    /// Windows arayüzünün (görev paneli / masaüstü) bağlandığı host sözleşmesi.
    /// </summary>
    public interface IAutoGbtHost
    {
        string HostName { get; }
        bool CanCreateSolidWorksDrawing { get; }

        ModelSummary Analyze();
        DrawingPlan Preview(DrawingKind kind, DrawingRequest request);
        string Explain(ModelSummary model, DrawingPlan plan);
        DrawingResult CreateDrawing(DrawingKind kind, DrawingRequest request);
        void NotifyUser(string message, bool isError = false);
    }
}
