/* UnboundOS Home — studio tab bar on Obsidian.
   No galaxy, no suns. Narrow vertical blades, teal tips, soft shadows. */
(() => {
  "use strict";

  const TAB_DEFS = [
    { id: "Hardware", title: "Hardware" },
    { id: "Files", title: "Files" },
    { id: "Network", title: "Network" },
    { id: "Session", title: "Games" },
    { id: "Settings", title: "Options" },
    { id: "Tools", title: "Tools" },
    { id: "Mods", title: "Mods" }
  ];
  const OPTIONS_ITEMS = [
    { id: "motion", title: "Interface motion", meta: "SET" },
    { id: "hud", title: "Home HUD", meta: "SET" },
    { id: "display", title: "Display", meta: "SET" },
    { id: "overclock", title: "Overclocking", meta: "SET" },
    { id: "startup", title: "Startup audit", meta: "SET" },
    { id: "cleanup", title: "Finish setup", meta: "SET" }
  ];
  const DEFAULT_FACES = [
    { id: "Session", title: "Games", kicker: "PLAY", monogram: "G", meta: "PLAY", hint: "Up or Down opens this list.", accent: "#F2E6C8", core: "#FFE9B0" },
    { id: "Tools", title: "Tools", kicker: "KIT", monogram: "T", meta: "OPEN", hint: "Up or Down opens Tools.", accent: "#E8D7A8", core: "#F0E0B8" },
    { id: "Mods", title: "Mods", kicker: "MOD", monogram: "M", meta: "OPEN", hint: "Up or Down opens Mods.", accent: "#E4D2A0", core: "#F0E0B8" },
    { id: "Network", title: "Network", kicker: "LINK", monogram: "N", meta: "SPLIT", hint: "Up or Down opens Network.", accent: "#C8D4F0", core: "#D8E0F4" },
    { id: "Files", title: "Files", kicker: "FS", monogram: "F", meta: "BROWSE", hint: "Up or Down opens Files.", accent: "#D8D4C8", core: "#E8E4D8" },
    { id: "Hardware", title: "Hardware", kicker: "HW", monogram: "H", meta: "READ", hint: "Up or Down opens Hardware.", accent: "#B8C8E8", core: "#C8D4F0" }
  ];

  const params = new URLSearchParams(location.search);
  const preview = params.has("preview");
  if (preview) document.body.classList.add("preview");

  const studioEl = document.getElementById("studio");
  const tabRowEl = document.getElementById("tabRow");
  const overlayEl = document.getElementById("overlay");
  const trackEl = document.getElementById("track");
  const listHeadEl = document.getElementById("listHead");
  const hosted = Boolean(window.chrome?.webview);
  const reduced = window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches === true;

  if (!studioEl || !tabRowEl) return;

  const state = {
    faces: DEFAULT_FACES,
    tab: 3,
    motion: !reduced,
    overlay: false,
    items: [],
    focus: 0,
    origin: "top",
    overlayTitle: "",
    overlayFront: ""
  };

  let drag = null;
  const tabs = [];

  function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, (ch) => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
    }[ch]));
  }

  function tabIndexOf(id) {
    const i = TAB_DEFS.findIndex((t) => t.id === id);
    return i >= 0 ? i : 3;
  }

  function wrapTab(i) {
    const n = TAB_DEFS.length;
    return ((i % n) + n) % n;
  }

  function faceInfo(id) {
    return state.faces.find((f) => f.id === id) || DEFAULT_FACES[0];
  }

  function syncMotionClass() {
    document.body.classList.toggle("reduce-motion", !state.motion);
  }

  function buildTabs() {
    tabRowEl.innerHTML = TAB_DEFS.map((def, i) => (
      `<button type="button" class="tab" data-index="${i}" role="tab" aria-selected="false" tabindex="-1">` +
        `<span class="stack">` +
          `<span class="blade">` +
            `<span class="tip"><span class="facet"></span></span>` +
            `<span class="tab-label">${escapeHtml(def.title)}</span>` +
          `</span>` +
          `<span class="mirror" aria-hidden="true"></span>` +
        `</span>` +
      `</button>`
    )).join("");
    tabs.length = 0;
    tabRowEl.querySelectorAll(".tab").forEach((el) => {
      tabs.push(el);
      el.addEventListener("click", () => {
        const idx = Number(el.getAttribute("data-index"));
        if (state.overlay) return;
        if (idx === state.tab) {
          previewOpen("top");
          send({ v: 1, type: "activate" });
        } else {
          if (!hosted) setTab(idx, !state.motion);
          send({ v: 1, type: "pick", face: TAB_DEFS[idx].id });
        }
      });
    });
  }

  function setTab(index, instant) {
    state.tab = wrapTab(index);
    const on = TAB_DEFS[state.tab];
    const skipEase = instant || !state.motion;
    if (skipEase) document.body.classList.add("reduce-motion");
    for (let i = 0; i < tabs.length; i++) {
      const focused = i === state.tab;
      tabs[i].classList.toggle("is-focus", focused);
      tabs[i].setAttribute("aria-selected", focused ? "true" : "false");
    }
    if (!skipEase) syncMotionClass();
    else if (state.motion) {
      requestAnimationFrame(() => syncMotionClass());
    }
    const cap = document.getElementById("previewTitle");
    const hint = document.getElementById("previewHint");
    if (cap) cap.textContent = on.title;
    if (hint) hint.textContent = "Left/Right move tabs · Up/Down open this list";
  }

  function send(msg) {
    if (hosted) window.chrome.webview.postMessage(JSON.stringify(msg));
  }

  function applyHostState(data) {
    if (Array.isArray(data.faces) && data.faces.length) state.faces = data.faces;
    if (data.motion === false) state.motion = false;
    if (data.motion === true) state.motion = true;
    syncMotionClass();
    if (state.overlay) return;
    if (isOptionsFront(data.front, data.node)) setTab(tabIndexOf("Settings"), !state.motion);
    else if (data.front) setTab(tabIndexOf(data.front), !state.motion);
    else if (typeof data.node === "number" && data.node >= 0) {
      const ids = ["Session", "Tools", "Mods", "Network", "Files", "Hardware"];
      setTab(tabIndexOf(ids[data.node] || "Session"), !state.motion);
    }
  }

  function isOptionsFront(front, node) {
    if (node === -1) return true;
    return /^settings$|^options$/i.test(String(front || ""));
  }

  function previewItems(id) {
    if (id === "Session") {
      return [
        { id: "session", title: "Session engine", meta: "PAGE" },
        { id: "steam", title: "Steam", meta: "KIT" },
        { id: "playnite", title: "Playnite", meta: "KIT" }
      ];
    }
    if (id === "Tools") {
      return [
        { id: "obs", title: "OBS", meta: "KIT" },
        { id: "discord", title: "Discord", meta: "KIT" },
        { id: "vortex", title: "Vortex", meta: "KIT" }
      ];
    }
    if (id === "Settings") return OPTIONS_ITEMS;
    const info = faceInfo(id);
    return [{ id: id.toLowerCase(), title: info.title, meta: info.meta || "OPEN" }];
  }

  function renderTrack() {
    if (!trackEl) return;
    trackEl.innerHTML = state.items.map((item, i) => {
      const focus = i === state.focus ? " focus" : "";
      const motion = state.motion ? " motion" : "";
      return `<button type="button" class="row${focus}${motion}" data-index="${i}"><span class="title">${escapeHtml(item.title)}</span></button>`;
    }).join("");
    trackEl.querySelectorAll(".row").forEach((row) => {
      row.addEventListener("click", () => {
        const i = Number(row.getAttribute("data-index"));
        state.focus = i;
        renderTrack();
        send({ v: 1, type: "select", index: i, item: state.items[i]?.id });
      });
    });
    syncTrack();
  }

  function syncTrack() {
    if (!trackEl) return;
    const rows = [...trackEl.querySelectorAll(".row")];
    const on = rows[state.focus];
    if (!on) return;
    const pane = document.getElementById("listPane");
    const mid = (pane?.clientHeight || 400) * 0.42;
    const y = mid - (on.offsetTop + on.offsetHeight * 0.5);
    if (!state.motion) trackEl.style.transition = "none";
    else trackEl.style.transition = "";
    trackEl.style.setProperty("--track-y", `${y}px`);
  }

  function showOverlay(items, focus, origin, instant) {
    state.overlay = true;
    state.items = items;
    state.focus = focus;
    state.origin = origin;
    studioEl.classList.add("is-overlay");
    if (listHeadEl) listHeadEl.textContent = state.overlayTitle || TAB_DEFS[state.tab].title;
    overlayEl.classList.remove("show", "ready", "motion");
    if (!instant && state.motion) overlayEl.classList.add("motion");
    overlayEl.classList.add("show");
    renderTrack();
    const arm = () => overlayEl.classList.add("ready");
    if (instant || !state.motion) arm();
    else requestAnimationFrame(() => requestAnimationFrame(arm));
  }

  function hideOverlay(instant) {
    state.overlay = false;
    studioEl.classList.remove("is-overlay");
    const fade = !instant && state.motion && overlayEl.classList.contains("ready");
    overlayEl.classList.remove("show");
    if (!fade) {
      overlayEl.classList.remove("ready", "motion");
    } else {
      overlayEl.classList.add("motion");
      overlayEl.classList.remove("ready");
      window.setTimeout(() => overlayEl.classList.remove("motion"), 720);
    }
    state.items = [];
    setTab(state.tab, instant || !state.motion);
  }

  function startOpen(msg) {
    if (msg.motion === false) state.motion = false;
    syncMotionClass();
    const options = isOptionsFront(msg.front, msg.node);
    if (options) {
      setTab(tabIndexOf("Settings"), !state.motion);
      state.overlayTitle = "Options";
      state.overlayFront = "Settings";
    } else {
      if (typeof msg.node === "number" && msg.node >= 0) {
        const ids = ["Session", "Tools", "Mods", "Network", "Files", "Hardware"];
        setTab(tabIndexOf(ids[msg.node] || msg.front || "Session"), !state.motion);
      } else if (msg.front) setTab(tabIndexOf(msg.front), !state.motion);
      state.overlayTitle = TAB_DEFS[state.tab].title;
      state.overlayFront = TAB_DEFS[state.tab].id;
    }
    const items = Array.isArray(msg.items) && msg.items.length
      ? msg.items
      : previewItems(options ? "Settings" : TAB_DEFS[state.tab].id);
    const origin = String(msg.origin || "top").toLowerCase();
    const focus = typeof msg.focus === "number" ? msg.focus : (origin === "bottom" ? items.length - 1 : 0);
    if (items.length) showOverlay(items, focus, origin, !state.motion);
    send({ v: 1, type: "opened", face: state.overlayFront, stay: true });
  }

  function onHostMessage(data) {
    if (!data || typeof data !== "object") return;
    if (data.type === "state") applyHostState(data);
    if (data.type === "open") startOpen(data);
    if (data.type === "focus") {
      if (typeof data.focus === "number") {
        state.focus = data.focus;
        renderTrack();
      }
    }
    if (data.type === "close") hideOverlay(!data.motion);
    if (data.type === "reset") {
      hideOverlay(true);
      applyHostState(data);
    }
  }

  function previewOpen(origin) {
    startOpen({
      front: TAB_DEFS[state.tab].id,
      node: TAB_DEFS[state.tab].id === "Settings" ? -1 : 0,
      origin,
      motion: state.motion,
      items: previewItems(TAB_DEFS[state.tab].id)
    });
  }

  function previewOpenOptions() {
    startOpen({
      front: "Settings",
      node: -1,
      origin: "top",
      motion: state.motion,
      items: OPTIONS_ITEMS
    });
  }

  function onKey(ev) {
    if (state.overlay) {
      if (ev.key === "Escape") {
        hideOverlay(!state.motion);
        send({ v: 1, type: "back" });
        ev.preventDefault();
      }
      if (ev.key === "ArrowUp" || ev.key === "ArrowDown") {
        const dir = ev.key === "ArrowUp" ? -1 : 1;
        state.focus = (state.focus + dir + state.items.length) % state.items.length;
        renderTrack();
        send({ v: 1, type: "cycle", index: state.focus });
        ev.preventDefault();
      }
      if (ev.key === "Enter") {
        send({ v: 1, type: "activate", index: state.focus, item: state.items[state.focus]?.id });
        ev.preventDefault();
      }
      return;
    }
    if (ev.key === "ArrowLeft") {
      if (!hosted) setTab(state.tab - 1, !state.motion);
      send({ v: 1, type: "turn", turn: "Left" });
      ev.preventDefault();
    }
    if (ev.key === "ArrowRight") {
      if (!hosted) setTab(state.tab + 1, !state.motion);
      send({ v: 1, type: "turn", turn: "Right" });
      ev.preventDefault();
    }
    if (ev.key === "ArrowUp") {
      if (!hosted) previewOpen("bottom");
      send({ v: 1, type: "turn", turn: "Up" });
      ev.preventDefault();
    }
    if (ev.key === "ArrowDown") {
      if (!hosted) previewOpen("top");
      send({ v: 1, type: "turn", turn: "Down" });
      ev.preventDefault();
    }
    if (ev.key === "Enter") {
      if (!hosted) previewOpen("top");
      send({ v: 1, type: "activate" });
      ev.preventDefault();
    }
    if ((ev.key === "o" || ev.key === "O") && preview && !hosted) {
      previewOpenOptions();
      ev.preventDefault();
    }
  }

  function onPointerDown(ev) {
    drag = { x: ev.clientX, y: ev.clientY, t: performance.now() };
  }

  function onPointerUp(ev) {
    if (!drag) return;
    const dx = ev.clientX - drag.x;
    const dy = ev.clientY - drag.y;
    drag = null;
    if (state.overlay) return;
    if (Math.abs(dx) > 64 && Math.abs(dx) > Math.abs(dy)) {
      if (!hosted) setTab(state.tab + (dx < 0 ? 1 : -1), !state.motion);
      send({ v: 1, type: "turn", turn: dx < 0 ? "Right" : "Left" });
    }
  }

  buildTabs();
  syncMotionClass();
  window.addEventListener("pointerdown", onPointerDown);
  window.addEventListener("pointerup", onPointerUp);
  window.addEventListener("keydown", onKey);

  if (hosted) {
    window.chrome.webview.addEventListener("message", (ev) => {
      const data = typeof ev.data === "string" ? JSON.parse(ev.data) : ev.data;
      onHostMessage(data);
    });
    send({ v: 1, type: "ready" });
  } else {
    applyHostState({
      type: "state",
      node: 0,
      front: "Session",
      motion: !reduced,
      faces: DEFAULT_FACES
    });
  }

  setTab(3, true);
  const startTabParam = (params.get("tab") || "").toLowerCase();
  if (!hosted && startTabParam) {
    const def = TAB_DEFS.find((t) => t.id.toLowerCase() === startTabParam || t.title.toLowerCase() === startTabParam);
    if (def) setTab(tabIndexOf(def.id), true);
  }
  const startOpenParam = (params.get("open") || "").toLowerCase();
  if (!hosted && (startOpenParam === "options" || startOpenParam === "settings")) {
    window.setTimeout(() => previewOpenOptions(), 60);
  } else if (!hosted && (startOpenParam === "games" || startOpenParam === "session")) {
    window.setTimeout(() => {
      setTab(tabIndexOf("Session"), true);
      previewOpen("top");
    }, 60);
  } else if (!hosted && TAB_DEFS.some((t) => t.id.toLowerCase() === startOpenParam)) {
    window.setTimeout(() => {
      const def = TAB_DEFS.find((t) => t.id.toLowerCase() === startOpenParam);
      setTab(tabIndexOf(def.id), true);
      previewOpen("top");
    }, 60);
  }
})();
