import { useEffect, useMemo, useState } from 'react'
import {
  PAPER_SIZES,
  SAMPLE_SHEET_PARTS,
  buildBendSteps,
  type DrawingStep,
  type SheetFormat,
  type SheetMetalPart,
} from './bendSteps'
import './App.css'

function isVerticalBend(part: SheetMetalPart, bendId: string): boolean {
  if (part.id === 'bracket') return bendId === 'BL-01'
  if (part.id === 'cover') return bendId === 'BL-03' || bendId === 'BL-04'
  return false
}

function FlatPatternSvg({
  part,
  highlightId,
  completedIds,
}: {
  part: SheetMetalPart
  highlightId?: string
  completedIds: string[]
}) {
  const pad = 18
  const w = part.flat.widthMm
  const h = part.flat.heightMm
  const vb = `${-pad} ${-pad} ${w + pad * 2} ${h + pad * 2}`

  return (
    <svg viewBox={vb} className="tech-svg" role="img" aria-label="Açınım büküm haritası">
      <rect x={0} y={0} width={w} height={h} className="flat-body" />

      {part.holes.map((hole) => (
        <g key={hole.id}>
          <circle cx={hole.xMm} cy={hole.yMm} r={hole.diameterMm / 2} className="flat-hole" />
          <text x={hole.xMm + hole.diameterMm * 0.8} y={hole.yMm - 2} className="svg-label">
            {hole.spec}
          </text>
        </g>
      ))}

      {part.bends.map((bend) => {
        const active = bend.id === highlightId
        const done = completedIds.includes(bend.id)
        const cls = active ? 'bend-line active' : done ? 'bend-line done' : 'bend-line'
        const vertical = isVerticalBend(part, bend.id)

        if (vertical) {
          const x = bend.xMm || bend.flangeMm
          return (
            <g key={bend.id}>
              <line x1={x} y1={0} x2={x} y2={h} className={cls} />
              <polygon
                points={`${x},${10} ${x - 5},${20} ${x + 5},${20}`}
                className={bend.direction === 'UP' ? 'bend-arrow up' : 'bend-arrow down'}
              />
              <text x={x + 3} y={16} className={active ? 'svg-label hot' : 'svg-label'}>
                {bend.id} · {bend.angleDeg}° · {bend.direction}
              </text>
            </g>
          )
        }

        const y = bend.yMm || bend.flangeMm
        return (
          <g key={bend.id}>
            <line x1={0} y1={y} x2={w} y2={y} className={cls} />
            <polygon
              points={`${12},${y} ${22},${y - 5} ${22},${y + 5}`}
              className={bend.direction === 'UP' ? 'bend-arrow up' : 'bend-arrow down'}
            />
            <text x={24} y={y - 3} className={active ? 'svg-label hot' : 'svg-label'}>
              {bend.id} · {bend.angleDeg}° · {bend.direction}
            </text>
          </g>
        )
      })}

      <line x1={0} y1={h + 8} x2={w} y2={h + 8} className="dim-line" />
      <text x={w / 2 - 16} y={h + 16} className="svg-dim">
        {w} mm
      </text>
      <line x1={w + 8} y1={0} x2={w + 8} y2={h} className="dim-line" />
      <text x={w + 10} y={h / 2} className="svg-dim">
        {h} mm
      </text>
    </svg>
  )
}

