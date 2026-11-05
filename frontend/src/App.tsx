import { lazy, Suspense, useEffect, useState, type FormEvent } from 'react';
import { defectLabel, getJson, number, percent, time, type Alarm, type Batch, type Defect,
  type Evidence, type LinesResponse, type Metrics, type Trend, type Window } from './api';

const QualityChart = lazy(() => import('./QualityChart').then(module => ({ default: module.QualityChart })));

type Dashboard = {
  lineId: number; key: string; metrics: Evidence<Metrics>; trend: Evidence<Trend[]>;
  defects: Evidence<Defect[]>; alarms: Evidence<Alarm[]>; batches: Evidence<Batch[]>;
};
const inputTime = (value: string) => new Date(value).toISOString().slice(0, 16);

export function App() {
  const [catalog, setCatalog] = useState<LinesResponse | null>(null);
  const [lineId, setLineId] = useState<number | null>(null);
  const [window, setWindow] = useState<Window | null>(null);
  const [from, setFrom] = useState('');
  const [to, setTo] = useState('');
  const [dashboard, setDashboard] = useState<Dashboard | null>(null);
  const [error, setError] = useState('');
  const [catalogError, setCatalogError] = useState('');
  const [filterError, setFilterError] = useState('');
  const [refresh, setRefresh] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    let active = true;
    setCatalogError('');
    const timeout = setTimeout(() => { controller.abort(); if (active) setCatalogError('Produktionslinien konnten nicht geladen werden.'); }, 10000);
    void getJson<LinesResponse>('/api/lines', controller.signal).then(data => {
      if (!active) return;
      setCatalog(data);
      setLineId(current => data.items.some(line => line.id === current) ? current :
        data.items.find(line => line.id === data.scenarioLineId)?.id ?? data.items[0]?.id ?? null);
      setWindow(current => current ?? data.defaultWindow);
      setFrom(current => current || inputTime(data.defaultWindow.from));
      setTo(current => current || inputTime(data.defaultWindow.to));
    }).catch(() => { if (active && !controller.signal.aborted) setCatalogError('Produktionslinien konnten nicht geladen werden.'); })
      .finally(() => clearTimeout(timeout));
    return () => { active = false; controller.abort(); clearTimeout(timeout); };
  }, [refresh]);

  useEffect(() => {
    if (lineId === null || !window) return;
    const controller = new AbortController();
    let active = true;
    setDashboard(null);
    setError('');
    const timeout = setTimeout(() => { controller.abort(); if (active) setError('Die Datenabfrage hat zu lange gedauert. Bitte erneut versuchen.'); }, 10000);
    const key = `${window.from}/${window.to}`;
    const query = new URLSearchParams({ from: window.from, to: window.to });
    const base = `/api/lines/${lineId}`;
    void Promise.all([
      getJson<Evidence<Metrics>>(`${base}/metrics?${query}`, controller.signal),
      getJson<Evidence<Trend[]>>(`${base}/quality-trend?${query}`, controller.signal),
      getJson<Evidence<Defect[]>>(`${base}/defects?${query}`, controller.signal),
      getJson<Evidence<Alarm[]>>(`${base}/alarms?${query}`, controller.signal),
      getJson<Evidence<Batch[]>>(`${base}/batches?${query}`, controller.signal),
    ]).then(([metrics, trend, defects, alarms, batches]) => {
      if (active) setDashboard({ lineId, key, metrics, trend, defects, alarms, batches });
    }).catch(() => { if (active && !controller.signal.aborted) setError('Produktionsdaten sind derzeit nicht verfügbar. Bitte erneut versuchen.'); })
      .finally(() => clearTimeout(timeout));
    return () => { active = false; controller.abort(); clearTimeout(timeout); };
  }, [lineId, window, refresh]);

  function applyWindow(event: FormEvent) {
    event.preventDefault();
    const start = new Date(`${from}Z`);
    const end = new Date(`${to}Z`);
    if (!Number.isFinite(start.valueOf()) || !Number.isFinite(end.valueOf()) || start >= end || end.valueOf() - start.valueOf() > 7 * 86400000) {
      setFilterError('Bitte einen gültigen Zeitraum von höchstens 7 Tagen wählen; der Beginn muss vor dem Ende liegen.');
      return;
    }
    setFilterError('');
    setWindow({ from: start.toISOString(), to: end.toISOString() });
  }

  const data = dashboard?.lineId === lineId && dashboard.key === `${window?.from}/${window?.to}` ? dashboard : null;
  const metrics = data?.metrics.data;
  const points = data?.trend.data ?? [];
  const last = points.filter(point => point.fpyPercent !== null).at(-1);
  const defects = data?.defects.data ?? [];
  const aoiDefects = defects.filter(defect => defect.stage === 'AOI');
  const maxDefects = Math.max(1, ...aoiDefects.map(defect => defect.count));
  const problem = catalogError || error;
  const loading = !problem && !data && (!catalog || catalog.items.length > 0);
  const lineName = catalog?.items.find(line => line.id === lineId)?.name;

  return <main>
    <header><span className="wordmark">MA / MANUFACTURING ASSISTANT</span><span className="edition">QUALITY OBSERVATORY · PHASE 02</span></header>
    <section className="intro"><div><p className="eyebrow">SMT PRODUCTION / QUALITÄTSÜBERSICHT</p><h1>Qualität im Verlauf.</h1><p className="description">Erstprüfung, Pastenbild und Maschinenereignisse.<br />Eine gemeinsame Sicht auf nachvollziehbare Daten.</p></div>
      <div className="demo-stamp"><span className="eyebrow">SYNTHETISCHE DEMODATEN</span><strong>{catalog?.demoDate ?? '—'}</strong><span>Alle Zeiten in UTC</span></div>
    </section>
    <form className="filters" onSubmit={applyWindow}>
      <label>Produktionslinie<select value={lineId ?? ''} disabled={!catalog} onChange={event => setLineId(Number(event.target.value))}>
        {!catalog && <option value="">Laden …</option>}{catalog?.items.map(line => <option key={line.id} value={line.id}>{line.name}</option>)}
      </select></label>
      <label>Von (UTC, inklusive)<input type="datetime-local" required value={from} onChange={event => setFrom(event.target.value)} /></label>
      <label>Bis (UTC, exklusive)<input type="datetime-local" required value={to} onChange={event => setTo(event.target.value)} /></label>
      <button type="submit" disabled={!catalog}>Zeitraum anwenden <span aria-hidden="true">↗</span></button>
    </form>
    {filterError && <p role="alert" className="error-message">{filterError}</p>}
    <div className="section-heading"><h2>{lineName ?? 'Produktionslinien'}</h2><p role="status">{loading ? 'Daten werden geladen …' : problem ? 'Datenverbindung unterbrochen' : catalog?.items.length === 0 ? 'Keine Linien verfügbar' : 'API & Datenbank erreichbar'}
      {data && window && <span> · {time(window.from)}–{time(window.to)} UTC</span>}</p></div>
    {problem && <div role="alert" className="error-message">{problem} <button onClick={() => setRefresh(value => value + 1)}>Erneut versuchen</button></div>}
    {!problem && catalog && catalog.items.length === 0 && <p>Keine Produktionslinien vorhanden.</p>}
    {!problem && data && metrics && <>
      {metrics.eligibleUnits === 0 && <p className="empty" role="status">Keine Erstprüfungen im gewählten Zeitraum. FPY ist nicht verfügbar.</p>}
      <section className="kpis" aria-label="Kennzahlen im ausgewählten Zeitraum">
        <article className="kpi primary"><p className="eyebrow">FIRST PASS YIELD / GESAMTFENSTER</p><strong data-testid="fpy">{percent(metrics.fpyPercent)}</strong><p>{number(metrics.firstPassAcceptedUnits)} / {number(metrics.eligibleUnits)} Einheiten erstgeprüft akzeptiert</p></article>
        <article className="kpi"><p className="eyebrow">LETZTE STUNDE MIT DATEN</p><strong>{percent(last?.fpyPercent ?? null)}</strong><p>{last ? `${time(last.timestamp)} UTC · ${number(last.eligibleUnits)} Einheiten` : 'Keine AOI-Erstprüfungen'}</p></article>
        <article className="kpi"><p className="eyebrow">SPI-ANOMALIEN</p><strong>{number(metrics.spiAnomalyUnits)}<small> / {number(metrics.spiInspectedUnits)}</small></strong><p>{percent(metrics.spiAnomalyPercent)} der SPI-Erstprüfungen</p></article>
        <article className="kpi"><p className="eyebrow">MASCHINENALARME</p><strong>{number(metrics.alarmCount)}</strong><p>Im ausgewählten Zeitraum</p></article>
      </section>
      <div className="analysis-grid">
        <section className="panel trend-panel"><div className="panel-title"><div><p className="eyebrow">01 / ZEITVERLAUF</p><h2>FPY & Pastenprüfung</h2></div><span className="tag">STÜNDLICH · UTC</span></div>
          {metrics.eligibleUnits > 0 || metrics.spiInspectedUnits > 0 ? <Suspense fallback={<p>Diagramm wird geladen …</p>}><QualityChart points={points} /></Suspense> : <p className="empty">Keine Trenddaten in diesem Zeitraum.</p>}
          <p className="chart-note">FPY: Prozent auf der linken Achse. SPI: Anzahl auffälliger Einheiten auf der rechten Achse.</p>
          <details><summary>Trenddaten und Stichproben anzeigen</summary><div className="table-scroll"><table><caption className="sr-only">Stündliche Erstprüfungen</caption><thead><tr><th>Stunde UTC</th><th>AOI akzeptiert / geprüft</th><th>FPY</th><th>SPI auffällig / geprüft</th></tr></thead><tbody>{points.map(point => <tr key={point.timestamp}><td>{time(point.timestamp)}</td><td>{number(point.firstPassAcceptedUnits)} / {number(point.eligibleUnits)}</td><td>{percent(point.fpyPercent)}</td><td>{number(point.spiAnomalyUnits)} / {number(point.spiInspectedUnits)}</td></tr>)}</tbody></table></div></details>
        </section>
        <section className="panel"><p className="eyebrow">02 / FEHLERBILD</p><h2>AOI-Defektverteilung</h2><p className="panel-note">Ein primärer Defekt je fehlgeschlagener Erstprüfung.</p>
          {aoiDefects.length ? <ul className="defect-list">{aoiDefects.map(defect => <li key={defect.code}><div><span>{defectLabel[defect.code] ?? defect.code}</span><strong>{number(defect.count)}</strong></div><div className="bar-track"><span style={{ width: `${100 * defect.count / maxDefects}%` }} /></div></li>)}</ul> : <p className="empty">Keine AOI-Defekte im Zeitraum.</p>}
          <div className="spi-note"><p className="panel-note">Pastenvolumen: Mittel {percent(metrics.averagePasteVolumePercent)}, Maximum {percent(metrics.maxPasteVolumePercent)} des Solls.</p><span className="eyebrow">SPI / PRIMÄRES FEHLERBILD</span>{defects.filter(defect => defect.stage === 'SPI').map(defect => <p key={defect.code}>{defectLabel[defect.code] ?? defect.code} <strong>{number(defect.count)}</strong></p>)}{!defects.some(defect => defect.stage === 'SPI') && <p>Keine SPI-Anomalien im Zeitraum.</p>}</div>
        </section>
      </div>
      <aside className="observation"><span className="eyebrow">EINORDNUNG</span><p>FPY, SPI-Anomalien und Alarme sind zeitlich vergleichbare Beobachtungen. Eine Korrelation belegt keine Ursache. Eine Grundursache wurde nicht nachgewiesen.</p></aside>
      <section className="panel alarms-panel"><div className="panel-title"><div><p className="eyebrow">03 / EREIGNISPROTOKOLL</p><h2>Maschinenalarme</h2></div><span className="tag">{number(data.alarms.data.length)} EREIGNISSE</span></div>
        {data.alarms.data.length ? <div className="table-scroll alarm-scroll"><table><caption className="sr-only">Maschinenalarme mit Batch-Referenz</caption><thead><tr><th>Zeit UTC</th><th>Maschine</th><th>Ereignis / Quelle</th><th>Schweregrad</th><th>Batch</th></tr></thead><tbody>{data.alarms.data.map(alarm => <tr key={alarm.id}><td>{time(alarm.occurredAt)}</td><td className="machine-code">{alarm.machineCode}</td><td><span title={alarm.message}>{defectLabel[alarm.code] ?? alarm.code}</span><small>Alarm #{alarm.id} · {alarm.code}</small></td><td><span className="warning">{alarm.severity}</span></td><td>{alarm.batchCode}</td></tr>)}</tbody></table></div> : <p className="empty">Keine Maschinenalarme im Zeitraum.</p>}
      </section>
      <details className="panel batches"><summary>04 / Batch-Nachweise ({data.batches.data.length})</summary><p className="panel-note">Batch-FPY und Zähler beziehen sich auf das ausgewählte Zeitfenster; Chargen werden bei zeitlicher Überlappung angezeigt.</p><div className="table-scroll"><table><caption className="sr-only">Produktionsbatches und Erstpass-Ergebnisse</caption><thead><tr><th>Batch</th><th>Produkt</th><th>Start UTC</th><th>Akzeptiert / geprüft</th><th>FPY</th></tr></thead><tbody>{data.batches.data.map(batch => <tr key={batch.id}><td>{batch.code}</td><td>{batch.productCode}</td><td>{time(batch.startedAt)}</td><td>{number(batch.firstPassAcceptedUnits)} / {number(batch.eligibleUnits)}</td><td>{percent(batch.fpyPercent)}</td></tr>)}</tbody></table></div></details>
      <p className="method-note">FPY = erstmals im finalen AOI akzeptierte Einheiten / erstmals im finalen AOI geprüfte Einheiten. Identität: Batch + Seriennummer. Nachprüfungen ändern das Erstpass-Ergebnis nicht. <a href={data.metrics.source} target="_blank" rel="noreferrer">Kennzahlenquelle ↗</a></p>
    </>}
    <footer><span>Nur synthetische Daten. Keine Anlagensteuerung.</span><span>Deterministische Analyse · Keine KI-Diagnose</span></footer>
  </main>;
}
