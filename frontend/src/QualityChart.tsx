import { Bar, CartesianGrid, ComposedChart, Legend, Line, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { time, type Trend } from './api';

export function QualityChart({ points }: { points: Trend[] }) {
  const lowerBound = Math.max(0, Math.min(90, ...points.flatMap(point => point.fpyPercent === null ? [] : [Math.floor(point.fpyPercent - 1)])));
  return <div className="chart" role="img" aria-label="Stündlicher FPY und Anzahl der SPI-Anomalien; Werte auch in der Trenddatentabelle verfügbar.">
    <ResponsiveContainer width="100%" height="100%" minWidth={0}>
      <ComposedChart data={points} margin={{ top: 16, right: 12, bottom: 8, left: 0 }}>
        <CartesianGrid strokeDasharray="3 5" vertical={false} stroke="#d6d9cd" />
        <XAxis dataKey="timestamp" tickFormatter={time} tick={{ fontSize: 11 }} tickLine={false} axisLine={false} />
        <YAxis yAxisId="fpy" domain={[lowerBound, 100]} unit=" %" tick={{ fontSize: 11 }} tickLine={false} axisLine={false} width={58} />
        <YAxis yAxisId="spi" orientation="right" allowDecimals={false} tick={{ fontSize: 11 }} tickLine={false} axisLine={false} width={42} />
        <Tooltip labelFormatter={label => `${time(String(label))} UTC`} contentStyle={{ borderRadius: 0, border: '1px solid #a5afa0' }} />
        <Legend wrapperStyle={{ fontSize: 12, paddingTop: 12 }} />
        <Bar yAxisId="spi" dataKey="spiAnomalyUnits" name="SPI-Anomalien (Einheiten)" fill="#d9b881" maxBarSize={32} />
        <Line yAxisId="fpy" dataKey="fpyPercent" name="FPY (%)" stroke="#235b45" strokeWidth={3} dot={{ r: 4 }} connectNulls={false} isAnimationActive={false} />
      </ComposedChart>
    </ResponsiveContainer>
  </div>;
}
