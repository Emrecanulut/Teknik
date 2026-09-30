using System;
using System.Drawing;
using System.Windows.Forms;
using AutoGBT.Core;
using AutoGBT.UI;

namespace AutoGBT.Desktop
{
    /// <summary>
    /// Windows masaüstü AutoGBT arayüzü.
    /// SolidWorks olmadan planlama ve önizleme; eklenti ile aynı WinForms paneli kullanır.
    /// </summary>
    public sealed class MainForm : Form
    {
        public MainForm()
        {
            Text = "AutoGBT — Teknik Resim Asistanı";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 700);
            Size = new Size(1280, 820);
            BackColor = Color.FromArgb(14, 20, 18);
            ForeColor = Color.FromArgb(232, 224, 208);
            Font = new Font("Segoe UI", 9F);

            var host = new DesktopAutoGbtHost(SampleParts.All[0]);
            var panel = new AutoGbtPanel(host, showSamplePicker: true)
            {
                Dock = DockStyle.Fill
            };

            var menu = new MenuStrip
            {
                BackColor = Color.FromArgb(22, 30, 27),
                ForeColor = Color.FromArgb(232, 224, 208)
            };
            var file = new ToolStripMenuItem("Dosya");
            file.DropDownItems.Add("Belgeler\\AutoGBT klasörünü aç", null, (_, __) =>
            {
                var dir = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "AutoGBT");
                System.IO.Directory.CreateDirectory(dir);
                System.Diagnostics.Process.Start("explorer.exe", dir);
            });
            file.DropDownItems.Add(new ToolStripSeparator());
            file.DropDownItems.Add("Çıkış", null, (_, __) => Close());

            var help = new ToolStripMenuItem("Yardım");
            help.DropDownItems.Add("SolidWorks eklentisi hakkında", null, (_, __) =>
            {
                MessageBox.Show(
                    this,
                    "Bu Windows uygulaması AutoGBT arayüzünü gösterir.\n\n" +
                    "Gerçek .SLDDRW üretimi için AutoGBT.Addin'i SolidWorks'e kurun:\n" +
                    "1) AutoGBT.sln → Release | x64 derleyin\n" +
                    "2) scripts\\install-addin.bat (yönetici)\n" +
                    "3) SolidWorks → Tools → Add-ins → AutoGBT Teknik Resim",
                    "AutoGBT",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            });

            menu.Items.Add(file);
            menu.Items.Add(help);
            MainMenuStrip = menu;

            Controls.Add(panel);
            Controls.Add(menu);
        }
    }
}
