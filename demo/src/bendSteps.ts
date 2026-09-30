export type SheetFormat = 'a3' | 'a4-land' | 'a4-port' | 'a2'

export interface BendPoint {
  id: string
  name: string
  /** Açınım üzerindeki konum (mm) */
  xMm: number
  yMm: number
  /** Büküm çizgisi uzunluğu (mm) */
  lengthMm: number
  /** Büküm açısı (iç açı / tamamlayıcı — üretim açısı) */
  angleDeg: number
  /** İç yarıçap (mm) */
  radiusMm: number
  /** Flanş boyu (mm) */
  flangeMm: number
  /** UP / DOWN */
  direction: 'UP' | 'DOWN'
  /** Büküm payı / allowance (mm) */
  allowanceMm: number
  /** Sıra */
  order: number
  note: string
}

export interface SheetMetalPart {
  id: string
  name: string
  material: string
  thicknessMm: number
  description: string
  /** Açınım dış ölçüleri mm */
  flat: { widthMm: number; heightMm: number }
  holes: { id: string; xMm: number; yMm: number; diameterMm: number; spec: string }[]
  bends: BendPoint[]
}

export interface PaperSize {
  id: SheetFormat
  label: string
  widthMm: number
  heightMm: number
}

export const PAPER_SIZES: PaperSize[] = [
  { id: 'a3', label: 'A3 Yatay', widthMm: 420, heightMm: 297 },
  { id: 'a4-land', label: 'A4 Yatay', widthMm: 297, heightMm: 210 },
  { id: 'a4-port', label: 'A4 Dikey', widthMm: 210, heightMm: 297 },
  { id: 'a2', label: 'A2 Yatay', widthMm: 594, heightMm: 420 },
]

/** Kural tabanlı büküm payı (yaklaşık): BA ≈ angle/180 * π * (R + k*T) */
export function bendAllowance(angleDeg: number, radiusMm: number, thicknessMm: number): number {
  const k = 0.446 // nötr eksen katsayısı (yumuşak çelik yaklaşık)
  return (angleDeg / 180) * Math.PI * (radiusMm + k * thicknessMm)
}

export function autoScale(
  geometryMaxMm: number,
  paper: PaperSize,
  marginMm = 40,
): { scale: number; label: string } {
  const usable = Math.min(paper.widthMm, paper.heightMm) - marginMm * 2
  if (geometryMaxMm <= 0) return { scale: 1, label: '1:1' }

  const candidates = [2, 1, 0.5, 0.2, 0.1] // çizim/gerçek
  for (const s of candidates) {
    if (geometryMaxMm * s <= usable) {
      if (s >= 1) return { scale: s, label: `${s}:1` }
      const inv = Math.round(1 / s)
      return { scale: s, label: `1:${inv}` }
    }
  }
  return { scale: usable / geometryMaxMm, label: 'OTOMATİK' }
}

export const SAMPLE_SHEET_PARTS: SheetMetalPart[] = [
  {
    id: 'bracket',
    name: 'L-Bracket-SM-02',
    material: 'DX51D+Z275',
    thicknessMm: 2,
    description: 'İki bükümlü L bracket — adım adım büküm sırası ile üretilir.',
    flat: { widthMm: 160, heightMm: 80 },
    holes: [
      { id: 'H1', xMm: 20, yMm: 20, diameterMm: 6.5, spec: 'Ø6.5' },
      { id: 'H2', xMm: 20, yMm: 60, diameterMm: 6.5, spec: 'Ø6.5' },
      { id: 'H3', xMm: 140, yMm: 25, diameterMm: 8, spec: 'M8' },
      { id: 'H4', xMm: 140, yMm: 55, diameterMm: 8, spec: 'M8' },
    ],
    bends: [
      {
        id: 'BL-01',
        name: 'Edge-Flange1',
        xMm: 80,
        yMm: 0,
        lengthMm: 80,
        angleDeg: 90,
        radiusMm: 2,
        flangeMm: 40,
        direction: 'UP',
        allowanceMm: bendAllowance(90, 2, 2),
        order: 1,
        note: 'İlk büküm: kısa flanş. Mengene / abkantta işaretli çizgiden bükün.',
      },
      {
        id: 'BL-02',
        name: 'Edge-Flange2',
        xMm: 0,
        yMm: 40,
        lengthMm: 80,
        angleDeg: 90,
        radiusMm: 2,
        flangeMm: 35,
        direction: 'DOWN',
        allowanceMm: bendAllowance(90, 2, 2),
        order: 2,
        note: 'İkinci büküm: yan flanş. BL-01 sonrası parça yönünü koruyun.',
      },
    ],
  },
  {
    id: 'cover',
    name: 'Kapak-Panel-A4',
    material: 'AlMg3',
    thicknessMm: 1.5,
    description: 'Dört flanşlı kapak — her kenar ayrı büküm adımı.',
    flat: { widthMm: 300, heightMm: 200 },
    holes: Array.from({ length: 8 }, (_, i) => ({
      id: `H${i + 1}`,
      xMm: 30 + (i % 4) * 80,
      yMm: i < 4 ? 30 : 170,
      diameterMm: 4.2,
      spec: 'Ø4.2',
    })),
    bends: [
      {
        id: 'BL-01',
        name: 'Flange-N',
        xMm: 0,
        yMm: 25,
        lengthMm: 300,
        angleDeg: 90,
        radiusMm: 1.5,
        flangeMm: 25,
        direction: 'UP',
        allowanceMm: bendAllowance(90, 1.5, 1.5),
        order: 1,
        note: 'Üst flanş — karşı kenardan önce bükülür.',
      },
      {
        id: 'BL-02',
        name: 'Flange-S',
        xMm: 0,
        yMm: 175,
        lengthMm: 300,
        angleDeg: 90,
        radiusMm: 1.5,
        flangeMm: 25,
        direction: 'UP',
        allowanceMm: bendAllowance(90, 1.5, 1.5),
        order: 2,
        note: 'Alt flanş — BL-01 ile aynı yön.',
      },
      {
        id: 'BL-03',
        name: 'Flange-W',
        xMm: 25,
        yMm: 0,
        lengthMm: 200,
        angleDeg: 90,
        radiusMm: 1.5,
        flangeMm: 25,
        direction: 'UP',
        allowanceMm: bendAllowance(90, 1.5, 1.5),
        order: 3,
        note: 'Sol flanş — köşe çakışmasına dikkat.',
      },
      {
        id: 'BL-04',
        name: 'Flange-E',
        xMm: 275,
        yMm: 0,
        lengthMm: 200,
        angleDeg: 90,
        radiusMm: 1.5,
        flangeMm: 25,
        direction: 'UP',
        allowanceMm: bendAllowance(90, 1.5, 1.5),
        order: 4,
        note: 'Sağ flanş — son adım; kontrol ölçüsü alın.',
      },
    ],
  },
]

