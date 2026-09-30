import type { ProfessionalSheetModel } from './professionalSheet'

/** A3 landscape viewBox in mm-ish units */
const W = 420
const H = 297
const M = 10

export function ProfessionalDrawingSvg({
  model,
  referenceHint = true,
}: {
  model: ProfessionalSheetModel
  referenceHint?: boolean
}) {
  return (
    <svg
      className="prof-sheet"
      viewBox={`0 0 ${W} ${H}`}
      role="img"
      aria-label={`${model.partName} teknik resmi`}
    >
      {/* Paper */}
      <rect x={0} y={0} width={W} height={H} fill="#f7f7f5" />
      <rect x={M} y={M} width={W - M * 2} height={H - M * 2} fill="none" stroke="#1a2744" strokeWidth={1.2} />
      <rect
        x={M + 4}
        y={M + 4}
        width={W - M * 2 - 8}
        height={H - M * 2 - 8}
        fill="none"
        stroke="#1a2744"
        strokeWidth={0.5}
      />

      {/* Zone grid labels */}
      {Array.from({ length: 8 }, (_, i) => (
        <text key={`c${i}`} x={M + 18 + i * ((W - M * 2 - 36) / 7)} y={M + 3.2} className="zone-label">
          {i + 1}
        </text>
      ))}
      {['A', 'B', 'C', 'D', 'E', 'F'].map((letter, i) => (
        <text key={letter} x={M + 2.5} y={M + 28 + i * ((H - M * 2 - 40) / 5)} className="zone-label">
          {letter}
        </text>
      ))}

      {/* Views */}
      <g transform="translate(40, 28)">
        <FrontView profile={model.profile} />
        <text x={55} y={8} className="view-caption">
          ÖN GÖRÜNÜŞ
        </text>
        {/* Section arrows A */}
        <line x1={110} y1={20} x2={110} y2={100} stroke="#1a2744" strokeWidth={0.7} strokeDasharray="4 2 1 2" />
        <polygon points="110,18 106,26 114,26" fill="#1a2744" />
        <polygon points="110,102 106,94 114,94" fill="#1a2744" />
        <text x={114} y={22} className="dim-text">
          A
        </text>
        <text x={114} y={100} className="dim-text">
          A
        </text>
      </g>

      <g transform="translate(170, 40)">
        <TopView profile={model.profile} />
        <text x={40} y={-4} className="view-caption">
          ÜST GÖRÜNÜŞ
        </text>
      </g>

      <g transform="translate(40, 145)">
        <SectionView profile={model.profile} />
        <text x={40} y={-4} className="view-caption">
          KESİT A-A
        </text>
      </g>

      <g transform="translate(280, 30)">
        <IsoView profile={model.profile} />
        <text x={20} y={-4} className="view-caption">
          İZOMETRİK
        </text>
      </g>

      {/* Sample dimensions around section/front */}
      <g className="dims">
        {model.profile === 'hub-flange' && (
          <>
            <DimHorizontal x1={55} x2={136} y={138} label="81" />
            <DimVertical x={36} y1={48} y2={135} label="87" />
            <text x={95} y={70} className="dim-text">
              Ø70
            </text>
            <text x={100} y={58} className="dim-text">
              Ø50
            </text>
            <text x={215} y={95} className="dim-text">
              Ø33.2
            </text>
            <text x={230} y={78} className="dim-text">
              8
            </text>
            <text x={248} y={110} className="dim-text">
              R15
            </text>
            <text x={70} y={125} className="dim-text">
              Ø10
            </text>
            <text x={48} y={118} className="dim-text">
              23
            </text>
          </>
        )}
        {model.profile !== 'hub-flange' &&
          model.dims.slice(0, 5).map((d, i) => (
            <text key={d.value + i} x={50 + i * 28} y={270} className="dim-text">
              {d.value}
            </text>
          ))}
      </g>

      {/* Title block */}
      <g transform={`translate(${W - M - 150}, ${H - M - 72})`}>
        <rect x={0} y={0} width={150} height={72} fill="#fff" stroke="#1a2744" strokeWidth={0.8} />
        <line x1={0} y1={18} x2={150} y2={18} stroke="#1a2744" strokeWidth={0.5} />
        <line x1={0} y1={36} x2={150} y2={36} stroke="#1a2744" strokeWidth={0.5} />
        <line x1={0} y1={54} x2={150} y2={54} stroke="#1a2744" strokeWidth={0.5} />
        <line x1={90} y1={18} x2={90} y2={72} stroke="#1a2744" strokeWidth={0.5} />
        <line x1={120} y1={36} x2={120} y2={72} stroke="#1a2744" strokeWidth={0.5} />

        <text x={4} y={12} className="block-label">
          AUTOGBT TEKNİK RESİM
        </text>
        <text x={4} y={30} className="block-value">
          {model.partName}
        </text>
        <text x={4} y={48} className="block-label">
          MALZEME
        </text>
        <text x={4} y={66} className="block-value">
          {model.material}
        </text>
        <text x={94} y={48} className="block-label">
          ÖLÇEK
        </text>
        <text x={94} y={66} className="block-value">
          {model.scale}
        </text>
        <text x={124} y={48} className="block-label">
          {model.sheet}
        </text>
        <text x={94} y={30} className="block-label">
          {model.process}
        </text>
        <text x={124} y={66} className="block-label">
          {model.drawingNo}
        </text>
      </g>

      {/* Notes */}
      <g transform={`translate(${M + 8}, ${H - M - 48})`}>
        {model.notes.map((n, i) => (
          <text key={n} x={0} y={i * 9} className="note-text">
            • {n}
          </text>
        ))}
      </g>

      {referenceHint && (
        <text x={M + 8} y={H - M - 4} className="note-text muted">
          Şablon: profesyonel A3 düzen (ön / üst / kesit A-A / izometrik)
        </text>
      )}
    </svg>
  )
}