function BendDetailSvg({ step, thickness }: { step: DrawingStep; thickness: number }) {
  const b = step.bend
  if (!b) return null

  return (
    <svg viewBox="0 0 280 168" className="tech-svg detail-svg" role="img" aria-label="Büküm detay profili">
      <text x="12" y="18" className="svg-label">
        Büküm detayı · {b.id}
      </text>

      {b.angleDeg === 90 ? (
        <>
          <path d="M 48 132 H 150 V 52" className="bend-profile-path" fill="none" />
          <path d="M 48 124 H 142 V 52" className="bend-profile-inner" fill="none" />
          <circle cx="150" cy="132" r="3.5" className="bend-point" />
          <text x="158" y="130" className="svg-label hot">
            Büküm noktası
          </text>
          <path d="M 172 132 A 24 24 0 0 0 150 108" className="angle-arc" fill="none" />
          <text x="176" y="118" className="svg-label hot">
            {b.angleDeg}°
          </text>
          <text x="70" y="148" className="svg-dim">
            taban
          </text>
          <text x="118" y="46" className="svg-dim">
            flanş {b.flangeMm} mm
          </text>
          <text x="112" y="124" className="svg-dim">
            R{b.radiusMm}
          </text>
          <text x="48" y="112" className="svg-dim">
            t={thickness} mm
          </text>
        </>
      ) : (
        <>
          <circle cx="140" cy="90" r="3.5" className="bend-point" />
          <text x="150" y="94" className="svg-label hot">
            {b.id} · {b.angleDeg}° · R{b.radiusMm}
          </text>
        </>
      )}

      <text x="12" y="162" className="svg-dim">
        Ne kadar: {b.angleDeg}° {b.direction === 'UP' ? 'yukarı' : 'aşağı'} · BA {b.allowanceMm.toFixed(2)} mm · {b.direction}
      </text>
    </svg>
  )
}

function FinalFormSvg({ part }: { part: SheetMetalPart }) {
  return (
    <svg viewBox="0 0 240 160" className="tech-svg" role="img" aria-label="Nihai form">
      <path d="M40 110 L110 70 L180 110 L110 150 Z" className="iso-face" />
      <path d="M40 110 L40 85 L110 45 L110 70 Z" className="iso-face side" />
      <path d="M180 110 L180 85 L110 45 L110 70 Z" className="iso-face front" />
      {part.bends.map((b, i) => (
        <text key={b.id} x={16} y={22 + i * 14} className="svg-label">
          {b.id}: {b.angleDeg}° / R{b.radiusMm} / flanş {b.flangeMm} / {b.direction}
        </text>
      ))}
      <text x="16" y="150" className="svg-dim">
        Nihai bükülmüş form — açı ve flanş kontrolü
      </text>
    </svg>
  )
}

