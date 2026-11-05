import { cleanup, render, screen } from '@testing-library/react';
import { afterEach, expect, it, vi } from 'vitest';
import { App } from './App';

afterEach(() => { cleanup(); vi.unstubAllGlobals(); });

it('fetches backend health through the same-origin API', async () => {
  const fetch = vi.fn().mockResolvedValue({ ok: true, status: 200, json: async () => ({ status: 'healthy' }) });
  vi.stubGlobal('fetch', fetch);
  render(<App />);
  expect(await screen.findByText('API & Datenbank erreichbar')).toBeInTheDocument();
  expect(fetch).toHaveBeenCalledWith('/api/health', expect.objectContaining({ signal: expect.any(AbortSignal) }));
});

it('shows an unhealthy database response', async () => {
  vi.stubGlobal('fetch', vi.fn().mockResolvedValue({ ok: false, status: 503, json: async () => ({ status: 'unhealthy' }) }));
  render(<App />);
  expect(await screen.findByText('Datenbank nicht verfügbar')).toBeInTheDocument();
});

it('shows a controlled connection error', async () => {
  vi.stubGlobal('fetch', vi.fn().mockRejectedValue(new Error('offline')));
  render(<App />);
  expect(await screen.findByText('API nicht erreichbar')).toBeInTheDocument();
});
