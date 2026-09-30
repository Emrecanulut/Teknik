/**
 * Torna / işleme profesyonel kağıt — 2. kullanıcı örneği (lathe sample).
 */
export interface LatheSheetModel {
  partName: string
  material: string
  scale: string
  drawingNo: string
  overallLength: string
  notes: string[]
}

export const LATHE_SAMPLE: LatheSheetModel = {
  partName: 'xometry_lathe_sample_v2.0',
  material: 'Stainless Steel',
  scale: '2:1',
  drawingNo: 'AGBT-LATHE-01',
  overallLength: '76 ±0.1',
  notes: [
    'General tolerance DIN ISO 2768 - medium',
    'Deburr and break sharp edges 0.1…0.3 mm',
    'Scratches and dents are not acceptable',
    'Surface Ra 3.2 µm (unless noted)',
  ],
}

const W = 420
const H = 297
const M = 8

export function LatheProfessionalSvg({
  model = LATHE_SAMPLE,
}: {
  model?: LatheSheetModel
}) {
  return (
    <svg className="prof-sheet lathe-sheet" viewBox={`0 0 ${W} ${H}`} role="img" aria-label={model.partName}>
      <rect width={W} height={H} fill="#fafafa" />
      <rect x={M} y={M} width={W - M * 2} height={H - M * 2} fill="none" stroke="#222" strokeWidth={1.1} />
      <rect x={M + 3} y={M + 3} width={W - M * 2 - 6} height={H - M * 2 - 6} fill="none" stroke="#222" strokeWidth={0.4} />

      {/* Main profile */}
      <g transform="translate(55, 55)">
        <text x={0} y={-8} className="view-caption">
          ANA PROFİL · {model.scale}
        </text>
        {/* shaft silhouette */}
        <path
          d="M0 40
             H18 V28 H38 V22 H70 V28 H95 V18 H130 V28 H155 V40 H170
             V70 H155 V82 H130 V92 H95 V82 H70 V88 H38 V82 H18 V70 H0 Z"
          fill="none"
          stroke="#222"
          strokeWidth={1.2}
        />
        {/* centerline */}
        <line x1={-6} y1={55} x2={176} y2={55} stroke="#222" strokeWidth={0.35} strokeDasharray="4 1.5 0.7 1.5" />
        {/* section arrows A */}
        <line x1={85} y1={10} x2={85} y2={100} stroke="#222" strokeWidth={0.55} strokeDasharray="3 1.2 0.6 1.2" />
        <text x={88} y={14} className="dim-text">
          A
        </text>
        <text x={88} y={98} className="dim-text">
          A
        </text>
        {/* dims */}
        <text x={60} y={8} className="dim-text">
          {model.overallLength}
        </text>
        <text x={20} y={20} className="dim-text">
          Ø40
        </text>
        <text x={48} y={16} className="dim-text">
          Ø15
        </text>
        <text x={100} y={14} className="dim-text">
          Ø10 H7
        </text>
        <text x={35} y={108} className="dim-text">
          1×45°
        </text>
        <text x={120} y={108} className="dim-text">
          R3
        </text>
        {/* balloons */}
        {[
          [10, 25, '1'],
          [45, 12, '5'],
          [90, 12, '12'],
          [140, 22, '18'],
          [160, 50, '22'],
        ].map(([x, y, n]) => (
          <g key={String(n)}>
            <circle cx={Number(x)} cy={Number(y)} r={4.2} fill="#fff" stroke="#1a5cff" strokeWidth={0.8} />
            <text x={Number(x) - 1.2} y={Number(y) + 1.4} className="balloon">
              {n}
            </text>
          </g>
        ))}
      </g>

      {/* End view */}
      <g transform="translate(290, 45)">
        <text x={10} y={-6} className="view-caption">
          YAN / UÇ
        </text>
        <circle cx={40} cy={45} r={38} fill="none" stroke="#222" strokeWidth={1.1} />
        <circle cx={40} cy={45} r={22} fill="none" stroke="#222" strokeWidth={0.9} />
        <circle cx={40} cy={45} r={10} fill="none" stroke="#222" strokeWidth={0.9} />
        {[0, 90, 180, 270].map((a) => {
          const r = 30
          const rad = (a * Math.PI) / 180
          return (
            <circle
              key={a}
              cx={40 + r * Math.cos(rad)}
              cy={45 + r * Math.sin(rad)}
              r={2.2}
              fill="none"
              stroke="#222"
            />
          )
        })}
        <text x={55} y={20} className="dim-text">
          Ø3 +0.1 ×4
        </text>
        <g>
          <circle cx={78} cy={12} r={4.2} fill="#fff" stroke="#1a5cff" strokeWidth={0.8} />
          <text x={76.5} y={13.5} className="balloon">
            24
          </text>
        </g>
      </g>

      {/* Section A-A */}
      <g transform="translate(55, 175)">
        <text x={0} y={-6} className="view-caption">
          KESİT A-A · 1:1
        </text>
        <path
          d="M0 20 H160 V60 H0 Z"
          fill="none"
          stroke="#222"
          strokeWidth={1.1}
        />
        {/* hatch */}
        <g stroke="#222" strokeWidth={0.3}>
          {Array.from({ length: 18 }, (_, i) => (
            <line key={i} x1={5 + i * 8} y1={22} x2={i * 8} y2={58} />
          ))}
        </g>
        <text x={20} y={14} className="dim-text">
          M4
        </text>
        <text x={70} y={14} className="dim-text">
          Ø10 H7
        </text>
        <g>
          <circle cx={150} cy={12} r={4.2} fill="#fff" stroke="#1a5cff" strokeWidth={0.8} />
          <text x={148.2} y={13.5} className="balloon">
            32
          </text>
        </g>
      </g>

      {/* Detail C */}
      <g transform="translate(290, 155)">
        <text x={0} y={-6} className="view-caption">
          DETAY C · 4:1
        </text>
        <path d="M10 40 H30 V28 H55 V45 H70" fill="none" stroke="#222" strokeWidth={1.2} />
        <path d="M30 28 Q34 34 38 28" fill="none" stroke="#222" strokeWidth={0.9} />
        <text x={12} y={55} className="dim-text">
          E 0.4×0.2 DIN 509
        </text>
        <text x={40} y={20} className="dim-text">
          15°
        </text>
        <text x={48} y={38} className="dim-text">
          R0.4
        </text>
      </g>

      {/* Surface finish */}
      <g transform="translate(300, 230)">
        <path d="M0 10 L8 0 L16 10" fill="none" stroke="#222" strokeWidth={0.9} />
        <line x1={8} y1={0} x2={8} y2={16} stroke="#222" strokeWidth={0.9} />
        <text x={20} y={10} className="dim-text">
          Ra 3.2
        </text>
      </g>

      {/* Title block */}
      <g transform={`translate(${W - M - 145}, ${H - M - 68})`}>
        <rect width={145} height={68} fill="#fff" stroke="#222" strokeWidth={0.8} />
        <line x1={0} y1={16} x2={145} y2={16} stroke="#222" strokeWidth={0.45} />
        <line x1={0} y1={34} x2={145} y2={34} stroke="#222" strokeWidth={0.45} />
        <line x1={0} y1={50} x2={145} y2={50} stroke="#222" strokeWidth={0.45} />
        <line x1={88} y1={16} x2={88} y2={68} stroke="#222" strokeWidth={0.45} />
        <text x={4} y={11} className="block-label">
          AUTOGBT · İŞLEME TEKNİK RESİM
        </text>
        <text x={4} y={28} className="block-value">
          {model.partName}
        </text>
        <text x={4} y={45} className="block-label">
          MATERIAL
        </text>
        <text x={4} y={62} className="block-value">
          {model.material}
        </text>
        <text x={92} y={28} className="block-label">
          SCALE {model.scale}
        </text>
        <text x={92} y={45} className="block-label">
          A3
        </text>
        <text x={92} y={62} className="block-label">
          {model.drawingNo}
        </text>
      </g>

      <g transform={`translate(${M + 6}, ${H - M - 40})`}>
        {model.notes.map((n, i) => (
          <text key={n} x={0} y={i * 8} className="note-text">
            • {n}
          </text>
        ))}
      </g>
    </svg>
  )
}
