import { useEffect, useMemo, useState } from 'react'
import {
  SAMPLE_PARTS,
  explain,
  planDrawing,
  recommendKinds,
  toTurkish,
  type DrawingKind,
  type DrawingPlan,
  type SamplePart,
  type SheetFormat,
} from './autogbt'
import './App.css'

const KIND_LABELS: { id: DrawingKind; label: string; blurb: string }[] = [
  { id: 'bend', label: 'Büküm', blurb: 'Açı, yarıçap, flanş ve büküm sırası' },
  { id: 'cut', label: 'Kesim', blurb: 'Açınım, kontur, delik ve büküm çizgileri' },
  { id: 'machining', label: 'İşleme', blurb: 'Çoklu görünüş, tolerans ve yüzey' },
]

function DrawingSheet({ plan, part }: { plan: DrawingPlan; part: SamplePart }) {
  return (
    <div className="sheet" data-kind={plan.kind} aria-label={plan.title}>
      <div className="sheet-frame">
        <header className="sheet-header">
          <div>
            <p className="sheet-kicker">AUTOGBT · {toTurkish(plan.kind).toUpperCase()}</p>
            <h3>{plan.title}</h3>
          </div>
          <div className="sheet-meta">
            <span>Ölçek {plan.scale}</span>
            <span>Güven %{Math.round(plan.confidence * 100)}</span>
          </div>
        </header>

        <div className="sheet-canvas">
          {plan.views.map((view) => (
            <article
              key={view.name}
              className={`view-block${view.iso ? ' is-iso' : ''}${view.flat ? ' is-flat' : ''}${view.section ? ' is-section' : ''}${view.detail ? ' is-detail' : ''}`}
              style={{
                left: `${view.x * 100}%`,
                bottom: `${view.y * 100}%`,
                width: `${view.w * 100}%`,
                height: `${view.h * 100}%`,
              }}
            >
              <span className="view-label">{view.name}</span>
              <svg viewBox="0 0 120 80" className="view-svg" aria-hidden="true">
                {view.flat ? (
                  <>
                    <rect x="18" y="22" width="84" height="42" className="contour" />
                    <line x1="18" y1="42" x2="102" y2="42" className="bend-up" />
                    <line x1="48" y1="22" x2="48" y2="64" className="bend-down" />
                    {part.holes.slice(0, 4).map((h, i) => (
                      <circle key={h.id} cx={30 + i * 18} cy={34} r={3.2} className="hole" />
                    ))}
                  </>
                ) : view.iso ? (
                  <>
                    <path d="M30 55 L60 35 L90 55 L60 75 Z" className="iso-top" />
                    <path d="M30 55 L30 40 L60 20 L60 35 Z" className="iso-side" />
                    <path d="M90 55 L90 40 L60 20 L60 35 Z" className="iso-front" />
                  </>
                ) : view.section ? (
                  <>
                    <rect x="28" y="18" width="64" height="48" className="contour" />
                    <path d="M28 30 H92 M28 54 H92" className="hatch" />
                    <circle cx="48" cy="42" r="8" className="hole" />
                    <circle cx="72" cy="42" r="5" className="hole" />
                  </>
                ) : (
                  <>
                    <rect x="24" y="20" width="72" height="44" className="contour" />
                    <line x1="24" y1="42" x2="96" y2="42" className="centerline" />
                    <circle cx="42" cy="34" r="4" className="hole" />
                    <circle cx="78" cy="34" r="4" className="hole" />
                    {plan.kind === 'bend' && (
                      <path d="M24 64 L24 48 L96 48 L96 30" className="bend-profile" />
                    )}
                  </>
                )}
              </svg>
              <p className="view-desc">{view.description}</p>
            </article>
          ))}
        </div>

        <footer className="sheet-footer">
          <div className="title-block">
            {plan.titleBlock.map((line) => (
              <span key={line}>{line}</span>
            ))}
          </div>
          <div className="ann-strip">
            {plan.annotations.slice(0, 3).map((a) => (
              <p key={a.text}>
                <strong>{a.kind}</strong> {a.text}
              </p>
            ))}
          </div>
        </footer>
      </div>
    </div>
  )
}

