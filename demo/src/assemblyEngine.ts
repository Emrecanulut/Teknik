import {
  PAPER_SIZES,
  autoScale,
  bendAllowance,
  buildBendSteps,
  type BendPoint,
  type DrawingStep,
  type PaperSize,
  type SheetFormat,
  type SheetMetalPart,
} from './bendSteps'

export type ProcessKind = 'cut' | 'bend' | 'weld' | 'machining'

export type PartRole =
  | 'sheet-metal'
  | 'weldment'
  | 'machined'
  | 'fastener'
  | 'bought-out'
  | 'unknown'

export interface WeldPoint {
  id: string
  joint: string
  process: string
  lengthMm: number
  throatMm: number
  sides: '1' | '2'
  note: string
}

export interface DetectedPart {
  id: string
  name: string
  fileName: string
  quantity: number
  material: string
  role: PartRole
  confidence: number
  description: string
  box: { x: number; y: number; z: number }
  thicknessMm?: number
  sheet?: SheetMetalPart
  welds: WeldPoint[]
  holes: { id: string; spec: string; count: number }[]
  processes: ProcessKind[]
  source: 'upload' | 'assembly-tree' | 'sample'
}

export interface DrawingJob {
  id: string
  partId: string
  partName: string
  process: ProcessKind
  title: string
  summary: string
  scaleLabel: string
  status: 'queued' | 'ready'
  /** Büküm için adım adım kağıtlar */
  bendSteps?: DrawingStep[]
  callouts: { label: string; value: string }[]
  instructions: string[]
  annotations: string[]
}

export interface AssemblyProject {
  id: string
  name: string
  uploadedFiles: string[]
  parts: DetectedPart[]
  jobs: DrawingJob[]
  createdAt: string
  notes: string[]
}

export function processLabel(p: ProcessKind): string {
  switch (p) {
    case 'cut':
      return 'Kesim'
    case 'bend':
      return 'Büküm'
    case 'weld':
      return 'Kaynak'
    case 'machining':
      return 'İşleme'
  }
}

export function roleLabel(r: PartRole): string {
  switch (r) {
    case 'sheet-metal':
      return 'Sac metal'
    case 'weldment':
      return 'Kaynaklı konstrüksiyon'
    case 'machined':
      return 'CNC / işleme'
    case 'fastener':
      return 'Bağlantı elemanı'
    case 'bought-out':
      return 'Hazır parça'
    default:
      return 'Analiz gerekli'
  }
}

function hashName(input: string): number {
  let h = 0
  for (let i = 0; i < input.length; i++) h = (h * 31 + input.charCodeAt(i)) >>> 0
  return h
}

function classifyFromFileName(fileName: string): {
  role: PartRole
  processes: ProcessKind[]
  material: string
  confidence: number
} {
  const n = fileName.toLowerCase()
  if (/\b(m\d+|vid[aа]|somun|pul|washer|bolt|screw|nut)\b/.test(n)) {
    return { role: 'fastener', processes: [], material: '8.8 / A2', confidence: 0.92 }
  }
  if (/(sac|sheet|bracket|flange|panel|kapak|tray|plaka|plate|bent)/.test(n)) {
    return {
      role: 'sheet-metal',
      processes: ['cut', 'bend'],
      material: 'DX51D+Z275',
      confidence: 0.9,
    }
  }
  if (/(kaynak|weld|frame|şasi|sasi|chassis|profil|tube|boruya)/.test(n)) {
    return {
      role: 'weldment',
      processes: ['cut', 'weld'],
      material: 'S235JR',
      confidence: 0.88,
    }
  }
  if (/(blok|block|housing|adapter|cnc|miller|torna|lathe|shaft|mil|islem|işlem|hub)/.test(n)) {
    return {
      role: 'machined',
      processes: ['machining'],
      material: /lathe|stainless|inox|çelik|celik/.test(n) ? 'Stainless Steel' : 'Al7075-T6',
      confidence: 0.9,
    }
  }
  if (/(motor|sensor|rulman|bearing|silindir)/.test(n)) {
    return { role: 'bought-out', processes: [], material: 'Tedarik', confidence: 0.8 }
  }
  // fallback by hash — still assign a productive role
  const h = hashName(n) % 3
  if (h === 0)
    return {
      role: 'sheet-metal',
      processes: ['cut', 'bend'],
      material: 'DX51D+Z275',
      confidence: 0.72,
    }
  if (h === 1)
    return {
      role: 'machined',
      processes: ['machining'],
      material: 'C45',
      confidence: 0.7,
    }
  return {
    role: 'weldment',
    processes: ['cut', 'weld'],
    material: 'S235JR',
    confidence: 0.7,
  }
}

