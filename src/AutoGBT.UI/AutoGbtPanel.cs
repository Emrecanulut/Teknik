using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using AutoGBT.Core;
using AutoGBT.Core.AI;
using AutoGBT.Core.Models;

namespace AutoGBT.UI
{
    /// <summary>
    /// Windows WinForms AutoGBT paneli — SolidWorks görev paneli ve masaüstü uygulaması ortak UI.
    /// </summary>
    public sealed class AutoGbtPanel : UserControl
    {
        private static readonly Color Bg = Color.FromArgb(14, 20, 18);
        private static readonly Color Panel = Color.FromArgb(22, 30, 27);
        private static readonly Color Field = Color.FromArgb(16, 23, 20);
        private static readonly Color Ink = Color.FromArgb(232, 224, 208);
        private static readonly Color Muted = Color.FromArgb(154, 148, 136);
        private static readonly Color Brass = Color.FromArgb(212, 160, 23);
        private static readonly Color Paper = Color.FromArgb(230, 220, 200);
        private static readonly Color PaperInk = Color.FromArgb(28, 34, 30);

        private readonly IAutoGbtHost _host;
        private readonly bool _showSamplePicker;

        private ComboBox? _samplePicker;
        private Label _status = null!;
        private TextBox _modelInfo = null!;
        private TextBox _report = null!;
        private TextBox _extra = null!;
        private ComboBox _sheetFormat = null!;
        private ComboBox _kindBox = null!;
        private CheckBox _bendTable = null!;
        private CheckBox _holeTable = null!;
        private CheckBox _autoDim = null!;
        private CheckBox _tolerance = null!;
        private CheckBox _finish = null!;
        private DrawingPreviewControl _preview = null!;
        private DrawingKind _selectedKind = DrawingKind.Cut;

