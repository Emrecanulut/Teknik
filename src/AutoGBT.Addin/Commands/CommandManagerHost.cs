using System;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swconst;

namespace AutoGBT.Addin
{
    /// <summary>
    /// AutoGBT komut çubuğu: Analiz, Büküm, Kesim, İşleme, Görev Paneli.
    /// </summary>
    internal sealed class CommandManagerHost
    {
        private readonly SwAddin _addin;
        private CommandGroup? _group;
        private const int MainGroupId = 55101;

        public CommandManagerHost(SwAddin addin)
        {
            _addin = addin;
        }

        public void Create()
        {
            var app = _addin.App ?? throw new InvalidOperationException("SW app yok.");
            var cmdMgr = GetCommandManagerSafe(app);

            int cmdGroupErr = 0;
            _group = cmdMgr.CreateCommandGroup2(
                MainGroupId,
                SwAddin.AddinTitle,
                "AutoGBT teknik resim komutları",
                "AutoGBT",
                -1,
                true,
                ref cmdGroupErr);

            if (_group == null) return;

            // Tip: 0 menu+toolbar, ikon indeksi -1 (metin tabanlı)
            _group.AddCommandItem2("AutoGBT Analiz", -1,
                "Aktif modeli AutoGBT ile analiz et",
                "AutoGBT Analiz",
                0, nameof(SwAddin.OnAnalyzeWithAutoGbt), nameof(SwAddin.OnEnable), 0,
                (int)swCommandItemType_e.swMenuItem | (int)swCommandItemType_e.swToolbarItem);

            _group.AddCommandItem2("Büküm Resmi", -1,
                "Büküm teknik resmi oluştur",
                "Büküm",
                1, nameof(SwAddin.OnCreateBend), nameof(SwAddin.OnEnable), 1,
                (int)swCommandItemType_e.swMenuItem | (int)swCommandItemType_e.swToolbarItem);

            _group.AddCommandItem2("Kesim Resmi", -1,
                "Kesim / açınım teknik resmi oluştur",
                "Kesim",
                2, nameof(SwAddin.OnCreateCut), nameof(SwAddin.OnEnable), 2,
                (int)swCommandItemType_e.swMenuItem | (int)swCommandItemType_e.swToolbarItem);

            _group.AddCommandItem2("İşleme Resmi", -1,
                "İşleme teknik resmi oluştur",
                "İşleme",
                3, nameof(SwAddin.OnCreateMachining), nameof(SwAddin.OnEnable), 3,
                (int)swCommandItemType_e.swMenuItem | (int)swCommandItemType_e.swToolbarItem);

            _group.AddCommandItem2("AutoGBT Paneli", -1,
                "AutoGBT görev panelini aç",
                "Panel",
                4, nameof(SwAddin.OnOpenTaskPane), "", 4,
                (int)swCommandItemType_e.swMenuItem | (int)swCommandItemType_e.swToolbarItem);

            _group.HasToolbar = true;
            _group.HasMenu = true;
            _group.Activate();
        }

        private CommandManager GetCommandManagerSafe(ISldWorks app)
        {
            return (CommandManager)app.GetCommandManager(_addin.Cookie);
        }

        public void Destroy()
        {
            try
            {
                var app = _addin.App;
                if (app == null) return;
                var cmdMgr = GetCommandManagerSafe(app);
                cmdMgr.RemoveCommandGroup2(MainGroupId, true);
            }
            catch
            {
                // ignore
            }
            _group = null;
        }
    }
}
