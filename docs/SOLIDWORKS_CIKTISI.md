# Gerçek SolidWorks teknik resim çıktısı

Web stüdyo **önizleme / sınıflandırma** içindir.
**Gerçek `.SLDDRW`** yalnızca Windows + SolidWorks eklentisiyle üretilir.

## Akış

1. SolidWorks’te montaj veya parçayı **kaydedin**
2. AutoGBT eklentisini açın
3. **Montajdaki TÜM parçalar** (veya komut: Montaj Tüm Resimler)
4. AutoGBT her parçayı ayırt eder:
   - **Kesim** → `AutoGBT_Drawings/Kesim/`
   - **Büküm** → `AutoGBT_Drawings/Buküm/`
   - **İşleme** → `AutoGBT_Drawings/Isleme/`
   - **Kaynak** → `AutoGBT_Drawings/Kaynak/`
5. Her dosya gerçek SolidWorks Drawing’dir: görünüşler + model ölçüleri + proses notları

## İşleme resmi içeriği (örnek stile göre)

- Ana profil görünüşü  
- Yan / uç görünüş  
- Kesit A-A  
- Detay C  
- Model Items ile ölçüler  
- Yüzey / tolerans / çapak notları  
- Başlık bilgisi  

Şablon yolu (opsiyonel): ortam değişkeni `AUTOGBT_DRAWING_TEMPLATE` = `.drwdot`
