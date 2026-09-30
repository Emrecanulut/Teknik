export type DrawingKind = 'bend' | 'cut' | 'machining'

export type SheetFormat = 'a3' | 'a4-land' | 'a4-port' | 'a2'

export interface BendInfo {
  id: string
  angle: number
  radius: number
  name: string
}

export interface HoleInfo {
  id: string
  diameter: number
  threaded: boolean
  spec: string
}

export interface SamplePart {
  id: string
  name: string
  material: string
  thicknessMm: number
  isSheetMetal: boolean
  bendCount: number
  holeCount: number
  featureCount: number
  box: { x: number; y: number; z: number }
  bends: BendInfo[]
  holes: HoleInfo[]
  description: string
}

export interface ViewPlan {
  name: string
  orientation: string
  x: number
  y: number
  w: number
  h: number
  flat?: boolean
  iso?: boolean
  section?: boolean
  detail?: boolean
  description: string
}

export interface AnnotationPlan {
  kind: string
  text: string
  priority: number
}

export interface DrawingPlan {
  kind: DrawingKind
  title: string
  summary: string
  scale: string
  confidence: number
  sheet: SheetFormat
  views: ViewPlan[]
  annotations: AnnotationPlan[]
  titleBlock: string[]
  checks: string[]
}

export const SAMPLE_PARTS: SamplePart[] = [
  {
    id: 'bracket',
    name: 'L-Bracket-SM-02',
    material: 'DX51D+Z275',
    thicknessMm: 2,
    isSheetMetal: true,
    bendCount: 2,
    holeCount: 4,
    featureCount: 11,
    box: { x: 120, y: 80, z: 40 },
    bends: [
      { id: 'BL-01', angle: 90, radius: 2, name: 'Edge-Flange1' },
      { id: 'BL-02', angle: 90, radius: 2, name: 'Edge-Flange2' },
    ],
    holes: [
      { id: '01', diameter: 6.5, threaded: false, spec: 'Ø6.5' },
      { id: '02', diameter: 6.5, threaded: false, spec: 'Ø6.5' },
      { id: '03', diameter: 8, threaded: true, spec: 'M8' },
      { id: '04', diameter: 8, threaded: true, spec: 'M8' },
    ],
    description: 'İki bükümlü sac bracket; montaj delikleri ve M8 bağlantı.',
  },
  {
    id: 'cover',
    name: 'Kapak-Panel-A4',
    material: 'AlMg3',
    thicknessMm: 1.5,
    isSheetMetal: true,
    bendCount: 4,
    holeCount: 8,
    featureCount: 18,
    box: { x: 280, y: 180, z: 25 },
    bends: [
      { id: 'BL-01', angle: 90, radius: 1.5, name: 'Flange-N' },
      { id: 'BL-02', angle: 90, radius: 1.5, name: 'Flange-S' },
      { id: 'BL-03', angle: 90, radius: 1.5, name: 'Flange-E' },
      { id: 'BL-04', angle: 90, radius: 1.5, name: 'Flange-W' },
    ],
    holes: Array.from({ length: 8 }, (_, i) => ({
      id: String(i + 1).padStart(2, '0'),
      diameter: 4.2,
      threaded: false,
      spec: 'Ø4.2',
    })),
    description: 'Dört kenarı flanşlı kapak paneli; lazer kesim + büküm.',
  },
  {
    id: 'block',
    name: 'Adapter-Block-CNC',
    material: 'Al7075-T6',
    thicknessMm: 0,
    isSheetMetal: false,
    bendCount: 0,
    holeCount: 6,
    featureCount: 24,
    box: { x: 90, y: 60, z: 35 },
    bends: [],
    holes: [
      { id: '01', diameter: 10, threaded: false, spec: 'Ø10H7' },
      { id: '02', diameter: 10, threaded: false, spec: 'Ø10H7' },
      { id: '03', diameter: 6, threaded: true, spec: 'M6' },
      { id: '04', diameter: 6, threaded: true, spec: 'M6' },
      { id: '05', diameter: 5, threaded: true, spec: 'M5' },
      { id: '06', diameter: 5, threaded: true, spec: 'M5' },
    ],
    description: 'CNC freze adapter bloğu; kademeli delikler ve cepler.',
  },
]

export function toTurkish(kind: DrawingKind): string {
  switch (kind) {
    case 'bend':
      return 'Büküm'
    case 'cut':
      return 'Kesim'
    case 'machining':
      return 'İşleme'
  }
}

function recommendScale(part: SamplePart, preferLarge: boolean): string {
  const max = Math.max(part.box.x, part.box.y, part.box.z)
  if (max < 80) return '2:1'
  if (max < 200) return preferLarge ? '1:1' : '1:2'
  if (max < 500) return '1:2'
  if (max < 1000) return '1:5'
  return '1:10'
}

