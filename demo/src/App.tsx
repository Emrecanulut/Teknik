import { useMemo, useRef, useState } from 'react'
import {
  analyzeUploads,
  jobsForPart,
  processLabel,
  roleLabel,
  type AssemblyProject,
  type DetectedPart,
  type DrawingJob,
  type ProcessKind,
} from './assemblyEngine'
import type { DrawingStep, SheetMetalPart } from './bendSteps'
import './App.css'

type Phase = 'landing' | 'processing' | 'studio'

function ProcessBadge({ process }: { process: ProcessKind }) {
  return <span className={`proc-badge proc-${process}`}>{processLabel(process)}</span>
}

function FlatMini({ sheet, highlightId }: { sheet: SheetMetalPart; highlightId?: string }) {
  const w = sheet.flat.widthMm
  const h = sheet.flat.heightMm
  const pad = 12
  return (
    <svg viewBox={`${-pad} ${-pad} ${w + pad * 2} ${h + pad * 2}`} className="tech-svg mini">
      <rect x={0} y={0} width={w} height={h} className="flat-body" />
      {sheet.bends.map((b, i) => {
        const vertical = i % 2 === 0
        const active = b.id === highlightId
        const cls = active ? 'bend-line active' : 'bend-line'
        if (vertical) {
          const x = b.xMm || b.flangeMm
          return <line key={b.id} x1={x} y1={0} x2={x} y2={h} className={cls} />
        }
        const y = b.yMm || b.flangeMm
        return <line key={b.id} x1={0} y1={y} x2={w} y2={y} className={cls} />
      })}
      {sheet.holes.map((hole) => (
        <circle key={hole.id} cx={hole.xMm} cy={hole.yMm} r={hole.diameterMm / 2} className="flat-hole" />
      ))}
    </svg>
  )
}