function FrontView({ profile }: { profile: ProfessionalSheetModel['profile'] }) {
  if (profile === 'hub-flange') {
    return (
      <g>
        {/* body */}
        <rect x={35} y={18} width={50} height={70} fill="none" stroke="#1a2744" strokeWidth={1.1} />
        <rect x={40} y={22} width={40} height={8} fill="none" stroke="#1a2744" strokeWidth={0.8} />
        {/* feet */}
        <path d="M20 88 H50 V78 H70 V88 H100 V98 H20 Z" fill="none" stroke="#1a2744" strokeWidth={1.1} />
        <circle cx={30} cy={93} r={3} fill="none" stroke="#1a2744" strokeWidth={0.8} />
        <circle cx={90} cy={93} r={3} fill="none" stroke="#1a2744" strokeWidth={0.8} />
        {/* centerline */}
        <line x1={60} y1={12} x2={60} y2={100} stroke="#1a2744" strokeWidth={0.4} strokeDasharray="3 1.5 0.5 1.5" />
      </g>
    )
  }
  if (profile === 'bracket') {
    return (
      <g>
        <path d="M20 30 H90 V95 H55 V55 H20 Z" fill="none" stroke="#1a2744" strokeWidth={1.1} />
        <circle cx={35} cy={42} r={3} fill="none" stroke="#1a2744" />
        <circle cx={75} cy={75} r={3} fill="none" stroke="#1a2744" />
      </g>
    )
  }
  return (
    <g>
      <rect x={25} y={25} width={70} height={55} fill="none" stroke="#1a2744" strokeWidth={1.1} />
      <circle cx={45} cy={45} r={6} fill="none" stroke="#1a2744" />
      <circle cx={75} cy={55} r={4} fill="none" stroke="#1a2744" />
    </g>
  )
}

function TopView({ profile }: { profile: ProfessionalSheetModel['profile'] }) {
  if (profile === 'hub-flange') {
    return (
      <g>
        <circle cx={55} cy={55} r={35} fill="none" stroke="#1a2744" strokeWidth={1.1} />
        <circle cx={55} cy={55} r={25} fill="none" stroke="#1a2744" strokeWidth={0.9} />
        <circle cx={55} cy={55} r={14} fill="none" stroke="#1a2744" strokeWidth={0.9} />
        {/* keyway */}
        <rect x={51} y={20} width={8} height={16} fill="none" stroke="#1a2744" strokeWidth={0.8} />
        {/* ears */}
        <rect x={8} y={48} width={18} height={14} rx={3} fill="none" stroke="#1a2744" />
        <rect x={84} y={48} width={18} height={14} rx={3} fill="none" stroke="#1a2744" />
        <circle cx={17} cy={55} r={3} fill="none" stroke="#1a2744" />
        <circle cx={93} cy={55} r={3} fill="none" stroke="#1a2744" />
        <line x1={55} y1={12} x2={55} y2={98} stroke="#1a2744" strokeWidth={0.35} strokeDasharray="3 1.5 0.5 1.5" />
        <line x1={12} y1={55} x2={98} y2={55} stroke="#1a2744" strokeWidth={0.35} strokeDasharray="3 1.5 0.5 1.5" />
      </g>
    )
  }
  if (profile === 'bracket') {
    return (
      <g>
        <rect x={15} y={30} width={90} height={50} fill="none" stroke="#1a2744" strokeWidth={1} />
        <line x1={15} y1={55} x2={105} y2={55} stroke="#1a2744" strokeDasharray="3 2" />
      </g>
    )
  }
  return (
    <g>
      <rect x={20} y={25} width={80} height={55} fill="none" stroke="#1a2744" />
    </g>
  )
}

