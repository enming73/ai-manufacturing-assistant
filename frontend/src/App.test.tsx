import { cleanup, fireEvent, render, screen, waitFor } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { App } from './App';
import type { Trend } from './api';

vi.mock('./QualityChart', () => ({ QualityChart: ({ points }: { points: Trend[] }) =>
  <div aria-label="Qualitätstrend">{points.length} Datenpunkte</div> }));

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

const from = '2026-10-15T06:00:00+00:00';
const to = '2026-10-15T12:00:00+00:00';
const catalog = { demoDate: '2026-10-15', scenarioLineId: 3, defaultWindow: { from, to },
  items: [{ id: 1, name: 'Line 1' }, { id: 3, name: 'Line 3' }] };

function apiData(path: string) {
  if (path === '/api/lines') return catalog;
  const url = new URL(path, 'http://localhost');
  const lineId = Number(url.pathname.split('/')[3]);
  const empty = url.searchParams.get('from')?.startsWith('2026-10-16');
  const first = lineId === 1;
  const metrics = { eligibleUnits: empty ? 0 : 2000, firstPassAcceptedUnits: empty ? 0 : first ? 1980 : 1942,
    fpyPercent: empty ? null : first ? 99 : 97.1, spiInspectedUnits: empty ? 0 : 2000,
    spiAnomalyUnits: empty ? 0 : 45, spiAnomalyPercent: empty ? null : 2.25,
    alarmCount: empty || first ? 0 : 1, averageCycleTimeSeconds: empty ? null : 2.8,
    averagePasteVolumePercent: empty ? null : 101.2, maxPasteVolumePercent: empty ? null : 143 };
  const resource = url.pathname.split('/').at(-1);
  const data = resource === 'metrics' ? metrics : empty ? [] : resource === 'quality-trend' ? [
    { timestamp: from, eligibleUnits: 1000, firstPassAcceptedUnits: 985, fpyPercent: 98.5, spiInspectedUnits: 1000, spiAnomalyUnits: 20 },
    { timestamp: '2026-10-15T07:00:00Z', eligibleUnits: 1000, firstPassAcceptedUnits: 957, fpyPercent: 95.7, spiInspectedUnits: 1000, spiAnomalyUnits: 25 },
  ] : resource === 'alarms' ? first ? [] : [
    { id: 7, machineId: 32, machineCode: 'M03', batchId: 300, batchCode: 'DEMO-BATCH', occurredAt: from,
      code: 'NOZZLE_PICKUP_FAILURE', severity: 'Warning', message: 'Synthetic' },
  ] : resource === 'defects' ? [{ stage: 'AOI', code: 'SOLDER_BRIDGE', count: 58 }, { stage: 'SPI', code: 'PASTE_VOLUME_HIGH', count: 45 }]
    : [{ id: 300, code: 'DEMO-BATCH', productCode: 'DEMO-PRODUCT', startedAt: from, completedAt: to,
      plannedUnits: 2000, eligibleUnits: 2000, firstPassAcceptedUnits: 1942, fpyPercent: 97.1 }];
  return { lineId, window: { from: url.searchParams.get('from'), to: url.searchParams.get('to') }, source: path, data };
}

function mockApi() {
  const fetch = vi.fn(async (path: string) => ({ ok: true, status: 200, json: async () => apiData(path) }));
  vi.stubGlobal('fetch', fetch);
  return fetch;
}

it('displays database-derived values, fixed demo date, evidence and causal limitation', async () => {
  const fetch = mockApi();
  render(<App />);
  expect(await screen.findByTestId('fpy')).toHaveTextContent('97,10 %');
  expect(screen.getByText('2026-10-15')).toBeInTheDocument();
  expect(screen.getByText('M03')).toBeInTheDocument();
  expect(await screen.findByText('2 Datenpunkte')).toBeInTheDocument();
  expect(screen.getByText(/Eine Korrelation belegt keine Ursache/)).toBeInTheDocument();
  expect(screen.getByText(/Maximum 143,00 %/)).toBeInTheDocument();
  expect(screen.getByRole('link', { name: /Kennzahlenquelle/ })).toHaveAttribute('href', expect.stringContaining('/api/lines/3/metrics?'));
  expect(fetch.mock.calls.every(([path]) => path.startsWith('/api/'))).toBe(true);
});

