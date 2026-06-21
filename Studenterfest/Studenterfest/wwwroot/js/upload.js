(function () {
  const fileInput = document.getElementById('file');
  const grid = document.getElementById('grid');
  const sendBtn = document.getElementById('send');
  const msg = document.getElementById('msg');
  const rvt = document.querySelector('meta[name="rvt"]')?.content || '';
  let queue = []; // { blob, thumbUrl, state }

  fileInput.addEventListener('change', async (e) => {
    const files = [...e.target.files];
    for (const f of files) {
      if (!f.type.startsWith('image/')) continue;
      const item = { blob: null, thumbUrl: URL.createObjectURL(f), state: '' };
      queue.push(item);
      renderGrid();
      try { item.blob = await resize(f); } catch { item.blob = f; }
      renderGrid();
    }
    sendBtn.disabled = queue.length === 0;
    fileInput.value = '';
  });

  function renderGrid() {
    grid.innerHTML = '';
    queue.forEach(it => {
      const d = document.createElement('div');
      d.className = 'thumb';
      d.innerHTML = '<img src="' + it.thumbUrl + '">' + (it.state ? '<div class="st">' + it.state + '</div>' : '');
      grid.appendChild(d);
    });
  }

  // Skalér ned i browseren så uploads er hurtige (respekterer EXIF-rotation)
  async function resize(file, max = 1600, quality = 0.85) {
    const bmp = await createImageBitmap(file, { imageOrientation: 'from-image' });
    let w = bmp.width, h = bmp.height;
    if (w > max || h > max) {
      const s = Math.min(max / w, max / h);
      w = Math.round(w * s); h = Math.round(h * s);
    }
    const c = document.createElement('canvas');
    c.width = w; c.height = h;
    c.getContext('2d').drawImage(bmp, 0, 0, w, h);
    return await new Promise(r => c.toBlob(r, 'image/jpeg', quality));
  }

  sendBtn.addEventListener('click', async () => {
    if (!queue.length) return;
    sendBtn.disabled = true;
    msg.className = 'msg';
    msg.textContent = 'Sender…';
    const who = (document.getElementById('who').value || '').trim();
    let ok = 0, fail = 0;

    for (const it of queue) {
      if (!it.blob) it.blob = await fetch(it.thumbUrl).then(r => r.blob());
      it.state = '⏳'; renderGrid();
      try {
        const fd = new FormData();
        fd.append('photo', it.blob, 'photo.jpg');
        if (who) fd.append('who', who);
        const r = await fetch('/Upload', {
          method: 'POST',
          headers: { 'RequestVerificationToken': rvt },
          body: fd
        });
        if (!r.ok) throw new Error();
        it.state = '✓'; ok++;
      } catch { it.state = '✗'; fail++; }
      renderGrid();
    }

    if (fail === 0) {
      msg.className = 'msg ok';
      msg.textContent = 'Tak! ' + ok + ' billede' + (ok > 1 ? 'r' : '') + ' sendt 🎉';
      queue = [];
      setTimeout(() => { grid.innerHTML = ''; renderMore(); }, 1200);
    } else {
      msg.className = 'msg err';
      msg.textContent = ok + ' sendt, ' + fail + ' fejlede. Prøv igen.';
      queue = queue.filter(it => it.state === '✗').map(it => ({ ...it, state: '' }));
      sendBtn.disabled = queue.length === 0;
      renderGrid();
    }
  });

  function renderMore() {
    if (document.getElementById('moreBtn')) return;
    const b = document.createElement('button');
    b.className = 'more'; b.id = 'moreBtn'; b.textContent = '+ Tilføj flere';
    b.onclick = () => { msg.textContent = ''; b.remove(); fileInput.click(); };
    msg.after(b);
  }
})();
