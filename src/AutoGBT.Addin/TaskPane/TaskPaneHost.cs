using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using AutoGBT.Core.Models;
using SolidWorks.Interop.sldworks;

namespace AutoGBT.Addin
{
    /// <summary>
    /// SolidWorks görev panelinde AutoGBT kontrol yüzeyi.
    /// </summary>
    internal sealed class TaskPaneHost
    {
        private readonly SwAddin _addin;
        private ITaskpaneView? _view;
        private AutoGbtTaskPaneControl? _control;

        public TaskPaneHost(SwAddin addin)
        {
            _addin = addin;
        }

        public void Create()
        {
            var app = _addin.App ?? throw new InvalidOperationException("SW app yok.");
            _control = new AutoGbtTaskPaneControl(_addin);

            // Bitmap yoksa boş dizi — SolidWorks varsayılan ikon kullanır.
            string[] bitmaps = Array.Empty<string>();
            _view = app.CreateTaskpaneView2("", "AutoGBT");
            if (_view == null) return;

            // WinForms kontrolünü host et
            _view.DisplayWindowFromHandlex64(_control.Handle.ToInt64());
        }

        public void Show()
        {
            _view?.ShowView();
            _control?.RefreshModelInfo();
        }

        public void Destroy()
        {
            try
            {
                _view?.DeleteView();
            }
            catch { /* ignore */ }
            _view = null;
            if (_control != null)
            {
                _control.Dispose();
                _control = null;
            }
        }
    }

    internal sealed class AutoGbtTaskPaneControl : UserControl
    {
        private readonly SwAddin _addin;
        private readonly Label _title;
        private readonly TextBox _modelInfo;
        private readonly TextBox _report;
        private readonly TextBox _extra;
        private readonly ComboBox _sheetFormat;
        private readonly CheckBox _bendTable;
        private readonly CheckBox _holeTable;
        private readonly CheckBox _autoDim;
        private readonly CheckBox _tolerance;
        private readonly CheckBox _finish;

        public AutoGbtTaskPaneControl(SwAddin addin)
        {
            _addin = addin;
            Width = 320;
            BackColor = Color.FromArgb(18, 24, 28);
            ForeColor = Color.FromArgb(236, 232, 224);
            Font = new Font("Segoe UI", 9F);

            _title = new Label
            {
                Text = "AutoGBT",
                Font = new Font("Segoe UI Semibold", 16F),
                ForeColor = Color.FromArgb(232, 168, 56),
                AutoSize = true,
                Location = new Point(12, 12)
            };

            var subtitle = new Label
            {
                Text = "Büküm · Kesim · İşleme teknik resimleri",
                ForeColor = Color.FromArgb(180, 176, 168),
                AutoSize = true,
                Location = new Point(14, 44)
            };

            _modelInfo = MakeMultiline(12, 72, 290, 70, true);
            _report = MakeMultiline(12, 320, 290, 160, true);

            var analyzeBtn = MakeButton("Modeli Analiz Et", 12, 150);
            analyzeBtn.Click += (_, __) => RefreshModelInfo();

            var previewBtn = MakeButton("Plan Önizle", 156, 150);
            previewBtn.Click += (_, __) => PreviewSelected();

            _sheetFormat = new ComboBox
            {
                Location = new Point(12, 188),
                Width = 290,
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(30, 38, 44),
                ForeColor = Color.FromArgb(236, 232, 224)
            };
            _sheetFormat.Items.AddRange(new object[]
            {
                "A3 Yatay (önerilen)",
                "A4 Yatay",
                "A4 Dikey",
                "A2 Yatay"
            });
            _sheetFormat.SelectedIndex = 0;

            _bendTable = MakeCheck("Büküm tablosu", 12, 220, true);
            _holeTable = MakeCheck("Delik tablosu", 150, 220, true);
            _autoDim = MakeCheck("Otomatik ölçü", 12, 244, true);
            _tolerance = MakeCheck("Tolerans bloğu", 150, 244, true);
            _finish = MakeCheck("Yüzey işleme notu", 12, 268, true);

            _extra = new TextBox
            {
                Location = new Point(12, 296),
                Width = 290,
                Height = 40,
                Multiline = true,
                BackColor = Color.FromArgb(30, 38, 44),
                ForeColor = Color.FromArgb(236, 232, 224),
                BorderStyle = BorderStyle.FixedSingle,
                Text = ""
            };
            var extraLabel = new Label
            {
                Text = "Ek talimat (AutoGBT)",
                Location = new Point(12, 278),
                AutoSize = true,
                ForeColor = Color.FromArgb(180, 176, 168)
            };

            var bendBtn = MakeButton("Büküm Resmi", 12, 490);
            bendBtn.Click += (_, __) => Create(DrawingKind.Bend);
            var cutBtn = MakeButton("Kesim Resmi", 156, 490);
            cutBtn.Click += (_, __) => Create(DrawingKind.Cut);
            var machBtn = MakeButton("İşleme Resmi", 12, 528);
            machBtn.Width = 290;
            machBtn.Click += (_, __) => Create(DrawingKind.Machining);

            Controls.Add(_title);
            Controls.Add(subtitle);
            Controls.Add(_modelInfo);
            Controls.Add(analyzeBtn);
            Controls.Add(previewBtn);
            Controls.Add(_sheetFormat);
            Controls.Add(_bendTable);
            Controls.Add(_holeTable);
            Controls.Add(_autoDim);
            Controls.Add(_tolerance);
            Controls.Add(_finish);
            Controls.Add(extraLabel);
            Controls.Add(_extra);
            Controls.Add(_report);
            Controls.Add(bendBtn);
            Controls.Add(cutBtn);
            Controls.Add(machBtn);

            var hint = new Label
            {
                Text = "Parçayı kaydedip komutu çalıştırın.",
                Location = new Point(12, 566),
                AutoSize = true,
                ForeColor = Color.FromArgb(140, 136, 128)
            };
            Controls.Add(hint);
        }