export type StepKind = 'overview' | 'bend' | 'final'

export interface DrawingStep {
  id: string
  index: number
  kind: StepKind
  title: string
  subtitle: string
  bend?: BendPoint
  /** Bu adıma kadar tamamlanan büküm id'leri */
  completedBendIds: string[]
  scaleLabel: string
  scaleFactor: number
  instructions: string[]
  callouts: { label: string; value: string }[]
}

export function buildBendSteps(part: SheetMetalPart, paper: PaperSize): DrawingStep[] {
  const maxDim = Math.max(part.flat.widthMm, part.flat.heightMm)
  const { scale, label } = autoScale(maxDim, paper)
  const ordered = [...part.bends].sort((a, b) => a.order - b.order)
  const steps: DrawingStep[] = []

  steps.push({
    id: 'overview',
    index: 0,
    kind: 'overview',
    title: `${part.name} — Açınım & Büküm Haritası`,
    subtitle: 'Tüm büküm noktaları ve otomatik ölçek',
    completedBendIds: [],
    scaleLabel: label,
    scaleFactor: scale,
    instructions: [
      `Otomatik ölçek: ${label} (${paper.label} sayfaya sığdırıldı)`,
      `Sac kalınlığı: ${part.thicknessMm} mm · Malzeme: ${part.material}`,
      `${ordered.length} büküm noktası işaretlendi — sırayla uygulayın`,
      'Kesikli çizgi = büküm hattı · Ok yönü = flanş hareketi',
    ],
    callouts: [
      { label: 'Açınım', value: `${part.flat.widthMm} × ${part.flat.heightMm} mm` },
      { label: 'Ölçek', value: label },
      { label: 'Büküm sayısı', value: String(ordered.length) },
      { label: 'Kalınlık', value: `${part.thicknessMm} mm` },
    ],
  })

  ordered.forEach((bend, i) => {
    const completed = ordered.slice(0, i).map((b) => b.id)
    const detailMax = Math.max(bend.flangeMm * 2 + bend.radiusMm * 4, bend.lengthMm, 80)
    const detailScale = autoScale(detailMax, paper, 50)

    steps.push({
      id: bend.id,
      index: i + 1,
      kind: 'bend',
      title: `Adım ${i + 1}/${ordered.length} — ${bend.id}`,
      subtitle: `${bend.name} · ${bend.angleDeg}° · R${bend.radiusMm} · ${bend.direction}`,
      bend,
      completedBendIds: completed,
      scaleLabel: detailScale.label,
      scaleFactor: detailScale.scale,
      instructions: [
        `Büküm noktası: ${bend.id} (${bend.xMm.toFixed(0)}, ${bend.yMm.toFixed(0)}) mm açınımda`,
        `Ne kadar bükülecek: ${bend.angleDeg}° (${bend.direction === 'UP' ? 'yukarı' : 'aşağı'})`,
        `İç yarıçap: R${bend.radiusMm} mm · Flanş boyu: ${bend.flangeMm} mm`,
        `Büküm payı (BA): ${bend.allowanceMm.toFixed(2)} mm`,
        bend.note,
      ],
      callouts: [
        { label: 'Açı', value: `${bend.angleDeg}°` },
        { label: 'Yarıçap', value: `R${bend.radiusMm}` },
        { label: 'Flanş', value: `${bend.flangeMm} mm` },
        { label: 'Yön', value: bend.direction },
        { label: 'BA', value: `${bend.allowanceMm.toFixed(2)} mm` },
        { label: 'Ölçek', value: detailScale.label },
      ],
    })
  })

  steps.push({
    id: 'final',
    index: ordered.length + 1,
    kind: 'final',
    title: `${part.name} — Nihai Bükülmüş Form`,
    subtitle: 'Tüm adımlar tamam — kontrol ölçüleri',
    completedBendIds: ordered.map((b) => b.id),
    scaleLabel: label,
    scaleFactor: scale,
    instructions: [
      'Tüm bükümler uygulandı — açı ve flanş ölçülerini kontrol edin',
      'Keskin kenarlar 0.2–0.5 mm kırılacak',
      ordered.map((b) => `${b.id}: ${b.angleDeg}° / R${b.radiusMm} / ${b.direction}`).join(' · '),
    ],
    callouts: [
      { label: 'Durum', value: 'TAMAM' },
      { label: 'Adım', value: `${ordered.length}/${ordered.length}` },
      { label: 'Ölçek', value: label },
    ],
  })

  return steps
}