it('refreshes all evidence when the selected line changes', async () => {
  const fetch = mockApi();
  render(<App />);
  await screen.findByTestId('fpy');
  fireEvent.change(screen.getByLabelText('Produktionslinie'), { target: { value: '1' } });
  await waitFor(() => expect(screen.getByTestId('fpy')).toHaveTextContent('99,00 %'));
  expect(screen.queryByText('M03')).not.toBeInTheDocument();
  expect(screen.getByText('Keine Maschinenalarme im Zeitraum.')).toBeInTheDocument();
  for (const endpoint of ['metrics', 'quality-trend', 'defects', 'alarms', 'batches'])
    expect(fetch.mock.calls.some(([path]) => path.startsWith(`/api/lines/1/${endpoint}?`))).toBe(true);
});

it('applies the same UTC window to every endpoint and shows the empty state', async () => {
  const fetch = mockApi();
  render(<App />);
  await screen.findByTestId('fpy');
  fireEvent.change(screen.getByLabelText('Von (UTC, inklusive)'), { target: { value: '2026-10-16T06:00' } });
  fireEvent.change(screen.getByLabelText('Bis (UTC, exklusive)'), { target: { value: '2026-10-16T07:00' } });
  fireEvent.click(screen.getByRole('button', { name: /Zeitraum anwenden/ }));
  expect(await screen.findByText(/Keine Erstprüfungen im gewählten Zeitraum/)).toBeInTheDocument();
  expect(screen.getByTestId('fpy')).toHaveTextContent('—');
  const windows = fetch.mock.calls.filter(([path]) => path.includes('2026-10-16')).map(([path]) => new URL(path, 'http://localhost').searchParams);
  expect(windows).toHaveLength(5);
  expect(windows.every(query => query.get('from') === '2026-10-16T06:00:00.000Z' && query.get('to') === '2026-10-16T07:00:00.000Z')).toBe(true);
});

it('rejects a reversed window without making new data requests', async () => {
  const fetch = mockApi();
  render(<App />);
  await screen.findByTestId('fpy');
  const count = fetch.mock.calls.length;
  fireEvent.change(screen.getByLabelText('Von (UTC, inklusive)'), { target: { value: '2026-10-16T06:00' } });
  fireEvent.click(screen.getByRole('button', { name: /Zeitraum anwenden/ }));
  expect(screen.getByRole('alert')).toHaveTextContent('Beginn muss vor dem Ende liegen');
  expect(fetch.mock.calls).toHaveLength(count);
});

it('shows a controlled error and can retry loading the line catalog', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));
  render(<App />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Produktionslinien konnten nicht geladen werden');
  mockApi();
  fireEvent.click(screen.getByRole('button', { name: 'Erneut versuchen' }));
  expect(await screen.findByTestId('fpy')).toHaveTextContent('97,10 %');
});

it('hides partial evidence when one dashboard request fails', async () => {
  vi.stubGlobal('fetch', vi.fn(async (path: string) => path.includes('/alarms') ?
    { ok: false, status: 503 } : { ok: true, json: async () => apiData(path) }));
  render(<App />);
  expect(await screen.findByRole('alert')).toHaveTextContent('Produktionsdaten sind derzeit nicht verfügbar');
  expect(screen.queryByTestId('fpy')).not.toBeInTheDocument();
});

it('ignores late responses belonging to a previously selected line', async () => {
  const pending: (() => void)[] = [];
  vi.stubGlobal('fetch', vi.fn((path: string) => path.startsWith('/api/lines/3/') ?
    new Promise(resolve => pending.push(() => resolve({ ok: true, json: async () => apiData(path) }))) :
    Promise.resolve({ ok: true, json: async () => apiData(path) })));
  render(<App />);
  await waitFor(() => expect(screen.getByLabelText('Produktionslinie')).toHaveValue('3'));
  fireEvent.change(screen.getByLabelText('Produktionslinie'), { target: { value: '1' } });
  expect(await screen.findByTestId('fpy')).toHaveTextContent('99,00 %');
  pending.forEach(resolve => resolve());
  await waitFor(() => expect(screen.getByTestId('fpy')).toHaveTextContent('99,00 %'));
});