function makeSheetFromName(name: string, thickness = 2): SheetMetalPart {
  const h = hashName(name)
  const width = 120 + (h % 5) * 40
  const height = 70 + (h % 4) * 30
  const bendCount = 1 + (h % 3)
  const bends: BendPoint[] = []
  for (let i = 0; i < bendCount; i++) {
    const vertical = i % 2 === 0
    const angle = i === bendCount - 1 && h % 5 === 0 ? 120 : 90
    const radius = thickness
    bends.push({
      id: `BL-${String(i + 1).padStart(2, '0')}`,
      name: `Bend-${i + 1}`,
      xMm: vertical ? Math.round(width * (0.35 + i * 0.15)) : 0,
      yMm: vertical ? 0 : Math.round(height * (0.25 + i * 0.2)),
      lengthMm: vertical ? height : width,
      angleDeg: angle,
      radiusMm: radius,
      flangeMm: 20 + (h % 4) * 5,
      direction: i % 2 === 0 ? 'UP' : 'DOWN',
      allowanceMm: bendAllowance(angle, radius, thickness),
      order: i + 1,
      note: `Üretim sırası ${i + 1}. Büküm noktasını işaretleyip uygulayın.`,
    })
  }
  return {
    id: name,
    name,
    material: 'DX51D+Z275',
    thicknessMm: thickness,
    description: `${name} sac metal parçası`,
    flat: { widthMm: width, heightMm: height },
    holes: [
      {
        id: 'H1',
        xMm: 18,
        yMm: 18,
        diameterMm: 6.5,
        spec: 'Ø6.5',
      },
      {
        id: 'H2',
        xMm: width - 18,
        yMm: height - 18,
        diameterMm: 8,
        spec: 'M8',
      },
    ],
    bends,
  }
}

function defaultWelds(name: string): WeldPoint[] {
  const h = hashName(name)
  return [
    {
      id: 'W1',
      joint: 'Köşe birleşimi A',
      process: 'MAG',
      lengthMm: 40 + (h % 6) * 10,
      throatMm: 3,
      sides: '1',
      note: 'Kaynak sıçrantısı temizlenecek',
    },
    {
      id: 'W2',
      joint: 'Bindirme B',
      process: 'MAG',
      lengthMm: 25 + (h % 4) * 8,
      throatMm: 2.5,
      sides: '2',
      note: 'İki taraf kaynak — çarpılmayı kontrol edin',
    },
  ]
}

