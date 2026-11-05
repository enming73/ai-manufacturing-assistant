export type Window = { from: string; to: string };
export type Line = { id: number; name: string };
export type LinesResponse = { demoDate: string; scenarioLineId: number; defaultWindow: Window; items: Line[] };
export type Evidence<T> = { lineId: number; window: Window; source: string; data: T };
export type Metrics = {
  eligibleUnits: number; firstPassAcceptedUnits: number; fpyPercent: number | null;
  spiInspectedUnits: number; spiAnomalyUnits: number; spiAnomalyPercent: number | null;
  alarmCount: number; averageCycleTimeSeconds: number | null;
  averagePasteVolumePercent: number | null; maxPasteVolumePercent: number | null;
};
export type Trend = { timestamp: string; eligibleUnits: number; firstPassAcceptedUnits: number;
  fpyPercent: number | null; spiInspectedUnits: number; spiAnomalyUnits: number };
export type Defect = { stage: string; code: string; count: number };
export type Alarm = { id: number; machineId: number; machineCode: string; batchId: number;
  batchCode: string; occurredAt: string; code: string; severity: string; message: string };
export type Batch = { id: number; code: string; productCode: string; startedAt: string; completedAt: string;
  plannedUnits: number; eligibleUnits: number; firstPassAcceptedUnits: number; fpyPercent: number | null };

export async function getJson<T>(path: string, signal: AbortSignal): Promise<T> {
  const response = await fetch(path, { signal, headers: { Accept: 'application/json' } });
  if (!response.ok) throw new Error(`HTTP ${response.status}`);
  return response.json() as Promise<T>;
}

export const number = (value: number) => value.toLocaleString('de-DE');
export const percent = (value: number | null) => value === null ? '—' :
  `${value.toLocaleString('de-DE', { minimumFractionDigits: 2, maximumFractionDigits: 2 })} %`;
export const time = (value: string) => new Date(value).toLocaleTimeString('de-DE', {
  timeZone: 'UTC', hour: '2-digit', minute: '2-digit',
});
export const defectLabel: Record<string, string> = {
  INSUFFICIENT_SOLDER: 'Zu wenig Lot', SOLDER_BRIDGE: 'Lötbrücke', COMPONENT_OFFSET: 'Bauteilversatz',
  PASTE_VOLUME_HIGH: 'Pastenvolumen zu hoch', NOZZLE_PICKUP_FAILURE: 'Düsen-Aufnahmefehler', FEEDER_LOW: 'Materialvorrat niedrig',
};
