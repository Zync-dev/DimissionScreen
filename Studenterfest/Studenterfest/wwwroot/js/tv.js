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

  // --- Live indstillinger (styres fra /admin) ---
  const titleText = document.getElementById('titleText');
  const subText = document.getElementById('subText');
  let currentJamUrl = CFG.jamUrl;
  let lyricsEnabled = CFG.lyricsEnabled !== false;

  function applySettings(d) {
    if (!d) return;
    document.body.classList.toggle('dark', d.theme === 'dark');
    if (d.title != null && titleText && titleText.textContent !== d.title) titleText.textContent = d.title;
    if (d.subtitle != null && subText && subText.textContent !== d.subtitle) subText.textContent = d.subtitle;
    if (d.jamUrl && d.jamUrl !== currentJamUrl) { currentJamUrl = d.jamUrl; makeQR('qrJam', currentJamUrl); }
    if (typeof d.lyrics === 'boolean' && d.lyrics !== lyricsEnabled) {
      lyricsEnabled = d.lyrics;
      if (!lyricsEnabled) hideLyrics();
      currentTrackKey = null; // tving genindlæsning når slået til igen
    }
  }
  function pollSettings() {
    if (!CFG.settingsUrl) return;
    fetch(CFG.settingsUrl).then(r => r.json()).then(applySettings).catch(() => {});
  }
  setInterval(pollSettings, 8000);

  // --- Slideshow (fuldt billede + udtonet baggrund, ingen beskæring) ---
  let photos = [];
  let shown = -1;
  let activeLayer = 0;
  const frames = [];
  const slide = document.getElementById('slide');
  const emptyEl = document.getElementById('empty');
  const countEl = document.getElementById('count');
  const newEl = document.getElementById('new');
  const creditEl = document.getElementById('credit');
  const creditName = document.getElementById('creditName');

  function setCredit(name) {
    if (name) { creditName.textContent = name; creditEl.classList.add('show'); }
    else { creditEl.classList.remove('show'); }
  }

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

  function display(photo, flash) {
    ensureLayers();
    const next = 1 - activeLayer;
    const fr = frames[next];
    fr.img.onload = () => {
      fr.bg.style.backgroundImage = 'url("' + photo.url + '")';
      fr.f.classList.add('on');
      frames[activeLayer].f.classList.remove('on');
      activeLayer = next;
    };
    fr.img.src = photo.url;
    emptyEl.style.display = 'none';
    setCredit(photo.name);
    if (flash) {
      newEl.classList.add('show');
      setTimeout(() => newEl.classList.remove('show'), 3000);
    }
  }

  async function fetchPhotos() {
    try {
      const r = await fetch(CFG.photosUrl);
      const data = await r.json();
      const items = data.map(d => ({ url: d.url, name: d.name || '' }));
      const isFirstLoad = photos.length === 0;
      const hadNew = items.length > photos.length;
      photos = items;
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
  const lyTrack = document.getElementById('lyTrack');
  let lyricsLines = [];
  let lyLineEls = [];
  let curLineIdx = -2;
  let currentTrackKey = null;

  function hideLyrics() {
    lyricsLines = [];
    lyLineEls = [];
    curLineIdx = -2;
    lyricsEl.hidden = true;
    lyTrack.innerHTML = '';
    lyTrack.style.transform = 'translateY(0)';
  }

  function buildLyrics(lines) {
    lyricsLines = lines;
    lyLineEls = lines.map(function (l) {
      const d = document.createElement('div');
      d.className = 'ly-line';
      d.textContent = l.text && l.text.length ? l.text : '\u00a0';
      lyTrack.appendChild(d);
      return d;
    });
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
        buildLyrics(data.lines);
        curLineIdx = -2;
        lyricsEl.hidden = false;
        requestAnimationFrame(function () { updateLyrics(curProgress); });
      }
    } catch (e) { }
  }

  function updateLyrics(posMs) {
    if (!lyLineEls.length) return;
    let idx = -1;
    for (let i = 0; i < lyricsLines.length; i++) {
      if (lyricsLines[i].t <= posMs) idx = i; else break;
    }
    if (idx === curLineIdx) return;
    curLineIdx = idx;

    // dybdeskarphed: aktiv linje skarp, naboer toner ud og slører med afstanden
    lyLineEls.forEach(function (el, i) {
      if (i === idx) {
        el.classList.add('active');
        el.style.filter = 'blur(0px)';
        el.style.opacity = '1';
        el.style.transform = 'scale(1)';
      } else {
        el.classList.remove('active');
        const d = Math.abs(i - idx);
        el.style.filter = 'blur(' + Math.min(d * 1.2, 4.5).toFixed(2) + 'px)';
        el.style.opacity = String(Math.max(0.12, 0.62 - d * 0.16));
        el.style.transform = 'scale(' + Math.max(0.94, 1 - d * 0.018).toFixed(3) + ')';
      }
    });

    if (idx < 0) { lyTrack.style.transform = 'translateY(0)'; return; }
    const cur = lyLineEls[idx];
    const focal = lyricsEl.clientHeight * 0.42;
    const y = cur.offsetTop + cur.offsetHeight / 2;
    lyTrack.style.transform = 'translateY(' + (focal - y) + 'px)';
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
      if (!lyricsEnabled) {
        hideLyrics();
      } else if (key !== currentTrackKey) {
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