function confidence(part: SamplePart, kind: DrawingKind): number {
  let score = 0.55
  if (part.material) score += 0.1
  if (part.box.x > 0) score += 0.08
  if (kind === 'cut' || kind === 'bend') score += part.isSheetMetal ? 0.15 : -0.1
  if (kind === 'machining') score += 0.1
  if (kind === 'bend' && part.bendCount > 0) score += 0.08
  if (part.holeCount > 0) score += 0.05
  return Math.max(0.35, Math.min(0.97, score))
}

export function recommendKinds(part: SamplePart): DrawingKind[] {
  const kinds: DrawingKind[] = []
  if (part.isSheetMetal) {
    kinds.push('cut')
    if (part.bendCount > 0) kinds.push('bend')
  }
  kinds.push('machining')
  return kinds
}

export function planDrawing(
  part: SamplePart,
  kind: DrawingKind,
  sheet: SheetFormat = 'a3',
  extra = '',
): DrawingPlan {
  const baseTitle =
    kind === 'bend'
      ? `${part.name} — Büküm Teknik Resmi`
      : kind === 'cut'
        ? `${part.name} — Kesim / Açınım Resmi`
        : `${part.name} — İşleme Teknik Resmi`

  let plan: DrawingPlan

  if (kind === 'bend') {
    plan = {
      kind,
      title: baseTitle,
      summary: `AutoGBT ${part.bendCount} büküm hattı tespit etti. Açı, yarıçap, flanş yüksekliği ve büküm sırası üretim için netleştirilir.`,
      scale: recommendScale(part, true),
      confidence: confidence(part, kind),
      sheet,
      views: [
        {
          name: 'Bükülü İzometrik',
          orientation: 'Isometric',
          x: 0.12,
          y: 0.52,
          w: 0.32,
          h: 0.34,
          iso: true,
          description: 'Bükülmüş nihai form; büküm yönleri oklarla.',
        },
        {
          name: 'Ön Görünüş',
          orientation: 'Front',
          x: 0.48,
          y: 0.52,
          w: 0.28,
          h: 0.34,
          description: 'Ana büküm açılarının ölçüldüğü görünüş.',
        },
        {
          name: 'Yan Görünüş',
          orientation: 'Right',
          x: 0.78,
          y: 0.52,
          w: 0.16,
          h: 0.34,
          description: 'Flanş yükseklikleri ve büküm yarıçapları.',
        },
        {
          name: 'Büküm Detayı',
          orientation: 'Detail',
          x: 0.12,
          y: 0.14,
          w: 0.22,
          h: 0.24,
          detail: true,
          description: 'İç yarıçap, nötr eksen ve büküm payı.',
        },
      ],
      annotations: [
        {
          kind: 'BendTable',
          text: part.bends.map((b) => `${b.id}: ${b.angle}° / R${b.radius}`).join(' · ') || 'Büküm kaydı yok',
          priority: 100,
        },
        {
          kind: 'Note',
          text: `Malzeme: ${part.material} | Sac kalınlığı: ${part.thicknessMm} mm`,
          priority: 90,
        },
        {
          kind: 'Note',
          text: 'Keskin kenarlar kırılacaktır (0.2–0.5 mm). Büküm sırası üretim notuna uygundur.',
          priority: 70,
        },
      ],
      titleBlock: [],
      checks: [],
    }
  } else if (kind === 'cut') {
    plan = {
      kind,
      title: baseTitle,
      summary: `AutoGBT açınım odaklı kesim resmi planladı. ${part.holeCount} delik/kesim ve ${part.bendCount} büküm çizgisi işaretlenecek.`,
      scale: recommendScale(part, false),
      confidence: confidence(part, kind),
      sheet,
      views: [
        {
          name: 'Açınım (Flat Pattern)',
          orientation: 'FlatPattern',
          x: 0.1,
          y: 0.28,
          w: 0.52,
          h: 0.52,
          flat: true,
          description: 'Lazer/plazma/punch için net açınım.',
        },
        {
          name: 'İzometrik Referans',
          orientation: 'Isometric',
          x: 0.68,
          y: 0.55,
          w: 0.24,
          h: 0.28,
          iso: true,
          description: 'Bükülmüş parçanın referansı.',
        },
        {
          name: 'Delik Detayı',
          orientation: 'Detail',
          x: 0.68,
          y: 0.16,
          w: 0.24,
          h: 0.24,
          detail: true,
          description: 'Kritik delik diametreleri.',
        },
      ],
      annotations: [
        {
          kind: 'Note',
          text: 'KESİM: Dış kontur + iç boşluklar. Büküm çizgileri KESİLMEZ — işaretlenir.',
          priority: 100,
        },
        {
          kind: 'HoleTable',
          text: part.holes
            .slice(0, 8)
            .map((h) => `${h.id}:${h.spec}`)
            .join(' · '),
          priority: 95,
        },
        {
          kind: 'Note',
          text: `Yaklaşık açınım: ${part.box.x} × ${part.box.y} mm`,
          priority: 85,
        },
      ],
      titleBlock: [],
      checks: [],
    }
  } else {
    plan = {
      kind,
      title: baseTitle,
      summary: `AutoGBT freze/torna işleme resmi planladı. ${part.featureCount} özellik ve ${part.holeCount} delik için çoklu görünüş + tolerans bloğu.`,
      scale: recommendScale(part, true),
      confidence: confidence(part, kind),
      sheet,
      views: [
        {
          name: 'Ön Görünüş',
          orientation: 'Front',
          x: 0.1,
          y: 0.38,
          w: 0.3,
          h: 0.4,
          description: 'Ana işleme yüzeyleri ve dış ölçüler.',
        },
        {
          name: 'Üst Görünüş',
          orientation: 'Top',
          x: 0.1,
          y: 0.1,
          w: 0.3,
          h: 0.24,
          description: 'Delik yerleşimleri ve cepler.',
        },
        {
          name: 'Yan Görünüş',
          orientation: 'Right',
          x: 0.44,
          y: 0.38,
          w: 0.24,
          h: 0.4,
          description: 'Derinlikler ve freze profilleri.',
        },
        {
          name: 'İzometrik',
          orientation: 'Isometric',
          x: 0.72,
          y: 0.52,
          w: 0.2,
          h: 0.3,
          iso: true,
          description: '3B referans.',
        },
        {
          name: 'Kesit A-A',
          orientation: 'Section',
          x: 0.72,
          y: 0.14,
          w: 0.2,
          h: 0.28,
          section: true,
          description: 'İç boşluk / kademeli delik kesiti.',
        },
      ],
      annotations: [
        {
          kind: 'HoleTable',
          text: part.holes.map((h) => `${h.id}:${h.spec}`).join(' · '),
          priority: 95,
        },
        {
          kind: 'Finish',
          text: 'İşlenmiş yüzeyler: Ra 3.2 µm (genel), sızdırmazlık Ra 1.6 µm.',
          priority: 88,
        },
        {
          kind: 'GD&T',
          text: 'Genel tolerans: ISO 2768-mK. Köşe R0.2.',
          priority: 90,
        },
        {
          kind: 'Note',
          text: `Malzeme: ${part.material}. Çapaklar alınacak.`,
          priority: 80,
        },
      ],
      titleBlock: [],
      checks: [],
    }
  }

  plan.titleBlock = [
    `Parça: ${part.name}`,
    `Tür: ${toTurkish(kind).toUpperCase()}`,
    `Malzeme: ${part.material}`,
    `Ölçek: ${plan.scale}`,
    'Çizen: AutoGBT',
    `Tarih: ${new Date().toLocaleDateString('tr-TR')}`,
  ]
  if (part.thicknessMm > 0) plan.titleBlock.push(`Kalınlık: ${part.thicknessMm} mm`)

  if (extra.trim()) {
    plan.annotations.unshift({
      kind: 'Note',
      text: `Kullanıcı notu: ${extra.trim()}`,
      priority: 99,
    })
    plan.summary += ' AutoGBT kullanıcı talimatlarını plana işledi.'
  }

  plan.checks = [
    'Başlık bloğu alanları dolduruldu',
    `Ölçek seçildi: ${plan.scale}`,
    `${plan.views.length} görünüş yerleştirilecek`,
    'Otomatik ölçü yerleştirme açık',
  ]
  if (kind === 'cut' && !part.isSheetMetal) {
    plan.checks.push('Uyarı: Parça sac metal değil — açınım doğrulanmalı')
  }
  if (kind === 'bend' && part.bendCount === 0) {
    plan.checks.push('Uyarı: Büküm özelliği yok — açıları manuel kontrol edin')
  }

  return plan
}

export function explain(part: SamplePart, plan: DrawingPlan): string {
  const lines = [
    'AutoGBT Analiz Raporu',
    '=====================',
    `Parça: ${part.name}`,
    `Resim türü: ${toTurkish(plan.kind)}`,
    `Önerilen ölçek: ${plan.scale}`,
    `Güven skoru: %${Math.round(plan.confidence * 100)}`,
    '',
    plan.summary,
    '',
    'Görünüşler:',
    ...plan.views.map((v) => `  • ${v.name}: ${v.description}`),
    '',
    'Açıklamalar / tablolar:',
    ...[...plan.annotations]
      .sort((a, b) => b.priority - a.priority)
      .map((a) => `  • [${a.kind}] ${a.text}`),
  ]
  return lines.join('\n')
}