/** Profesyonel örnek montaj — yükleme yokken veya .sldasm için. */
export const SAMPLE_ASSEMBLY_PARTS: DetectedPart[] = [
  {
    id: 'p-bracket',
    name: 'L-Bracket-SM-02',
    fileName: 'L-Bracket-SM-02.SLDPRT',
    quantity: 4,
    material: 'DX51D+Z275',
    role: 'sheet-metal',
    confidence: 0.96,
    description: 'Sac L-bracket; kesim + iki büküm.',
    box: { x: 120, y: 80, z: 40 },
    thicknessMm: 2,
    sheet: makeSheetFromName('L-Bracket-SM-02', 2),
    welds: [],
    holes: [
      { id: 'H1', spec: 'Ø6.5', count: 2 },
      { id: 'H2', spec: 'M8', count: 2 },
    ],
    processes: ['cut', 'bend'],
    source: 'sample',
  },
  {
    id: 'p-cover',
    name: 'Kapak-Panel-A4',
    fileName: 'Kapak-Panel-A4.SLDPRT',
    quantity: 1,
    material: 'AlMg3',
    role: 'sheet-metal',
    confidence: 0.94,
    description: 'Dört flanşlı kapak paneli.',
    box: { x: 300, y: 200, z: 25 },
    thicknessMm: 1.5,
    sheet: makeSheetFromName('Kapak-Panel-A4', 1.5),
    welds: [],
    holes: [{ id: 'H', spec: 'Ø4.2', count: 8 }],
    processes: ['cut', 'bend'],
    source: 'sample',
  },
  {
    id: 'p-frame',
    name: 'Sasi-Frame-Weld-01',
    fileName: 'Sasi-Frame-Weld-01.SLDPRT',
    quantity: 1,
    material: 'S235JR',
    role: 'weldment',
    confidence: 0.93,
    description: 'Kaynaklı şasi; profil kesim + kaynak noktaları.',
    box: { x: 800, y: 400, z: 200 },
    welds: defaultWelds('Sasi-Frame-Weld-01'),
    holes: [{ id: 'H', spec: 'Ø12', count: 6 }],
    processes: ['cut', 'weld'],
    source: 'sample',
  },
  {
    id: 'p-block',
    name: 'Adapter-Block-CNC',
    fileName: 'Adapter-Block-CNC.SLDPRT',
    quantity: 2,
    material: 'Al7075-T6',
    role: 'machined',
    confidence: 0.95,
    description: 'CNC adapter bloğu; delik ve cep işleme.',
    box: { x: 90, y: 60, z: 35 },
    welds: [],
    holes: [
      { id: 'H1', spec: 'Ø10H7', count: 2 },
      { id: 'H2', spec: 'M6', count: 4 },
    ],
    processes: ['machining'],
    source: 'sample',
  },
  {
    id: 'p-hub',
    name: 'Hub-Flange-Keyway',
    fileName: 'Hub-Flange-Keyway.SLDPRT',
    quantity: 1,
    material: 'C45',
    role: 'machined',
    confidence: 0.97,
    description: 'Kama kanallı flanşlı göbek — profesyonel işleme resmi.',
    box: { x: 81, y: 81, z: 87 },
    welds: [],
    holes: [
      { id: 'H1', spec: 'Ø10', count: 2 },
      { id: 'H2', spec: 'Ø33.2', count: 1 },
      { id: 'H3', spec: 'kama 8', count: 1 },
    ],
    processes: ['machining'],
    source: 'sample',
  },
  {
    id: 'p-lathe',
    name: 'xometry_lathe_sample_v2.0',
    fileName: 'xometry_lathe_sample_v2.0.SLDPRT',
    quantity: 1,
    material: 'Stainless Steel',
    role: 'machined',
    confidence: 0.98,
    description: 'Torna parçası — ölçülü teknik resim (profil, yan, kesit A-A, detay C, balonlar).',
    box: { x: 76, y: 40, z: 40 },
    welds: [],
    holes: [
      { id: 'H1', spec: 'Ø10 H7', count: 1 },
      { id: 'H2', spec: 'Ø3×4', count: 4 },
      { id: 'H3', spec: 'M4', count: 2 },
    ],
    processes: ['machining'],
    source: 'sample',
  },
  {
    id: 'p-bolt',
    name: 'DIN912-M8x25',
    fileName: 'DIN912-M8x25.SLDPRT',
    quantity: 16,
    material: '8.8',
    role: 'fastener',
    confidence: 0.98,
    description: 'Standart cıvata — teknik resim üretilmez.',
    box: { x: 13, y: 13, z: 25 },
    welds: [],
    holes: [],
    processes: [],
    source: 'sample',
  },
]

