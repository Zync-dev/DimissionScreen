(function () {
  const CFG = window.TV_CONFIG || {};

  // --- QR-koder ---
  function makeQR(elId, text) {
    const el = document.getElementById(elId);
    if (!el) return;
    el.innerHTML = '';
    if (window.QRCode && text) {
      new QRCode(el, {
        text, width: 260, height: 260,
        colorDark: '#000', colorLight: '#fff',
        correctLevel: QRCode.CorrectLevel.M
      });
    } else {
      el.innerHTML = '<div class="fallback">' + (text || '(mangler link)') + '</div>';
    }
  }
  makeQR('qrPhoto', CFG.uploadUrl);
  makeQR('qrJam', CFG.jamUrl);

  // --- Slideshow ---
  let photos = [];
  let shown = -1;
  let activeLayer = 0;
  const layers = [];
  const slide = document.getElementById('slide');
  const emptyEl = document.getElementById('empty');
  const countEl = document.getElementById('count');
  const newEl = document.getElementById('new');

  function ensureLayers() {
    if (layers.length) return;
    for (let i = 0; i < 2; i++) {
      const im = document.createElement('img');
      slide.insertBefore(im, newEl);
      layers.push(im);
    }
  }

  function display(url, flash) {
    ensureLayers();
    const next = 1 - activeLayer;
    layers[next].onload = () => {
      layers[next].classList.add('on');
      layers[activeLayer].classList.remove('on');
      activeLayer = next;
    };
    layers[next].src = url;
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
    } catch (e) { /* ignorér – prøver igen næste interval */ }
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
  const jamtag = document.getElementById('jamtag');
  const jamtagText = document.getElementById('jamtagText');
  const songEl = document.getElementById('song');
  const artistEl = document.getElementById('artist');
  const artEl = document.getElementById('art');
  const progEl = document.getElementById('prog');
  const elapsedEl = document.getElementById('elapsed');
  const totalEl = document.getElementById('total');

  let curProgress = 0, curDuration = 0, playing = false, lastTick = Date.now();
  const fmt = s => { s = Math.max(0, Math.floor(s)); return Math.floor(s / 60) + ':' + String(s % 60).padStart(2, '0'); };

  function setLive(on, txt) {
    live.classList.toggle('off', !on);
    liveText.textContent = txt;
    jamtag.classList.toggle('off', !on);
  }

  async function fetchNowPlaying() {
    try {
      const r = await fetch(CFG.nowPlayingUrl);
      const d = await r.json();
      if (!d.connected) { setLive(false, 'Spotify ikke forbundet'); jamtagText.textContent = 'Forbind Spotify'; playing = false; return; }
      if (!d.isPlaying || !d.title) { setLive(false, 'Intet spiller'); jamtagText.textContent = 'Sat på pause'; playing = false; return; }

      setLive(true, 'SPOTIFY JAM · LIVE');
      jamtagText.textContent = 'Spiller nu fra jammet';
      songEl.textContent = d.title;
      artistEl.textContent = d.album ? (d.artist + ' · ' + d.album) : d.artist;
      if (d.art) { artEl.style.backgroundImage = 'url(' + d.art + ')'; artEl.style.backgroundSize = 'cover'; artEl.textContent = ''; }
      curProgress = d.progressMs; curDuration = d.durationMs; playing = true; lastTick = Date.now();
      totalEl.textContent = fmt(curDuration / 1000);
    } catch (e) { /* ignorér */ }
  }

  fetchNowPlaying();
  setInterval(fetchNowPlaying, 4000);

  // Lokal opdatering hvert sekund så baren bevæger sig flydende mellem polls
  setInterval(() => {
    if (!playing || !curDuration) return;
    const now = Date.now();
    curProgress = Math.min(curDuration, curProgress + (now - lastTick));
    lastTick = now;
    progEl.style.width = (curProgress / curDuration * 100) + '%';
    elapsedEl.textContent = fmt(curProgress / 1000);
  }, 1000);
})();
