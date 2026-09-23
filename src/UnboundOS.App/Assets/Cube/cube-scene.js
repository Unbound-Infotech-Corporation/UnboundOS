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

  const canvas = document.getElementById("gl");
  const fallback = document.getElementById("fallback");
  const overlayEl = document.getElementById("overlay");
  const trackEl = document.getElementById("track");
  const listHeadEl = document.getElementById("listHead");
  const nodeLabelEl = document.getElementById("nodeLabel");
  const hosted = Boolean(window.chrome?.webview);
  const reduced = window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches === true;

  if (typeof THREE === "undefined" || !canvas) {
    showFallback();
    return;
  }

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

  const renderer = new THREE.WebGLRenderer({
    canvas,
    antialias: true,
    alpha: false,
    powerPreference: "high-performance"
  });
  renderer.setClearColor(0x05070a, 1);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.08;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;

  const scene = new THREE.Scene();
  scene.background = new THREE.Color(0x05070a);
  const camera = new THREE.PerspectiveCamera(26, 16 / 9, 0.1, 80);
  camera.position.set(0, 2.15, 7.15);
  camera.lookAt(0, 1.05, 0);

  const clock = new THREE.Clock();
  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const labelWorld = new THREE.Vector3();
  let running = false;
  let drag = null;

  scene.add(new THREE.HemisphereLight(0x243040, 0x05070a, 0.42));

  const key = new THREE.DirectionalLight(0xfff3e4, 1.55);
  key.position.set(-3.2, 6.4, 5.2);
  key.castShadow = true;
  key.shadow.mapSize.set(2048, 2048);
  key.shadow.camera.left = -7;
  key.shadow.camera.right = 7;
  key.shadow.camera.top = 5;
  key.shadow.camera.bottom = -2;
  key.shadow.camera.near = 1;
  key.shadow.camera.far = 18;
  key.shadow.bias = -0.00035;
  key.shadow.radius = 3.2;
  scene.add(key);

  const fill = new THREE.DirectionalLight(0xb9c6d6, 0.32);
  fill.position.set(4.4, 3.2, 2.6);
  scene.add(fill);

  const rim = new THREE.DirectionalLight(0x7f96aa, 0.22);
  rim.position.set(0.4, 2.4, -5.2);
  scene.add(rim);

  const floor = new THREE.Mesh(
    new THREE.PlaneGeometry(48, 28),
    new THREE.MeshPhysicalMaterial({
      color: 0x080b10,
      roughness: 0.32,
      metalness: 0.38,
      clearcoat: 0.18,
      clearcoatRoughness: 0.5
    })
  );
  floor.rotation.x = -Math.PI / 2;
  floor.receiveShadow = true;
  scene.add(floor);

  const blobTex = makeBlobTexture();
  const blobGeo = new THREE.PlaneGeometry(1.15, 0.55);
  const tabs = TAB_DEFS.map((def, i) => makeTab(def, (i - 3) * 0.98));

  function makeBlobTexture() {
    const c = document.createElement("canvas");
    c.width = c.height = 256;
    const ctx = c.getContext("2d");
    const g = ctx.createRadialGradient(128, 128, 12, 128, 128, 120);
    g.addColorStop(0, "rgba(0,0,0,0.55)");
    g.addColorStop(0.45, "rgba(0,0,0,0.22)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, 256, 256);
    const tex = new THREE.CanvasTexture(c);
    tex.minFilter = THREE.LinearFilter;
    tex.magFilter = THREE.LinearFilter;
    tex.generateMipmaps = false;
    return tex;
  }

  function makeTab(def, x) {
    const group = new THREE.Group();
    group.position.x = x;
    group.rotation.x = -0.075;
    const bodyMat = new THREE.MeshPhysicalMaterial({
      color: 0x1a2026,
      roughness: 0.4,
      metalness: 0.7,
      clearcoat: 0.28,
      clearcoatRoughness: 0.32
    });
    const edgeMat = new THREE.MeshPhysicalMaterial({
      color: 0x2a323a,
      roughness: 0.28,
      metalness: 0.78,
      clearcoat: 0.45,
      clearcoatRoughness: 0.2
    });
    const tipMat = new THREE.MeshPhysicalMaterial({
      color: 0x00e6f5,
      emissive: 0x00f0ff,
      emissiveIntensity: 0.62,
      roughness: 0.22,
      metalness: 0.3,
      clearcoat: 0.55,
      clearcoatRoughness: 0.18
    });
    const body = new THREE.Mesh(new THREE.BoxGeometry(0.108, 2.62, 0.2), bodyMat);
    body.position.y = 1.31;
    body.castShadow = true;
    body.receiveShadow = true;
    const edgeL = new THREE.Mesh(new THREE.BoxGeometry(0.01, 2.62, 0.204), edgeMat);
    edgeL.position.set(-0.054, 1.31, 0);
    edgeL.castShadow = true;
    const edgeR = edgeL.clone();
    edgeR.position.x = 0.054;
    const lip = new THREE.Mesh(new THREE.BoxGeometry(0.118, 0.02, 0.22), edgeMat);
    lip.position.y = 2.63;
    lip.castShadow = true;
    const tip = new THREE.Mesh(new THREE.BoxGeometry(0.116, 0.058, 0.216), tipMat);
    tip.position.y = 2.67;
    tip.castShadow = true;
    group.add(body, edgeL, edgeR, lip, tip);
    scene.add(group);

    const blob = new THREE.Mesh(
      blobGeo,
      new THREE.MeshBasicMaterial({
        map: blobTex,
        transparent: true,
        depthWrite: false,
        opacity: 0.42
      })
    );
    blob.rotation.x = -Math.PI / 2;
    blob.position.set(x, 0.008, 0.06);
    scene.add(blob);

    return {
      def,
      group,
      body,
      tip,
      blob,
      lift: 0,
      liftGoal: 0,
      glow: 0.62,
      glowGoal: 0.62
    };
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

  function setTab(index, instant) {
    state.tab = wrapTab(index);
    const on = TAB_DEFS[state.tab];
    for (let i = 0; i < tabs.length; i++) {
      const focused = i === state.tab;
      tabs[i].liftGoal = focused ? 0.13 : 0;
      tabs[i].glowGoal = focused ? 1.35 : 0.55;
      if (instant || !state.motion) {
        tabs[i].lift = tabs[i].liftGoal;
        tabs[i].glow = tabs[i].glowGoal;
      }
    }
    if (nodeLabelEl) {
      nodeLabelEl.textContent = on.title;
      nodeLabelEl.hidden = state.overlay;
    }
    const cap = document.getElementById("previewTitle");
    const hint = document.getElementById("previewHint");
    if (cap) cap.textContent = on.title;
    if (hint) hint.textContent = "Left/Right move tabs · Up/Down open this list";
    if (instant || !state.motion) poseTabs(0);
  }

  function poseTabs(step) {
    const k = state.motion ? 1 - Math.exp(-step * 7.2) : 1;
    for (const tab of tabs) {
      tab.lift += (tab.liftGoal - tab.lift) * k;
      tab.glow += (tab.glowGoal - tab.glow) * k;
      tab.group.position.y = tab.lift;
      tab.tip.material.emissiveIntensity = tab.glow;
      const on = tab === tabs[state.tab];
      tab.blob.material.opacity = on ? 0.62 : 0.34;
      tab.blob.scale.set(on ? 1.18 : 0.92, on ? 1.12 : 0.88, 1);
      const dim = state.overlay && !on ? 0.42 : 1;
      tab.body.material.color.setHex(on ? 0x222830 : 0x1a2026);
      tab.body.material.opacity = 1;
      tab.group.traverse((child) => {
        if (child.material && child.material.emissiveIntensity == null) {
          child.material.color.multiplyScalar(1);
        }
      });
      tab.group.scale.setScalar(on ? 1 : 0.985);
      tab.body.material.metalness = on ? 0.74 : 0.68;
      if (dim < 1) {
        tab.tip.material.emissiveIntensity *= 0.45;
      }
    }
  }

  function syncNodeLabel() {
    if (!nodeLabelEl || state.overlay) {
      if (nodeLabelEl) nodeLabelEl.hidden = true;
      return;
    }
    const tab = tabs[state.tab];
    tab.tip.getWorldPosition(labelWorld);
    labelWorld.y += 0.2;
    labelWorld.project(camera);
    const w = canvas.clientWidth || 1;
    const h = canvas.clientHeight || 1;
    const x = (labelWorld.x * 0.5 + 0.5) * w;
    const y = (-labelWorld.y * 0.5 + 0.5) * h;
    nodeLabelEl.hidden = false;
    nodeLabelEl.style.transform = `translate(${x}px, ${y}px) translate(-50%, -110%)`;
  }

  function send(msg) {
    if (hosted) window.chrome.webview.postMessage(JSON.stringify(msg));
  }

  function applyHostState(data) {
    if (Array.isArray(data.faces) && data.faces.length) state.faces = data.faces;
    if (data.motion === false) state.motion = false;
    if (data.motion === true) state.motion = true;
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

  function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, (ch) => ({
      "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;"
    }[ch]));
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
    if (listHeadEl) listHeadEl.textContent = state.overlayTitle || TAB_DEFS[state.tab].title;
    overlayEl.classList.remove("show", "ready", "motion");
    if (!instant && state.motion) overlayEl.classList.add("motion");
    overlayEl.classList.add("show");
    renderTrack();
    const arm = () => overlayEl.classList.add("ready");
    if (instant || !state.motion) arm();
    else requestAnimationFrame(() => requestAnimationFrame(arm));
    if (nodeLabelEl) nodeLabelEl.hidden = true;
  }

  function hideOverlay(instant) {
    state.overlay = false;
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
    setLoop(true);
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

  function hitTab(clientX, clientY) {
    const rect = canvas.getBoundingClientRect();
    pointer.x = ((clientX - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((clientY - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const meshes = tabs.flatMap((t) => [t.body, t.tip]);
    const hits = raycaster.intersectObjects(meshes, false);
    if (!hits.length) return -1;
    return tabs.findIndex((t) => t.body === hits[0].object || t.tip === hits[0].object);
  }

  function onPointerDown(ev) {
    drag = { x: ev.clientX, y: ev.clientY, t: performance.now() };
    canvas.setPointerCapture?.(ev.pointerId);
  }

  function onPointerUp(ev) {
    if (!drag) return;
    const dx = ev.clientX - drag.x;
    const dy = ev.clientY - drag.y;
    const dt = Math.max(16, performance.now() - drag.t);
    drag = null;
    if (state.overlay) return;
    if (Math.abs(dx) > 64 && Math.abs(dx) > Math.abs(dy)) {
      setTab(state.tab + (dx < 0 ? 1 : -1), !state.motion);
      send({ v: 1, type: "turn", turn: dx < 0 ? "Right" : "Left" });
      return;
    }
    const idx = hitTab(ev.clientX, ev.clientY);
    if (idx < 0) return;
    if (idx === state.tab) {
      previewOpen("top");
      send({ v: 1, type: "activate" });
    } else {
      setTab(idx, !state.motion);
      send({ v: 1, type: "pick", face: TAB_DEFS[idx].id });
    }
  }

  function resize() {
    const w = canvas.clientWidth || window.innerWidth;
    const h = canvas.clientHeight || window.innerHeight;
    renderer.setSize(w, h, false);
    camera.aspect = w / Math.max(1, h);
    camera.updateProjectionMatrix();
  }

  function renderFrame() {
    const dt = Math.min(clock.getDelta(), 0.05);
    poseTabs(dt);
    syncNodeLabel();
    renderer.render(scene, camera);
  }

  function loop() {
    if (!running) return;
    renderFrame();
    requestAnimationFrame(loop);
  }

  function setLoop(on) {
    if (on && !running) {
      running = true;
      clock.getDelta();
      requestAnimationFrame(loop);
    }
    if (!on) running = false;
  }

  function showFallback() {
    if (fallback) fallback.hidden = false;
    if (canvas) canvas.style.display = "none";
  }

  canvas.addEventListener("pointerdown", onPointerDown);
  window.addEventListener("pointerup", onPointerUp);
  window.addEventListener("keydown", onKey);
  window.addEventListener("resize", resize);

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

  resize();
  setTab(3, true);
  setLoop(true);
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
