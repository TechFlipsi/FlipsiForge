// FlipsiForge Server v0.2.0 — API Wrapper
// Alle /api/* Endpoints als fetch-Wrapper. Vanilla JS, keine Abhängigkeiten.
// (c) 2026 TechFlipsi / Fabian Kirchweger — GPL-3.0

const Api = (() => {
  const BASE = '';
  const JSON_HEADERS = { 'Content-Type': 'application/json' };

  async function req(method, path, body) {
    const opts = { method, headers: JSON_HEADERS };
    if (body !== undefined) opts.body = JSON.stringify(body);
    const res = await fetch(BASE + path, opts);
    if (res.status === 204) return null;
    const ct = res.headers.get('content-type') || '';
    if (ct.includes('application/json')) {
      const data = await res.json();
      if (!res.ok) throw { status: res.status, body: data };
      return data;
    }
    const text = await res.text();
    if (!res.ok) throw { status: res.status, body: text };
    return text;
  }

  return {
    // Health
    health: () => req('GET', '/api/health'),

    // Settings
    getSettings: () => req('GET', '/api/settings'),
    putSettings: (s) => req('PUT', '/api/settings', s),
    patchSettings: (field, value) => req('PATCH', `/api/settings/${field}`, value),

    // Printers
    listPrinters: (includeInactive = false) => req('GET', `/api/printers?includeInactive=${includeInactive}`),
    getPrinter: (id) => req('GET', `/api/printers/${id}`),
    addPrinter: (p) => req('POST', '/api/printers', p),
    updatePrinter: (id, p) => req('PUT', `/api/printers/${id}`, p),
    activatePrinter: (id) => req('PATCH', `/api/printers/${id}/activate`),
    deletePrinter: (id, keepHistory = true) => req('DELETE', `/api/printers/${id}?keepHistory=${keepHistory}`),

    // Printer maintenance + live
    listMaintenance: (id) => req('GET', `/api/printers/${id}/maintenance`),
    addMaintenance: (id, body) => req('POST', `/api/printers/${id}/maintenance`, body),
    maintenanceRecommendations: (id, onlineMode = false) => req('GET', `/api/printers/${id}/maintenance/recommendations?onlineMode=${onlineMode}`),
    connectPrinter: (id) => req('POST', `/api/printers/${id}/connect`),
    printerStatus: (id) => req('GET', `/api/printers/${id}/status`),
    printerTemps: (id) => req('GET', `/api/printers/${id}/temps`),
    printerJob: (id) => req('GET', `/api/printers/${id}/job`),

    // Spools
    listSpools: (includeArchived = false) => req('GET', `/api/spools?includeArchived=${includeArchived}`),
    getSpool: (id) => req('GET', `/api/spools/${id}`),
    addSpool: (s) => req('POST', '/api/spools', s),
    updateSpool: (id, s) => req('PUT', `/api/spools/${id}`, s),
    setSpoolStatus: (id, status) => req('PATCH', `/api/spools/${id}/status`, { status }),
    deleteSpool: (id, keepHistory = true) => req('DELETE', `/api/spools/${id}?keepHistory=${keepHistory}`),

    // Filament DB
    listFilamentBrands: () => req('GET', '/api/filament-brands'),
    filterFilamentBrands: (brand) => req('GET', `/api/filament-brands/${encodeURIComponent(brand)}`),

    // Print jobs / history
    listPrintJobs: () => req('GET', '/api/print-jobs'),
    addPrintJob: (j) => req('POST', '/api/print-jobs', j),
    listPrintHistory: () => req('GET', '/api/print-history'),

    // Files
    listFiles: (extension, folder) => {
      const params = new URLSearchParams();
      if (extension) params.set('extension', extension);
      if (folder) params.set('folder', folder);
      const q = params.toString();
      return req('GET', `/api/files${q ? '?' + q : ''}`);
    },
    scanFiles: (body) => req('POST', '/api/files/scan', body),
    getFile: (id) => req('GET', `/api/files/${id}`),
    toggleFavorite: (id) => req('POST', `/api/files/${id}/favorite`),
    logUsage: (id, action) => req('POST', `/api/files/${id}/usage`, { action }),
    deleteFile: (id) => req('DELETE', `/api/files/${id}`),
    searchFiles: (q) => req('GET', `/api/files/search?q=${encodeURIComponent(q)}`),

    // AI
    aiStatus: () => req('GET', '/api/ai/status'),
    aiEmbed: (text) => req('POST', '/api/ai/embed', { text }),
    aiChat: (message, history = []) => req('POST', '/api/ai/chat', { message, history }),
    aiSlicerProfile: (printerId, spoolId, goal) => req('POST', '/api/ai/slicer-profile', { printerId, spoolId, goal }),

    // Bot
    botMessages: () => req('GET', '/api/bot/messages'),
    botDismiss: () => req('POST', '/api/bot/dismiss'),
    patchBotSettings: (patch) => req('PATCH', '/api/bot/settings', patch),

    // Backup / Restore
    createBackup: () => req('POST', '/api/backup'),
    listBackups: () => req('GET', '/api/backup/list'),
    restore: (backupPath) => req('POST', '/api/restore', { backupPath }),

    // Export / Cache
    exportAll: () => req('POST', '/api/export'),
    clearCache: () => req('DELETE', '/api/cache'),

    // Statistics
    statistics: () => req('GET', '/api/statistics'),
    filesStatistics: () => req('GET', '/api/statistics/files'),
    filamentStatistics: () => req('GET', '/api/statistics/filament'),
  };
})();

window.Api = Api;