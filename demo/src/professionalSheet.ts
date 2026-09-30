/**
 * Profesyonel A3 teknik resim şablonu — kullanıcı örneğine göre:
 * - Bölge çerçeve (1–8 / A–F)
 * - Ön + üst + kesit A-A + gölgeli izometrik
 * - Ölçü çizgileri / Ø / R
 * - Başlık bloğu
 */

export interface ProfDim {
  kind: 'linear' | 'diameter' | 'radius'
  value: string
  x1: number
  y1: number
  x2: number
  y2: number
  labelX: number
  labelY: number
}

export interface ProfView {
  id: string
  name: string
  kind: 'front' | 'top' | 'section' | 'iso'
  x: number
  y: number
  w: number
  h: number
}

export interface ProfessionalSheetModel {
  partName: string
  material: string
  scale: string
  sheet: 'A3' | 'A4' | 'A2'
  drawingNo: string
  weight?: string
  process: string
  notes: string[]
  dims: ProfDim[]
  /** SVG path fragments for orthographic silhouette (viewBox 0 0 100 100 local) */
  profile: 'hub-flange' | 'bracket' | 'plate' | 'block'
}

/** Kullanıcı örneğine yakın: flanşlı göbek / hub parçası */
export const HUB_FLANGE_EXAMPLE: ProfessionalSheetModel = {
  partName: 'Hub-Flange-Keyway',
  material: 'C45',
  scale: '1:2',
  sheet: 'A3',
  drawingNo: 'AGBT-001',
  weight: '1.2 kg',
  process: 'İşleme',
  notes: [
    'Ölçüler milimetredir',
    'Genel tolerans: ISO 2768-mK',
    'İşlenmemiş belirtilmemiş',
  ],
  profile: 'hub-flange',
  dims: [
    { kind: 'diameter', value: 'Ø70', x1: 0, y1: 0, x2: 0, y2: 0, labelX: 18, labelY: 42 },
    { kind: 'diameter', value: 'Ø50', x1: 0, y1: 0, x2: 0, y2: 0, labelX: 22, labelY: 28 },
    { kind: 'linear', value: '87', x1: 0, y1: 0, x2: 0, y2: 0, labelX: 8, labelY: 55 },
    { kind: 'linear', value: '81', x1: 0, y1: 0, x2: 0, y2: 0, labelX: 48, labelY: 92 },
    { kind: 'linear', value: '8', x1: 0, y1: 0, x2: 0, y2: 0, labelX: 52, labelY: 48 },
    { kind: 'radius', value: 'R15', x1: 0, y1: 0, x2: 0, y2: 0, labelX: 58, labelY: 38 },
    { kind: 'diameter', value: 'Ø10', x1: 0, y1: 0, x2: 0, y2: 0, labelX: 12, labelY: 78 },
  ],
}

export function sheetFromDetectedPart(input: {
  name: string
  material: string
  process: string
  role?: string
}): ProfessionalSheetModel {
  const role = input.role ?? ''
  const profile =
    /hub|flange|blok|adapter|cnc|housing/i.test(input.name) || role === 'machined'
      ? 'hub-flange'
      : /bracket|sac|panel|kapak/i.test(input.name) || role === 'sheet-metal'
        ? 'bracket'
        : role === 'weldment'
          ? 'plate'
          : 'block'

  return {
    partName: input.name,
    material: input.material,
    scale: profile === 'hub-flange' ? '1:2' : '1:1',
    sheet: 'A3',
    drawingNo: `AGBT-${Math.abs(hash(input.name) % 900 + 100)}`,
    weight: '—',
    process: input.process,
    notes: [
      'Ölçüler milimetredir',
      'Genel tolerans: ISO 2768-mK',
      'Keskin kenarlar kırılacaktır',
    ],
    profile,
    dims: HUB_FLANGE_EXAMPLE.dims,
  }
}

function hash(s: string): number {
  let h = 0
  for (let i = 0; i < s.length; i++) h = (h * 31 + s.charCodeAt(i)) | 0
  return h
}