export default function App() {
  const [partId, setPartId] = useState(SAMPLE_PARTS[0].id)
  const [kind, setKind] = useState<DrawingKind>('cut')
  const [sheet, setSheet] = useState<SheetFormat>('a3')
  const [extra, setExtra] = useState('')
  const [busy, setBusy] = useState(false)
  const [plan, setPlan] = useState<DrawingPlan | null>(null)
  const [report, setReport] = useState('')
  const [revealed, setRevealed] = useState(false)

  const part = useMemo(
    () => SAMPLE_PARTS.find((p) => p.id === partId) as SamplePart,
    [partId],
  )

  const suggested = recommendKinds(part)

  useEffect(() => {
    if (!suggested.includes(kind)) setKind(suggested[0])
  }, [partId]) // eslint-disable-line react-hooks/exhaustive-deps

  async function runAutoGbt(nextKind: DrawingKind = kind) {
    setBusy(true)
    setRevealed(false)
    await new Promise((r) => setTimeout(r, 650))
    const next = planDrawing(part, nextKind, sheet, extra)
    setPlan(next)
    setReport(explain(part, next))
    setKind(nextKind)
    setBusy(false)
    requestAnimationFrame(() => setRevealed(true))
  }

  useEffect(() => {
    void runAutoGbt('cut')
    // İlk yüklemede örnek plan
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [])

  return (
    <div className="page">
      <header className="hero">
        <nav className="nav">
          <span className="brand-mark" aria-hidden="true" />
          <span className="nav-brand">AutoGBT</span>
          <span className="nav-side">SolidWorks Eklentisi</span>
        </nav>

        <div className="hero-copy">
          <p className="eyebrow">Teknik resim asistanı</p>
          <h1 className="brand-hero">AutoGBT</h1>
          <p className="lede">
            Katı modellerinizden büküm, kesim ve işleme teknik resimlerini
            detaylı görünüşler, tablolar ve üretim notlarıyla otomatik üretir.
          </p>
          <div className="cta-row">
            <a className="btn primary" href="#studio">
              Stüdyoyu dene
            </a>
            <a className="btn ghost" href="#install">
              SolidWorks kurulumu
            </a>
          </div>
        </div>

        <div className="hero-visual" aria-hidden="true">
          <div className="hero-grid" />
          <div className="hero-plate">
            <span>BÜKÜM</span>
            <span>KESİM</span>
            <span>İŞLEME</span>
          </div>
        </div>
      </header>

      <main>
        <section id="studio" className="studio">
          <div className="studio-head">
            <h2>AutoGBT stüdyosu</h2>
            <p>Örnek bir parçayı seçin; AutoGBT teknik resim planını oluştursun.</p>
          </div>

          <div className="studio-layout">
            <aside className="controls">
              <label>
                Örnek parça
                <select value={partId} onChange={(e) => setPartId(e.target.value)}>
                  {SAMPLE_PARTS.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.name}
                    </option>
                  ))}
                </select>
              </label>

              <div className="part-card-lite">
                <p>{part.description}</p>
                <ul>
                  <li>Malzeme: {part.material}</li>
                  <li>Sac: {part.isSheetMetal ? `Evet · ${part.thicknessMm} mm` : 'Hayır'}</li>
                  <li>
                    Büküm {part.bendCount} · Delik {part.holeCount} · Özellik {part.featureCount}
                  </li>
                  <li>
                    Kutu {part.box.x}×{part.box.y}×{part.box.z} mm
                  </li>
                </ul>
                <p className="suggest">
                  Öneri:{' '}
                  {suggested.map((k) => toTurkish(k)).join(' · ')}
                </p>
              </div>

              <div className="kind-grid" role="tablist" aria-label="Resim türü">
                {KIND_LABELS.map((k) => (
                  <button
                    key={k.id}
                    type="button"
                    role="tab"
                    aria-selected={kind === k.id}
                    className={kind === k.id ? 'kind active' : 'kind'}
                    onClick={() => void runAutoGbt(k.id)}
                  >
                    <strong>{k.label}</strong>
                    <span>{k.blurb}</span>
                  </button>
                ))}
              </div>

              <label>
                Sayfa formatı
                <select
                  value={sheet}
                  onChange={(e) => setSheet(e.target.value as SheetFormat)}
                >
                  <option value="a3">A3 Yatay</option>
                  <option value="a4-land">A4 Yatay</option>
                  <option value="a4-port">A4 Dikey</option>
                  <option value="a2">A2 Yatay</option>
                </select>
              </label>

              <label>
                Ek talimat
                <textarea
                  rows={3}
                  placeholder="Örn. Kritik deliklere ±0.05 konum toleransı ekle"
                  value={extra}
                  onChange={(e) => setExtra(e.target.value)}
                />
              </label>

              <button
                type="button"
                className="btn primary wide"
                disabled={busy}
                onClick={() => void runAutoGbt(kind)}
              >
                {busy ? 'AutoGBT planlıyor…' : 'Teknik resmi oluştur'}
              </button>
            </aside>

            <div className={`preview${revealed ? ' revealed' : ''}${busy ? ' busy' : ''}`}>
              {plan ? <DrawingSheet plan={plan} part={part} /> : null}
              {busy && <div className="busy-veil">AutoGBT modelı okuyor…</div>}
            </div>
          </div>

          {report && (
            <pre className="report" aria-live="polite">
              {report}
            </pre>
          )}
        </section>

        <section className="features">
          <h2>Eklentide ne var?</h2>
          <div className="feature-row">
            <article>
              <h3>AutoGBT motoru</h3>
              <p>
                Parçayı analiz eder; sac metal, büküm, delik ve kutu bilgisinden
                optimal görünüş yerleşimini ve notları üretir.
              </p>
            </article>
            <article>
              <h3>Üç resim türü</h3>
              <p>
                Büküm tablolu büküm resmi, flat pattern kesim resmi ve toleranslı
                işleme resmi — tek tıkla SolidWorks çizimine aktarılır.
              </p>
            </article>
            <article>
              <h3>Görev paneli</h3>
              <p>
                SolidWorks içinde AutoGBT paneli: analiz, önizleme, sayfa formatı
                ve üretim seçenekleri.
              </p>
            </article>
          </div>
        </section>

        <section id="install" className="install">
          <h2>SolidWorks’e kurulum</h2>
          <ol>
            <li>Windows’ta Visual Studio 2022 + SolidWorks API redistributable kurun.</li>
            <li>
              <code>AutoGBT.sln</code> çözümünü <strong>Release | x64</strong> olarak derleyin.
            </li>
            <li>
              Yönetici olarak <code>scripts/install-addin.bat</code> çalıştırın.
            </li>
            <li>
              SolidWorks → Tools → Add-ins → <em>AutoGBT Teknik Resim</em> etkinleştirin.
            </li>
            <li>
              Bir parçayı kaydedin; komut çubuğundan Büküm / Kesim / İşleme seçin.
            </li>
          </ol>
          <p className="note">
            Bu sayfa, eklentinin AutoGBT planlayıcısını tarayıcıda gösterir.
            Gerçek .SLDDRW üretimi SolidWorks API üzerinden Windows’ta çalışır.
          </p>
        </section>
      </main>

      <footer className="site-footer">
        <span>AutoGBT</span>
        <span>Büküm · Kesim · İşleme</span>
      </footer>
    </div>
  )
}