        public AutoGbtPanel(IAutoGbtHost host, bool showSamplePicker = false)
        {
            _host = host ?? throw new ArgumentNullException(nameof(host));
            _showSamplePicker = showSamplePicker;
            DoubleBuffered = true;
            BackColor = Bg;
            ForeColor = Ink;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            Dock = DockStyle.Fill;
            MinimumSize = new Size(360, 640);
            BuildLayout();
            RefreshModelInfo();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(10),
                BackColor = Bg
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var header = new Panel { Dock = DockStyle.Fill, BackColor = Bg };
            var title = new Label
            {
                Text = "AutoGBT",
                Font = new Font("Segoe UI Semibold", 18F),
                ForeColor = Brass,
                AutoSize = true,
                Location = new Point(2, 4)
            };
            var subtitle = new Label
            {
                Text = "Büküm · Kesim · İşleme  |  " + _host.HostName,
                ForeColor = Muted,
                AutoSize = true,
                Location = new Point(4, 34)
            };
            header.Controls.Add(title);
            header.Controls.Add(subtitle);

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                BackColor = Bg
            };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 340));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var left = BuildLeftColumn();
            var right = BuildRightColumn();
            body.Controls.Add(left, 0, 0);
            body.Controls.Add(right, 1, 0);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(body, 0, 1);
            Controls.Add(root);

            // Dar görev paneli: tek sütun
            Resize += (_, __) =>
            {
                if (Width < 520)
                {
                    body.ColumnStyles[0].Width = Width - 24;
                    body.SetColumnSpan(left, 2);
                    right.Visible = false;
                }
                else
                {
                    body.ColumnStyles[0].Width = 340;
                    body.SetColumn(left, 0);
                    body.SetColumnSpan(left, 1);
                    right.Visible = true;
                }
            };
        }

        private Control BuildLeftColumn()
        {
            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = Panel,
                Padding = new Padding(12)
            };

            int y = 8;
            void Add(Control c)
            {
                scroll.Controls.Add(c);
            }

            if (_showSamplePicker)
            {
                Add(MakeLabel("Örnek parça", 12, y)); y += 20;
                _samplePicker = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Location = new Point(12, y),
                    Width = 300,
                    BackColor = Field,
                    ForeColor = Ink,
                    FlatStyle = FlatStyle.Flat
                };
                foreach (var p in SampleParts.All)
                    _samplePicker.Items.Add(p.PartName);
                _samplePicker.SelectedIndex = 0;
                _samplePicker.SelectedIndexChanged += (_, __) =>
                {
                    if (_host is DesktopAutoGbtHost desktop && _samplePicker.SelectedIndex >= 0)
                    {
                        desktop.SetModel(SampleParts.All[_samplePicker.SelectedIndex]);
                        RefreshModelInfo();
                    }
                };
                Add(_samplePicker);
                y += 36;
            }

            Add(MakeLabel("Model özeti", 12, y)); y += 20;
            _modelInfo = MakeMultiline(12, y, 300, 88, true);
            Add(_modelInfo);
            y += 98;

            var analyzeBtn = MakeButton("Modeli Analiz Et", 12, y, 145);
            analyzeBtn.Click += (_, __) => RefreshModelInfo();
            var previewBtn = MakeButton("Plan Önizle", 167, y, 145);
            previewBtn.Click += (_, __) => PreviewSelected();
            Add(analyzeBtn);
            Add(previewBtn);
            y += 42;

            Add(MakeLabel("Teknik resim türü", 12, y)); y += 20;
            _kindBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(12, y),
                Width = 300,
                BackColor = Field,
                ForeColor = Ink,
                FlatStyle = FlatStyle.Flat
            };
            _kindBox.Items.AddRange(new object[] { "Büküm", "Kesim / Açınım", "İşleme", "Kaynak" });
            _kindBox.SelectedIndex = 1;
            _kindBox.SelectedIndexChanged += (_, __) =>
            {
                _selectedKind = _kindBox.SelectedIndex switch
                {
                    0 => DrawingKind.Bend,
                    2 => DrawingKind.Machining,
                    3 => DrawingKind.Weld,
                    _ => DrawingKind.Cut
                };
            };
            Add(_kindBox);
            y += 36;

            Add(MakeLabel("Sayfa formatı", 12, y)); y += 20;
            _sheetFormat = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(12, y),
                Width = 300,
                BackColor = Field,
                ForeColor = Ink,
                FlatStyle = FlatStyle.Flat
            };
            _sheetFormat.Items.AddRange(new object[]
            {
                "A3 Yatay (önerilen)",
                "A4 Yatay",
                "A4 Dikey",
                "A2 Yatay"
            });
            _sheetFormat.SelectedIndex = 0;
            Add(_sheetFormat);
            y += 36;

            _bendTable = MakeCheck("Büküm tablosu", 12, y, true);
            _holeTable = MakeCheck("Delik tablosu", 160, y, true);
            Add(_bendTable); Add(_holeTable); y += 26;
            _autoDim = MakeCheck("Otomatik ölçü", 12, y, true);
            _tolerance = MakeCheck("Tolerans bloğu", 160, y, true);
            Add(_autoDim); Add(_tolerance); y += 26;
            _finish = MakeCheck("Yüzey işleme notu", 12, y, true);
            Add(_finish); y += 30;

            Add(MakeLabel("Ek talimat (AutoGBT)", 12, y)); y += 20;
            _extra = MakeMultiline(12, y, 300, 52, false);
            Add(_extra);
            y += 62;

            var bendBtn = MakeButton("Büküm Resmi", 12, y, 145);
            bendBtn.Click += (_, __) => Create(DrawingKind.Bend);
            var cutBtn = MakeButton("Kesim Resmi", 167, y, 145);
            cutBtn.Click += (_, __) => Create(DrawingKind.Cut);
            Add(bendBtn); Add(cutBtn); y += 40;

            var machBtn = MakeButton("İşleme Resmi", 12, y, 145);
            machBtn.Click += (_, __) => Create(DrawingKind.Machining);
            var weldBtn = MakeButton("Kaynak Resmi", 167, y, 145);
            weldBtn.Click += (_, __) => Create(DrawingKind.Weld);
            Add(machBtn); Add(weldBtn); y += 40;

            if (_host.SupportsAssemblyBatch)
            {
                var batchBtn = MakeButton("Montajdaki TÜM parçalar", 12, y, 300);
                batchBtn.BackColor = Color.FromArgb(200, 140, 40);
                batchBtn.Click += (_, __) => RunAssemblyBatch();
                Add(batchBtn); y += 42;
            }

            _status = new Label
            {
                Text = _host.CanCreateSolidWorksDrawing
                    ? "SolidWorks bağlı — .SLDDRW üretilebilir."
                    : "Windows önizleme — plan Documents\\AutoGBT altına yazılır.",
                Location = new Point(12, y),
                Width = 300,
                Height = 36,
                ForeColor = Muted
            };
            Add(_status);
            y += 44;

            Add(MakeLabel("AutoGBT raporu", 12, y)); y += 20;
            _report = MakeMultiline(12, y, 300, 180, true);
            Add(_report);

            return scroll;
        }

        private Control BuildRightColumn()
        {
            var wrap = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Bg
            };
            _preview = new DrawingPreviewControl { Dock = DockStyle.Fill };
            wrap.Controls.Add(_preview);
            return wrap;
        }

        public void RefreshModelInfo()
        {
            try
            {
                var model = _host.Analyze();
                var sb = new StringBuilder();
                sb.AppendLine(model.PartName);
                sb.AppendLine($"Malzeme: {model.Material}");
                sb.AppendLine($"Sac: {(model.IsSheetMetal ? "Evet" : "Hayır")}  Kalınlık: {model.ThicknessMm:0.##} mm");
                sb.AppendLine($"Büküm: {model.BendCount}   Delik: {model.HoleCount}   Özellik: {model.FeatureCount}");
                sb.AppendLine($"Kutu: {model.BoundingBox.X:0.#} × {model.BoundingBox.Y:0.#} × {model.BoundingBox.Z:0.#} mm");
                if (model.Notes.Count > 0)
                    sb.AppendLine(model.Notes[0]);
                _modelInfo.Text = sb.ToString();

                var kinds = new AutoGbtAssistant().RecommendKinds(model);
                _selectedKind = kinds[0];
                _kindBox.SelectedIndex = _selectedKind switch
                {
                    DrawingKind.Bend => 0,
                    DrawingKind.Machining => 2,
                    DrawingKind.Weld => 3,
                    _ => 1
                };
                PreviewSelected();
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
                var model = _host.Analyze();
                var plan = _host.Preview(_selectedKind, BuildRequest(_selectedKind));
                _report.Text = _host.Explain(model, plan);
                _preview.ShowPlan(plan, model);
                _status.Text = $"Önizleme: {AutoGbtAssistant.ToTurkish(plan.Kind)} · ölçek {plan.RecommendedScale} · güven %{plan.Confidence * 100:0}";
                _status.ForeColor = Brass;
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
                _selectedKind = kind;
                _kindBox.SelectedIndex = kind switch
                {
                    DrawingKind.Bend => 0,
                    DrawingKind.Machining => 2,
                    DrawingKind.Weld => 3,
                    _ => 1
                };

                Cursor = Cursors.WaitCursor;
                var result = _host.CreateDrawing(kind, BuildRequest(kind));
                Cursor = Cursors.Default;

                if (result.Plan != null)
                {
                    var model = _host.Analyze();
                    _preview.ShowPlan(result.Plan, model);
                    _report.Text = result.Message + "\r\n" + result.DrawingPath + "\r\n\r\n" +
                                   _host.Explain(model, result.Plan);
                }
                else
                {
                    _report.Text = result.Message;
                }

                _status.Text = result.Success ? "Tamam: " + result.Message : result.Message;
                _status.ForeColor = result.Success ? Brass : Color.FromArgb(196, 92, 62);
                _host.NotifyUser(result.Message, !result.Success);

                if (result.Success && !string.IsNullOrEmpty(result.DrawingPath))
                {
                    MessageBox.Show(
                        this,
                        result.Message + "\r\n\r\n" + result.DrawingPath,
                        "AutoGBT",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                _report.Text = ex.Message;
                MessageBox.Show(this, ex.Message, "AutoGBT", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RunAssemblyBatch()
        {
            try
            {
                Cursor = Cursors.WaitCursor;
                var report = _host.RunAssemblyBatch(BuildRequest(_selectedKind));
                Cursor = Cursors.Default;
                _report.Text = report;
                _status.Text = "Montaj toplu üretim tamamlandı.";
                _status.ForeColor = Brass;
                MessageBox.Show(this, report, "AutoGBT Montaj", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                _report.Text = ex.Message;
                MessageBox.Show(this, ex.Message, "AutoGBT", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

        private static Label MakeLabel(string text, int x, int y) => new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            ForeColor = Muted,
            Font = new Font("Segoe UI Semibold", 8.5F)
        };

        private static TextBox MakeMultiline(int x, int y, int w, int h, bool readOnly) => new TextBox
        {
            Location = new Point(x, y),
            Width = w,
            Height = h,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = readOnly,
            BackColor = Field,
            ForeColor = Ink,
            BorderStyle = BorderStyle.FixedSingle
        };

        private static Button MakeButton(string text, int x, int y, int width) => new Button
        {
            Text = text,
            Location = new Point(x, y),
            Width = width,
            Height = 32,
            FlatStyle = FlatStyle.Flat,
            BackColor = Brass,
            ForeColor = Color.FromArgb(20, 17, 10),
            Font = new Font("Segoe UI Semibold", 9F),
            Cursor = Cursors.Hand
        };

        private static CheckBox MakeCheck(string text, int x, int y, bool isChecked) => new CheckBox
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true,
            Checked = isChecked,
            ForeColor = Ink
        };
    }

    /// <summary>
    /// Windows üzerinde teknik resim yerleşim önizlemesi.
    /// </summary>
    public sealed class DrawingPreviewControl : Control
    {
        private DrawingPlan? _plan;
        private ModelSummary? _model;

        public DrawingPreviewControl()
        {
            DoubleBuffered = true;
            BackColor = Color.FromArgb(14, 20, 18);
        }

        public void ShowPlan(DrawingPlan plan, ModelSummary model)
        {
            _plan = plan;
            _model = model;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(BackColor);

            if (_plan == null || _model == null)
            {
                using var f = new Font("Segoe UI", 11F);
                TextRenderer.DrawText(g, "AutoGBT plan önizlemesi burada görünür.", f,
                    ClientRectangle, Color.FromArgb(154, 148, 136),
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            int pad = 16;
            var sheet = new Rectangle(pad, pad, Math.Max(40, Width - pad * 2), Math.Max(40, Height - pad * 2));
            using (var paper = new SolidBrush(Color.FromArgb(230, 220, 200)))
                g.FillRectangle(paper, sheet);
            using (var border = new Pen(Color.FromArgb(28, 34, 30), 2))
                g.DrawRectangle(border, sheet);
            using (var inner = new Pen(Color.FromArgb(80, 28, 34, 30), 1))
                g.DrawRectangle(inner, Rectangle.Inflate(sheet, -8, -8));

            using var titleFont = new Font("Segoe UI Semibold", 11F);
            using var mono = new Font("Consolas", 8F);
            using var small = new Font("Segoe UI", 7.5F);
            using var ink = new SolidBrush(Color.FromArgb(28, 34, 30));
            using var muted = new SolidBrush(Color.FromArgb(90, 80, 60));

            g.DrawString("AUTOGBT · " + AutoGbtAssistant.ToTurkish(_plan.Kind).ToUpperInvariant(), mono, ink, sheet.Left + 14, sheet.Top + 12);
            g.DrawString(_plan.Title, titleFont, ink, sheet.Left + 14, sheet.Top + 28);
            g.DrawString($"Ölçek {_plan.RecommendedScale}   Güven %{_plan.Confidence * 100:0}", mono, muted, sheet.Right - 210, sheet.Top + 14);

            var canvas = new Rectangle(sheet.Left + 12, sheet.Top + 55, sheet.Width - 24, sheet.Height - 150);
            using (var dash = new Pen(Color.FromArgb(120, 28, 34, 30)) { DashStyle = DashStyle.Dash })
            {
                foreach (var view in _plan.Views)
                {
                    int x = canvas.Left + (int)(view.RelativeX * canvas.Width);
                    int y = canvas.Bottom - (int)((view.RelativeY + view.RelativeHeight) * canvas.Height);
                    int w = Math.Max(40, (int)(view.RelativeWidth * canvas.Width));
                    int h = Math.Max(40, (int)(view.RelativeHeight * canvas.Height));
                    var r = Rectangle.Intersect(new Rectangle(x, y, w, h), canvas);
                    if (r.Width < 8 || r.Height < 8) continue;

                    g.DrawRectangle(dash, r);
                    g.DrawString(view.Name, mono, ink, r.Left + 4, r.Top + 3);
                    DrawGlyph(g, r, view, _plan.Kind);
                    g.DrawString(view.Description, small, muted,
                        new RectangleF(r.Left + 4, r.Bottom - 28, r.Width - 8, 26));
                }
            }

            var footer = new Rectangle(sheet.Left + 12, sheet.Bottom - 86, sheet.Width - 24, 70);
            using (var pen = new Pen(Color.FromArgb(28, 34, 30)))
                g.DrawRectangle(pen, footer);
            var block = string.Join("  |  ", _plan.TitleBlockFields.Take(6));
            g.DrawString(block, mono, ink, new RectangleF(footer.Left + 6, footer.Top + 6, footer.Width - 12, 28));
            var ann = string.Join("   ", _plan.Annotations.OrderByDescending(a => a.Priority).Take(2).Select(a => $"[{a.Kind}] {a.Text}"));
            g.DrawString(ann, small, muted, new RectangleF(footer.Left + 6, footer.Top + 36, footer.Width - 12, 30));
        }

        private static void DrawGlyph(Graphics g, Rectangle r, ViewPlan view, DrawingKind kind)
        {
            using var pen = new Pen(Color.FromArgb(28, 34, 30), 1.6f);
            var cx = r.Left + r.Width / 2;
            var cy = r.Top + r.Height / 2 + 4;
            int w = Math.Min(r.Width - 20, 90);
            int h = Math.Min(r.Height - 36, 50);
            var box = new Rectangle(cx - w / 2, cy - h / 2, w, h);

            if (view.IsFlatPattern || (kind == DrawingKind.Cut && !view.IsIsometric))
            {
                g.DrawRectangle(pen, box);
                using var dash = new Pen(Color.FromArgb(28, 34, 30), 1) { DashStyle = DashStyle.Dash };
                g.DrawLine(dash, box.Left, cy, box.Right, cy);
                g.DrawEllipse(pen, box.Left + 10, box.Top + 10, 8, 8);
                g.DrawEllipse(pen, box.Right - 22, box.Top + 10, 8, 8);
            }
            else if (view.IsIsometric)
            {
                Point[] top =
                {
                    new Point(cx, cy - h / 2),
                    new Point(cx + w / 2, cy - h / 6),
                    new Point(cx, cy + h / 6),
                    new Point(cx - w / 2, cy - h / 6)
                };
                g.DrawPolygon(pen, top);
                g.DrawLine(pen, top[3], new Point(top[3].X, top[3].Y + h / 3));
                g.DrawLine(pen, top[2], new Point(top[2].X, top[2].Y + h / 3));
                g.DrawLine(pen, top[1], new Point(top[1].X, top[1].Y + h / 3));
            }
            else if (view.IsSection)
            {
                g.DrawRectangle(pen, box);
                for (int i = box.Left; i < box.Right; i += 8)
                    g.DrawLine(pen, i, box.Top + 8, i + 6, box.Bottom - 8);
                g.DrawEllipse(pen, cx - 8, cy - 8, 16, 16);
            }
            else
            {
                g.DrawRectangle(pen, box);
                g.DrawLine(pen, box.Left, cy, box.Right, cy);
                if (kind == DrawingKind.Bend)
                {
                    g.DrawLines(pen, new[]
                    {
                        new Point(box.Left, box.Bottom - 4),
                        new Point(box.Left, cy),
                        new Point(box.Right, cy),
                        new Point(box.Right, box.Top + 6)
                    });
                }
            }
        }
    }
}