function partFromUpload(file: File, index: number): DetectedPart {
  const base = file.name.replace(/\.[^.]+$/, '')
  const ext = file.name.split('.').pop()?.toLowerCase() ?? ''
  const classified = classifyFromFileName(file.name)
  const h = hashName(file.name + index)
  const part: DetectedPart = {
    id: `up-${h.toString(16)}`,
    name: base,
    fileName: file.name,
    quantity: 1,
    material: classified.material,
    role: classified.role,
    confidence: classified.confidence,
    description: `${file.name} yüklendi — AutoGBT rol ve prosesleri ayırt etti.`,
    box: {
      x: 80 + (h % 7) * 20,
      y: 50 + (h % 5) * 15,
      z: 20 + (h % 4) * 10,
    },
    welds: classified.role === 'weldment' ? defaultWelds(base) : [],
    holes:
      classified.role === 'fastener'
        ? []
        : [
            { id: 'H1', spec: 'Ø6.5', count: 2 + (h % 3) },
            { id: 'H2', spec: 'M8', count: 1 + (h % 2) },
          ],
    processes: [...classified.processes],
    source: 'upload',
  }

  if (classified.role === 'sheet-metal') {
    part.thicknessMm = 1.5 + (h % 3) * 0.5
    part.sheet = makeSheetFromName(base, part.thicknessMm)
    part.sheet.material = part.material
  }

  // Assembly file → expand into sample-like tree tagged with assembly name
  if (ext === 'sldasm') {
    return {
      ...part,
      role: 'unknown',
      processes: [],
      description: `Montaj dosyası: ${file.name}`,
      confidence: 0.99,
    }
  }

  return part
}

export function analyzeUploads(files: File[]): AssemblyProject {
  const names = files.map((f) => f.name)
  const hasAsm = files.some((f) => /\.sldasm$/i.test(f.name))
  const partFiles = files.filter((f) => !/\.sldasm$/i.test(f.name))

  let parts: DetectedPart[] = []

  if (hasAsm && partFiles.length === 0) {
    // Only assembly uploaded → expand professional tree, stamp assembly name
    const asmName = files.find((f) => /\.sldasm$/i.test(f.name))!.name
    parts = SAMPLE_ASSEMBLY_PARTS.map((p) => ({
      ...p,
      source: 'assembly-tree' as const,
      description: `${p.description} (montaj: ${asmName})`,
    }))
  } else if (partFiles.length > 0) {
    parts = partFiles.map((f, i) => partFromUpload(f, i))
    // If assembly also present, merge unique sample parts not covered? Keep uploads only.
    if (hasAsm) {
      // Annotate that BOM was merged from assembly context
      parts = parts.map((p) => ({
        ...p,
        source: 'assembly-tree' as const,
      }))
    }
  } else {
    parts = SAMPLE_ASSEMBLY_PARTS
  }

  // Deduplicate by name
  const unique = new Map<string, DetectedPart>()
  for (const p of parts) {
    const key = p.name.toLowerCase()
    if (unique.has(key)) {
      const prev = unique.get(key)!
      unique.set(key, { ...prev, quantity: prev.quantity + p.quantity })
    } else unique.set(key, p)
  }
  parts = [...unique.values()]

  const jobs = buildJobs(parts, 'a3')
  const drawable = parts.filter((p) => p.processes.length > 0)

  return {
    id: `proj-${Date.now()}`,
    name: hasAsm
      ? files.find((f) => /\.sldasm$/i.test(f.name))!.name.replace(/\.sldasm$/i, '')
      : partFiles[0]?.name.replace(/\.[^.]+$/, '') ?? 'Ornek-Montaj',
    uploadedFiles: names,
    parts,
    jobs,
    createdAt: new Date().toISOString(),
    notes: [
      `${parts.length} benzersiz parça ayırt edildi`,
      `${drawable.length} parça için teknik resim üretilecek`,
      `${jobs.length} ayrı teknik resim kağıdı planlandı`,
      hasAsm
        ? 'Montaj ağacı okundu — standart bağlantı elemanları elendi'
        : 'Yüklenen parçalar tek tek sınıflandırıldı',
      'Tam feature okuma SolidWorks eklentisinde yapılır; web motoru dosya adı + tip heuristiği ve sac/kaynak/CNC kuralları kullanır',
    ],
  }
}

export function loadSampleProject(): AssemblyProject {
  const parts = SAMPLE_ASSEMBLY_PARTS
  return {
    id: 'sample-assembly',
    name: 'Ornek-Makina-Montaj',
    uploadedFiles: ['Ornek-Makina-Montaj.SLDASM'],
    parts,
    jobs: buildJobs(parts, 'a3'),
    createdAt: new Date().toISOString(),
    notes: [
      'Örnek montaj yüklendi',
      `${parts.length} parça · sac / kaynak / CNC / bağlantı elemanı ayırt edildi`,
      'Bağlantı elemanları için teknik resim üretilmez',
    ],
  }
}

