import { useEffect, useState } from 'react';

type Status = 'loading' | 'healthy' | 'unhealthy' | 'error';

export function App() {
  const [status, setStatus] = useState<Status>('loading');
  const [attempt, setAttempt] = useState(0);
  useEffect(() => {
    const controller = new AbortController();
    let active = true;
    const timeout = setTimeout(() => { controller.abort(); if (active) setStatus('error'); }, 8000);
    setStatus('loading');
    void fetch('/api/health', { signal: controller.signal }).then(async response => {
      if (response.status !== 200 && response.status !== 503) throw new Error('Unexpected response');
      const data: unknown = await response.json();
      if (!data || typeof data !== 'object' || !('status' in data) ||
          (data.status !== 'healthy' && data.status !== 'unhealthy')) throw new Error('Invalid health response');
      if (response.ok !== (data.status === 'healthy')) throw new Error('Inconsistent health response');
      setStatus(data.status);
    }).catch(() => { if (!controller.signal.aborted) setStatus('error'); })
      .finally(() => clearTimeout(timeout));
    return () => { active = false; controller.abort(); clearTimeout(timeout); };
  }, [attempt]);

  const labels: Record<Status, string> = {
    loading: 'Verbindung wird geprüft …', healthy: 'API & Datenbank erreichbar',
    unhealthy: 'Datenbank nicht verfügbar', error: 'API nicht erreichbar',
  };
  return <main>
    <header><span className="wordmark">MA / MANUFACTURING ASSISTANT</span><span className="edition">DEMO · PHASE 01</span></header>
    <section className="intro"><p className="eyebrow">SMT PRODUCTION / SYSTEMSTATUS</p><h1>Die Grundlage<br/>für klare Antworten.</h1><p className="description">Ein Ausgangspunkt für evidenzbasierte Fertigungsanalysen. Die Verbindung zur Produktionsplattform beginnt hier.</p></section>
    <section className="connection" aria-labelledby="connection-title"><div><p className="eyebrow">01 / VERBINDUNG</p><h2 id="connection-title">Plattformstatus</h2></div><div className="status-area"><p role="status" className={`status ${status}`}><span className="indicator" aria-hidden="true"/>{labels[status]}</p><button onClick={() => setAttempt(value => value + 1)} disabled={status === 'loading'}>Erneut prüfen <span aria-hidden="true">↗</span></button></div></section>
    <footer><span>Nur synthetische Daten. Keine Anlagensteuerung.</span><span>Diagnosefunktionen folgen in einer späteren Phase.</span></footer>
  </main>;
}