function SectionView({ profile }: { profile: ProfessionalSheetModel['profile'] }) {
  if (profile === 'hub-flange') {
    return (
      <g>
        <path
          d="M30 10 H90 V85 H70 V95 H40 V85 H30 Z"
          fill="none"
          stroke="#1a2744"
          strokeWidth={1.1}
        />
        {/* bore steps */}
        <path d="M48 10 V40 H52 V10" fill="none" stroke="#1a2744" />
        <path d="M45 40 V75 H55 V40" fill="none" stroke="#1a2744" />
        {/* hatch */}
        <g stroke="#1a2744" strokeWidth={0.35}>
          {Array.from({ length: 14 }, (_, i) => (
            <line key={i} x1={32 + i * 4} y1={15} x2={28 + i * 4} y2={40} />
          ))}
          {Array.from({ length: 10 }, (_, i) => (
            <line key={`b${i}`} x1={34 + i * 5} y1={78} x2={30 + i * 5} y2={93} />
          ))}
        </g>
      </g>
    )
  }
  return (
    <g>
      <rect x={25} y={15} width={70} height={70} fill="none" stroke="#1a2744" />
      {Array.from({ length: 12 }, (_, i) => (
        <line key={i} x1={30 + i * 5} y1={20} x2={25 + i * 5} y2={80} stroke="#1a2744" strokeWidth={0.35} />
      ))}
    </g>
  )
}

function IsoView({ profile }: { profile: ProfessionalSheetModel['profile'] }) {
  if (profile === 'hub-flange') {
    return (
      <g>
        <defs>
          <linearGradient id="metal" x1="0" y1="0" x2="1" y2="1">
            <stop offset="0%" stopColor="#c5c9cf" />
            <stop offset="50%" stopColor="#9aa1ab" />
            <stop offset="100%" stopColor="#6e7682" />
          </linearGradient>
        </defs>
        {/* simplified shaded iso hub */}
        <ellipse cx={55} cy={28} rx={28} ry={10} fill="url(#metal)" stroke="#2a3340" strokeWidth={0.8} />
        <path d="M27 28 V70 C27 78 83 78 83 70 V28" fill="url(#metal)" stroke="#2a3340" strokeWidth={0.8} />
        <ellipse cx={55} cy={70} rx={28} ry={10} fill="#7d8591" stroke="#2a3340" strokeWidth={0.8} />
        <ellipse cx={55} cy={28} rx={12} ry={4.5} fill="#5c6570" stroke="#2a3340" strokeWidth={0.6} />
        {/* feet */}
        <path d="M18 72 L30 66 L30 78 L18 84 Z" fill="#8b929c" stroke="#2a3340" strokeWidth={0.7} />
        <path d="M80 66 L92 72 L92 84 L80 78 Z" fill="#8b929c" stroke="#2a3340" strokeWidth={0.7} />
        <ellipse cx={24} cy={76} rx={3} ry={1.6} fill="#4a515a" />
        <ellipse cx={86} cy={76} rx={3} ry={1.6} fill="#4a515a" />
      </g>
    )
  }
  return (
    <g>
      <path d="M30 70 L55 50 L90 65 L65 85 Z" fill="#b7bdc6" stroke="#2a3340" />
      <path d="M30 70 L30 50 L55 30 L55 50 Z" fill="#9aa1ab" stroke="#2a3340" />
      <path d="M90 65 L90 45 L55 30 L55 50 Z" fill="#7d8591" stroke="#2a3340" />
    </g>
  )
}

function DimHorizontal({ x1, x2, y, label }: { x1: number; x2: number; y: number; label: string }) {
  const mid = (x1 + x2) / 2
  return (
    <g>
      <line x1={x1} y1={y} x2={x2} y2={y} stroke="#1a2744" strokeWidth={0.45} />
      <line x1={x1} y1={y - 2} x2={x1} y2={y + 2} stroke="#1a2744" strokeWidth={0.45} />
      <line x1={x2} y1={y - 2} x2={x2} y2={y + 2} stroke="#1a2744" strokeWidth={0.45} />
      <text x={mid - 4} y={y - 2} className="dim-text">
        {label}
      </text>
    </g>
  )
}

function DimVertical({ x, y1, y2, label }: { x: number; y1: number; y2: number; label: string }) {
  const mid = (y1 + y2) / 2
  return (
    <g>
      <line x1={x} y1={y1} x2={x} y2={y2} stroke="#1a2744" strokeWidth={0.45} />
      <line x1={x - 2} y1={y1} x2={x + 2} y2={y1} stroke="#1a2744" strokeWidth={0.45} />
      <line x1={x - 2} y1={y2} x2={x + 2} y2={y2} stroke="#1a2744" strokeWidth={0.45} />
      <text x={x - 10} y={mid} className="dim-text">
        {label}
      </text>
    </g>
  )
}
