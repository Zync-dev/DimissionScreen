(function () {
  const CFG = window.TV_CONFIG || {};

  // --- QR-koder ---
  function makeQR(elId, text) {
    const el = document.getElementById(elId);
    if (!el) return;
    el.innerHTML = '';
    if (window.QRCode && text) {
      new QRCode(el, { text, width: 260, height: 260, colorDark: '#000', colorLight: '#fff', correctLevel: QRCode.CorrectLevel.M });
    } else {
      el.innerHTML = '<div class="fallback">' + (text || '(mangler link)') + '</div>';
    }
  }
  makeQR('qrPhoto', CFG.uploadUrl);
  makeQR('qrJam', CFG.jamUrl);

  // --- Slideshow (fuldt billede + udtonet baggrund, ingen beskæring) ---
  let photos = [];
  let shown = -1;
  let activeLayer = 0;
  const frames = [];
  const slide = document.getElementById('slide');
  const emptyEl = document.getElementById('empty');
  const countEl = document.getElementById('count');
  const newEl = document.getElementById('new');

  function ensureLayers() {
    if (frames.length) return;
    for (let i = 0; i < 2; i++) {
      const f = document.createElement('div');
      f.className = 'frame';
      const bg = document.createElement('div');
      bg.className = 'bg';
      const img = document.createElement('img');
      f.appendChild(bg);
      f.appendChild(img);
      slide.insertBefore(f, newEl);
      frames.push({ f, bg, img });
    }
  }

  function display(url, flash) {
    ensureLayers();
    const next = 1 - activeLayer;
    const fr = frames[next];
    fr.img.onload = () => {
      fr.bg.style.backgroundImage = 'url("' + url + '")';
      fr.f.classList.add('on');
      frames[activeLayer].f.classList.remove('on');
      activeLayer = next;
    };
    fr.img.src = url;
    emptyEl.style.display = 'none';
    if (flash) {
      newEl.classList.add('show');
      setTimeout(() => newEl.classList.remove('show'), 3000);
    }
  }

  async function fetchPhotos() {
    try {
      const r = await fetch(CFG.photosUrl);
      const data = await r.json();
      const urls = data.map(d => d.url);
      const isFirstLoad = photos.length === 0;
      const hadNew = urls.length > photos.length;
      photos = urls;
      countEl.style.display = photos.length ? 'block' : 'none';
      countEl.textContent = photos.length + (photos.length === 1 ? ' billede' : ' billeder');
      if (photos.length && (isFirstLoad || hadNew)) {
        shown = photos.length - 1;
        display(photos[shown], !isFirstLoad);
      }
    } catch (e) { }
  }

  function advance() {
    if (!photos.length) return;
    shown = (shown + 1) % photos.length;
    display(photos[shown], false);
  }

  fetchPhotos();
  setInterval(fetchPhotos, 5000);
  setInterval(advance, 6500);

  // --- Spotify "spiller nu" ---
  const live = document.getElementById('live');
  const liveText = document.getElementById('liveText');
  const jamtagText = document.getElementById('jamtagText');
  const songEl = document.getElementById('song');
  const artistEl = document.getElementById('artist');
  const artEl = document.getElementById('art');
  const progEl = document.getElementById('prog');
  const elapsedEl = document.getElementById('elapsed');
  const totalEl = document.getElementById('total');

  // --- Lyrics ---
  const lyricsEl = document.getElementById('lyrics');
  const lyPrev = document.getElementById('lyPrev');
  const lyCur = document.getElementById('lyCur');
  const lyN1 = document.getElementById('lyN1');
  const lyN2 = document.getElementById('lyN2');
  const lyN3 = document.getElementById('lyN3');
  const lySlots = [lyPrev, lyCur, lyN1, lyN2, lyN3];
  let lyricsLines = [];
  let curLineIdx = -2;
  let currentTrackKey = null;

  function hideLyrics() {
    lyricsLines = [];
    curLineIdx = -2;
    lyricsEl.hidden = true;
    lySlots.forEach(el => { el.textContent = ''; });
  }

  async function loadLyrics(d) {
    hideLyrics();
    try {
      const u = new URLSearchParams({
        trackId: d.id || '',
        track: d.title || '',
        artist: d.artist || '',
        album: d.album || '',
        durationMs: String(d.durationMs || 0)
      });
      const r = await fetch(CFG.lyricsUrl + '&' + u.toString());
      const data = await r.json();
      if (data.found && Array.isArray(data.lines) && data.lines.length) {
        lyricsLines = data.lines;
        curLineIdx = -2;
        lyricsEl.hidden = false;
        updateLyrics(curProgress);
      }
    } catch (e) { }
  }

  function updateLyrics(posMs) {
    if (!lyricsLines.length) return;
    let idx = -1;
    for (let i = 0; i < lyricsLines.length; i++) {
      if (lyricsLines[i].t <= posMs) idx = i; else break;
    }
    if (idx === curLineIdx) return;
    curLineIdx = idx;

    const at = i => (i >= 0 && i < lyricsLines.length) ? lyricsLines[i].text : '';
    lyPrev.textContent = idx >= 1 ? at(idx - 1) : '';
    lyCur.textContent = idx >= 0 ? at(idx) : '';
    lyN1.textContent = at(idx + 1);
    lyN2.textContent = at(idx + 2);
    lyN3.textContent = at(idx + 3);

    lyricsEl.classList.remove('anim');
    void lyricsEl.offsetWidth; // genstarter animationen
    lyricsEl.classList.add('anim');
  }

  let curProgress = 0, curDuration = 0, playing = false, lastTick = Date.now();
  const fmt = s => { s = Math.max(0, Math.floor(s)); return Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0'); };

  function setArt(url) {
    if (url) { artEl.style.backgroundImage = 'url("' + url + '")'; artEl.classList.add('has-art'); }
    else { artEl.style.backgroundImage = ''; artEl.classList.remove('has-art'); }
  }

  async function fetchNowPlaying() {
    try {
      const r = await fetch(CFG.nowPlayingUrl);
      const d = await r.json();
      if (!d.connected) { live.classList.remove('on'); liveText.textContent = 'spotify ikke forbundet'; jamtagText.textContent = 'forbind spotify'; playing = false; currentTrackKey = null; hideLyrics(); return; }
      if (!d.isPlaying || !d.title) { live.classList.remove('on'); liveText.textContent = 'intet spiller'; jamtagText.textContent = 'sat på pause'; playing = false; currentTrackKey = null; hideLyrics(); return; }

      live.classList.add('on');
      liveText.textContent = 'live fra jammet';
      jamtagText.textContent = 'spiller nu';
      songEl.textContent = d.title;
      artistEl.textContent = d.album ? (d.artist + ' · ' + d.album) : d.artist;
      setArt(d.art);
      curProgress = d.progressMs; curDuration = d.durationMs; playing = true; lastTick = Date.now();
      totalEl.textContent = fmt(curDuration / 1000);

      const key = d.id || (d.title + '|' + d.artist);
      if (key !== currentTrackKey) {
        currentTrackKey = key;
        loadLyrics(d);
      }
    } catch (e) { }
  }

  fetchNowPlaying();
  setInterval(fetchNowPlaying, 4000);

  setInterval(() => {
    if (!playing || !curDuration) return;
    const now = Date.now();
    curProgress = Math.min(curDuration, curProgress + (now - lastTick));
    lastTick = now;
    progEl.style.width = (curProgress / curDuration * 100) + '%';
    elapsedEl.textContent = fmt(curProgress / 1000);
    updateLyrics(curProgress);
  }, 1000);
})();