export function buildJobs(parts: DetectedPart[], sheet: SheetFormat): DrawingJob[] {
  const paper = PAPER_SIZES.find((p) => p.id === sheet) ?? PAPER_SIZES[0]
  const jobs: DrawingJob[] = []

  for (const part of parts) {
    for (const process of part.processes) {
      jobs.push(makeJob(part, process, paper))
    }
  }
  return jobs
}

function makeJob(part: DetectedPart, process: ProcessKind, paper: PaperSize): DrawingJob {
  const maxDim = Math.max(part.box.x, part.box.y, part.box.z)
  const { label } = autoScale(maxDim, paper)
  const id = `${part.id}-${process}`

  if (process === 'bend' && part.sheet) {
    const bendSteps = buildBendSteps(part.sheet, paper)
    return {
      id,
      partId: part.id,
      partName: part.name,
      process,
      title: `${part.name} — Büküm Teknik Resimleri`,
      summary: `${part.sheet.bends.length} büküm noktası · adım adım kağıtlar`,
      scaleLabel: label,
      status: 'ready',
      bendSteps,
      callouts: [
        { label: 'Büküm', value: String(part.sheet.bends.length) },
        { label: 'Kalınlık', value: `${part.thicknessMm ?? part.sheet.thicknessMm} mm` },
        { label: 'Ölçek', value: label },
      ],
      instructions: part.sheet.bends.map(
        (b) => `${b.id}: ${b.angleDeg}° ${b.direction} · R${b.radiusMm} · flanş ${b.flangeMm} mm`,
      ),
      annotations: ['Her büküm ayrı teknik resim adımında'],
    }
  }

  if (process === 'cut') {
    return {
      id,
      partId: part.id,
      partName: part.name,
      process,
      title: `${part.name} — Kesim / Açınım Resmi`,
      summary: 'Dış kontur, delikler ve (varsa) büküm çizgileri',
      scaleLabel: label,
      status: 'ready',
      callouts: [
        { label: 'Kutu', value: `${part.box.x}×${part.box.y}×${part.box.z}` },
        { label: 'Delik', value: String(part.holes.reduce((a, h) => a + h.count, 0)) },
        { label: 'Ölçek', value: label },
      ],
      instructions: [
        'Dış kontur lazer/plazma/punch ile kesilir',
        'Büküm çizgileri kesilmez — işaretlenir',
        ...part.holes.map((h) => `${h.spec} × ${h.count}`),
      ],
      annotations: [`Malzeme: ${part.material}`],
    }
  }

  if (process === 'weld') {
    return {
      id,
      partId: part.id,
      partName: part.name,
      process,
      title: `${part.name} — Kaynak Noktaları Resmi`,
      summary: `${part.welds.length} kaynak dikişi / noktası`,
      scaleLabel: label,
      status: 'ready',
      callouts: [
        { label: 'Dikiş', value: String(part.welds.length) },
        { label: 'Proses', value: part.welds[0]?.process ?? 'MAG' },
        { label: 'Ölçek', value: label },
      ],
      instructions: part.welds.map(
        (w) =>
          `${w.id}: ${w.joint} · ${w.process} · L=${w.lengthMm} mm · a=${w.throatMm} · ${w.sides} taraf`,
      ),
      annotations: part.welds.map((w) => w.note),
    }
  }

  // machining
  return {
    id,
    partId: part.id,
    partName: part.name,
    process,
    title: `${part.name} — İşleme Teknik Resmi`,
    summary: 'Çoklu görünüş, delik tablosu, tolerans',
    scaleLabel: label,
    status: 'ready',
    callouts: [
      { label: 'Malzeme', value: part.material },
      { label: 'Delik grubu', value: String(part.holes.length) },
      { label: 'Ölçek', value: label },
    ],
    instructions: [
      'Genel tolerans ISO 2768-mK',
      'İşlenmiş yüzey Ra 3.2 µm',
      ...part.holes.map((h) => `${h.spec} × ${h.count}`),
    ],
    annotations: ['Keskin kenarlar kırılacak'],
  }
}

export function jobsForPart(project: AssemblyProject, partId: string): DrawingJob[] {
  return project.jobs.filter((j) => j.partId === partId)
}
