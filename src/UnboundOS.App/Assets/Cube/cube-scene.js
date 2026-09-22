/* UnboundOS Home — user plate + living edge-on galaxy. */
(() => {
  "use strict";

  const NODE_IDS = ["Session", "Tools", "Mods", "Network", "Files", "Hardware"];
  const NODE_X = [0, 1.14, 2.22, -1.14, -2.22, -3.3];
  const DEFAULT_FACES = [
    { id: "Session", title: "Games", kicker: "PLAY", monogram: "G", meta: "PLAY", hint: "Up or Down opens this list over the galaxy.", accent: "#F2E6C8", core: "#FFE9B0" },
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
  const hosted = Boolean(window.chrome?.webview);
  const reduced = window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches === true;

  if (typeof THREE === "undefined" || !canvas) {
    showFallback();
    return;
  }

  const state = {
    faces: DEFAULT_FACES,
    node: 0,
    visualX: 0,
    targetX: 0,
    motion: !reduced,
    overlay: false,
    items: [],
    focus: 0,
    origin: "top",
    dragging: false,
    moved: false,
    pressX: 0,
    pressY: 0,
    lastX: 0,
    lastY: 0,
    lastT: 0,
    vx: 0,
    vy: 0
  };

  let renderer;
  try {
    renderer = new THREE.WebGLRenderer({
      canvas,
      antialias: true,
      alpha: false,
      powerPreference: "high-performance",
      preserveDrawingBuffer: true
    });
  } catch (err) {
    showFallback();
    return;
  }
  if (!renderer.getContext()) {
    showFallback();
    return;
  }

  fallback.hidden = true;
  renderer.setClearColor(0x000000, 1);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.02;

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(30, 1, 0.08, 80);
  camera.position.set(0, 0.02, 6.45);
  camera.lookAt(0, 0, 0);

  const rig = new THREE.Group();
  scene.add(rig);

  const starMap = spriteTex(64, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    g.addColorStop(0, "rgba(255,255,255,1)");
    g.addColorStop(0.18, "rgba(255,246,220,0.85)");
    g.addColorStop(0.42, "rgba(180,200,255,0.22)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  });

  const glowMap = spriteTex(128, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    g.addColorStop(0, "rgba(255,250,230,0.95)");
    g.addColorStop(0.22, "rgba(255,220,150,0.35)");
    g.addColorStop(0.55, "rgba(140,170,230,0.08)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  });

  const farStars = buildHalo(4200, 36, 1.2, 0x9aa8c8, 0x51f, 0.92);
  const midStars = buildHalo(2200, 18, 1.8, 0xd8dce8, 0x77a, 0.55);
  const nearStars = buildHalo(700, 9, 2.6, 0xf6f0e2, 0x91c, 0.22);
  const shear = buildShear(1600);
  const orbiters = buildOrbiters(240);
  const nodes = buildNodes();
  scene.add(farStars, midStars, nearStars);
  const vignette = buildVignette();
  scene.add(vignette);

  let plate = null;
  new THREE.TextureLoader().load("home-plate.png", (tex) => {
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = renderer.capabilities.getMaxAnisotropy();
    tex.minFilter = THREE.LinearFilter;
    tex.magFilter = THREE.LinearFilter;
    const aspect = 1672 / 941;
    const h = 7.15;
    const mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(h * aspect, h),
      new THREE.MeshBasicMaterial({ map: tex, transparent: true, depthWrite: false })
    );
    mesh.position.z = -2.85;
    rig.add(mesh);
    plate = mesh;
    if (!looping) renderFrame(0);
  }, undefined, () => {
    // Plate missing: keep the living starfield and node cores.
  });

  const clock = new THREE.Clock();
  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const nodeMeshes = nodes.map((n) => n.hit);

  function nodeX(i) {
    return NODE_X[wrapNode(i)];
  }

  function wrapNode(i) {
    const n = NODE_IDS.length;
    return ((i % n) + n) % n;
  }

  function faceInfo(id) {
    return state.faces.find((f) => f.id === id) || DEFAULT_FACES[0];
  }

  function buildHalo(count, spread, size, color, seed, flatten) {
    const rng = mulberry(seed);
    const pos = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (rng() - 0.5) * spread;
      pos[i * 3 + 1] = (rng() - 0.5) * spread * flatten;
      pos[i * 3 + 2] = (rng() - 0.5) * spread * 0.55 - 3;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    return new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      color,
      size,
      transparent: true,
      opacity: 0.88,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: false
    }));
  }

  function buildShear(count) {
    const rng = mulberry(0xa11e);
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const orbits = [];
    const warm = new THREE.Color(0xffe7b8);
    const cool = new THREE.Color(0x9ab0d8);
    for (let i = 0; i < count; i++) {
      const radius = 0.35 + rng() * 5.4;
      const phase = rng() * Math.PI * 2;
      const y = gauss(rng) * (0.08 + radius * 0.018);
      const z = gauss(rng) * 0.12;
      pos[i * 3] = Math.cos(phase) * radius;
      pos[i * 3 + 1] = y;
      pos[i * 3 + 2] = Math.sin(phase) * radius * 0.08 + z;
      const t = Math.min(1, radius / 5.2);
      const c = warm.clone().lerp(cool, t);
      col[i * 3] = c.r;
      col[i * 3 + 1] = c.g;
      col[i * 3 + 2] = c.b;
      orbits.push({
        radius,
        phase,
        y,
        z,
        omega: (0.042 / (0.55 + radius)) * (rng() > 0.5 ? 1 : -1) * (0.7 + rng() * 0.6)
      });
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      size: 0.085,
      vertexColors: true,
      transparent: true,
      opacity: 0.78,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: true
    }));
    pts.userData.orbits = orbits;
    rig.add(pts);
    return pts;
  }

  function buildOrbiters(count) {
    const rng = mulberry(0x0b17);
    const list = [];
    for (let i = 0; i < count; i++) {
      const node = i % NODE_IDS.length;
      const sprite = new THREE.Sprite(new THREE.SpriteMaterial({
        map: starMap,
        color: rng() > 0.55 ? 0xc9d8ff : 0xfff1d0,
        transparent: true,
        depthWrite: false,
        blending: THREE.AdditiveBlending,
        opacity: 0.55
      }));
      sprite.scale.setScalar(0.045 + rng() * 0.04);
      sprite.userData = {
        node,
        radius: 0.1 + rng() * 0.42,
        phase: rng() * Math.PI * 2,
        omega: (0.12 + rng() * 0.22) * (rng() > 0.5 ? 1 : -1),
        tilt: (rng() - 0.5) * 0.35
      };
      rig.add(sprite);
      list.push(sprite);
    }
    return list;
  }

  function buildNodes() {
    const list = [];
    for (let i = 0; i < NODE_IDS.length; i++) {
      const group = new THREE.Group();
      group.position.set(nodeX(i), 0, 0.04);
      const info = faceInfo(NODE_IDS[i]);
      const coreCol = new THREE.Color(info.core || "#FFE9B0");
      const bloom = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: coreCol,
        transparent: true,
        opacity: i === 0 ? 0.22 : 0.12,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      bloom.scale.set(i === 0 ? 2.4 : 1.35, i === 0 ? 0.85 : 0.55, 1);
      group.add(bloom);
      const core = new THREE.Sprite(new THREE.SpriteMaterial({
        map: starMap,
        color: 0xffffff,
        transparent: true,
        opacity: i === 0 ? 0.55 : 0.35,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      core.scale.set(i === 0 ? 0.22 : 0.14, i === 0 ? 0.22 : 0.14, 1);
      group.add(core);
      const label = makeLabel(info.title);
      label.position.y = -0.82;
      group.add(label);
      const hit = new THREE.Mesh(
        new THREE.SphereGeometry(0.38, 10, 8),
        new THREE.MeshBasicMaterial({ visible: false })
      );
      hit.userData.face = NODE_IDS[i];
      hit.userData.node = i;
      group.add(hit);
      group.userData.node = i;
      rig.add(group);
      list.push({ group, bloom, core, label, hit });
    }
    return list;
  }

  function buildVignette() {
    const tex = spriteTex(256, (ctx, s) => {
      const g = ctx.createRadialGradient(s / 2, s / 2, s * 0.28, s / 2, s / 2, s * 0.62);
      g.addColorStop(0, "rgba(0,0,0,0)");
      g.addColorStop(1, "rgba(0,0,0,0.55)");
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, s, s);
    });
    const mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(2, 2),
      new THREE.MeshBasicMaterial({ map: tex, transparent: true, depthTest: false, depthWrite: false })
    );
    mesh.frustumCulled = false;
    mesh.renderOrder = 10;
    return mesh;
  }

  function tickLiving(dt) {
    if (!state.motion) return;
    const pos = shear.geometry.attributes.position;
    const orbits = shear.userData.orbits;
    const arr = pos.array;
    for (let i = 0; i < orbits.length; i++) {
      const o = orbits[i];
      o.phase += o.omega * dt;
      arr[i * 3] = Math.cos(o.phase) * o.radius;
      arr[i * 3 + 1] = o.y;
      arr[i * 3 + 2] = Math.sin(o.phase) * o.radius * 0.08 + o.z;
    }
    pos.needsUpdate = true;

    for (const s of orbiters) {
      const u = s.userData;
      u.phase += u.omega * dt;
      const nx = nodeX(u.node);
      const pull = 0.08;
      s.position.x += (nx + Math.cos(u.phase) * u.radius - s.position.x) * pull;
      s.position.y = Math.sin(u.phase) * u.radius * 0.22 * Math.cos(u.tilt);
      s.position.z = Math.sin(u.phase * 0.7) * u.radius * 0.12;
    }
  }

  function setNode(index, instant) {
    state.node = wrapNode(index);
    state.targetX = nodeX(state.node);
    if (instant || !state.motion) state.visualX = state.targetX;
    const info = faceInfo(NODE_IDS[state.node]);
    syncCaption(info.title, info.hint);
  }

  function showOverlay(items, focus, origin, instant) {
    state.overlay = true;
    state.items = items || [];
    state.focus = typeof focus === "number" ? focus : 0;
    state.origin = origin === "bottom" ? "bottom" : "top";
    overlayEl.classList.remove("show", "ready", "from-top", "from-bottom", "motion");
    overlayEl.classList.add(state.origin === "bottom" ? "from-bottom" : "from-top");
    if (!instant && state.motion) overlayEl.classList.add("motion");
    renderTrack();
    applyOverlayFocus(true);
    overlayEl.classList.add("show");
    const arm = () => overlayEl.classList.add("ready");
    if (instant || !state.motion) arm();
    else requestAnimationFrame(() => requestAnimationFrame(arm));
  }

  function hideOverlay() {
    state.overlay = false;
    state.items = [];
    overlayEl.classList.remove("show", "ready");
    trackEl.innerHTML = "";
    const info = faceInfo(NODE_IDS[state.node]);
    syncCaption(info.title, info.hint);
  }

  function renderTrack() {
    trackEl.innerHTML = "";
    trackEl.classList.toggle("motion", state.motion);
    state.items.forEach((item, i) => {
      const el = document.createElement("button");
      el.type = "button";
      el.className = "row" + (state.motion ? " motion" : "") + (i === state.focus ? " focus" : "");
      el.innerHTML = `<span class="title">${escapeHtml(item.title || "")}</span><span class="meta">${escapeHtml(item.meta || "")}</span>`;
      el.addEventListener("click", (ev) => {
        ev.stopPropagation();
        if (i === state.focus) {
          send({ v: 1, type: "select", index: i, item: item.id, face: NODE_IDS[state.node] });
        } else {
          state.focus = i;
          applyOverlayFocus(false);
          send({ v: 1, type: "cycle", index: i, item: item.id });
        }
      });
      trackEl.appendChild(el);
    });
  }

  function applyOverlayFocus(instant) {
    const rows = trackEl.children;
    for (let i = 0; i < rows.length; i++) {
      rows[i].classList.toggle("focus", i === state.focus);
    }
    const item = state.items[state.focus];
    if (item) syncCaption(item.title, `${item.meta || ""} · up/down move · Enter opens · Esc returns`);
    const rowH = 52;
    const y = (state.items.length * 0.5 - state.focus - 0.5) * rowH;
    trackEl.style.transition = instant || !state.motion ? "none" : "";
    trackEl.style.transform = `translate3d(0, ${y}px, 0)`;
  }

  function startOpen(msg) {
    if (typeof msg.node === "number") setNode(msg.node, !state.motion);
    else if (msg.front) {
      const idx = NODE_IDS.indexOf(msg.front);
      if (idx >= 0) setNode(idx, !state.motion);
    }
    if (msg.motion === false) state.motion = false;
    const items = Array.isArray(msg.items) ? msg.items : previewItems(NODE_IDS[state.node]);
    const origin = String(msg.origin || "top").toLowerCase();
    const focus = typeof msg.focus === "number" ? msg.focus : (origin === "bottom" ? items.length - 1 : 0);
    if (items.length) showOverlay(items, focus, origin, !state.motion);
    send({ v: 1, type: "opened", face: NODE_IDS[state.node], stay: true });
    setLoop(true);
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
    const info = faceInfo(id);
    return [
      { id: id.toLowerCase(), title: info.title, meta: "PAGE" },
      { id: id.toLowerCase() + "-2", title: info.hint.replace("Up or Down opens ", ""), meta: info.meta }
    ];
  }

  function onHostMessage(data) {
    const type = data && data.type ? String(data.type).toLowerCase() : "state";
    if (type === "open") {
      startOpen(data);
      return;
    }
    if (type === "focus") {
      if (typeof data.focus === "number") {
        state.focus = data.focus;
        if (state.overlay) applyOverlayFocus(!state.motion);
      }
      return;
    }
    if (type === "reset") {
      hideOverlay();
      applyHostState({ ...data, type: "state" });
      return;
    }
    applyHostState(data);
  }

  function applyHostState(msg) {
    if (!msg || msg.type && msg.type !== "state") return;
    if (Array.isArray(msg.faces) && msg.faces.length) state.faces = msg.faces;
    if (typeof msg.node === "number") setNode(msg.node, msg.motion === false);
    else if (msg.front) {
      const idx = NODE_IDS.indexOf(msg.front);
      if (idx >= 0) setNode(idx, msg.motion === false);
    }
    state.motion = msg.motion !== false && !reduced;
    if (!state.motion) state.visualX = state.targetX;
    syncCaption(faceInfo(NODE_IDS[state.node]).title, faceInfo(NODE_IDS[state.node]).hint);
    setLoop(true);
    if (!state.motion) renderFrame(0);
  }

  function syncCaption(title, hint) {
    const t = document.getElementById("previewTitle");
    const h = document.getElementById("previewHint");
    if (t) t.textContent = title || "";
    if (h) h.textContent = hint || "Left/Right shift nodes · Up/Down open this list";
  }

  function renderFrame(dt) {
    const now = performance.now();
    const step = Math.min(Math.max(dt, 0), 0.033);
    if (state.motion) {
      const k = 1 - Math.exp(-step * 3.35);
      state.visualX += (state.targetX - state.visualX) * k;
      tickLiving(step);
      farStars.rotation.y = now * 0.000008;
      midStars.rotation.y = -now * 0.000014;
      nearStars.rotation.y = now * 0.000022;
    }
    rig.position.x = -state.visualX * 0.82;
    farStars.position.x = -state.visualX * 0.05;
    midStars.position.x = -state.visualX * 0.1;
    nearStars.position.x = -state.visualX * 0.2;
    camera.position.y = state.motion ? 0.02 + Math.sin(now * 0.00025) * 0.012 : 0.02;
    camera.lookAt(0, 0, 0);
    const pulse = state.motion ? 0.9 + 0.1 * Math.sin(now * 0.0011) : 0.92;
    for (const n of nodes) {
      const on = n.group.userData.node === state.node;
      n.bloom.material.opacity = (on ? 0.38 : 0.12) * pulse;
      n.core.material.opacity = on ? 0.75 : 0.28;
      n.label.material.opacity = on ? 0.42 : 0;
    }
    vignette.position.copy(camera.position);
    vignette.quaternion.copy(camera.quaternion);
    vignette.translateZ(-1.15);
    renderer.render(scene, camera);
  }

  let looping = false;
  function setLoop(on) {
    if (on && !looping) {
      looping = true;
      clock.getDelta();
      renderer.setAnimationLoop(() => renderFrame(Math.min(clock.getDelta(), 0.033)));
    } else if (!on && looping) {
      looping = false;
      renderer.setAnimationLoop(null);
    }
  }

  function resize() {
    const w = canvas.clientWidth || window.innerWidth;
    const h = canvas.clientHeight || window.innerHeight;
    renderer.setSize(w, h, false);
    camera.aspect = Math.max(w / Math.max(h, 1), 0.4);
    camera.updateProjectionMatrix();
    if (state.overlay) applyOverlayFocus(true);
    if (!looping) renderFrame(0);
  }

  function send(msg) {
    if (hosted) window.chrome.webview.postMessage(JSON.stringify(msg));
  }

  function onPointerDown(ev) {
    if (state.overlay) return;
    state.dragging = true;
    state.moved = false;
    state.pressX = ev.clientX;
    state.pressY = ev.clientY;
    state.lastX = ev.clientX;
    state.lastY = ev.clientY;
    state.lastT = performance.now();
    canvas.setPointerCapture?.(ev.pointerId);
  }

  function onPointerMove(ev) {
    if (!state.dragging) return;
    const dx = ev.clientX - state.pressX;
    const dy = ev.clientY - state.pressY;
    const now = performance.now();
    const dt = Math.max(0.001, (now - state.lastT) / 1000);
    state.vx = (ev.clientX - state.lastX) / dt;
    state.vy = (ev.clientY - state.lastY) / dt;
    state.lastX = ev.clientX;
    state.lastY = ev.clientY;
    state.lastT = now;
    if (Math.abs(dx) + Math.abs(dy) > 8) state.moved = true;
  }

  function onPointerUp(ev) {
    if (!state.dragging) return;
    state.dragging = false;
    canvas.releasePointerCapture?.(ev.pointerId);
    const dx = ev.clientX - state.pressX;
    const dy = ev.clientY - state.pressY;
    if (state.moved) {
      send({ v: 1, type: "dragEnd", dx, dy, vx: state.vx, vy: state.vy });
      if (!hosted) {
        if (Math.abs(dx) >= Math.abs(dy)) localTurn(dx < 0 ? "Right" : "Left");
        else localTurn(dy > 0 ? "Down" : "Up");
      }
      return;
    }
    pick(ev.clientX, ev.clientY);
  }

  function pick(x, y) {
    const rect = canvas.getBoundingClientRect();
    pointer.x = ((x - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((y - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(nodeMeshes, false);
    if (hits.length) {
      const face = hits[0].object.userData.face;
      if (hosted) {
        send({ v: 1, type: "pick", face });
        return;
      }
      const idx = NODE_IDS.indexOf(face);
      if (idx === state.node) previewOpen("top");
      else setNode(idx, false);
      return;
    }
    if (hosted) send({ v: 1, type: "activate" });
    else previewOpen("top");
  }

  function previewOpen(origin) {
    const items = previewItems(NODE_IDS[state.node]);
    startOpen({
      front: NODE_IDS[state.node],
      node: state.node,
      motion: state.motion,
      origin,
      focus: origin === "bottom" ? items.length - 1 : 0,
      items
    });
  }

  function localTurn(turn) {
    if (turn === "Left") setNode(visualNeighbor(-1), !state.motion);
    if (turn === "Right") setNode(visualNeighbor(1), !state.motion);
    if (turn === "Up") previewOpen("bottom");
    if (turn === "Down") previewOpen("top");
  }

  function visualNeighbor(delta) {
    const order = [5, 4, 3, 0, 1, 2];
    const at = order.indexOf(state.node);
    return order[wrapIndex(at + delta, order.length)];
  }

  function onKey(ev) {
    if (state.overlay) {
      if (ev.key === "Escape") {
        ev.preventDefault();
        send({ v: 1, type: "back" });
        if (!hosted) hideOverlay();
        return;
      }
      if (ev.key === "Enter" || ev.key === " ") {
        ev.preventDefault();
        const item = state.items[state.focus];
        if (item) send({ v: 1, type: "select", index: state.focus, item: item.id, face: NODE_IDS[state.node] });
        return;
      }
      if (ev.key === "ArrowUp" || ev.key === "ArrowDown") {
        ev.preventDefault();
        state.focus = wrapIndex(state.focus + (ev.key === "ArrowUp" ? -1 : 1), state.items.length);
        applyOverlayFocus(false);
        send({ v: 1, type: "cycle", index: state.focus, item: state.items[state.focus].id });
      }
      return;
    }
    if (ev.key === "Enter" || ev.key === " ") {
      ev.preventDefault();
      if (hosted) send({ v: 1, type: "activate" });
      else previewOpen("top");
      return;
    }
    const map = { ArrowLeft: "Left", ArrowRight: "Right", ArrowUp: "Up", ArrowDown: "Down" };
    const turn = map[ev.key];
    if (!turn) return;
    ev.preventDefault();
    if (hosted) send({ v: 1, type: "turn", turn });
    else localTurn(turn);
  }

  function onWheel(ev) {
    ev.preventDefault();
    if (state.overlay) {
      state.focus = wrapIndex(state.focus + (ev.deltaY < 0 ? -1 : 1), state.items.length);
      applyOverlayFocus(false);
      send({ v: 1, type: "cycle", index: state.focus, item: state.items[state.focus].id });
      return;
    }
    const turn = ev.deltaY < 0 ? "Left" : "Right";
    if (hosted) send({ v: 1, type: "turn", turn });
    else localTurn(turn);
  }

  function wrapIndex(i, n) {
    if (n <= 0) return 0;
    return ((i % n) + n) % n;
  }

  function makeLabel(text) {
    const tex = spriteTex(512, (ctx, s) => {
      ctx.clearRect(0, 0, s, s);
      ctx.font = "600 62px 'Segoe UI', sans-serif";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillStyle = "rgba(244,239,226,0.92)";
      ctx.shadowColor = "rgba(0,0,0,0.85)";
      ctx.shadowBlur = 16;
      ctx.fillText(String(text || "").toUpperCase(), s / 2, s / 2);
    });
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({
      map: tex,
      transparent: true,
      opacity: 0.08,
      depthWrite: false
    }));
    sprite.scale.set(1.1, 0.22, 1);
    return sprite;
  }

  function spriteTex(size, draw) {
    const c = document.createElement("canvas");
    c.width = c.height = size;
    draw(c.getContext("2d"), size);
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    return tex;
  }

  function mulberry(a) {
    return function rng() {
      a |= 0;
      a = a + 0x6D2B79F5 | 0;
      let t = Math.imul(a ^ a >>> 15, 1 | a);
      t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t;
      return ((t ^ t >>> 14) >>> 0) / 4294967296;
    };
  }

  function gauss(rng) {
    return (rng() + rng() + rng() - 1.5) * 0.85;
  }

  function escapeHtml(s) {
    return String(s).replace(/[&<>"']/g, (ch) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", "\"": "&quot;", "'": "&#39;" }[ch]));
  }

  function showFallback() {
    if (fallback) fallback.hidden = false;
    if (canvas) canvas.style.display = "none";
  }

  canvas.addEventListener("pointerdown", onPointerDown);
  window.addEventListener("pointermove", onPointerMove);
  window.addEventListener("pointerup", onPointerUp);
  window.addEventListener("pointercancel", onPointerUp);
  window.addEventListener("keydown", onKey);
  canvas.addEventListener("wheel", onWheel, { passive: false });
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
  setLoop(true);
  if (!state.motion) renderFrame(0);
})();