function TechPaper({
  part,
  step,
  paperLabel,
  stepLabel,
}: {
  part: SheetMetalPart
  step: DrawingStep
  paperLabel: string
  stepLabel: string
}) {
  return (
    <article className="paper" aria-label={step.title}>
      <header className="paper-head">
        <div>
          <p className="paper-kicker">AUTOGBT · TEKNİK RESİM KAĞIDI · {stepLabel}</p>
          <h3>{step.title}</h3>
          <p className="paper-sub">{step.subtitle}</p>
        </div>
        <div className="paper-meta">
          <span>{paperLabel}</span>
          <span>Ölçek {step.scaleLabel}</span>
          <span>Otomatik</span>
        </div>
      </header>

      <div className="paper-body">
        {step.kind === 'overview' && <FlatPatternSvg part={part} completedIds={[]} />}
        {step.kind === 'bend' && (
          <div className="paper-split">
            <FlatPatternSvg
              part={part}
              highlightId={step.bend?.id}
              completedIds={step.completedBendIds}
            />
            <BendDetailSvg step={step} thickness={part.thicknessMm} />
          </div>
        )}
        {step.kind === 'final' && <FinalFormSvg part={part} />}
      </div>

      <div className="callout-row">
        {step.callouts.map((c) => (
          <div key={c.label} className="callout">
            <span>{c.label}</span>
            <strong>{c.value}</strong>
          </div>
        ))}
      </div>

      <ol className="step-instructions">
        {step.instructions.map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ol>

      <footer className="paper-foot">
        <span>Parça: {part.name}</span>
        <span>Malzeme: {part.material}</span>
        <span>Kalınlık: {part.thicknessMm} mm</span>
        <span>Çizen: AutoGBT</span>
        <span>{new Date().toLocaleDateString('tr-TR')}</span>
      </footer>
    </article>
  )
}

export default function App() {
  const [partId, setPartId] = useState(SAMPLE_SHEET_PARTS[0].id)
  const [sheet, setSheet] = useState<SheetFormat>('a3')
  const [stepIndex, setStepIndex] = useState(0)
  const [anim, setAnim] = useState(true)

  const part = useMemo(
    () => SAMPLE_SHEET_PARTS.find((p) => p.id === partId) as SheetMetalPart,
    [partId],
  )
  const paper = PAPER_SIZES.find((p) => p.id === sheet) ?? PAPER_SIZES[0]
  const steps = useMemo(() => buildBendSteps(part, paper), [part, paper])
  const step = steps[Math.min(stepIndex, steps.length - 1)]

  useEffect(() => {
    setStepIndex(0)
  }, [partId, sheet])

  useEffect(() => {
    setAnim(false)
    const id = window.setTimeout(() => setAnim(true), 30)
    return () => window.clearTimeout(id)
  }, [stepIndex, partId])

  function go(delta: number) {
    setStepIndex((i) => Math.max(0, Math.min(steps.length - 1, i + delta)))
  }

  return (
    <div className="page steps-page">
      <header className="hero compact-hero">
        <nav className="nav">
          <span className="brand-mark" aria-hidden="true" />
          <span className="nav-brand">AutoGBT</span>
          <span className="nav-side">Adım adım teknik resim</span>
        </nav>
        <div className="hero-copy">
          <p className="eyebrow">Büküm üretim sırası</p>
          <h1 className="brand-hero tight">AutoGBT</h1>
          <p className="lede">
            Otomatik ölçek, büküm noktaları ve her bükümün kaç derece olması
            gerektiği — teknik resim kağıdında adım adım.
          </p>
        </div>
      </header>

      <main className="wizard">
        <aside className="wizard-side">
          <label>
            Sac parça
            <select value={partId} onChange={(e) => setPartId(e.target.value)}>
              {SAMPLE_SHEET_PARTS.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.name}
                </option>
              ))}
            </select>
          </label>
          <p className="side-desc">{part.description}</p>

          <label>
            Teknik resim kağıdı
            <select value={sheet} onChange={(e) => setSheet(e.target.value as SheetFormat)}>
              {PAPER_SIZES.map((p) => (
                <option key={p.id} value={p.id}>
                  {p.label} ({p.widthMm}×{p.heightMm} mm)
                </option>
              ))}
            </select>
          </label>

          <div className="step-rail">
            {steps.map((s, i) => (
              <button
                key={s.id}
                type="button"
                className={`step-chip${i === stepIndex ? ' active' : ''}${i < stepIndex ? ' done' : ''}`}
                onClick={() => setStepIndex(i)}
              >
                <span className="step-num">{i + 1}</span>
                <span>
                  <strong>
                    {s.kind === 'bend' ? s.bend?.id : s.kind === 'overview' ? 'Harita' : 'Final'}
                  </strong>
                  <small>
                    {s.kind === 'bend'
                      ? `${s.bend?.angleDeg}° · ${s.bend?.direction}`
                      : s.scaleLabel}
                  </small>
                </span>
              </button>
            ))}
          </div>

          <div className="wizard-nav">
            <button type="button" className="btn ghost" disabled={stepIndex === 0} onClick={() => go(-1)}>
              Önceki
            </button>
            <button
              type="button"
              className="btn primary"
              disabled={stepIndex >= steps.length - 1}
              onClick={() => go(1)}
            >
              Sonraki adım
            </button>
          </div>
        </aside>

        <section className={`wizard-stage${anim ? ' show' : ''}`}>
          <TechPaper
            part={part}
            step={step}
            paperLabel={paper.label}
            stepLabel={`${stepIndex + 1} / ${steps.length}`}
          />
        </section>
      </main>
    </div>
  )
}
