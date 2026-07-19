// FlipsiForge Server v0.2.0 — Dashboard Logik
// Vanilla JS — fetcht /api/health, /api/printers, /api/spools, /api/statistics,
// zeigt als Cards/Tabellen. Tab-Wechsel client-seitig.
// (c) 2026 TechFlipsi / Fabian Kirchweger — GPL-3.0

(function () {
  'use strict';

  // === Tab-Navigation ===
  const tabs = document.querySelectorAll('.tab');
  const panels = document.querySelectorAll('.panel');
  tabs.forEach(t => t.addEventListener('click', () => {
    tabs.forEach(x => x.classList.remove('active'));
    panels.forEach(p => p.classList.remove('active'));
    t.classList.add('active');
    document.getElementById('panel-' + t.dataset.tab).classList.add('active');
    loadTab(t.dataset.tab);
  }));

  function loadTab(name) {
    switch (name) {
      case 'printers': loadPrinters(); break;
      case 'spools': loadSpools(); break;
      case 'filament': loadFilament(); break;
      case 'stats': loadStats(); break;
      case 'files': loadFiles(); break;
      case 'system': loadSystem(); break;
    }
  }

  // === Helpers ===
  const fmt = {
    bytes: (n) => {
      if (n < 1024) return n + ' B';
      if (n < 1024 * 1024) return (n / 1024).toFixed(1) + ' KB';
      if (n < 1024 * 1024 * 1024) return (n / (1024 * 1024)).toFixed(1) + ' MB';
      return (n / (1024 * 1024 * 1024)).toFixed(1) + ' GB';
    },
    date: (s) => s ? new Date(s).toLocaleString('de-DE') : '—',
    status: (s) => `<span class="tag">${s}</span>`,
  };

  function clearTbody(id) {
    const tb = document.getElementById(id);
    tb.innerHTML = `<tr><td colspan="99" class="empty">lade…</td></tr>`;
    return tb;
  }
  function emptyTbody(id, msg) {
    document.getElementById(id).innerHTML = `<tr><td colspan="99" class="empty">${msg}</td></tr>`;
  }

  function btn(text, onclick, classes = 'cta small') {
    const b = document.createElement('button');
    b.textContent = text;
    b.className = classes;
    b.addEventListener('click', onclick);
    return b;
  }

  // === Health ===
  async function loadHealth() {
    const el = document.getElementById('health');
    try {
      const h = await Api.health();
      el.classList.add('ok');
      document.getElementById('health-dot').textContent = '●';
      document.getElementById('health-text').textContent = `OK · v${h.version}`;
      document.getElementById('health-mode').textContent = `${h.mode} · AI ${h.ai ? '✓' : '✗'} · UI ${h.webui ? '✓' : '✗'}`;
    } catch (e) {
      el.classList.add('err');
      document.getElementById('health-text').textContent = 'Server nicht erreichbar';
    }
  }

  // === Printers ===
  async function loadPrinters() {
    const tb = clearTbody('printers-tbody');
    try {
      const list = await Api.listPrinters();
      tb.innerHTML = '';
      if (list.length === 0) { emptyTbody('printers-tbody', 'Keine aktiven Drucker. [+ Drucker] zum Hinzufügen.'); return; }
      list.forEach(p => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>${p.id}</td>
          <td>${p.brand}</td>
          <td>${p.model}</td>
          <td>${p.protocol}</td>
          <td>${p.ipAddress || p.usbPort || '—'}</td>
          <td>${p.buildVolumeX}×${p.buildVolumeY}×${p.buildVolumeZ}</td>
          <td>${p.isActive ? '🟢 aktiv' : '⚫ inaktiv'}</td>
          <td class="row"></td>`;
        const actions = tr.querySelector('td.row');
        actions.appendChild(btn('Status', async () => {
          try { const s = await Api.printerStatus(p.id); alert(JSON.stringify(s, null, 2)); }
          catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
        }));
        actions.appendChild(btn('Wartung', async () => {
          try { const r = await Api.maintenanceRecommendations(p.id, false); alert(JSON.stringify(r, null, 2)); }
          catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
        }));
        actions.appendChild(btn('Löschen', async () => {
          if (!confirm(`Drucker "${p.brand} ${p.model}" löschen?\n\nOK = archivieren (IsActive=false)\nAbbrechen = abbrechen`)) return;
          try { await Api.deletePrinter(p.id, true); loadPrinters(); }
          catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
        }, 'cta small danger'));
        tb.appendChild(tr);
      });
    } catch (e) {
      emptyTbody('printers-tbody', 'Fehler beim Laden: ' + JSON.stringify(e));
    }
  }

  document.getElementById('btn-add-printer').addEventListener('click', () => {
    const form = document.getElementById('printer-form');
    form.classList.remove('hidden');
    form.innerHTML = `
      <h3>Drucker hinzufügen</h3>
      <label>Marke <input id="p-brand" type="text" /></label>
      <label>Modell <input id="p-model" type="text" /></label>
      <label>Protokoll
        <select id="p-proto">
          <option>KlipperMoonraker</option><option>Marlin</option>
          <option>BambuLab</option><option>PrusaLink</option><option>OctoPrint</option>
        </select>
      </label>
      <label>IP-Adresse <input id="p-ip" type="text" placeholder="192.168.1.50" /></label>
      <label>Bauvolumen XYZ <input id="p-vol" type="text" placeholder="235×235×275" /></label>
      <div class="actions">
        <button class="cta" id="p-save">Speichern</button>
        <button class="cta ghost" id="p-cancel">Abbrechen</button>
      </div>`;
    document.getElementById('p-cancel').addEventListener('click', () => form.classList.add('hidden'));
    document.getElementById('p-save').addEventListener('click', async () => {
      const [x, y, z] = document.getElementById('p-vol').value.split('×').map(s => parseInt(s) || 0);
      try {
        await Api.addPrinter({
          brand: document.getElementById('p-brand').value,
          model: document.getElementById('p-model').value,
          protocol: document.getElementById('p-proto').value,
          ipAddress: document.getElementById('p-ip').value,
          buildVolumeX: x, buildVolumeY: y, buildVolumeZ: z,
          nozzleDiameter: 0.4,
        });
        form.classList.add('hidden');
        loadPrinters();
      } catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
    });
  });

  // === Spools ===
  async function loadSpools() {
    const tb = clearTbody('spools-tbody');
    try {
      const list = await Api.listSpools();
      tb.innerHTML = '';
      if (list.length === 0) { emptyTbody('spools-tbody', 'Keine aktiven Spulen.'); return; }
      list.forEach(s => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>${s.id}</td>
          <td>${s.brand}</td>
          <td>${s.materialName} (${s.materialType})</td>
          <td><span class="tag" style="background:${s.colorHex}20;color:${s.colorHex}">${s.colorHex}</span></td>
          <td>${s.remainingWeightG}/${s.totalWeightG}g</td>
          <td>${fmt.status(s.status)}</td>
          <td class="row"></td>`;
        const actions = tr.querySelector('td.row');
        actions.appendChild(btn('Archiv', async () => {
          try { await Api.setSpoolStatus(s.id, 'Archived'); loadSpools(); }
          catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
        }));
        actions.appendChild(btn('Leer', async () => {
          try { await Api.setSpoolStatus(s.id, 'Empty'); loadSpools(); }
          catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
        }));
        tb.appendChild(tr);
      });
    } catch (e) {
      emptyTbody('spools-tbody', 'Fehler: ' + JSON.stringify(e));
    }
  }

  document.getElementById('btn-add-spool').addEventListener('click', async () => {
    const brand = prompt('Marke?');
    if (!brand) return;
    const material = prompt('Material? (PLA, PETG, TPU, ABS, ASA, PC, Other)', 'PLA');
    try {
      await Api.addSpool({
        brand, materialName: material, materialType: material,
        colorHex: '#000000', totalWeightG: 1000, remainingWeightG: 1000, densityGcm3: 1.24,
      });
      loadSpools();
    } catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
  });

  // === Filament-DB ===
  let allFilament = [];
  async function loadFilament() {
    const tb = clearTbody('filament-tbody');
    try {
      if (allFilament.length === 0) allFilament = await Api.listFilamentBrands();
      document.getElementById('filament-count').textContent = allFilament.length;
      renderFilament(allFilament);
    } catch (e) {
      emptyTbody('filament-tbody', 'Fehler: ' + JSON.stringify(e));
    }
  }
  function renderFilament(list) {
    const tb = document.getElementById('filament-tbody');
    tb.innerHTML = '';
    if (list.length === 0) { emptyTbody('filament-tbody', 'Keine Einträge.'); return; }
    list.forEach(b => {
      const tr = document.createElement('tr');
      const props = [];
      if (b.isUVResistant) props.push('UV');
      if (b.isFoodSafe) props.push('Food');
      if (b.isFlexible) props.push('Flex');
      if (b.isHeatResistant) props.push('Hitze');
      if (b.needsEnclosure) props.push('Enclosure');
      tr.innerHTML = `
        <td>${b.brand}</td>
        <td>${b.productName}</td>
        <td>${b.materialType}</td>
        <td>${b.hotendMin}-${b.hotendMax}°C</td>
        <td>${b.bedMin}-${b.bedMax}°C</td>
        <td>${b.speedMin}-${b.speedMax}mm/s</td>
        <td>${b.layerHeightMin}-${b.layerHeightMax}mm</td>
        <td>${props.map(p => `<span class="tag">${p}</span>`).join(' ')}</td>`;
      tb.appendChild(tr);
    });
  }
  document.getElementById('filament-filter').addEventListener('input', (e) => {
    const q = e.target.value.toLowerCase();
    renderFilament(allFilament.filter(b =>
      b.brand.toLowerCase().includes(q) || b.productName.toLowerCase().includes(q)));
  });

  // === Stats ===
  async function loadStats() {
    const grid = document.getElementById('stats-grid');
    const fgrid = document.getElementById('filament-stats-grid');
    const fileGrid = document.getElementById('files-stats-grid');
    grid.innerHTML = '<div class="stat-card"><div class="label">lade…</div></div>';
    fgrid.innerHTML = '';
    fileGrid.innerHTML = '';
    try {
      const s = await Api.statistics();
      grid.innerHTML = '';
      grid.appendChild(statCard('Drucke gesamt', s.totalPrints));
      grid.appendChild(statCard('Erfolgreich', s.successfulPrints));
      grid.appendChild(statCard('Erfolgsquote', s.successRate.toFixed(1) + '%'));
      grid.appendChild(statCard('Filament gesamt', s.totalFilamentG + 'g'));
      grid.appendChild(statCard('Kosten gesamt', '€' + s.totalCostEur));

      const f = await Api.filamentStatistics();
      fgrid.appendChild(statCard('Spulen gesamt', f.totalSpools));
      fgrid.appendChild(statCard('Restgewicht', f.totalRemainingG + 'g'));
      fgrid.appendChild(statCard('Verbraucht', f.totalConsumedG + 'g'));
      fgrid.appendChild(statCard('Aktiv', f.activeCount));
      fgrid.appendChild(statCard('Leer', f.emptyCount));

      const fi = await Api.filesStatistics();
      fileGrid.appendChild(statCard('Dateien gesamt', fi.total));
      fileGrid.appendChild(statCard('Favoriten', fi.favoritesCount));
      fileGrid.appendChild(statCard('Größe gesamt', fmt.bytes(fi.totalSizeBytes)));
    } catch (e) {
      grid.innerHTML = `<div class="stat-card"><div class="label">Fehler</div><div class="value">${JSON.stringify(e)}</div></div>`;
    }
  }
  function statCard(label, value, sub) {
    const d = document.createElement('div');
    d.className = 'stat-card';
    d.innerHTML = `<div class="label">${label}</div><div class="value">${value}</div>${sub ? `<div class="sub">${sub}</div>` : ''}`;
    return d;
  }

  // === Files ===
  async function loadFiles() {
    const tb = clearTbody('files-tbody');
    try {
      const list = await Api.listFiles();
      tb.innerHTML = '';
      if (list.length === 0) { emptyTbody('files-tbody', 'Keine Dateien. Ordner-Pfad eingeben und [Scan starten] klicken.'); return; }
      list.forEach(f => {
        const fav = (f.tags || '').includes('★');
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>${f.id}</td>
          <td>${f.fileName}</td>
          <td><span class="tag">${f.extension}</span></td>
          <td title="${f.path}">${f.path.length > 50 ? '…' + f.path.slice(-50) : f.path}</td>
          <td>${fmt.bytes(f.fileSizeBytes)}</td>
          <td>${fmt.date(f.lastModified)}</td>
          <td><span class="${fav ? 'fav-on' : 'fav-off'}" data-id="${f.id}">★</span></td>`;
        tb.appendChild(tr);
      });
      tb.querySelectorAll('.fav-off, .fav-on').forEach(el => {
        el.addEventListener('click', async () => {
          try { await Api.toggleFavorite(parseInt(el.dataset.id)); loadFiles(); }
          catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
        });
      });
    } catch (e) {
      emptyTbody('files-tbody', 'Fehler: ' + JSON.stringify(e));
    }
  }

  document.getElementById('btn-scan').addEventListener('click', async () => {
    const folder = document.getElementById('files-folder').value.trim();
    if (!folder) { alert('Bitte Ordner-Pfad eingeben'); return; }
    try {
      const r = await Api.scanFiles({ folders: [folder], extensions: [] });
      alert(`Scan fertig: ${r.newFiles} neue, ${r.updatedFiles} aktualisierte Dateien`);
      loadFiles();
    } catch (e) { alert('Fehler: ' + JSON.stringify(e)); }
  });
  document.getElementById('files-search').addEventListener('input', async (e) => {
    const q = e.target.value.trim();
    if (q.length < 3) { if (q.length === 0) loadFiles(); return; }
    try {
      const r = await Api.searchFiles(q);
      const tb = document.getElementById('files-tbody');
      tb.innerHTML = '';
      [...r.filenameMatches, ...r.aiMatches].forEach(m => {
        const tr = document.createElement('tr');
        tr.innerHTML = `
          <td>${m.file.id}</td>
          <td>${m.file.fileName} ${m.badge ? `<span class="tag ai">${m.badge}</span>` : ''}</td>
          <td><span class="tag">${m.file.extension}</span></td>
          <td>${m.file.path}</td>
          <td>${fmt.bytes(m.file.fileSizeBytes)}</td>
          <td>${fmt.date(m.file.lastModified)}</td>
          <td></td>`;
        tb.appendChild(tr);
      });
      if (tb.children.length === 0) emptyTbody('files-tbody', `Keine Treffer für "${q}"`);
    } catch (e) { /* ignore */ }
  });

  // === System ===
  async function loadSystem() {
    const form = document.getElementById('settings-form');
    try {
      const s = await Api.getSettings();
      form.AiModel.value = s.aiModel || 'Auto';
      form.AiEnabled.checked = s.aiEnabled;
      form.ServerMode.value = s.serverMode || 'Full';
      form.WebUiEnabled.checked = s.webUiEnabled;
      form.Language.value = s.language || 'de';
      form.WatchFolders.value = (s.watchFolders || []).join(',');
      form.NextcloudUrl.value = s.nextcloudUrl || '';
      form.NextcloudUser.value = s.nextcloudUser || '';
      form.NextcloudPassword.value = s.nextcloudPassword || '';
      form.TelegramBotToken.value = s.telegramBotToken || '';
      form.TelegramChatId.value = s.telegramChatId || '';
    } catch (e) {
      document.getElementById('system-out').textContent = 'Fehler: ' + JSON.stringify(e);
    }
  }
  document.getElementById('settings-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const f = e.target;
    const body = {
      id: 1,
      aiModel: f.AiModel.value,
      aiEnabled: f.AiEnabled.checked,
      serverMode: f.ServerMode.value,
      webUiEnabled: f.WebUiEnabled.checked,
      language: f.Language.value,
      watchFolders: f.WatchFolders.value.split(',').map(s => s.trim()).filter(Boolean),
      nextcloudUrl: f.NextcloudUrl.value || null,
      nextcloudUser: f.NextcloudUser.value || null,
      nextcloudPassword: f.NextcloudPassword.value || null,
      telegramBotToken: f.TelegramBotToken.value || null,
      telegramChatId: f.TelegramChatId.value ? parseInt(f.TelegramChatId.value) : null,
    };
    try {
      const r = await Api.putSettings(body);
      document.getElementById('system-out').textContent = '✓ Gespeichert: ' + JSON.stringify(r, null, 2);
    } catch (e) {
      document.getElementById('system-out').textContent = '✗ Fehler: ' + JSON.stringify(e);
    }
  });
  document.getElementById('btn-export').addEventListener('click', async () => {
    try {
      const r = await Api.exportAll();
      document.getElementById('system-out').textContent = JSON.stringify(r, null, 2).slice(0, 5000) + '\n… (gekürzt)';
    } catch (e) { document.getElementById('system-out').textContent = '✗ ' + JSON.stringify(e); }
  });
  document.getElementById('btn-backup').addEventListener('click', async () => {
    try {
      const r = await Api.createBackup();
      document.getElementById('system-out').textContent = '✓ Backup: ' + JSON.stringify(r, null, 2);
    } catch (e) { document.getElementById('system-out').textContent = '✗ ' + JSON.stringify(e); }
  });
  document.getElementById('btn-cache-clear').addEventListener('click', async () => {
    if (!confirm('Cache leeren (Thumbnails, Embeddings, Temp-Files)?')) return;
    try {
      const r = await Api.clearCache();
      document.getElementById('system-out').textContent = '✓ Cache geleert: ' + JSON.stringify(r);
    } catch (e) { document.getElementById('system-out').textContent = '✗ ' + JSON.stringify(e); }
  });

  // === Init ===
  loadHealth();
  loadPrinters();
  setInterval(loadHealth, 30000);
})();