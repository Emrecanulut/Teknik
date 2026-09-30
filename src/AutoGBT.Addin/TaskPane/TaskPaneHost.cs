using System;
using System.Windows.Forms;
using AutoGBT.UI;
using SolidWorks.Interop.sldworks;

namespace AutoGBT.Addin
{
    /// <summary>
    /// SolidWorks görev panelinde Windows WinForms AutoGBT arayüzünü host eder.
    /// </summary>
    internal sealed class TaskPaneHost
    {
        private readonly SwAddin _addin;
        private ITaskpaneView? _view;
        private Form? _hostForm;
        private AutoGbtPanel? _panel;

        public TaskPaneHost(SwAddin addin)
        {
            _addin = addin;
        }

        public void Create()
        {
            var app = _addin.App ?? throw new InvalidOperationException("SW app yok.");
            var service = _addin.Service ?? throw new InvalidOperationException("AutoGBT servisi yok.");

            var host = new SolidWorksAutoGbtHost(app, service);
            _panel = new AutoGbtPanel(host, showSamplePicker: false)
            {
                Dock = DockStyle.Fill
            };

            // SolidWorks task pane HWND host için top-level olmayan form
            _hostForm = new Form
            {
                FormBorderStyle = FormBorderStyle.None,
                TopLevel = false,
                ShowInTaskbar = false,
                Width = 420,
                Height = 780,
                BackColor = System.Drawing.Color.FromArgb(14, 20, 18)
            };
            _hostForm.Controls.Add(_panel);
            _hostForm.Show();

            _view = app.CreateTaskpaneView2("", "AutoGBT");
            if (_view == null) return;

            _view.DisplayWindowFromHandlex64(_hostForm.Handle.ToInt64());
        }

        public void Show()
        {
            _view?.ShowView();
            _panel?.RefreshModelInfo();
        }

        public void Destroy()
        {
            try { _view?.DeleteView(); } catch { /* ignore */ }
            _view = null;

            if (_panel != null)
            {
                _panel.Dispose();
                _panel = null;
            }
            if (_hostForm != null)
            {
                _hostForm.Dispose();
                _hostForm = null;
            }
        }
    }
}