        public void RefreshModelInfo()
        {
            try
            {
                var model = _addin.Service!.Analyze();
                var sb = new StringBuilder();
                sb.AppendLine(model.PartName);
                sb.AppendLine($"Malzeme: {model.Material}");
                sb.AppendLine($"Sac: {(model.IsSheetMetal ? "Evet" : "Hayır")}  Kalınlık: {model.ThicknessMm:0.##} mm");
                sb.AppendLine($"Büküm: {model.BendCount}   Delik: {model.HoleCount}   Özellik: {model.FeatureCount}");
                sb.AppendLine($"Kutu: {model.BoundingBox.X:0.#} × {model.BoundingBox.Y:0.#} × {model.BoundingBox.Z:0.#} mm");
                _modelInfo.Text = sb.ToString();
            }
            catch (Exception ex)
            {
                _modelInfo.Text = "Analiz yapılamadı:\r\n" + ex.Message;
            }
        }

        private void PreviewSelected()
        {
            try
            {
                // Varsayılan önizleme: sac ise kesim, değilse işleme
                var model = _addin.Service!.Analyze();
                var kind = model.IsSheetMetal ? DrawingKind.Cut : DrawingKind.Machining;
                var plan = _addin.Service.Preview(kind, BuildRequest(kind));
                _report.Text = _addin.Service.Assistant.Explain(model, plan);
            }
            catch (Exception ex)
            {
                _report.Text = ex.Message;
            }
        }

        private void Create(DrawingKind kind)
        {
            try
            {
                var result = _addin.Service!.Create(kind, BuildRequest(kind));
                _report.Text = result.Success
                    ? result.Message + "\r\n" + result.DrawingPath + "\r\n\r\n" +
                      (result.Plan != null
                          ? _addin.Service.Assistant.Explain(_addin.Service.Analyze(), result.Plan)
                          : "")
                    : result.Message;

                _addin.App?.SendMsgToUser2(
                    result.Message,
                    result.Success
                        ? (int)SolidWorks.Interop.swconst.swMessageBoxIcon_e.swMbInformation
                        : (int)SolidWorks.Interop.swconst.swMessageBoxIcon_e.swMbWarning,
                    (int)SolidWorks.Interop.swconst.swMessageBoxBtn_e.swMbOk);
            }
            catch (Exception ex)
            {
                _report.Text = ex.Message;
            }
        }

        private DrawingRequest BuildRequest(DrawingKind kind)
        {
            return new DrawingRequest
            {
                Kind = kind,
                SheetFormat = _sheetFormat.SelectedIndex switch
                {
                    1 => DrawingSheetFormat.A4Landscape,
                    2 => DrawingSheetFormat.A4Portrait,
                    3 => DrawingSheetFormat.A2Landscape,
                    _ => DrawingSheetFormat.A3Landscape
                },
                IncludeBendTable = _bendTable.Checked,
                IncludeHoleTable = _holeTable.Checked,
                AutoDimension = _autoDim.Checked,
                ShowToleranceBlock = _tolerance.Checked,
                ShowSurfaceFinish = _finish.Checked,
                ShowBendNotes = true,
                ShowFlatPattern = true,
                ExtraInstructions = _extra.Text.Trim(),
                DrawnBy = "AutoGBT"
            };
        }

        private TextBox MakeMultiline(int x, int y, int w, int h, bool readOnly)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = w,
                Height = h,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                ReadOnly = readOnly,
                BackColor = Color.FromArgb(30, 38, 44),
                ForeColor = Color.FromArgb(236, 232, 224),
                BorderStyle = BorderStyle.FixedSingle
            };
        }

        private Button MakeButton(string text, int x, int y)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Width = 134,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(232, 168, 56),
                ForeColor = Color.FromArgb(18, 24, 28),
                Cursor = Cursors.Hand
            };
        }

        private CheckBox MakeCheck(string text, int x, int y, bool isChecked)
        {
            return new CheckBox
            {
                Text = text,
                Location = new Point(x, y),
                AutoSize = true,
                Checked = isChecked,
                ForeColor = Color.FromArgb(236, 232, 224)
            };
        }
    }
}
