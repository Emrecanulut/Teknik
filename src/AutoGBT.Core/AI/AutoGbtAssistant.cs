using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using AutoGBT.Core.Models;

namespace AutoGBT.Core.AI
{
    /// <summary>
    /// AutoGBT — model özetinden detaylı teknik resim planı üreten akıllı asistan.
    /// İsteğe bağlı olarak OpenAI uyumlu bir uç noktaya bağlanabilir; yoksa kural tabanlı
    /// üretim motoruyla çalışır (çevrimdışı / anahtar gerektirmez).
    /// </summary>
    public sealed class AutoGbtAssistant
    {
        private readonly AutoGbtOptions _options;

        public AutoGbtAssistant(AutoGbtOptions? options = null)
        {
            _options = options ?? AutoGbtOptions.Default;
        }

        public DrawingPlan PlanDrawing(ModelSummary model, DrawingRequest request)
        {
            var plan = request.Kind switch
            {
                DrawingKind.Bend => PlanBendDrawing(model, request),
                DrawingKind.Cut => PlanCutDrawing(model, request),
                DrawingKind.Weld => PlanWeldDrawing(model, request),
                DrawingKind.Machining => PlanMachiningDrawing(model, request),
                _ => PlanMachiningDrawing(model, request)
            };

            plan.Confidence = ComputeConfidence(model, request);
            plan.QualityChecks = BuildQualityChecks(model, request, plan);
            EnrichWithUserInstructions(plan, request);
            return plan;
        }

        public string Explain(ModelSummary model, DrawingPlan plan)
        {
            var sb = new StringBuilder();
            sb.AppendLine("AutoGBT Analiz Raporu");
            sb.AppendLine("=====================");
            sb.AppendLine($"Parça: {model.PartName}");
            sb.AppendLine($"Resim türü: {ToTurkish(plan.Kind)}");
            sb.AppendLine($"Önerilen ölçek: {plan.RecommendedScale}");
            sb.AppendLine($"Güven skoru: %{plan.Confidence * 100:0}");
            sb.AppendLine();
            sb.AppendLine(plan.Summary);
            sb.AppendLine();
            sb.AppendLine("Görünüşler:");
            foreach (var view in plan.Views)
                sb.AppendLine($"  • {view.Name}: {view.Description}");
            sb.AppendLine();
            sb.AppendLine("Açıklamalar / tablolar:");
            foreach (var ann in plan.Annotations.OrderByDescending(a => a.Priority))
                sb.AppendLine($"  • [{ann.Kind}] {ann.Text}");
            return sb.ToString();
        }

        public IReadOnlyList<DrawingKind> RecommendKinds(ModelSummary model)
        {
            var list = new List<DrawingKind>();
            if (model.IsSheetMetal)
            {
                list.Add(DrawingKind.Cut);
                if (model.BendCount > 0) list.Add(DrawingKind.Bend);
            }
            if (model.IsWeldment)
            {
                if (!list.Contains(DrawingKind.Cut)) list.Add(DrawingKind.Cut);
                list.Add(DrawingKind.Weld);
            }
            if (!model.IsSheetMetal && !model.IsWeldment)
                list.Add(DrawingKind.Machining);
            else if (model.HoleCount > 0 && !model.IsSheetMetal)
                list.Add(DrawingKind.Machining);
            return list;
        }

