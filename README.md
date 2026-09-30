# AutoGBT — Windows SolidWorks Teknik Resim Eklentisi

Windows üzerinde çalışan **WinForms** arayüzlü SolidWorks eklentisi.
**AutoGBT** asistanı katı modellerinizden **büküm**, **kesim (açınım)** ve **işleme**
teknik resimlerini planlar ve SolidWorks çizimine aktarır.

## Ne üretir?

Montaj veya parçaları yükleyin / açın. AutoGBT:

1. Tüm **farklı parçaları** ayırt eder (bağlantı elemanlarını eler)
2. Her parçayı sınıflandırır: **sac / kaynak / CNC**
3. Proses bazlı ayrı teknik resimler çıkarır: **Kesim · Büküm · Kaynak · İşleme**
4. Bükümleri **adım adım** (nokta, açı, pay, ölçek) sunar

## Kullanım yüzeyleri

| Uygulama | Ne işe yarar |
| --- | --- |
| **Web stüdyo** (`demo/`) | Dosya yükle → parça listesi → ayrı teknik resim kağıtları |
| **AutoGBT.Desktop.exe** | Windows WinForms önizleme |
| **AutoGBT.Addin** | SolidWorks’te gerçek montaj tarama + `.SLDDRW` toplu üretim |

## Depo yapısı

```
AutoGBT.sln
src/AutoGBT.Core/         Modeller + AutoGBT planlayıcı
src/AutoGBT.UI/           Ortak Windows WinForms paneli
src/AutoGBT.Desktop/      Bağımsız Windows uygulaması (.exe)
src/AutoGBT.SolidWorks/   SolidWorks API (analiz + çizim)
src/AutoGBT.Addin/        COM eklenti + görev paneli host
scripts/build-windows.bat
scripts/install-addin.bat
```

## Gereksinimler (Windows)

- Windows 10/11 x64
- [.NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework/net48)
- Visual Studio 2022 (Desktop development with .NET)
- SolidWorks 2020+ (yalnızca eklenti / `.SLDDRW` için)

## Hızlı başlangıç — Windows arayüzü

Developer Command Prompt / PowerShell:

```bat
scripts\build-windows.bat
```

Masaüstü uygulamayı çalıştırın:

```bat
src\AutoGBT.Desktop\bin\x64\Release\net48\AutoGBT.Desktop.exe
```

Bu pencerede örnek parçalarla Büküm / Kesim / İşleme planlarını görebilir,
raporları `%USERPROFILE%\Documents\AutoGBT\` altına yazabilirsiniz.

## SolidWorks eklentisi

1. `scripts\build-windows.bat` (SolidWorks API DLL’leri kurulu olmalı)
2. Yönetici olarak `scripts\install-addin.bat`
3. SolidWorks → **Tools → Add-ins** → **AutoGBT Teknik Resim**
4. Parçayı kaydedin → komut çubuğu veya sağdaki **AutoGBT** görev paneli

API yolu varsayılan:

`C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist`

```bat
msbuild src\AutoGBT.Addin\AutoGBT.Addin.csproj /p:Configuration=Release /p:Platform=x64 ^
  /p:SolidWorksApiPath="D:\SOLIDWORKS\api\redist"
```

Kaldırma: `scripts\uninstall-addin.bat`

## Arayüz özellikleri

- Model analizi (malzeme, sac, büküm, delik)
- Teknik resim türü seçimi: Büküm / Kesim / İşleme
- Sayfa formatı, büküm/delik tablosu, tolerans, yüzey notları
- AutoGBT ek talimat alanı
- Sağ panelde teknik resim yerleşim önizlemesi (Windows çizimi)
- SolidWorks’te tek tıkla `.SLDDRW` oluşturma

## İsteğe bağlı ortam değişkenleri

| Değişken | Açıklama |
| --- | --- |
| `AUTOGBT_DRAWING_TEMPLATE` | Özel `.drwdot` şablon yolu |
| `AUTOGBT_API_KEY` | İleride bulut LLM (şu an kural tabanlı motor) |

## Not

Bu ortam Linux bulut ajanıdır; WinForms `.exe` yalnızca Windows’ta açılır.
Kaynak kod Windows hedefidir (`net48` + WinForms).
