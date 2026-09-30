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
- **Derleyici (birini kurun):**
  - [Visual Studio 2022 Community](https://visualstudio.microsoft.com/tr/downloads/) — workload: **.NET desktop development**
  - veya [Build Tools for Visual Studio 2022](https://visualstudio.microsoft.com/tr/downloads/#build-tools-for-visual-studio-2022) — aynı workload
- [.NET Framework 4.8 Developer Pack](https://dotnet.microsoft.com/download/dotnet-framework/net48) (VS ile genelde gelir)
- SolidWorks 2020+ (yalnızca eklenti / `.SLDDRW` için; masaüstü uygulama SolidWorks’siz de açılır)

`scripts\build-windows.bat` MSBuild’i otomatik bulur; Developer Command Prompt şart değildir.

## Tek adım kurulum (Windows)

```bat
git pull
scripts\kur-hepsini.bat
```

## Visual Studio ile çalıştırma (en kolay)

1. `AutoGBT.sln` dosyasını açın  
2. Solution Explorer’da **AutoGBT.Desktop** → sağ tık → **Set as Startup Project**  
3. Üstte yapılandırma: **Debug** + **Any CPU**  
4. **F5** (Start)

> Tüm solution’ı Build ederseniz SolidWorks’siz PC’de Addin/SolidWorks projeleri kırmızı kalabilir; sorun değil. Startup proje **Desktop** olsun.

Takılırsa tanı raporu:

```bat
scripts\tani.bat
```

Masaüstü uygulamada örnek parçalarla Büküm / Kesim / İşleme planlarını görebilir,
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