        private DrawingPlan PlanBendDrawing(ModelSummary model, DrawingRequest request)
        {
            var plan = new DrawingPlan
            {
                Kind = DrawingKind.Bend,
                Title = string.IsNullOrWhiteSpace(request.Title)
                    ? $"{model.PartName} — Büküm Teknik Resmi"
                    : request.Title,
                SheetFormat = request.SheetFormat,
                RecommendedScale = RecommendScale(model, preferLarge: true),
                Summary = BuildBendSummary(model)
            };

            plan.Views.Add(new ViewPlan
            {
                Name = "Bükülü İzometrik",
                Orientation = "Isometric",
                IsIsometric = true,
                RelativeX = 0.12,
                RelativeY = 0.55,
                RelativeWidth = 0.32,
                RelativeHeight = 0.32,
                Description = "Bükülmüş nihai form; büküm yönleri oklarla işaretlenir."
            });

            plan.Views.Add(new ViewPlan
            {
                Name = "Ön Görünüş",
                Orientation = "Front",
                RelativeX = 0.48,
                RelativeY = 0.58,
                RelativeWidth = 0.28,
                RelativeHeight = 0.28,
                Description = "Ana büküm açılarının ölçüldüğü ön görünüş."
            });

            plan.Views.Add(new ViewPlan
            {
                Name = "Yan Görünüş",
                Orientation = "Right",
                RelativeX = 0.78,
                RelativeY = 0.58,
                RelativeWidth = 0.18,
                RelativeHeight = 0.28,
                Description = "Flanş yükseklikleri ve büküm yarıçapları."
            });

            if (model.BendCount > 0)
            {
                plan.Views.Add(new ViewPlan
                {
                    Name = "Büküm Detayı",
                    Orientation = "Front",
                    IsDetail = true,
                    RelativeX = 0.12,
                    RelativeY = 0.18,
                    RelativeWidth = 0.22,
                    RelativeHeight = 0.22,
                    Description = "İç yarıçap, nötr eksen ve büküm payı detayı."
                });
            }

            if (request.IncludeBendTable || request.ShowBendNotes)
            {
                plan.Annotations.Add(new AnnotationPlan
                {
                    Kind = "BendTable",
                    Text = BuildBendTableText(model),
                    TargetView = "Ön Görünüş",
                    Priority = 100
                });
            }

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = $"Malzeme: {model.Material} | Sac kalınlığı: {model.ThicknessMm:0.##} mm",
                Priority = 90
            });

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = "Büküm sırası üretim notlarına göre uygulanır. Keskin kenarlar kırılacaktır (0.2–0.5 mm).",
                Priority = 70
            });

            foreach (var bend in model.Bends.Take(8))
            {
                plan.Annotations.Add(new AnnotationPlan
                {
                    Kind = "Dimension",
                    Text = $"{bend.BendLineId}: {bend.AngleDeg:0.#}° / R{bend.RadiusMm:0.##}",
                    TargetView = "Ön Görünüş",
                    Priority = 80
                });
            }

            FillTitleBlock(plan, model, request, "BÜKÜM");
            return plan;
        }

        private DrawingPlan PlanCutDrawing(ModelSummary model, DrawingRequest request)
        {
            var plan = new DrawingPlan
            {
                Kind = DrawingKind.Cut,
                Title = string.IsNullOrWhiteSpace(request.Title)
                    ? $"{model.PartName} — Kesim / Açınım Resmi"
                    : request.Title,
                SheetFormat = request.SheetFormat,
                RecommendedScale = RecommendScale(model, preferLarge: false),
                Summary = BuildCutSummary(model)
            };

            plan.Views.Add(new ViewPlan
            {
                Name = "Açınım (Flat Pattern)",
                Orientation = "FlatPattern",
                IsFlatPattern = true,
                RelativeX = 0.18,
                RelativeY = 0.35,
                RelativeWidth = 0.50,
                RelativeHeight = 0.48,
                Description = "Lazer/plazma/punch kesim için net açınım; büküm çizgileri kesikli gösterilir."
            });

            plan.Views.Add(new ViewPlan
            {
                Name = "İzometrik Referans",
                Orientation = "Isometric",
                IsIsometric = true,
                RelativeX = 0.72,
                RelativeY = 0.58,
                RelativeWidth = 0.22,
                RelativeHeight = 0.28,
                Description = "Bükülmüş parçanın referans izometrisi."
            });

            if (model.HoleCount > 0)
            {
                plan.Views.Add(new ViewPlan
                {
                    Name = "Delik Detayı",
                    Orientation = "FlatPattern",
                    IsDetail = true,
                    RelativeX = 0.72,
                    RelativeY = 0.18,
                    RelativeWidth = 0.22,
                    RelativeHeight = 0.22,
                    Description = "Kritik delik diametreleri ve konum toleransları."
                });
            }

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = "KESİM: Dış kontur + iç boşluklar. Büküm çizgileri KESİLMEZ — işaretlenir.",
                TargetView = "Açınım (Flat Pattern)",
                Priority = 100
            });

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = $"Açınım dış ölçüleri ve net alan: {model.BoundingBox.X:0.#} × {model.BoundingBox.Y:0.#} mm (yaklaşık).",
                Priority = 85
            });

            if (request.IncludeHoleTable && model.HoleCount > 0)
            {
                plan.Annotations.Add(new AnnotationPlan
                {
                    Kind = "HoleTable",
                    Text = BuildHoleTableText(model),
                    TargetView = "Açınım (Flat Pattern)",
                    Priority = 95
                });
            }

            if (request.ShowBendNotes && model.BendCount > 0)
            {
                plan.Annotations.Add(new AnnotationPlan
                {
                    Kind = "Note",
                    Text = "Büküm çizgileri: UP = sürekli ince, DOWN = kesikli. Etiketler BL-xx ile eşleşir.",
                    Priority = 75
                });
            }

            FillTitleBlock(plan, model, request, "KESİM");
            return plan;
        }

        private DrawingPlan PlanWeldDrawing(ModelSummary model, DrawingRequest request)
        {
            var plan = new DrawingPlan
            {
                Kind = DrawingKind.Weld,
                Title = string.IsNullOrWhiteSpace(request.Title)
                    ? $"{model.PartName} — Kaynak Noktaları Resmi"
                    : request.Title,
                SheetFormat = request.SheetFormat,
                RecommendedScale = RecommendScale(model, preferLarge: true),
                Summary = "AutoGBT kaynak dikişleri / noktaları için görünüş ve kaynak tablosu planladı."
            };

            plan.Views.Add(new ViewPlan
            {
                Name = "İzometrik Kaynak",
                Orientation = "Isometric",
                IsIsometric = true,
                RelativeX = 0.12,
                RelativeY = 0.35,
                RelativeWidth = 0.42,
                RelativeHeight = 0.45,
                Description = "Kaynak dikişleri balonlarla işaretlenir."
            });

            plan.Views.Add(new ViewPlan
            {
                Name = "Ön Görünüş",
                Orientation = "Front",
                RelativeX = 0.58,
                RelativeY = 0.42,
                RelativeWidth = 0.30,
                RelativeHeight = 0.36,
                Description = "Ana birleşim hatları ve kaynak sembolleri."
            });

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "WeldTable",
                Text = "Kaynak Tablosu: No | Birleşim | Proses | Boy | a (mm)",
                Priority = 100
            });

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = "Kaynak: MAG (aksi belirtilmedikçe). Sıçrantı temizlenecek, çarpılma kontrol edilecek.",
                Priority = 90
            });

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = $"Malzeme: {model.Material}. Kaynak sonrası çapak alma uygulanır.",
                Priority = 80
            });

            FillTitleBlock(plan, model, request, "KAYNAK");
            return plan;
        }

        private DrawingPlan PlanMachiningDrawing(ModelSummary model, DrawingRequest request)
        {
            var plan = new DrawingPlan
            {
                Kind = DrawingKind.Machining,
                Title = string.IsNullOrWhiteSpace(request.Title)
                    ? $"{model.PartName} — İşleme Teknik Resmi"
                    : request.Title,
                SheetFormat = request.SheetFormat,
                RecommendedScale = RecommendScale(model, preferLarge: true),
                Summary = BuildMachiningSummary(model)
            };

            // Profesyonel A3 düzen (kullanıcı örneği): ön sol-üst, üst orta,
            // kesit A-A sol-alt, gölgeli izometrik sağ-üst.
            plan.Views.Add(new ViewPlan
            {
                Name = "Ön Görünüş",
                Orientation = "Front",
                RelativeX = 0.10,
                RelativeY = 0.55,
                RelativeWidth = 0.28,
                RelativeHeight = 0.32,
                Description = "Ana siluet + A-A kesit çizgisi."
            });

            plan.Views.Add(new ViewPlan
            {
                Name = "Üst Görünüş",
                Orientation = "Top",
                RelativeX = 0.40,
                RelativeY = 0.52,
                RelativeWidth = 0.28,
                RelativeHeight = 0.34,
                Description = "Delik / kama / flanş yerleşimi."
            });

            plan.Views.Add(new ViewPlan
            {
                Name = "Kesit A-A",
                Orientation = "Section",
                IsSection = true,
                RelativeX = 0.10,
                RelativeY = 0.14,
                RelativeWidth = 0.32,
                RelativeHeight = 0.34,
                Description = "İç çap basamakları, et kalınlığı, taralı kesit."
            });

            plan.Views.Add(new ViewPlan
            {
                Name = "İzometrik",
                Orientation = "Isometric",
                IsIsometric = true,
                RelativeX = 0.70,
                RelativeY = 0.55,
                RelativeWidth = 0.24,
                RelativeHeight = 0.32,
                Description = "Gölgeli 3B referans (shaded with edges)."
            });

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = "Ölçüler milimetredir. Genel tolerans: ISO 2768-mK. Başlık bloğu + A3 çerçeve şablonu kullanın.",
                Priority = 92
            });

            if (request.IncludeHoleTable && model.HoleCount > 0)
            {
                plan.Annotations.Add(new AnnotationPlan
                {
                    Kind = "HoleTable",
                    Text = BuildHoleTableText(model),
                    TargetView = "Üst Görünüş",
                    Priority = 95
                });
            }

            if (request.ShowSurfaceFinish)
            {
                plan.Annotations.Add(new AnnotationPlan
                {
                    Kind = "Finish",
                    Text = "İşlenmiş yüzeyler: Ra 3.2 µm (genel), sızdırmazlık yüzeyleri Ra 1.6 µm.",
                    Priority = 88
                });
            }

            if (request.ShowToleranceBlock)
            {
                plan.Annotations.Add(new AnnotationPlan
                {
                    Kind = "GD&T",
                    Text = "Genel tolerans: ISO 2768-mK. Aksi belirtilmedikçe köşe R0.2.",
                    Priority = 90
                });
            }

            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = $"Malzeme: {model.Material}. Çapaklar alınacak, keskin kenarlar kırılacak.",
                Priority = 80
            });

            FillTitleBlock(plan, model, request, "İŞLEME");
            return plan;
        }

        private static void FillTitleBlock(DrawingPlan plan, ModelSummary model, DrawingRequest request, string kindLabel)
        {
            plan.TitleBlockFields.Add($"Parça: {model.PartName}");
            plan.TitleBlockFields.Add($"Tür: {kindLabel}");
            plan.TitleBlockFields.Add($"Malzeme: {model.Material}");
            plan.TitleBlockFields.Add($"Ölçek: {plan.RecommendedScale}");
            plan.TitleBlockFields.Add($"Çizen: {request.DrawnBy}");
            plan.TitleBlockFields.Add($"Tarih: {DateTime.Now:dd.MM.yyyy}");
            if (model.ThicknessMm > 0)
                plan.TitleBlockFields.Add($"Kalınlık: {model.ThicknessMm:0.##} mm");
        }

        private static void EnrichWithUserInstructions(DrawingPlan plan, DrawingRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ExtraInstructions)) return;
            plan.Annotations.Add(new AnnotationPlan
            {
                Kind = "Note",
                Text = "Kullanıcı notu: " + request.ExtraInstructions.Trim(),
                Priority = 99
            });
            plan.Summary += " AutoGBT kullanıcı talimatlarını plana işledi.";
        }

        private static string BuildBendSummary(ModelSummary model)
        {
            return string.Format(
                CultureInfo.GetCultureInfo("tr-TR"),
                "AutoGBT {0} büküm hattı tespit etti. Büküm resmi; açı, yarıçap, flanş yüksekliği ve büküm sırasını üretim için netleştirir.",
                model.BendCount);
        }

        private static string BuildCutSummary(ModelSummary model)
        {
            return string.Format(
                CultureInfo.GetCultureInfo("tr-TR"),
                "AutoGBT açınım (flat pattern) odaklı kesim resmi planladı. {0} delik/kesim ve {1} büküm çizgisi işaretlenecek.",
                model.HoleCount, model.BendCount);
        }

        private static string BuildMachiningSummary(ModelSummary model)
        {
            return string.Format(
                CultureInfo.GetCultureInfo("tr-TR"),
                "AutoGBT freze/torna işleme resmi planladı. {0} özellik ve {1} delik için çoklu görünüş + tolerans bloğu önerildi.",
                model.FeatureCount, model.HoleCount);
        }

        private static string BuildBendTableText(ModelSummary model)
        {
            if (model.Bends.Count == 0)
                return "Büküm tablosu: kayıt yok (manuel kontrol önerilir).";

            var lines = new List<string> { "Büküm Tablosu: No | Açı | Yarıçap" };
            foreach (var b in model.Bends)
                lines.Add($"{b.BendLineId} | {b.AngleDeg:0.#}° | R{b.RadiusMm:0.##}");
            return string.Join(" || ", lines);
        }

        private static string BuildHoleTableText(ModelSummary model)
        {
            if (model.Holes.Count == 0)
                return "Delik tablosu: kayıt yok.";

            var lines = new List<string> { "Delik Tablosu: Ad | Ø / Diş" };
            int i = 1;
            foreach (var h in model.Holes.Take(20))
            {
                var spec = h.IsThreaded && !string.IsNullOrEmpty(h.ThreadSpec)
                    ? h.ThreadSpec
                    : (h.DiameterMm > 0 ? $"Ø{h.DiameterMm:0.##}" : h.Name);
                lines.Add($"{i:00} | {spec}");
                i++;
            }
            return string.Join(" || ", lines);
        }

        private static string RecommendScale(ModelSummary model, bool preferLarge)
        {
            var max = Math.Max(model.BoundingBox.X, Math.Max(model.BoundingBox.Y, model.BoundingBox.Z));
            if (max <= 0) return preferLarge ? "1:1" : "1:2";
            if (max < 80) return "2:1";
            if (max < 200) return "1:1";
            if (max < 500) return "1:2";
            if (max < 1000) return "1:5";
            return "1:10";
        }

        private static double ComputeConfidence(ModelSummary model, DrawingRequest request)
        {
            double score = 0.55;
            if (!string.IsNullOrWhiteSpace(model.Material) && model.Material != "Belirtilmemiş") score += 0.1;
            if (model.BoundingBox.X > 0) score += 0.08;
            if (request.Kind == DrawingKind.Cut || request.Kind == DrawingKind.Bend)
                score += model.IsSheetMetal ? 0.15 : -0.1;
            if (request.Kind == DrawingKind.Machining) score += 0.1;
            if (model.BendCount > 0 && request.Kind == DrawingKind.Bend) score += 0.08;
            if (model.HoleCount > 0) score += 0.05;
            return Math.Max(0.35, Math.Min(0.97, score));
        }

        private static List<string> BuildQualityChecks(ModelSummary model, DrawingRequest request, DrawingPlan plan)
        {
            var checks = new List<string>
            {
                "Başlık bloğu alanları dolduruldu",
                $"Ölçek seçildi: {plan.RecommendedScale}",
                $"{plan.Views.Count} görünüş yerleştirilecek"
            };

            if (request.Kind == DrawingKind.Cut && !model.IsSheetMetal)
                checks.Add("Uyarı: Parça sac metal olarak işaretli değil — açınım doğrulanmalı");
            if (request.Kind == DrawingKind.Bend && model.BendCount == 0)
                checks.Add("Uyarı: Büküm özelliği bulunamadı — açıları manuel kontrol edin");
            if (request.AutoDimension)
                checks.Add("Otomatik ölçü yerleştirme açık");
            if (request.ShowToleranceBlock)
                checks.Add("Tolerans / genel not bloğu eklenecek");

            return checks;
        }

        private static bool HasDeepFeatures(ModelSummary model)
        {
            return model.FeatureCount > 15 || model.BoundingBox.Z > 40;
        }

        public static string ToTurkish(DrawingKind kind) => kind switch
        {
            DrawingKind.Bend => "Büküm",
            DrawingKind.Cut => "Kesim",
            DrawingKind.Machining => "İşleme",
            DrawingKind.Weld => "Kaynak",
            _ => kind.ToString()
        };
    }

    public sealed class AutoGbtOptions
    {
        public string? ApiKey { get; set; }
        public string? ApiBaseUrl { get; set; }
        public string ModelName { get; set; } = "gpt-4.1-mini";
        public bool PreferCloudLlm { get; set; }

        public static AutoGbtOptions Default => new AutoGbtOptions();

        public static AutoGbtOptions FromEnvironment()
        {
            return new AutoGbtOptions
            {
                ApiKey = Environment.GetEnvironmentVariable("AUTOGBT_API_KEY"),
                ApiBaseUrl = Environment.GetEnvironmentVariable("AUTOGBT_API_BASE"),
                PreferCloudLlm = !string.IsNullOrWhiteSpace(
                    Environment.GetEnvironmentVariable("AUTOGBT_API_KEY"))
            };
        }
    }
}