function JobPaper({
  job,
  part,
  bendStep,
}: {
  job: DrawingJob
  part: DetectedPart
  bendStep?: DrawingStep
}) {
  return (
    <article className="paper job-paper" aria-label={job.title}>
      <header className="paper-head">
        <div>
          <p className="paper-kicker">
            AUTOGBT · {processLabel(job.process).toUpperCase()} · {part.fileName}
          </p>
          <h3>{bendStep ? bendStep.title : job.title}</h3>
          <p className="paper-sub">{bendStep ? bendStep.subtitle : job.summary}</p>
        </div>
        <div className="paper-meta">
          <span>Adet ×{part.quantity}</span>
          <span>Ölçek {bendStep?.scaleLabel ?? job.scaleLabel}</span>
          <span>Güven %{Math.round(part.confidence * 100)}</span>
        </div>
      </header>

      {job.process === 'bend' && bendStep?.bend && (
        <div className="bend-amount">
          <div>
            <span className="bend-amount-label">Ne kadar bükülecek</span>
            <p className="bend-amount-value">
              {bendStep.bend.angleDeg}°
              <small>{bendStep.bend.direction === 'UP' ? 'yukarı' : 'aşağı'}</small>
            </p>
          </div>
          <ul>
            <li>
              Nokta <strong>{bendStep.bend.id}</strong>
            </li>
            <li>
              R <strong>{bendStep.bend.radiusMm}</strong>
            </li>
            <li>
              Flanş <strong>{bendStep.bend.flangeMm} mm</strong>
            </li>
            <li>
              BA <strong>{bendStep.bend.allowanceMm.toFixed(2)} mm</strong>
            </li>
          </ul>
        </div>
      )}

      <div className="paper-body">
        {part.sheet && (job.process === 'cut' || job.process === 'bend') && (
          <FlatMini
            sheet={part.sheet}
            highlightId={bendStep?.bend?.id}
          />
        )}
        {job.process === 'weld' && (
          <div className="weld-map">
            {part.welds.map((w) => (
              <div key={w.id} className="weld-card">
                <strong>{w.id}</strong>
                <span>{w.joint}</span>
                <span>
                  {w.process} · L={w.lengthMm} · a={w.throatMm} · {w.sides} taraf
                </span>
                <em>{w.note}</em>
              </div>
            ))}
          </div>
        )}
        {job.process === 'machining' && (
          <div className="machine-map">
            <div className="iso-block" aria-hidden="true">
              <span>Ön</span>
              <span>Üst</span>
              <span>Yan</span>
              <span>İzo</span>
            </div>
            <ul>
              {part.holes.map((h) => (
                <li key={h.id}>
                  {h.spec} × {h.count}
                </li>
              ))}
            </ul>
          </div>
        )}
      </div>

      <div className="callout-row">
        {(bendStep?.callouts ?? job.callouts).map((c) => (
          <div key={c.label} className="callout">
            <span>{c.label}</span>
            <strong>{c.value}</strong>
          </div>
        ))}
      </div>

      <ol className="step-instructions">
        {(bendStep?.instructions ?? job.instructions).map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ol>

      <footer className="paper-foot">
        <span>{part.name}</span>
        <span>{part.material}</span>
        <span>{roleLabel(part.role)}</span>
        <span>AutoGBT</span>
      </footer>
    </article>
  )
}

export default function App() {
  const inputRef = useRef<HTMLInputElement>(null)
  const [phase, setPhase] = useState<Phase>('landing')
  const [project, setProject] = useState<AssemblyProject | null>(null)
  const [status, setStatus] = useState('')
  const [selectedPartId, setSelectedPartId] = useState<string>('')
  const [selectedJobId, setSelectedJobId] = useState<string>('')
  const [bendStepIndex, setBendStepIndex] = useState(0)
  const [dragOver, setDragOver] = useState(false)

  const selectedPart = useMemo(
    () => project?.parts.find((p) => p.id === selectedPartId) ?? null,
    [project, selectedPartId],
  )
  const partJobs = useMemo(
    () => (project && selectedPartId ? jobsForPart(project, selectedPartId) : []),
    [project, selectedPartId],
  )
  const selectedJob = useMemo(
    () => partJobs.find((j) => j.id === selectedJobId) ?? partJobs[0] ?? null,
    [partJobs, selectedJobId],
  )
  const bendSteps = selectedJob?.bendSteps
  const bendStep = bendSteps?.[Math.min(bendStepIndex, (bendSteps?.length ?? 1) - 1)]

  async function runAnalysis(files: File[]) {
    if (!files.length) return
    setPhase('processing')
    setStatus('Dosyalar okunuyor…')
    await wait(500)
    setStatus('Montaj / parça ağacı ayırt ediliyor…')
    await wait(700)
    setStatus('Büküm · kesim · kaynak · işleme prosesleri sınıflandırılıyor…')
    await wait(700)
    setStatus('Teknik resim kağıtları planlanıyor…')
    await wait(500)

    const proj = analyzeUploads(files)
    setProject(proj)
    const firstDrawable = proj.parts.find((p) => p.processes.length > 0) ?? proj.parts[0]
    setSelectedPartId(firstDrawable.id)
    const jobs = jobsForPart(proj, firstDrawable.id)
    setSelectedJobId(jobs[0]?.id ?? '')
    setBendStepIndex(0)
    setPhase('studio')
  }

  function openSample() {
    void runAnalysis([
      new File([new Uint8Array([0])], 'Ornek-Makina-Montaj.SLDASM', {
        type: 'application/octet-stream',
      }),
    ])
  }

  function onFiles(list: FileList | File[] | null) {
    if (!list) return
    void runAnalysis([...list])
  }

  function selectPart(part: DetectedPart) {
    setSelectedPartId(part.id)
    const jobs = project ? jobsForPart(project, part.id) : []
    setSelectedJobId(jobs[0]?.id ?? '')
    setBendStepIndex(0)
  }

  function selectJob(job: DrawingJob) {
    setSelectedJobId(job.id)
    setBendStepIndex(0)
  }

  return (
    <div className="page assembly-page">
      {phase !== 'studio' && (
        <header className="hero compact-hero">
          <nav className="nav">
            <span className="brand-mark" aria-hidden="true" />
            <span className="nav-brand">AutoGBT</span>
            <span className="nav-side">Montaj → Parça → Teknik Resim</span>
          </nav>
          <div className="hero-copy">
            <p className="eyebrow">Profesyonel teknik resim hattı</p>
            <h1 className="brand-hero tight">AutoGBT</h1>
            <p className="lede">
              SolidWorks montajınızı yükleyin. AutoGBT tüm farklı parçaları ayırt eder;
              büküm, kesim, kaynak ve işleme teknik resimlerini ayrı ayrı üretir.
            </p>
          </div>
        </header>
      )}

      {phase === 'landing' && (
        <section className="upload-section">
          <div
            className={`dropzone${dragOver ? ' over' : ''}`}
            onDragEnter={(e) => {
              e.preventDefault()
              setDragOver(true)
            }}
            onDragOver={(e) => e.preventDefault()}
            onDragLeave={() => setDragOver(false)}
            onDrop={(e) => {
              e.preventDefault()
              setDragOver(false)
              onFiles(e.dataTransfer.files)
            }}
          >
            <p className="drop-title">SolidWorks dosyalarını sürükleyip bırakın</p>
            <p className="drop-sub">.SLDASM montaj · .SLDPRT parçalar · birden fazla dosya</p>
            <div className="drop-actions">
              <button type="button" className="btn primary" onClick={() => inputRef.current?.click()}>
                Dosya seç
              </button>
              <button type="button" className="btn ghost" onClick={openSample}>
                Örnek montajı aç
              </button>
            </div>
            <input
              ref={inputRef}
              type="file"
              multiple
              accept=".sldasm,.sldprt,.SLDASM,.SLDPRT,.step,.stp"
              hidden
              onChange={(e) => onFiles(e.target.files)}
            />
          </div>
          <ul className="upload-points">
            <li>Montajdaki tüm parçalar tek tek listelenir</li>
            <li>Sac / kaynak / CNC / bağlantı elemanı otomatik ayırt edilir</li>
            <li>Her proses için ayrı teknik resim kağıdı üretilir</li>
            <li>Bükümler adım adım (nokta, açı, pay) çıkarılır</li>
          </ul>
        </section>
      )}

      {phase === 'processing' && (
        <section className="processing">
          <div className="processing-card">
            <div className="spinner" aria-hidden="true" />
            <h2>AutoGBT analiz ediyor</h2>
            <p>{status}</p>
          </div>
        </section>
      )}

      {phase === 'studio' && project && (
        <main className="assembly-studio">
          <header className="studio-bar">
            <div>
              <p className="eyebrow">Proje</p>
              <h2>{project.name}</h2>
              <p className="studio-meta">
                {project.parts.length} parça · {project.jobs.length} teknik resim ·{' '}
                {project.uploadedFiles.join(', ')}
              </p>
            </div>
            <div className="studio-bar-actions">
              <button type="button" className="btn ghost" onClick={() => window.print()}>
                Yazdır
              </button>
              <button
                type="button"
                className="btn ghost"
                onClick={() => {
                  setPhase('landing')
                  setProject(null)
                }}
              >
                Yeni yükleme
              </button>
            </div>
          </header>

          <div className="studio-notes">
            {project.notes.map((n) => (
              <span key={n}>{n}</span>
            ))}
          </div>

          <div className="studio-grid">
            <aside className="parts-pane">
              <h3>Ayırt edilen parçalar</h3>
              <div className="parts-list">
                {project.parts.map((part) => (
                  <button
                    key={part.id}
                    type="button"
                    className={`part-row${part.id === selectedPartId ? ' active' : ''}${part.processes.length === 0 ? ' skipped' : ''}`}
                    onClick={() => selectPart(part)}
                  >
                    <div className="part-row-top">
                      <strong>{part.name}</strong>
                      <span>×{part.quantity}</span>
                    </div>
                    <div className="part-row-mid">
                      <em>{roleLabel(part.role)}</em>
                      <span>%{Math.round(part.confidence * 100)}</span>
                    </div>
                    <div className="part-row-badges">
                      {part.processes.length === 0 ? (
                        <span className="proc-badge proc-skip">Resim yok</span>
                      ) : (
                        part.processes.map((p) => <ProcessBadge key={p} process={p} />)
                      )}
                    </div>
                  </button>
                ))}
              </div>
            </aside>

            <section className="jobs-pane">
              {selectedPart && (
                <>
                  <div className="job-head">
                    <div>
                      <h3>{selectedPart.name}</h3>
                      <p>{selectedPart.description}</p>
                    </div>
                    <div className="job-tabs">
                      {partJobs.length === 0 && <span className="muted">Bu parça için teknik resim üretilmez.</span>}
                      {partJobs.map((job) => (
                        <button
                          key={job.id}
                          type="button"
                          className={`job-tab${selectedJob?.id === job.id ? ' active' : ''}`}
                          onClick={() => selectJob(job)}
                        >
                          {processLabel(job.process)}
                        </button>
                      ))}
                    </div>
                  </div>

                  {selectedJob && (
                    <>
                      {selectedJob.bendSteps && selectedJob.bendSteps.length > 0 && (
                        <div className="bend-stepper">
                          <button
                            type="button"
                            className="btn ghost"
                            disabled={bendStepIndex === 0}
                            onClick={() => setBendStepIndex((i) => Math.max(0, i - 1))}
                          >
                            Önceki adım
                          </button>
                          <div className="bend-step-rail">
                            {selectedJob.bendSteps.map((s, i) => (
                              <button
                                key={s.id}
                                type="button"
                                className={`step-dot${i === bendStepIndex ? ' active' : ''}`}
                                onClick={() => setBendStepIndex(i)}
                              >
                                {i + 1}
                              </button>
                            ))}
                          </div>
                          <button
                            type="button"
                            className="btn ghost"
                            disabled={bendStepIndex >= selectedJob.bendSteps.length - 1}
                            onClick={() =>
                              setBendStepIndex((i) =>
                                Math.min((selectedJob.bendSteps?.length ?? 1) - 1, i + 1),
                              )
                            }
                          >
                            Sonraki adım
                          </button>
                        </div>
                      )}

                      <JobPaper job={selectedJob} part={selectedPart} bendStep={bendStep} />
                    </>
                  )}
                </>
              )}
            </section>

            <aside className="queue-pane">
              <h3>Üretim kuyruğu</h3>
              <p className="muted">{project.jobs.length} ayrı kağıt</p>
              <ol className="queue-list">
                {project.jobs.map((job) => (
                  <li key={job.id}>
                    <button
                      type="button"
                      className={job.id === selectedJob?.id ? 'active' : ''}
                      onClick={() => {
                        setSelectedPartId(job.partId)
                        setSelectedJobId(job.id)
                        setBendStepIndex(0)
                      }}
                    >
                      <strong>{processLabel(job.process)}</strong>
                      <span>{job.partName}</span>
                    </button>
                  </li>
                ))}
              </ol>
              <button
                type="button"
                className="btn primary wide"
                onClick={() => {
                  // Jump through all drawable parts' first jobs sequentially feel
                  if (!project.jobs.length) return
                  const idx = Math.max(
                    0,
                    project.jobs.findIndex((j) => j.id === selectedJob?.id),
                  )
                  const next = project.jobs[(idx + 1) % project.jobs.length]
                  setSelectedPartId(next.partId)
                  setSelectedJobId(next.id)
                  setBendStepIndex(0)
                }}
              >
                Sonraki teknik resim
              </button>
              <button type="button" className="btn ghost wide" onClick={openSample}>
                Örnek montajı yeniden yükle
              </button>
            </aside>
          </div>
        </main>
      )}
    </div>
  )
}

function wait(ms: number) {
  return new Promise((r) => setTimeout(r, ms))
}
