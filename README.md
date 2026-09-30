# AutoGBT — SolidWorks Teknik Resim Eklentisi

Katı modellerinizden **büküm**, **kesim (açınım)** ve **işleme** teknik resimlerini
otomatik üreten bir SolidWorks eklentisi. İçindeki **AutoGBT** asistanı modeli
analiz eder; görünüş yerleşimi, büküm/delik tabloları, tolerans ve üretim
notlarını planlayıp SolidWorks çizimine aktarır.

## Ne yapar?

| Komut | Çıktı |
| --- | --- |
| **AutoGBT Analiz** | Malzeme, sac kalınlığı, büküm/delik sayısı, önerilen resim türleri |
| **Büküm Resmi** | İzometrik + ön/yan görünüş, büküm detayı, büküm tablosu |
| **Kesim Resmi** | Flat pattern açınım, büküm çizgileri, delik tablosu, izometrik referans |
| **İşleme Resmi** | Ön/üst/yan/izometrik (+ kesit), delik tablosu, yüzey ve ISO 2768 notları |
| **AutoGBT Paneli** | SolidWorks görev panelinden seçenekli üretim |

## Depo yapısı

```
AutoGBT.sln                 SolidWorks eklenti çözümü (.NET Framework 4.8)
src/AutoGBT.Core/           Analiz, AutoGBT planlayıcı, Drawing API üretimi
src/AutoGBT.Addin/          COM eklenti, komut çubuğu, görev paneli
scripts/                    install-addin.bat / uninstall-addin.bat
demo/                       AutoGBT stüdyosu (tarayıcı önizlemesi)
```

## SolidWorks kurulumu (Windows)

Gereksinimler:

- SolidWorks 2020+ (x64)
- Visual Studio 2022 (Desktop development with .NET)
- SolidWorks API redistributable (`SolidWorks.Interop.*.dll`)

Derleme:

```bat
msbuild AutoGBT.sln /p:Configuration=Release /p:Platform=x64
```

API DLL yolu varsayılan:

`C:\Program Files\SOLIDWORKS Corp\SOLIDWORKS\api\redist`

Farklıysa:

```bat
msbuild AutoGBT.sln /p:Configuration=Release /p:Platform=x64 ^
  /p:SolidWorksApiPath="D:\SOLIDWORKS\api\redist"
```

Kurulum (yönetici):

```bat
scripts\install-addin.bat
```

SolidWorks → **Tools → Add-ins** → **AutoGBT Teknik Resim** işaretleyin.

Kaldırma:

```bat
scripts\uninstall-addin.bat
```

### Kullanım

1. Parça veya montajı açın ve **kaydedin** (görünüşler dosya yoluna ihtiyaç duyar).
2. Komut çubuğundan **Büküm / Kesim / İşleme** seçin veya görev panelini açın.
3. AutoGBT `.SLDDRW` dosyasını parça klasörüne yazar (`_Buküm`, `_Kesim`, `_Isleme`).

İsteğe bağlı ortam değişkenleri:

| Değişken | Açıklama |
| --- | --- |
| `AUTOGBT_DRAWING_TEMPLATE` | Özel `.drwdot` şablon yolu |
| `AUTOGBT_API_KEY` | İleride bulut LLM bağlamak için (şu an kural tabanlı motor yeterli) |
| `AUTOGBT_API_BASE` | Özel API taban URL |

## Demo stüdyosu (tarayıcı)

SolidWorks olmadan AutoGBT planlayıcısını denemek için:

```bash
cd demo
npm install
npm run dev
```

Varsayılan adres: [http://127.0.0.1:43147](http://127.0.0.1:43147)

Demo örnek parçalar üzerinden büküm / kesim / işleme planlarını ve sanal teknik
resim yerleşimini gösterir. Gerçek `.SLDDRW` üretimi yalnızca SolidWorks
eklentisinde yapılır.

## AutoGBT nasıl planlar?

1. Feature ağacından sac metal, büküm, delik ve sınır kutusu okunur.
2. Seçilen resim türüne göre görünüşler, tablolar ve notlar üretilir.
3. Ölçek parça boyutuna göre önerilir; kalite uyarıları rapora eklenir.
4. SolidWorks Drawing API ile görünüşler ve notlar çizime yerleştirilir.

## Lisans

Bu proje örnek / başlangıç eklentisidir; kendi üretim şablonlarınıza göre
özelleştirin.
