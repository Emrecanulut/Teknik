using System;
using System.Runtime.InteropServices;
using AutoGBT.Core.AI;
using AutoGBT.Core.Models;
using AutoGBT.SolidWorks;
using AutoGBT.SolidWorks.Analysis;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;
using SolidWorks.Interop.swconst;

namespace AutoGBT.Addin
{
    /// <summary>
    /// SolidWorks COM eklenti giriş noktası — AutoGBT Windows / SolidWorks arayüzü.
    /// </summary>
    [ComVisible(true)]
    [Guid("8F3C2A91-6B4E-4D7A-9C11-A7E5D2B8F401")]
    [ProgId("AutoGBT.Addin")]
    public class SwAddin : ISwAddin
    {
        public const string AddinGuid = "8F3C2A91-6B4E-4D7A-9C11-A7E5D2B8F401";
        public const string AddinTitle = "AutoGBT Teknik Resim";
        public const string AddinDescription =
            "Katı modellerden büküm, kesim ve işleme teknik resimlerini AutoGBT ile üretir.";

        private ISldWorks? _swApp;
        private int _addinCookie;
        private CommandManagerHost? _commands;
        private TaskPaneHost? _taskPane;
        private AutoGbtService? _service;

        public ISldWorks? App => _swApp;
        public AutoGbtService? Service => _service;
        public int Cookie => _addinCookie;

        #region ISwAddin

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            _swApp = (ISldWorks)ThisSW;
            _addinCookie = Cookie;
            _swApp.SetAddinCallbackInfo2(0, this, Cookie);

            _service = new AutoGbtService(_swApp);
            _commands = new CommandManagerHost(this);
            _commands.Create();

            _taskPane = new TaskPaneHost(this);
            _taskPane.Create();

            _swApp.SendMsgToUser2(
                "AutoGBT yüklendi. Windows görev panelinden veya komut çubuğundan Büküm / Kesim / İşleme resmi oluşturabilirsiniz.",
                (int)swMessageBoxIcon_e.swMbInformation,
                (int)swMessageBoxBtn_e.swMbOk);

            return true;
        }

        public bool DisconnectFromSW()
        {
            try
            {
                _taskPane?.Destroy();
                _commands?.Destroy();
            }
            catch
            {
                // SolidWorks kapanırken COM hatalarını yut.
            }

            _taskPane = null;
            _commands = null;
            _service = null;
            _swApp = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return true;
        }

        #endregion

        #region Callbacks (CommandManager)

        public void OnCreateBend()
        {
            RunSafe(() => ShowResult(_service!.CreateBendDrawing()));
        }

        public void OnCreateCut()
        {
            RunSafe(() => ShowResult(_service!.CreateCutDrawing()));
        }

        public void OnCreateMachining()
        {
            RunSafe(() => ShowResult(_service!.CreateMachiningDrawing()));
        }

        public void OnOpenTaskPane()
        {
            RunSafe(() => _taskPane?.Show());
        }

        public void OnAnalyzeWithAutoGbt()
        {
            RunSafe(() =>
            {
                var model = _service!.Analyze();
                var kinds = _service.Assistant.RecommendKinds(model);
                var msg =
                    "AutoGBT Model Analizi\n\n" +
                    ModelAnalyzer.FormatSummary(model) +
                    "\n\nÖnerilen resimler:\n- " +
                    string.Join("\n- ", System.Linq.Enumerable.Select(kinds, AutoGbtAssistant.ToTurkish));
                _swApp!.SendMsgToUser2(msg,
                    (int)swMessageBoxIcon_e.swMbInformation,
                    (int)swMessageBoxBtn_e.swMbOk);
            });
        }

        public int OnEnable()
        {
            try
            {
                var doc = _swApp?.ActiveDoc as ModelDoc2;
                if (doc == null) return 0;
                var t = doc.GetType();
                return (t == (int)swDocumentTypes_e.swDocPART
                        || t == (int)swDocumentTypes_e.swDocASSEMBLY) ? 1 : 0;
            }
            catch
            {
                return 0;
            }
        }

        #endregion

        private void RunSafe(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _swApp?.SendMsgToUser2(
                    "AutoGBT hata: " + ex.Message,
                    (int)swMessageBoxIcon_e.swMbStop,
                    (int)swMessageBoxBtn_e.swMbOk);
            }
        }

        private void ShowResult(DrawingResult result)
        {
            var icon = result.Success
                ? swMessageBoxIcon_e.swMbInformation
                : swMessageBoxIcon_e.swMbWarning;

            var msg = result.Message;
            if (result.Success && !string.IsNullOrEmpty(result.DrawingPath))
                msg += "\n\nDosya: " + result.DrawingPath;
            if (result.Warnings.Count > 0)
                msg += "\n\n" + string.Join("\n", result.Warnings);

            _swApp!.SendMsgToUser2(msg, (int)icon, (int)swMessageBoxBtn_e.swMbOk);
        }

        #region COM Registration

        [ComRegisterFunction]
        public static void RegisterFunction(Type t)
        {
            try
            {
                var keyPath = $@"SOFTWARE\SolidWorks\Addins\{{{AddinGuid}}}";
                using var rk = Microsoft.Win32.Registry.LocalMachine.CreateSubKey(keyPath);
                rk?.SetValue(null, 0);
                rk?.SetValue("Description", AddinDescription);
                rk?.SetValue("Title", AddinTitle);

                using var cu = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(
                    $@"Software\SolidWorks\AddInsStartup\{{{AddinGuid}}}");
                cu?.SetValue(null, 1, Microsoft.Win32.RegistryValueKind.DWord);
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show("AutoGBT kayıt hatası: " + ex.Message);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type t)
        {
            try
            {
                Microsoft.Win32.Registry.LocalMachine.DeleteSubKeyTree(
                    $@"SOFTWARE\SolidWorks\Addins\{{{AddinGuid}}}", false);
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(
                    $@"Software\SolidWorks\AddInsStartup\{{{AddinGuid}}}", false);
            }
            catch
            {
                // ignore
            }
        }

        #endregion
    }
}
