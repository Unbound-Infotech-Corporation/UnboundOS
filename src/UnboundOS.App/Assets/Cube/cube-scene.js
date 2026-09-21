/* UnboundOS Home — original biomechanical control altar.
   Scorn-class art direction as inspiration only. No third-party assets. */
(() => {
  "use strict";

  const PX_PER_TURN = 96;
  const SPRING_K = 86;
  const SPRING_D = 15;
  const LAYOUT_K = 72;
  const LAYOUT_D = 15;
  const DEFAULT_FACES = [
    { id: "Session", title: "Games", kicker: "PLAY", monogram: "G", meta: "PLAY", hint: "Installed library. Cycle the blocks, Enter launches.", accent: "#6FA896", seam: "#8A6B4A", core: "#5C2A2E", plate: "#2A221C", glyph: "games" },
    { id: "Tools", title: "Tools", kicker: "KIT", monogram: "T", meta: "OPEN", hint: "OBS, Vortex, Discord, Playnite, utilities.", accent: "#7A8B6A", seam: "#6B4A38", core: "#4A3428", plate: "#1C1814", glyph: "tools" },
    { id: "Hardware", title: "Hardware", kicker: "HW", monogram: "H", meta: "READ", hint: "Inventory from this PC. Optional official HWiNFO.", accent: "#7A4A42", seam: "#8A7060", core: "#2A1C1A", plate: "#141010", glyph: "hardware" },
    { id: "Network", title: "Network", kicker: "LINK", monogram: "N", meta: "SPLIT", hint: "Prefer a game NIC. Park bulk traffic.", accent: "#6A5A78", seam: "#8A6B4A", core: "#3A2A38", plate: "#1A1418", glyph: "network" },
    { id: "Mods", title: "Mods", kicker: "MOD", monogram: "M", meta: "OPEN", hint: "Workshop and Vortex discovery. Vortex stays in charge.", accent: "#8A7A58", seam: "#5C3A32", core: "#4A2E28", plate: "#181410", glyph: "mods" },
    { id: "Files", title: "Files", kicker: "FS", monogram: "F", meta: "BROWSE", hint: "Daily folders. Explorer stays for anticheat.", accent: "#9A8A72", seam: "#6A5848", core: "#3A3028", plate: "#1A1612", glyph: "files" }
  ];

  const FACE_LAYOUT = {
    Session: { dir: [0, 0, 1], up: [0, 1, 0] },
    Tools: { dir: [1, 0, 0], up: [0, 1, 0] },
    Hardware: { dir: [0, 0, -1], up: [0, 1, 0] },
    Network: { dir: [-1, 0, 0], up: [0, 1, 0] },
    Mods: { dir: [0, 1, 0], up: [0, 0, -1] },
    Files: { dir: [0, -1, 0], up: [0, 0, 1] }
  };

  const BILE = "#C4A46A";
  const BLOOD = "#5C2A2E";
  const CAVITY = 0x080605;

  const params = new URLSearchParams(location.search);
  const preview = params.has("preview");
  if (preview) {
    document.body.classList.add("preview");
  }

  const canvas = document.getElementById("gl");
  const fallback = document.getElementById("fallback");
  const hosted = Boolean(window.chrome?.webview);
  const reduced = window.matchMedia?.("(prefers-reduced-motion: reduce)")?.matches === true;

  if (typeof THREE === "undefined" || !canvas) {
    showFallback();
    return;
  }

  const state = {
    faces: DEFAULT_FACES,
    yaw: 0,
    pitch: 0,
    restYaw: 28,
    restPitch: -17,
    visualYaw: 0,
    visualPitch: 0,
    yawVel: 0,
    pitchVel: 0,
    front: "Session",
    motion: !reduced,
    accent: new THREE.Color("#6FA896"),
    targetAccent: new THREE.Color("#6FA896"),
    seam: new THREE.Color("#8A6B4A"),
    targetSeam: new THREE.Color("#8A6B4A"),
    core: new THREE.Color("#5C2A2E"),
    targetCore: new THREE.Color("#5C2A2E"),
    burstUntil: 0,
    idle: 0,
    dragging: false,
    moved: false,
    pressX: 0,
    pressY: 0,
    lastX: 0,
    lastY: 0,
    lastT: 0,
    vx: 0,
    vy: 0,
    poseYaw: 0,
    posePitch: 0,
    opening: false,
    openT: 0,
    openDuration: 1.08,
    browse: false,
    stay: false,
    mode: "list",
    items: [],
    focus: 0,
    lastFocus: -1,
    camZ: 5.7,
    camY: 1.52
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
  renderer.setClearColor(CAVITY, 1);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.75));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 0.68;
  renderer.shadowMap.enabled = true;
  renderer.shadowMap.type = THREE.PCFSoftShadowMap;

  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(CAVITY, 0.068);
  scene.environment = makeEnvMap();

  const camera = new THREE.PerspectiveCamera(26, 1, 0.08, 48);
  camera.position.set(0, 1.52, 5.7);
  camera.lookAt(0, -0.28, 0);
  const _out = new THREE.Vector3();
  const _away = new THREE.Vector3();

  const restRig = new THREE.Group();
  const spinRig = new THREE.Group();
  scene.add(restRig);
  restRig.add(spinRig);

  const artifact = new THREE.Group();
  spinRig.add(artifact);

  const plates = [];
  const plateMats = [];
  const arcs = [];
  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const clock = new THREE.Clock();

  const boneShared = paintBoneStack(0x6b0a11);
  const oxidizedShared = paintOxidized(0x4a2e18);

  const marrowLight = new THREE.PointLight(0xc48a3c, 0.35, 5.5, 1.6);
  marrowLight.position.set(0, -0.08, 0.12);
  artifact.add(marrowLight);

  const veinLight = new THREE.PointLight(0x6fa896, 0.16, 4.2, 1.8);
  veinLight.position.set(0.18, 0.12, 0.22);
  artifact.add(veinLight);

  const bruiseKick = new THREE.PointLight(0x6a5a78, 0.22, 5.2, 2.1);
  bruiseKick.position.set(-0.45, -0.15, 0.55);
  artifact.add(bruiseKick);

  const key = new THREE.DirectionalLight(0xe8d4b0, 0.62);
  key.position.set(-2.8, 5.1, 3.6);
  key.castShadow = true;
  key.shadow.mapSize.set(1024, 1024);
  key.shadow.camera.near = 1;
  key.shadow.camera.far = 16;
  key.shadow.camera.left = -4.2;
  key.shadow.camera.right = 4.2;
  key.shadow.camera.top = 4.2;
  key.shadow.camera.bottom = -4.2;
  key.shadow.bias = -0.0007;
  scene.add(key);

  const fill = new THREE.DirectionalLight(0x3a2a32, 0.2);
  fill.position.set(2.6, 0.15, 4.2);
  scene.add(fill);

  const rim = new THREE.DirectionalLight(0xc5d4d8, 1.85);
  rim.position.set(3.8, 1.6, -3.1);
  scene.add(rim);

  const cavityGlow = new THREE.PointLight(0xc48a3c, 2.6, 8.5, 1.15);
  cavityGlow.position.set(0.15, -0.62, 0.85);
  scene.add(cavityGlow);

  scene.add(new THREE.HemisphereLight(0x8a7a68, 0x0c0806, 0.16));
  scene.add(new THREE.AmbientLight(0x140e0c, 0.06));

  const openSpot = new THREE.SpotLight(0xe8c9a0, 0, 16, 0.5, 0.7, 1.25);
  openSpot.position.set(-1.4, 4.8, 3.4);
  openSpot.target.position.set(0, 0, 0);
  scene.add(openSpot);
  scene.add(openSpot.target);

  const creviceVein = new THREE.PointLight(0x6fa896, 0, 3.4, 1.6);
  const creviceBruise = new THREE.PointLight(0x6a5a78, 0, 3.2, 1.7);
  const creviceAmber = new THREE.PointLight(0xc4a46a, 0, 2.8, 1.8);
  creviceVein.position.set(0.35, 0.2, 0.4);
  creviceBruise.position.set(-0.4, -0.15, 0.25);
  creviceAmber.position.set(0.1, 0.45, -0.3);
  artifact.add(creviceVein, creviceBruise, creviceAmber);

  const chassis = new THREE.Mesh(
    new THREE.BoxGeometry(1.58, 1.58, 1.58),
    boneMat(boneShared.map, boneShared.bump, { color: 0x8a7358, roughness: 0.7 })
  );
  chassis.castShadow = true;
  chassis.receiveShadow = true;

  const hullGroup = new THREE.Group();
  artifact.add(hullGroup);
  hullGroup.add(chassis);

  dressCore();
  buildPlates();
  const floor = buildBay();
  const haze = buildHaze();
  const motes = buildMotes();
  const bricks = buildGreeble();
  const shafts = buildShafts();

  function faceInfo(id) {
    return state.faces.find((f) => f.id === id) || DEFAULT_FACES[0];
  }

  function boneMat(map, bump, extra) {
    return new THREE.MeshPhysicalMaterial(Object.assign({
      map,
      bumpMap: bump,
      bumpScale: 0.042,
      color: 0xe8dcc4,
      roughness: 0.58,
      metalness: 0.08,
      clearcoat: 0.62,
      clearcoatRoughness: 0.28,
      sheen: 0.38,
      sheenColor: new THREE.Color(0xc4a090),
      sheenRoughness: 0.52,
      envMapIntensity: 0.55
    }, extra || {}));
  }

  function oxidizedMat(map, extra) {
    return new THREE.MeshPhysicalMaterial(Object.assign({
      map: map || oxidizedShared.map,
      bumpMap: oxidizedShared.bump,
      bumpScale: 0.03,
      color: 0x3a2e26,
      roughness: 0.48,
      metalness: 0.82,
      clearcoat: 0.22,
      clearcoatRoughness: 0.72,
      envMapIntensity: 1.18
    }, extra || {}));
  }

  function cartilageMat(extra) {
    return new THREE.MeshPhysicalMaterial(Object.assign({
      color: 0xb8a090,
      roughness: 0.68,
      metalness: 0.03,
      sheen: 0.82,
      sheenColor: new THREE.Color(0x8a6a78),
      sheenRoughness: 0.38,
      clearcoat: 0.58,
      clearcoatRoughness: 0.26,
      envMapIntensity: 0.85
    }, extra || {}));
  }

  function paintBoneStack(seed) {
    const size = 512;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const h = document.createElement("canvas");
    h.width = h.height = size;
    const ctx = c.getContext("2d");
    const hx = h.getContext("2d");
    const rng = mulberry(seed);
    fillIvory(ctx, size, rng);
    pores(ctx, hx, size, rng, 4200);
    haversian(ctx, hx, size, rng, 18);
    sutures(ctx, hx, size, rng, 14);
    marrow(ctx, size, rng, 10);
    grain(ctx, size, rng);
    const map = canvasTex(c, true);
    const bump = canvasTex(h, false);
    map.wrapS = map.wrapT = bump.wrapS = bump.wrapT = THREE.RepeatWrapping;
    return { map, bump };
  }

  function paintOxidized(seed) {
    const size = 512;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const h = document.createElement("canvas");
    h.width = h.height = size;
    const ctx = c.getContext("2d");
    const hx = h.getContext("2d");
    const rng = mulberry(seed);
    ctx.fillStyle = "#3A3028";
    ctx.fillRect(0, 0, size, size);
    hx.fillStyle = "#808080";
    hx.fillRect(0, 0, size, size);
    for (let i = 0; i < 40; i++) {
      ctx.fillStyle = i % 2 ? "rgba(90,70,48,0.35)" : "rgba(28,22,18,0.4)";
      ctx.beginPath();
      ctx.ellipse(rng() * size, rng() * size, 20 + rng() * 80, 10 + rng() * 40, rng() * Math.PI, 0, Math.PI * 2);
      ctx.fill();
    }
    gritWarm(ctx, size, rng, 5000);
    scratches(ctx, hx, size, rng, 90);
    const map = canvasTex(c, true);
    const bump = canvasTex(h, false);
    return { map, bump };
  }

  function paintPlate(info) {
    const size = 1024;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const h = document.createElement("canvas");
    h.width = h.height = size;
    const e = document.createElement("canvas");
    e.width = e.height = size;
    const ctx = c.getContext("2d");
    const hx = h.getContext("2d");
    const ex = e.getContext("2d");
    const rng = mulberry(hash(info.id + "-bone"));
    fillIvory(ctx, size, rng);
    hx.fillStyle = "#888888";
    hx.fillRect(0, 0, size, size);
    ex.fillStyle = "#000";
    ex.fillRect(0, 0, size, size);
    pores(ctx, hx, size, rng, 7600);
    haversian(ctx, hx, size, rng, 22);
    sutures(ctx, hx, size, rng, 16);
    marrow(ctx, size, rng, 12);
    grain(ctx, size, rng);
    carveFrame(ctx, hx, size);
    carveSocket(ctx, hx, ex, size, info);
    capillaries(ctx, ex, size, rng, info);
    etchGlyph(ctx, hx, ex, size, info);
    return {
      map: canvasTex(c, true),
      bump: canvasTex(h, false),
      emissive: canvasTex(e, false)
    };
  }

  function fillIvory(ctx, size, rng) {
    ctx.fillStyle = "#8A7358";
    ctx.fillRect(0, 0, size, size);
    const vg = ctx.createRadialGradient(size * 0.38, size * 0.28, 10, size * 0.5, size * 0.52, size * 0.82);
    vg.addColorStop(0, "rgba(196, 176, 148, 0.55)");
    vg.addColorStop(0.4, "rgba(110, 86, 64, 0.22)");
    vg.addColorStop(1, "rgba(28, 18, 14, 0.72)");
    ctx.fillStyle = vg;
    ctx.fillRect(0, 0, size, size);
    gritWarm(ctx, size, rng, 6400);
  }

  function gritWarm(ctx, size, rng, count) {
    ctx.save();
    for (let i = 0; i < count; i++) {
      const n = 90 + rng() * 80;
      ctx.globalAlpha = 0.05 + rng() * 0.12;
      ctx.fillStyle = `rgb(${n | 0},${(n * 0.86) | 0},${(n * 0.68) | 0})`;
      ctx.fillRect(rng() * size, rng() * size, 1 + rng() * 2.4, 1);
    }
    ctx.restore();
  }

  function pores(ctx, hx, size, rng, count) {
    ctx.save();
    for (let i = 0; i < count; i++) {
      const x = rng() * size;
      const y = rng() * size;
      const r = 0.4 + rng() * 1.6;
      ctx.globalAlpha = 0.12 + rng() * 0.28;
      ctx.fillStyle = rng() > 0.7 ? "rgba(92,42,46,0.55)" : "rgba(58,44,32,0.7)";
      ctx.beginPath();
      ctx.arc(x, y, r, 0, Math.PI * 2);
      ctx.fill();
      hx.fillStyle = "rgba(20,20,20,0.55)";
      hx.beginPath();
      hx.arc(x, y, r * 1.2, 0, Math.PI * 2);
      hx.fill();
    }
    ctx.restore();
  }

  function haversian(ctx, hx, size, rng, count) {
    ctx.save();
    for (let i = 0; i < count; i++) {
      const x = rng() * size;
      const y = rng() * size;
      const rx = 10 + rng() * 28;
      const ry = 8 + rng() * 18;
      ctx.strokeStyle = "rgba(90,70,52,0.28)";
      ctx.lineWidth = 1.2;
      for (let k = 1; k <= 3; k++) {
        ctx.beginPath();
        ctx.ellipse(x, y, rx * k * 0.55, ry * k * 0.55, rng(), 0, Math.PI * 2);
        ctx.stroke();
      }
      hx.strokeStyle = "rgba(30,30,30,0.35)";
      hx.lineWidth = 2;
      hx.beginPath();
      hx.ellipse(x, y, rx, ry, 0, 0, Math.PI * 2);
      hx.stroke();
    }
    ctx.restore();
  }

  function sutures(ctx, hx, size, rng, count) {
    ctx.save();
    ctx.lineCap = "round";
    for (let i = 0; i < count; i++) {
      let x = rng() * size;
      let y = rng() * size;
      ctx.strokeStyle = "rgba(42, 28, 22, 0.55)";
      ctx.lineWidth = 1.4 + rng();
      ctx.beginPath();
      ctx.moveTo(x, y);
      hx.beginPath();
      hx.moveTo(x, y);
      const segs = 6 + ((rng() * 8) | 0);
      for (let s = 0; s < segs; s++) {
        x += rng() * 48 - 10;
        y += rng() * 36 - 8;
        ctx.lineTo(x, y);
        hx.lineTo(x, y);
      }
      ctx.stroke();
      hx.strokeStyle = "rgba(18,18,18,0.5)";
      hx.lineWidth = 3;
      hx.stroke();
    }
    ctx.restore();
  }

  function marrow(ctx, size, rng, count) {
    ctx.save();
    for (let i = 0; i < count; i++) {
      const g = ctx.createRadialGradient(rng() * size, rng() * size, 4, rng() * size, rng() * size, 40 + rng() * 90);
      g.addColorStop(0, i % 3 === 0 ? "rgba(92,42,46,0.18)" : "rgba(106,90,120,0.12)");
      g.addColorStop(1, "rgba(0,0,0,0)");
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, size, size);
    }
    ctx.restore();
  }

  function grain(ctx, size, rng) {
    ctx.save();
    ctx.globalAlpha = 0.06;
    ctx.strokeStyle = "rgba(90,70,50,1)";
    ctx.lineWidth = 1;
    for (let i = 0; i < 70; i++) {
      const y = rng() * size;
      ctx.beginPath();
      ctx.moveTo(0, y);
      ctx.lineTo(size, y + rng() * 8 - 4);
      ctx.stroke();
    }
    ctx.restore();
  }

  function scratches(ctx, hx, size, rng, count) {
    ctx.save();
    ctx.strokeStyle = "rgba(196, 176, 140, 0.12)";
    ctx.lineWidth = 1;
    for (let i = 0; i < count; i++) {
      const x = rng() * size;
      const y = rng() * size;
      ctx.beginPath();
      ctx.moveTo(x, y);
      ctx.lineTo(x + rng() * 90 - 16, y + rng() * 14 - 7);
      ctx.stroke();
    }
    ctx.restore();
  }

  function carveFrame(ctx, hx, size) {
    ctx.save();
    ctx.strokeStyle = "rgba(42, 32, 24, 0.72)";
    ctx.lineWidth = 38;
    ctx.strokeRect(28, 28, size - 56, size - 56);
    ctx.strokeStyle = "rgba(232, 214, 184, 0.28)";
    ctx.lineWidth = 6;
    ctx.strokeRect(52, 52, size - 104, size - 104);
    ctx.restore();
    hx.save();
    hx.strokeStyle = "#202020";
    hx.lineWidth = 28;
    hx.strokeRect(28, 28, size - 56, size - 56);
    hx.restore();
  }

  function carveSocket(ctx, hx, ex, size, info) {
    const cx = size * 0.5;
    const cy = size * 0.66;
    ctx.save();
    ctx.strokeStyle = "rgba(22, 14, 12, 0.82)";
    ctx.lineWidth = 14;
    for (let r = 118; r >= 52; r -= 22) {
      ctx.beginPath();
      ctx.arc(cx, cy, r, 0, Math.PI * 2);
      ctx.stroke();
      hx.strokeStyle = "#141414";
      hx.lineWidth = 8;
      hx.beginPath();
      hx.arc(cx, cy, r, 0, Math.PI * 2);
      hx.stroke();
    }
    ctx.fillStyle = "rgba(8, 6, 5, 0.88)";
    ctx.beginPath();
    ctx.arc(cx, cy, 44, 0, Math.PI * 2);
    ctx.fill();
    ctx.strokeStyle = hexAlpha(info.seam || "#8A6B4A", 0.55);
    ctx.lineWidth = 4;
    ctx.beginPath();
    ctx.arc(cx, cy, 64, 0, Math.PI * 2);
    ctx.stroke();
    ctx.restore();
    ex.save();
    ex.strokeStyle = hexAlpha(info.accent || "#6FA896", 0.7);
    ex.lineWidth = 2.4;
    ex.beginPath();
    ex.arc(cx, cy, 64, 0, Math.PI * 2);
    ex.stroke();
    ex.restore();
  }

  function capillaries(ctx, ex, size, rng, info) {
    const accent = info.accent || "#6FA896";
    ctx.save();
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    for (let i = 0; i < 9; i++) {
      let x = 80 + rng() * 860;
      let y = 80 + rng() * 860;
      ctx.strokeStyle = "rgba(42, 28, 22, 0.55)";
      ctx.lineWidth = 3.2;
      ctx.beginPath();
      ctx.moveTo(x, y);
      ex.beginPath();
      ex.moveTo(x, y);
      const segs = 4 + ((rng() * 5) | 0);
      for (let s = 0; s < segs; s++) {
        x += rng() * 80 - 28;
        y += rng() * 70 - 22;
        ctx.lineTo(x, y);
        ex.lineTo(x, y);
      }
      ctx.stroke();
      ex.strokeStyle = hexAlpha(i % 4 === 0 ? BILE : accent, 0.42);
      ex.lineWidth = 1.6;
      ex.stroke();
    }
    ctx.restore();
  }

  function etchGlyph(ctx, hx, ex, size, info) {
    const glyph = String(info.glyph || info.id || "games").toLowerCase();
    ctx.save();
    ctx.translate(size * 0.5, size * 0.3);
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    ctx.strokeStyle = "rgba(28, 18, 14, 0.92)";
    ctx.fillStyle = "rgba(22, 14, 12, 0.35)";
    ctx.lineWidth = 13;
    drawGlyph(ctx, glyph);
    ctx.strokeStyle = "rgba(214, 196, 164, 0.28)";
    ctx.lineWidth = 3;
    drawGlyph(ctx, glyph);
    ctx.restore();

    hx.save();
    hx.translate(size * 0.5, size * 0.3);
    hx.lineCap = "round";
    hx.strokeStyle = "#101010";
    hx.lineWidth = 14;
    drawGlyph(hx, glyph);
    hx.restore();

    ex.save();
    ex.translate(size * 0.5, size * 0.3);
    ex.lineCap = "round";
    ex.strokeStyle = hexAlpha(info.accent || "#6FA896", 0.7);
    ex.lineWidth = 2.2;
    drawGlyph(ex, glyph);
    ex.restore();

    const label = String(info.title || "").toUpperCase();
    if (!label) return;
    ctx.save();
    ctx.textAlign = "center";
    ctx.font = "600 32px 'Segoe UI', serif";
    const spaced = label.split("").join("  ");
    ctx.fillStyle = "rgba(42, 32, 24, 0.78)";
    ctx.fillText(spaced, size * 0.5, size * 0.82);
    ctx.fillStyle = "rgba(214, 196, 164, 0.22)";
    ctx.fillText(spaced, size * 0.5, size * 0.818);
    ctx.restore();
  }

  function drawGlyph(ctx, glyph) {
    ctx.beginPath();
    if (glyph === "games") {
      rounded(ctx, -132, -78, 264, 156, 18);
      ctx.stroke();
      ctx.beginPath();
      ctx.arc(-64, 0, 30, 0, Math.PI * 2);
      ctx.moveTo(-64, -16);
      ctx.lineTo(-64, 16);
      ctx.moveTo(-80, 0);
      ctx.lineTo(-48, 0);
      ctx.stroke();
      for (const [x, y] of [[62, -20], [90, 0], [62, 20], [34, 0]]) {
        ctx.beginPath();
        ctx.arc(x, y, 9, 0, Math.PI * 2);
        ctx.stroke();
      }
      return;
    }
    if (glyph === "tools") {
      ctx.moveTo(-18, -98);
      ctx.lineTo(18, -98);
      ctx.lineTo(18, -16);
      ctx.lineTo(62, 62);
      ctx.lineTo(36, 88);
      ctx.lineTo(-36, 18);
      ctx.lineTo(-62, 44);
      ctx.lineTo(-88, 18);
      ctx.lineTo(-18, -52);
      ctx.closePath();
      ctx.stroke();
      ctx.strokeRect(-16, -16, 32, 32);
      return;
    }
    if (glyph === "network") {
      ctx.arc(-80, 36, 20, 0, Math.PI * 2);
      ctx.stroke();
      ctx.beginPath();
      ctx.arc(80, 36, 20, 0, Math.PI * 2);
      ctx.stroke();
      ctx.beginPath();
      ctx.arc(0, -62, 24, 0, Math.PI * 2);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(-64, 24);
      ctx.lineTo(-16, -46);
      ctx.moveTo(64, 24);
      ctx.lineTo(16, -46);
      ctx.moveTo(-60, 36);
      ctx.lineTo(60, 36);
      ctx.stroke();
      return;
    }
    if (glyph === "mods") {
      ctx.strokeRect(-98, -44, 80, 80);
      ctx.strokeRect(-18, -98, 80, 80);
      ctx.strokeRect(18, -8, 80, 80);
      return;
    }
    if (glyph === "files") {
      ctx.moveTo(-98, -36);
      ctx.lineTo(-36, -36);
      ctx.lineTo(-10, -72);
      ctx.lineTo(98, -72);
      ctx.lineTo(98, 80);
      ctx.lineTo(-98, 80);
      ctx.closePath();
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(-72, -8);
      ctx.lineTo(72, -8);
      ctx.moveTo(-72, 28);
      ctx.lineTo(72, 28);
      ctx.stroke();
      return;
    }
    if (glyph === "hardware") {
      ctx.strokeRect(-80, -62, 160, 124);
      ctx.strokeRect(-44, -26, 88, 52);
      for (let i = -54; i <= 54; i += 27) {
        ctx.moveTo(i, -62);
        ctx.lineTo(i, -84);
        ctx.moveTo(i, 62);
        ctx.lineTo(i, 84);
      }
      ctx.stroke();
      return;
    }
    ctx.strokeRect(-70, -70, 140, 140);
  }

  function rounded(ctx, x, y, w, h, r) {
    ctx.moveTo(x + r, y);
    ctx.lineTo(x + w - r, y);
    ctx.quadraticCurveTo(x + w, y, x + w, y + r);
    ctx.lineTo(x + w, y + h - r);
    ctx.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
    ctx.lineTo(x + r, y + h);
    ctx.quadraticCurveTo(x, y + h, x, y + h - r);
    ctx.lineTo(x, y + r);
    ctx.quadraticCurveTo(x, y, x + r, y);
  }

  function dressCore() {
    const ox = oxidizedMat();
    const cart = cartilageMat();
    const h = 0.79;
    const len = h * 2;
    const edges = [
      { p: [0, h, h], r: [0, 0, Math.PI / 2] },
      { p: [0, h, -h], r: [0, 0, Math.PI / 2] },
      { p: [0, -h, h], r: [0, 0, Math.PI / 2] },
      { p: [0, -h, -h], r: [0, 0, Math.PI / 2] },
      { p: [h, 0, h], r: [0, 0, 0] },
      { p: [h, 0, -h], r: [0, 0, 0] },
      { p: [-h, 0, h], r: [0, 0, 0] },
      { p: [-h, 0, -h], r: [0, 0, 0] },
      { p: [h, h, 0], r: [Math.PI / 2, 0, 0] },
      { p: [h, -h, 0], r: [Math.PI / 2, 0, 0] },
      { p: [-h, h, 0], r: [Math.PI / 2, 0, 0] },
      { p: [-h, -h, 0], r: [Math.PI / 2, 0, 0] }
    ];
    for (const e of edges) {
      const rib = new THREE.Mesh(new THREE.CylinderGeometry(0.048, 0.062, len, 8), cart);
      rib.position.set(e.p[0], e.p[1], e.p[2]);
      rib.rotation.set(e.r[0], e.r[1], e.r[2]);
      rib.castShadow = true;
      hullGroup.add(rib);
    }
    for (const x of [-h, h]) {
      for (const y of [-h, h]) {
        for (const z of [-h, h]) {
          const boss = new THREE.Mesh(new THREE.SphereGeometry(0.11, 12, 10), cartilageMat({ color: 0x6a5044, roughness: 0.62 }));
          boss.position.set(x, y, z);
          boss.castShadow = true;
          hullGroup.add(boss);
          const rivet = new THREE.Mesh(new THREE.SphereGeometry(0.035, 8, 6), ox);
          rivet.position.set(x * 0.92, y * 0.92, z * 0.92);
          hullGroup.add(rivet);
        }
      }
    }
    const tendons = [
      [[-h, -h, -h], [h, h, -h]],
      [[-h, h, h], [h, -h, h]],
      [[-h, -h, h], [h, h, h]],
      [[h, -h, -h], [-h, h, h]]
    ];
    for (const [a, b] of tendons) {
      const curve = new THREE.CatmullRomCurve3([
        new THREE.Vector3(...a),
        new THREE.Vector3((a[0] + b[0]) * 0.5, (a[1] + b[1]) * 0.5 + 0.12, (a[2] + b[2]) * 0.5),
        new THREE.Vector3(...b)
      ]);
      const tube = new THREE.Mesh(
        new THREE.TubeGeometry(curve, 24, 0.018, 6, false),
        cartilageMat({ color: 0x8a6b4a, roughness: 0.55, clearcoat: 0.7 })
      );
      hullGroup.add(tube);
    }
  }

  function buildPlates() {
    const geom = new THREE.BoxGeometry(1.62, 1.62, 0.055);
    for (const info of state.faces) {
      const layout = FACE_LAYOUT[info.id];
      if (!layout) continue;
      const dir = new THREE.Vector3(...layout.dir);
      const up = new THREE.Vector3(...layout.up);
      const maps = paintPlate(info);
      const mat = boneMat(maps.map, maps.bump, {
        color: 0xc4b090,
        emissive: new THREE.Color(info.accent || "#6FA896"),
        emissiveMap: maps.emissive,
        emissiveIntensity: info.id === state.front ? 0.22 : 0.08,
        roughness: 0.56
      });
      const plate = new THREE.Mesh(geom, mat);
      plate.position.copy(dir).multiplyScalar(0.84);
      orientPlate(plate, dir, up);
      plate.userData.face = info.id;
      plate.userData.dir = dir.clone();
      plate.userData.restPos = plate.position.clone();
      plate.userData.peel = 0.12 + (hash(info.id) % 10) * 0.012;
      plate.castShadow = true;
      plate.receiveShadow = true;
      hullGroup.add(plate);
      plates.push(plate);
      plateMats.push(mat);
      addSocket(plate, info);
    }
  }

  function addSocket(plate, info) {
    const ox = oxidizedMat(null, { color: 0x2e261e });
    const cart = cartilageMat({ color: 0x6a5048 });
    const socket = new THREE.Group();
    socket.position.set(0, -0.28, 0);
    plate.add(socket);
    const lip = new THREE.Mesh(new THREE.TorusGeometry(0.26, 0.042, 10, 28), ox);
    lip.position.z = 0.042;
    socket.add(lip);
    const iris = new THREE.Mesh(new THREE.TorusGeometry(0.16, 0.016, 8, 22), cart);
    iris.position.z = 0.05;
    socket.add(iris);
    const well = new THREE.Mesh(
      new THREE.RingGeometry(0.09, 0.155, 24),
      new THREE.MeshPhysicalMaterial({
        color: 0x080605,
        roughness: 0.55,
        metalness: 0.35,
        emissive: new THREE.Color(info.accent || "#6FA896"),
        emissiveIntensity: 0.12
      })
    );
    well.position.z = 0.036;
    socket.add(well);
    const voidCap = new THREE.Mesh(
      new THREE.CircleGeometry(0.09, 20),
      new THREE.MeshPhysicalMaterial({ color: 0x050403, roughness: 1, metalness: 0 })
    );
    voidCap.position.z = 0.03;
    socket.add(voidCap);
    for (let i = 0; i < 8; i++) {
      const a = (i / 8) * Math.PI * 2;
      const rivet = new THREE.Mesh(new THREE.CylinderGeometry(0.016, 0.016, 0.028, 8), ox);
      rivet.rotation.x = Math.PI / 2;
      rivet.position.set(Math.cos(a) * 0.34, Math.sin(a) * 0.34, 0.04);
      socket.add(rivet);
    }
    const duct = new THREE.Mesh(new THREE.CylinderGeometry(0.028, 0.034, 0.22, 8), ox);
    duct.rotation.z = Math.PI / 2;
    duct.position.set(0.58, -0.5, 0.02);
    plate.add(duct);
  }

  function rebuildPlateTextures() {
    for (const mesh of plates) {
      const info = faceInfo(mesh.userData.face);
      const maps = paintPlate(info);
      const m = mesh.material;
      m.map?.dispose();
      m.bumpMap?.dispose();
      m.emissiveMap?.dispose();
      m.map = maps.map;
      m.bumpMap = maps.bump;
      m.emissiveMap = maps.emissive;
      m.emissive.set(info.accent || "#6FA896");
      m.needsUpdate = true;
    }
  }

  function buildBay() {
    const size = 1024;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const ctx = c.getContext("2d");
    const rng = mulberry(0x0a0808);
    ctx.fillStyle = "#120e0c";
    ctx.fillRect(0, 0, size, size);
    for (let i = 0; i < 28; i++) {
      ctx.fillStyle = i % 2 ? "rgba(42,32,24,0.35)" : "rgba(18,12,10,0.5)";
      ctx.beginPath();
      ctx.ellipse(rng() * size, rng() * size, 40 + rng() * 160, 18 + rng() * 70, rng() * Math.PI, 0, Math.PI * 2);
      ctx.fill();
    }
    gritWarm(ctx, size, rng, 5000);
    ctx.strokeStyle = "rgba(28,20,16,0.7)";
    ctx.lineWidth = 10;
    for (let r = 80; r < 480; r += 70) {
      ctx.beginPath();
      ctx.arc(size * 0.5, size * 0.5, r, 0, Math.PI * 2);
      ctx.stroke();
    }
    const tex = canvasTex(c, true);
    tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
    tex.repeat.set(2, 2);

    const mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(18, 18),
      new THREE.MeshPhysicalMaterial({
        map: tex,
        color: 0x8a7a68,
        roughness: 0.86,
        metalness: 0.08,
        envMapIntensity: 0.35
      })
    );
    mesh.rotation.x = -Math.PI / 2;
    mesh.position.y = -0.95;
    mesh.receiveShadow = true;
    scene.add(mesh);

    const cavityTex = (() => {
      const g = document.createElement("canvas");
      g.width = g.height = 256;
      const gx = g.getContext("2d");
      const rad = gx.createRadialGradient(128, 128, 6, 128, 128, 124);
      rad.addColorStop(0, "rgba(196,138,60,0.55)");
      rad.addColorStop(0.35, "rgba(92,42,46,0.22)");
      rad.addColorStop(0.7, "rgba(26,16,12,0.08)");
      rad.addColorStop(1, "rgba(0,0,0,0)");
      gx.fillStyle = rad;
      gx.fillRect(0, 0, 256, 256);
      return canvasTex(g, true);
    })();

    const cavity = new THREE.Mesh(
      new THREE.PlaneGeometry(4.2, 4.2),
      new THREE.MeshBasicMaterial({
        map: cavityTex,
        transparent: true,
        opacity: 0.7,
        blending: THREE.AdditiveBlending,
        depthWrite: false
      })
    );
    cavity.rotation.x = -Math.PI / 2;
    cavity.position.y = -0.942;
    scene.add(cavity);

    const wet = new THREE.Mesh(
      new THREE.PlaneGeometry(18, 18),
      new THREE.MeshPhysicalMaterial({
        color: 0x2a2018,
        roughness: 0.18,
        metalness: 0.05,
        transparent: true,
        opacity: 0.22,
        envMapIntensity: 1.4,
        depthWrite: false
      })
    );
    wet.rotation.x = -Math.PI / 2;
    wet.position.y = -0.938;
    scene.add(wet);

    const contact = new THREE.Mesh(
      new THREE.CircleGeometry(1.15, 40),
      new THREE.MeshBasicMaterial({
        color: 0x000000,
        transparent: true,
        opacity: 0.62,
        depthWrite: false
      })
    );
    contact.rotation.x = -Math.PI / 2;
    contact.position.y = -0.936;
    scene.add(contact);

    const pedestal = new THREE.Group();
    const ox = oxidizedMat();
    const bone = boneMat(boneShared.map, boneShared.bump, { color: 0x5a4a3a, roughness: 0.78 });
    const base = new THREE.Mesh(new THREE.CylinderGeometry(1.35, 1.55, 0.18, 28), bone);
    base.position.y = -0.86;
    base.castShadow = true;
    base.receiveShadow = true;
    pedestal.add(base);
    const column = new THREE.Mesh(new THREE.CylinderGeometry(0.62, 0.78, 0.22, 20), ox);
    column.position.y = -0.72;
    column.castShadow = true;
    pedestal.add(column);
    const capital = new THREE.Mesh(new THREE.TorusGeometry(0.7, 0.07, 10, 28), cartilageMat());
    capital.rotation.x = Math.PI / 2;
    capital.position.y = -0.62;
    pedestal.add(capital);
    for (let i = 0; i < 8; i++) {
      const a = (i / 8) * Math.PI * 2;
      const rib = new THREE.Mesh(new THREE.BoxGeometry(0.07, 0.28, 0.09), bone);
      rib.position.set(Math.cos(a) * 0.7, -0.78, Math.sin(a) * 0.7);
      pedestal.add(rib);
    }
    scene.add(pedestal);

    const alcove = [];
    const wallMat = new THREE.MeshPhysicalMaterial({
      map: tex,
      color: 0x2a221c,
      roughness: 0.9,
      metalness: 0.04,
      envMapIntensity: 0.22
    });
    const back = new THREE.Mesh(new THREE.PlaneGeometry(16, 9), wallMat);
    back.position.set(0, 1.6, -5.4);
    scene.add(back);
    alcove.push(back);
    const left = new THREE.Mesh(new THREE.PlaneGeometry(10, 9), wallMat.clone());
    left.position.set(-6.2, 1.6, -1.4);
    left.rotation.y = Math.PI * 0.42;
    scene.add(left);
    alcove.push(left);
    const right = new THREE.Mesh(new THREE.PlaneGeometry(10, 9), wallMat.clone());
    right.position.set(6.4, 1.6, -1.6);
    right.rotation.y = -Math.PI * 0.4;
    scene.add(right);
    alcove.push(right);
    for (let i = 0; i < 6; i++) {
      const rib = new THREE.Mesh(
        new THREE.CylinderGeometry(0.08, 0.11, 7.5, 8),
        cartilageMat({ color: 0x6a5848 })
      );
      rib.rotation.z = Math.PI / 2;
      rib.position.set(0, 3.6, -4.6 + i * 0.12);
      rib.rotation.y = (i - 2.5) * 0.04;
      scene.add(rib);
      alcove.push(rib);
    }

    return { mesh, cavity, wet, contact, pedestal, alcove };
  }

  function buildHaze() {
    const tex = (() => {
      const c = document.createElement("canvas");
      c.width = c.height = 256;
      const ctx = c.getContext("2d");
      const rng = mulberry(0x0a2e);
      ctx.clearRect(0, 0, 256, 256);
      for (let i = 0; i < 70; i++) {
        const g = ctx.createRadialGradient(rng() * 256, rng() * 256, 4, rng() * 256, rng() * 256, 20 + rng() * 50);
        g.addColorStop(0, `rgba(196,168,120,${0.07 + rng() * 0.1})`);
        g.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = g;
        ctx.fillRect(0, 0, 256, 256);
      }
      return canvasTex(c, true);
    })();
    const group = [];
    for (let i = 0; i < 4; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(5.5, 3.4),
        new THREE.MeshBasicMaterial({
          map: tex,
          transparent: true,
          opacity: 0.14,
          blending: THREE.AdditiveBlending,
          depthWrite: false,
          side: THREE.DoubleSide
        })
      );
      mesh.position.set((i - 1.5) * 0.7, 0.85 + (i % 2) * 0.35, -0.4 - i * 0.15);
      mesh.rotation.y = -0.2 + i * 0.18;
      scene.add(mesh);
      group.push(mesh);
    }
    return group;
  }

  function buildMotes() {
    const count = 160;
    const geo = new THREE.BufferGeometry();
    const pos = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (Math.random() - 0.5) * 7;
      pos[i * 3 + 1] = Math.random() * 3.6 - 0.8;
      pos[i * 3 + 2] = (Math.random() - 0.5) * 7;
    }
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      color: 0xc4b090,
      size: 0.012,
      transparent: true,
      opacity: 0.16,
      blending: THREE.AdditiveBlending,
      depthWrite: false
    }));
    scene.add(pts);
    return pts;
  }

  function makeOrganBlock(size, rng) {
    const group = new THREE.Group();
    const bone = boneMat(boneShared.map, boneShared.bump, {
      color: rng() > 0.5 ? 0xe0d0b4 : 0xc8b49a,
      roughness: 0.48 + rng() * 0.14
    });
    const ox = oxidizedMat(null, { color: rng() > 0.5 ? 0x6a5848 : 0x4a3a30 });
    const outer = new THREE.Mesh(new THREE.BoxGeometry(size, size, size), bone);
    outer.castShadow = true;
    outer.receiveShadow = true;
    group.add(outer);
    if (size > 0.2) {
      const inset = size * 0.16;
      const inner = size - inset * 2;
      const depth = size * 0.12;
      const faces = [
        [0, 0, size / 2 - depth * 0.35],
        [0, 0, -size / 2 + depth * 0.35],
        [size / 2 - depth * 0.35, 0, 0],
        [-size / 2 + depth * 0.35, 0, 0],
        [0, size / 2 - depth * 0.35, 0],
        [0, -size / 2 + depth * 0.35, 0]
      ];
      for (let i = 0; i < faces.length; i++) {
        const [x, y, z] = faces[i];
        const geo = Math.abs(z) > Math.abs(x) && Math.abs(z) > Math.abs(y)
          ? new THREE.BoxGeometry(inner, inner, depth)
          : Math.abs(x) > Math.abs(y)
            ? new THREE.BoxGeometry(depth, inner, inner)
            : new THREE.BoxGeometry(inner, depth, inner);
        const mesh = new THREE.Mesh(geo, ox);
        mesh.position.set(x, y, z);
        group.add(mesh);
      }
      const lip = new THREE.Mesh(new THREE.TorusGeometry(size * 0.18, size * 0.03, 6, 16), cartilageMat());
      lip.position.z = size / 2 - depth * 0.1;
      group.add(lip);
    }
    return group;
  }

  function buildGreeble() {
    const rng = mulberry(0x7e77e);
    const list = [];
    const cell = 0.58;
    const n = 3;
    const origin = -cell * (n / 2) + cell / 2;

    const add = (x, y, z, size, extra = 0) => {
      const group = makeOrganBlock(size, rng);
      const rest = new THREE.Vector3(x, y, z);
      const out = rest.clone();
      if (out.lengthSq() < 0.01) out.set(0.15, 0.35, 0.4);
      else out.normalize();
      const explode = 0.05 + size * 0.05 + extra;
      const target = rest.clone().addScaledVector(out, explode);
      target.x += (rng() - 0.5) * 0.08;
      target.y += (rng() - 0.5) * 0.08;
      target.z += (rng() - 0.5) * 0.08;
      group.position.copy(rest);
      group.visible = false;
      group.userData.rest = rest.clone();
      group.userData.target = target;
      group.userData.targetRot = new THREE.Euler(
        (rng() - 0.5) * 0.18,
        (rng() - 0.5) * 0.22,
        (rng() - 0.5) * 0.14
      );
      group.userData.delay = rng() * 0.16;
      group.userData.restScale = 1;
      group.userData.targetScale = 1 + rng() * 0.06;
      group.userData.size = size;
      group.userData.item = null;
      group.userData.itemIndex = -1;
      initBrickLayout(group, target, group.userData.targetScale);
      artifact.add(group);
      list.push(group);

      if (rng() > 0.58) {
        const glowCol = rng() > 0.5 ? 0x6fa896 : 0xc4a46a;
        const slit = new THREE.Mesh(
          new THREE.BoxGeometry(size * 0.07, size * 0.07, size * 1.06),
          new THREE.MeshPhysicalMaterial({
            color: 0x2a221c,
            emissive: glowCol,
            emissiveIntensity: 0,
            roughness: 0.4,
            metalness: 0.2,
            transparent: true,
            opacity: 0,
            depthWrite: false
          })
        );
        slit.position.copy(rest).addScaledVector(out, size * 0.2);
        slit.lookAt(0, 0, 0);
        slit.visible = false;
        slit.userData.rest = slit.position.clone();
        slit.userData.target = slit.position.clone().addScaledVector(out, 0.12);
        slit.userData.targetRot = new THREE.Euler(0, 0, 0);
        slit.userData.delay = rng() * 0.12;
        slit.userData.glow = true;
        slit.userData.restScale = 1;
        slit.userData.targetScale = 1;
        initBrickLayout(slit, slit.userData.target, 1);
        artifact.add(slit);
        list.push(slit);
      }
    };

    for (let z = 0; z < n; z++) {
      for (let y = 0; y < n; y++) {
        for (let x = 0; x < n; x++) {
          const cx = origin + x * cell;
          const cy = origin + y * cell;
          const cz = origin + z * cell;
          const edge = x === 0 || x === n - 1 || y === 0 || y === n - 1 || z === 0 || z === n - 1;
          const size = edge ? 0.28 + rng() * 0.28 : 0.18 + rng() * 0.16;
          add(cx + (rng() - 0.5) * 0.08, cy + (rng() - 0.5) * 0.08, cz + (rng() - 0.5) * 0.08, size, 0);
          if (edge && rng() > 0.45) {
            const faceOut = new THREE.Vector3(
              x === 0 ? -1 : x === n - 1 ? 1 : 0,
              y === 0 ? -1 : y === n - 1 ? 1 : 0,
              z === 0 ? -1 : z === n - 1 ? 1 : 0
            );
            add(
              cx + faceOut.x * 0.22,
              cy + faceOut.y * 0.22,
              cz + faceOut.z * 0.22,
              0.18 + rng() * 0.14,
              0.04
            );
          }
        }
      }
    }

    const swirl = new THREE.Mesh(
      new THREE.IcosahedronGeometry(0.34, 2),
      cartilageMat({
        color: 0x8a6a78,
        emissive: 0x5c2a2e,
        emissiveIntensity: 0,
        transparent: true,
        opacity: 0
      })
    );
    swirl.visible = false;
    swirl.userData.rest = new THREE.Vector3(0, 0, 0);
    swirl.userData.target = new THREE.Vector3(0.05, 0.04, -0.03);
    swirl.userData.targetRot = new THREE.Euler(0.4, 0.8, 0.2);
    swirl.userData.delay = 0.05;
    swirl.userData.glow = true;
    swirl.userData.core = true;
    swirl.userData.restScale = 0.6;
    swirl.userData.targetScale = 1.05;
    initBrickLayout(swirl, swirl.userData.target, 1.05);
    artifact.add(swirl);
    list.push(swirl);

    return list;
  }

  function buildShafts() {
    const tex = (() => {
      const c = document.createElement("canvas");
      c.width = 64;
      c.height = 256;
      const ctx = c.getContext("2d");
      const g = ctx.createLinearGradient(32, 0, 32, 256);
      g.addColorStop(0, "rgba(232,201,160,0)");
      g.addColorStop(0.4, "rgba(196,138,60,0.22)");
      g.addColorStop(1, "rgba(92,42,46,0)");
      ctx.fillStyle = g;
      ctx.fillRect(22, 0, 20, 256);
      return canvasTex(c, true);
    })();
    const group = [];
    for (let i = 0; i < 3; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(0.55, 5.8),
        new THREE.MeshBasicMaterial({
          map: tex,
          transparent: true,
          opacity: 0,
          blending: THREE.AdditiveBlending,
          depthWrite: false,
          side: THREE.DoubleSide
        })
      );
      mesh.position.set(-0.55 + i * 0.55, 0.35, 0.15);
      mesh.rotation.y = -0.35 + i * 0.35;
      mesh.visible = false;
      scene.add(mesh);
      group.push(mesh);
    }
    return group;
  }

  function restoreHull() {
    hullGroup.visible = true;
    chassis.scale.setScalar(1);
    for (const plate of plates) {
      if (plate.userData.restPos) plate.position.copy(plate.userData.restPos);
      plate.rotation.z = 0;
      plate.scale.setScalar(1);
    }
  }

  function setHullVisible(on) {
    hullGroup.visible = on;
  }

  function setFloorVisible(opacity) {
    const glow = Math.max(opacity, 0.22);
    floor.mesh.visible = true;
    floor.cavity.material.opacity = 0.7 * glow;
    floor.wet.material.opacity = 0.22 * Math.max(opacity, 0.35);
    floor.contact.material.opacity = 0.62 * Math.max(opacity, 0.4);
    floor.pedestal.visible = true;
    const hazeOn = state.motion && opacity > 0.08;
    for (const h of haze) {
      h.visible = hazeOn;
      h.material.opacity = 0.14 * opacity;
    }
  }

  function startOpen(msg) {
    if (msg && msg.front) state.front = msg.front;
    if (msg && msg.motion === false) state.motion = false;
    state.items = Array.isArray(msg && msg.items) ? msg.items : previewItems();
    state.mode = String((msg && msg.mode) || "list").toLowerCase();
    state.stay = msg && msg.stay === true || state.mode === "carousel" || state.mode === "mosaic";
    state.focus = typeof (msg && msg.focus) === "number" ? msg.focus : 0;
    if (state.opening) return;
    if (!state.motion) {
      if (state.stay && state.items.length) {
        snapBrowse();
      }
      send({ v: 1, type: "opened", face: state.front, stay: state.stay });
      return;
    }
    state.opening = true;
    state.browse = false;
    state.openT = 0;
    state.dragging = false;
    spawnArcs();
    restoreHull();
    for (const b of bricks) {
      b.visible = true;
      b.position.copy(b.userData.rest);
      b.rotation.set(0, 0, 0);
      b.scale.setScalar(b.userData.restScale || 1);
      clearBrickMotion(b, b.userData.target);
      if (b.userData.glow && b.material) {
        b.material.opacity = 0;
        if (b.material.emissiveIntensity != null) b.material.emissiveIntensity = 0;
      }
    }
    for (const s of shafts) s.visible = true;
    setLoop(true);
  }

  function previewItems() {
    if (state.front === "Session") {
      return [
        { id: "session", title: "Session engine", meta: "PAGE", kind: "page", glyph: "G" },
        { id: "steam", title: "Steam", meta: "KIT", kind: "tool", glyph: "S" },
        { id: "playnite", title: "Playnite", meta: "KIT", kind: "tool", glyph: "P" }
      ];
    }
    if (state.front === "Tools") {
      return [
        { id: "obs", title: "OBS Studio", meta: "KIT", kind: "tool", glyph: "O" },
        { id: "vortex", title: "Vortex", meta: "KIT", kind: "tool", glyph: "V" },
        { id: "discord", title: "Discord", meta: "KIT", kind: "tool", glyph: "D" }
      ];
    }
    return [];
  }

  function playableBricks() {
    return bricks
      .filter((b) => !b.userData.glow && !b.userData.core && b.userData.size)
      .sort((a, b) => (b.userData.size || 0) - (a.userData.size || 0));
  }

  function paintItemLabel(item, focused) {
    const c = document.createElement("canvas");
    c.width = 256;
    c.height = 256;
    const ctx = c.getContext("2d");
    ctx.fillStyle = "#C8B49A";
    ctx.fillRect(0, 0, 256, 256);
    ctx.fillStyle = "rgba(42, 32, 24, 0.55)";
    ctx.fillRect(18, 18, 220, 220);
    ctx.strokeStyle = hexAlpha(focused ? BILE : "#6A5848", focused ? 0.9 : 0.45);
    ctx.lineWidth = 4;
    ctx.strokeRect(28, 28, 200, 200);
    ctx.beginPath();
    ctx.arc(128, 108, 36, 0, Math.PI * 2);
    ctx.stroke();
    ctx.fillStyle = hexAlpha(focused ? "#E8DCC4" : "#C4B090", 0.95);
    ctx.font = "600 64px 'Segoe UI', serif";
    ctx.textAlign = "center";
    ctx.fillText((item.glyph || item.title || "?").slice(0, 1), 128, 128);
    ctx.font = "600 16px 'Segoe UI', serif";
    ctx.fillStyle = "rgba(232, 220, 196, 0.82)";
    ctx.fillText(String(item.title || "").slice(0, 18), 128, 196);
    return canvasTex(c, true);
  }

  function bindItems() {
    const playable = playableBricks();
    for (const b of playable) {
      if (b.userData.label) {
        b.remove(b.userData.label);
        b.userData.label.material.map?.dispose();
        b.userData.label.material.dispose();
        b.userData.label = null;
      }
      b.userData.item = null;
      b.userData.itemIndex = -1;
    }
    const n = Math.min(state.items.length, playable.length);
    for (let i = 0; i < n; i++) {
      const b = playable[i];
      const item = state.items[i];
      b.userData.item = item;
      b.userData.itemIndex = i;
      const plane = new THREE.Mesh(
        new THREE.PlaneGeometry(Math.max(0.22, (b.userData.size || 0.3) * 0.72), Math.max(0.22, (b.userData.size || 0.3) * 0.72)),
        new THREE.MeshBasicMaterial({
          map: paintItemLabel(item, i === state.focus),
          transparent: true,
          opacity: 0.95
        })
      );
      plane.position.z = (b.userData.size || 0.3) * 0.52;
      b.add(plane);
      b.userData.label = plane;
    }
    state.lastFocus = state.focus;
    applyFocus();
  }

  function initBrickLayout(obj, pos, scale) {
    obj.userData.layoutPos = pos.clone();
    obj.userData.pendingPos = pos.clone();
    obj.userData.layoutScale = scale;
    obj.userData.pendingScale = scale;
    obj.userData.posVel = new THREE.Vector3();
    obj.userData.scaleVel = 0;
    obj.userData.delayLeft = 0;
    obj.userData.lit = 0;
    obj.userData.layoutLit = 0;
    obj.userData.pendingLit = 0;
    obj.userData.rotVel = new THREE.Vector3();
    obj.userData.layoutRot = obj.userData.targetRot
      ? new THREE.Euler(obj.userData.targetRot.x, obj.userData.targetRot.y, obj.userData.targetRot.z)
      : new THREE.Euler();
    obj.userData.pendingRot = obj.userData.layoutRot.clone();
  }

  function clearBrickMotion(b, pose) {
    const p = pose || b.userData.rest;
    if (b.userData.layoutPos && p) b.userData.layoutPos.copy(p);
    if (b.userData.pendingPos && p) b.userData.pendingPos.copy(p);
    b.userData.layoutScale = b.userData.targetScale || 1;
    b.userData.pendingScale = b.userData.layoutScale;
    if (b.userData.posVel) b.userData.posVel.set(0, 0, 0);
    if (b.userData.rotVel) b.userData.rotVel.set(0, 0, 0);
    b.userData.scaleVel = 0;
    b.userData.delayLeft = 0;
    b.userData.lit = 0;
    b.userData.layoutLit = 0;
    b.userData.pendingLit = 0;
    const rot = b.userData.targetRot;
    if (rot) {
      if (!b.userData.layoutRot) b.userData.layoutRot = new THREE.Euler();
      if (!b.userData.pendingRot) b.userData.pendingRot = new THREE.Euler();
      b.userData.layoutRot.set(rot.x, rot.y, rot.z);
      b.userData.pendingRot.set(rot.x, rot.y, rot.z);
    }
  }

  function rearrangeDelay(distance, isFocus) {
    if (isFocus) return 0;
    return 0.022 + Math.min(Math.max(distance, 0), 1.8) * 0.048;
  }

  function rearrangePush(distance, carousel) {
    const mag = carousel ? 0.12 : 0.07;
    return mag * (0.35 + Math.exp(-(distance * distance) / 0.55));
  }

  function paintBoundLabel(index, focused) {
    if (index == null || index < 0 || !state.items[index]) return;
    for (const b of bricks) {
      if (b.userData.itemIndex !== index || !b.userData.label) continue;
      b.userData.label.material.map?.dispose();
      b.userData.label.material.map = paintItemLabel(state.items[index], focused);
      b.userData.label.material.needsUpdate = true;
      break;
    }
  }

  function applyBrickLit(b, amount) {
    const want = Math.max(0, amount);
    b.traverse((child) => {
      if (!child.isMesh || !child.material) return;
      const m = child.material;
      if (m.emissive) {
        m.emissive.copy(state.accent);
        m.emissiveIntensity = 0.015 + want * 0.28;
        if (m.envMapIntensity != null) m.envMapIntensity = 1.05 + want * 0.55;
        if (m.clearcoat != null) m.clearcoat = 0.4 + want * 0.25;
      }
      if (child === b.userData.label) {
        m.opacity = 0.78 + want * 0.2;
      }
    });
  }

  function commitLayout(b) {
    if (!b.userData.layoutPos || !b.userData.pendingPos) return;
    b.userData.layoutPos.copy(b.userData.pendingPos);
    b.userData.layoutScale = b.userData.pendingScale;
    b.userData.layoutLit = b.userData.pendingLit;
    if (b.userData.pendingRot) {
      if (!b.userData.layoutRot) b.userData.layoutRot = new THREE.Euler();
      b.userData.layoutRot.copy(b.userData.pendingRot);
    }
  }

  function snapBrickLayout(b) {
    commitLayout(b);
    if (b.userData.layoutPos) b.position.copy(b.userData.layoutPos);
    b.scale.setScalar(b.userData.layoutScale || 1);
    if (b.userData.layoutRot) {
      b.rotation.set(b.userData.layoutRot.x, b.userData.layoutRot.y, b.userData.layoutRot.z);
    }
    if (b.userData.posVel) b.userData.posVel.set(0, 0, 0);
    if (b.userData.rotVel) b.userData.rotVel.set(0, 0, 0);
    b.userData.scaleVel = 0;
    b.userData.delayLeft = 0;
    b.userData.lit = b.userData.layoutLit || 0;
    applyBrickLit(b, b.userData.lit);
  }

  function applyFocus() {
    const carousel = state.mode === "carousel";
    let focusBase = null;
    for (const b of bricks) {
      if (b.userData.itemIndex === state.focus && state.focus >= 0) {
        focusBase = b.userData.target || b.userData.rest;
        break;
      }
    }

    for (const b of bricks) {
      if (b.userData.glow || b.userData.core) continue;
      const base = b.userData.target || b.userData.rest;
      if (!base) continue;
      if (!b.userData.layoutPos) initBrickLayout(b, base, b.userData.targetScale || 1);

      const idx = b.userData.itemIndex;
      const focused = idx === state.focus && idx >= 0;
      const playable = idx >= 0;
      const dist = focusBase ? base.distanceTo(focusBase) : 0;

      _out.copy(base);
      if (_out.lengthSq() < 0.01) _out.set(0, 0.3, 0.6);
      else _out.normalize();

      const pending = b.userData.pendingPos;
      pending.copy(base);

      if (focused) {
        pending.addScaledVector(_out, carousel ? 0.54 : 0.28);
        pending.z += carousel ? 0.11 : 0.05;
        b.userData.pendingScale = (b.userData.targetScale || 1) * (carousel ? 1.28 : 1.16);
        b.userData.pendingLit = 1;
        b.userData.delayLeft = 0;
        if (!b.userData.pendingRot) b.userData.pendingRot = new THREE.Euler();
        b.userData.pendingRot.set(0, 0, 0);
      } else if (playable) {
        if (focusBase && dist > 0.001) {
          _away.copy(base).sub(focusBase);
          if (_away.lengthSq() < 1e-6) _away.copy(_out);
          else _away.normalize();
          pending.addScaledVector(_away, rearrangePush(dist, carousel));
        }
        pending.addScaledVector(_out, carousel ? -0.028 : -0.014);
        pending.y -= 0.008 * Math.min(dist, 1.2);
        pending.z -= carousel ? 0.012 : 0.006;
        b.userData.pendingScale = (b.userData.targetScale || 1) * (carousel ? 0.97 : 0.985);
        b.userData.pendingLit = Math.exp(-(dist * dist) / 1.1) * 0.14;
        b.userData.delayLeft = state.motion ? rearrangeDelay(dist, false) : 0;
        const rot = b.userData.targetRot;
        if (rot) {
          if (!b.userData.pendingRot) b.userData.pendingRot = new THREE.Euler();
          b.userData.pendingRot.set(rot.x, rot.y, rot.z);
        }
      } else {
        if (focusBase && dist > 0.001) {
          _away.copy(base).sub(focusBase);
          if (_away.lengthSq() < 1e-6) _away.copy(_out);
          else _away.normalize();
          pending.addScaledVector(_away, rearrangePush(dist, carousel) * 0.32);
        }
        pending.addScaledVector(_out, -0.016);
        pending.z -= 0.012;
        b.userData.pendingScale = (b.userData.targetScale || 1) * 0.985;
        b.userData.pendingLit = 0;
        b.userData.delayLeft = state.motion ? 0.03 + Math.min(dist, 1.6) * 0.04 : 0;
        const rot = b.userData.targetRot;
        if (rot) {
          if (!b.userData.pendingRot) b.userData.pendingRot = new THREE.Euler();
          b.userData.pendingRot.set(rot.x, rot.y, rot.z);
        }
      }

      if (!state.motion) {
        snapBrickLayout(b);
      } else if (b.userData.delayLeft <= 0) {
        commitLayout(b);
      }
    }

    if (state.lastFocus !== state.focus) {
      if (state.lastFocus >= 0) paintBoundLabel(state.lastFocus, false);
      paintBoundLabel(state.focus, true);
      state.lastFocus = state.focus;
    }

    const item = state.items[state.focus];
    if (item) {
      const title = document.getElementById("previewTitle");
      const hint = document.getElementById("previewHint");
      if (title) title.textContent = item.title;
      if (hint) hint.textContent = `${item.meta || ""} · arrows cycle · Enter launches · Esc returns`;
    }

    if (!state.motion) {
      aimBrowseCamera(true);
    }
  }

  function tickBrowse(dt) {
    state.idle = 0;
    const step = Math.min(Math.max(dt, 0), 0.033);
    if (!state.motion) {
      for (const b of bricks) {
        if (b.userData.glow || b.userData.core) continue;
        snapBrickLayout(b);
      }
      aimBrowseCamera(true);
      return;
    }

    for (const b of bricks) {
      if (b.userData.glow || b.userData.core) continue;
      if (!b.userData.layoutPos || !b.userData.pendingPos) continue;
      if (!b.userData.posVel) b.userData.posVel = new THREE.Vector3();

      if (b.userData.delayLeft > 0) {
        b.userData.delayLeft -= step;
        if (b.userData.delayLeft <= 0) {
          b.userData.delayLeft = 0;
          commitLayout(b);
        }
      } else {
        commitLayout(b);
      }

      const target = b.userData.layoutPos;
      const vel = b.userData.posVel;
      vel.x += ((target.x - b.position.x) * LAYOUT_K - vel.x * LAYOUT_D) * step;
      vel.y += ((target.y - b.position.y) * LAYOUT_K - vel.y * LAYOUT_D) * step;
      vel.z += ((target.z - b.position.z) * LAYOUT_K - vel.z * LAYOUT_D) * step;
      b.position.x += vel.x * step;
      b.position.y += vel.y * step;
      b.position.z += vel.z * step;

      const sTarget = b.userData.layoutScale || 1;
      const sVel = b.userData.scaleVel || 0;
      const nextVel = sVel + ((sTarget - b.scale.x) * LAYOUT_K - sVel * LAYOUT_D) * step;
      b.userData.scaleVel = nextVel;
      b.scale.setScalar(Math.max(0.2, b.scale.x + nextVel * step));

      if (!b.userData.rotVel) b.userData.rotVel = new THREE.Vector3();
      const rot = b.userData.layoutRot;
      if (rot) {
        const rv = b.userData.rotVel;
        rv.x += ((rot.x - b.rotation.x) * LAYOUT_K - rv.x * LAYOUT_D) * step;
        rv.y += ((rot.y - b.rotation.y) * LAYOUT_K - rv.y * LAYOUT_D) * step;
        rv.z += ((rot.z - b.rotation.z) * LAYOUT_K - rv.z * LAYOUT_D) * step;
        b.rotation.x += rv.x * step;
        b.rotation.y += rv.y * step;
        b.rotation.z += rv.z * step;
      }

      const want = b.userData.layoutLit || 0;
      const lit = b.userData.lit || 0;
      b.userData.lit = lit + (want - lit) * Math.min(1, step * 7);
      applyBrickLit(b, b.userData.lit);
    }

    aimBrowseCamera(false, step);
  }

  function aimBrowseCamera(immediate, step) {
    let fx = 0;
    let fy = 0;
    for (const b of bricks) {
      if (b.userData.itemIndex === state.focus && state.focus >= 0) {
        fx = b.position.x * 0.12;
        fy = b.position.y * 0.06;
        break;
      }
    }
    const ty = state.camY - 0.12 + fy;
    const tz = state.camZ - 0.18;
    if (immediate || !state.motion) {
      camera.position.set(fx, ty, tz);
    } else {
      const a = 1 - Math.exp(-(step || 0.016) * 4.2);
      camera.position.x += (fx - camera.position.x) * a;
      camera.position.y += (ty - camera.position.y) * a;
      camera.position.z += (tz - camera.position.z) * a;
    }
    camera.lookAt(fx * 0.55, -0.04 + fy, 0);
  }

  function snapBrowse() {
    state.opening = false;
    state.browse = true;
    setHullVisible(false);
    setFloorVisible(0.35);
    renderer.toneMappingExposure = 0.92;
    key.intensity = 0.95;
    fill.intensity = 0.28;
    rim.intensity = 1.2;
    creviceVein.intensity = 0.85;
    creviceBruise.intensity = 0.55;
    creviceAmber.intensity = 0.9;
    for (const b of bricks) {
      b.visible = true;
      if (b.userData.target) b.position.copy(b.userData.target);
      b.scale.setScalar(b.userData.targetScale || 1);
      clearBrickMotion(b, b.userData.target);
      if (b.userData.glow && b.material) {
        b.material.opacity = 0.55;
        if (b.material.emissiveIntensity != null) b.material.emissiveIntensity = 0.35;
      }
    }
    bindItems();
    setLoop(state.motion || true);
    if (!state.motion) renderFrame(0);
  }

  function enterBrowse() {
    state.opening = false;
    state.browse = true;
    setHullVisible(false);
    bindItems();
    setLoop(true);
  }

  function cycleFocus(delta) {
    if (!state.items.length) return;
    state.focus = ((state.focus + delta) % state.items.length + state.items.length) % state.items.length;
    applyFocus();
    send({ v: 1, type: "cycle", index: state.focus, item: state.items[state.focus].id });
  }

  function selectFocused() {
    const item = state.items[state.focus];
    if (!item) return;
    send({ v: 1, type: "select", index: state.focus, item: item.id, face: state.front });
  }

  function resetAssembly() {
    state.opening = false;
    state.browse = false;
    state.stay = false;
    state.items = [];
    state.focus = 0;
    state.lastFocus = -1;
    state.openT = 0;
    renderer.toneMappingExposure = 0.68;
    scene.fog.density = 0.068;
    openSpot.intensity = 0;
    creviceVein.intensity = 0;
    creviceBruise.intensity = 0;
    creviceAmber.intensity = 0;
    key.intensity = 0.62;
    fill.intensity = 0.2;
    rim.intensity = 1.35;
    camera.position.set(0, state.camY, state.camZ);
    camera.lookAt(0, -0.28, 0);
    restoreHull();
    setFloorVisible(1);
    for (const b of bricks) {
      if (b.userData.label) {
        b.remove(b.userData.label);
        b.userData.label.material.map?.dispose();
        b.userData.label.material.dispose();
        b.userData.label = null;
      }
      b.userData.item = null;
      b.userData.itemIndex = -1;
      b.visible = false;
      b.position.copy(b.userData.rest);
      b.rotation.set(0, 0, 0);
      b.scale.setScalar(b.userData.restScale || 1);
      clearBrickMotion(b, b.userData.rest);
      if (!b.userData.glow && !b.userData.core) applyBrickLit(b, 0);
    }
    for (const s of shafts) {
      s.visible = false;
      s.material.opacity = 0;
    }
    clearArcs();
    setLoop(state.motion);
    if (!state.motion) renderFrame(0);
  }

  function easeOut(t) {
    return 1 - Math.pow(1 - clamp(t, 0, 1), 3);
  }

  function tickOpen(dt) {
    if (!state.opening) return;
    state.openT += dt;
    const t = state.openT / state.openDuration;
    const e = easeOut(t);
    for (const plate of plates) {
      const rest = plate.userData.restPos;
      const dir = plate.userData.dir;
      if (rest && dir) {
        plate.position.copy(rest).addScaledVector(dir, e * 0.62);
        plate.rotation.z = (plate.userData.peel || 0.14) * e;
        plate.scale.setScalar(1 + e * 0.05);
      }
    }
    chassis.scale.setScalar(1 - e * 0.22);
    hullGroup.visible = e < 0.9;
    for (const b of bricks) {
      const local = easeOut((state.openT - b.userData.delay) / (state.openDuration * 0.82));
      b.position.lerpVectors(b.userData.rest, b.userData.target, local);
      b.rotation.x = b.userData.targetRot.x * local;
      b.rotation.y = b.userData.targetRot.y * local;
      b.rotation.z = b.userData.targetRot.z * local;
      const s0 = b.userData.restScale || 1;
      const s1 = b.userData.targetScale || 1;
      b.scale.setScalar(s0 + (s1 - s0) * local);
      if (b.userData.glow && b.material) {
        b.material.opacity = 0.55 * local;
        if (b.material.emissiveIntensity != null) b.material.emissiveIntensity = 0.4 * local;
      }
    }
    camera.position.z = state.camZ - e * 0.18;
    camera.position.y = state.camY - e * 0.12;
    camera.lookAt(0, -0.12 + e * 0.06, 0);
    renderer.toneMappingExposure = 0.68 + e * 0.28;
    scene.fog.density = 0.068 - e * 0.018;
    setFloorVisible(1 - e * 0.55);
    key.intensity = 0.62 + e * 0.45;
    fill.intensity = 0.2 + e * 0.18;
    rim.intensity = 1.35 + e * 0.2;
    openSpot.color.set(0xe8c9a0);
    openSpot.intensity = 3.4 * (0.25 + 0.75 * Math.sin(e * Math.PI));
    marrowLight.intensity = 0.55 + e * 1.1;
    creviceVein.intensity = 1.1 * e;
    creviceBruise.intensity = 0.7 * e;
    creviceAmber.intensity = 1.2 * e;
    for (const s of shafts) {
      s.material.opacity = 0.38 * Math.sin(e * Math.PI);
      s.rotation.y += dt * 0.08;
    }
    if (t >= 1) {
      if (state.stay && state.items.length) {
        enterBrowse();
      }
      send({ v: 1, type: "opened", face: state.front, stay: state.stay });
      state.opening = false;
    }
  }

  function spawnArcs() {
    if (!state.motion) {
      return;
    }
    clearArcs();
    const corners = [];
    const h = 0.9;
    for (const x of [-h, h]) {
      for (const y of [-h, h]) {
        for (const z of [-h, h]) {
          corners.push(new THREE.Vector3(x, y, z));
        }
      }
    }
    const n = 7;
    for (let i = 0; i < n; i++) {
      const a = corners[(Math.random() * corners.length) | 0];
      let b = corners[(Math.random() * corners.length) | 0];
      if (b === a) {
        b = corners[(i + 3) % corners.length];
      }
      const pts = jagged(a, b, 16, 0.16 + Math.random() * 0.1);
      const curve = new THREE.CatmullRomCurve3(pts);
      const tube = new THREE.TubeGeometry(curve, 40, i < 3 ? 0.014 : 0.007, 6, false);
      const col = i % 3 === 0 ? new THREE.Color(0x8a6b4a) : i % 3 === 1 ? new THREE.Color(BLOOD) : new THREE.Color(BILE);
      const mat = new THREE.MeshPhysicalMaterial({
        color: col,
        roughness: 0.42,
        metalness: 0.18,
        clearcoat: 0.55,
        transparent: true,
        opacity: 0.82
      });
      const mesh = new THREE.Mesh(tube, mat);
      artifact.add(mesh);
      arcs.push({ mesh, mat, born: performance.now(), life: 720 + Math.random() * 320 });
    }
    state.burstUntil = performance.now() + 780;
  }

  function jagged(a, b, segments, chaos) {
    const pts = [];
    const dir = b.clone().sub(a);
    const side = new THREE.Vector3(dir.y, dir.z, dir.x).normalize();
    const up = dir.clone().cross(side).normalize();
    for (let i = 0; i <= segments; i++) {
      const t = i / segments;
      const p = a.clone().lerp(b, t);
      if (i > 0 && i < segments) {
        const mag = chaos * (1 - Math.abs(2 * t - 1));
        p.addScaledVector(side, (Math.random() * 2 - 1) * mag);
        p.addScaledVector(up, (Math.random() * 2 - 1) * mag);
      }
      pts.push(p);
    }
    return pts;
  }

  function clearArcs() {
    for (const arc of arcs) {
      artifact.remove(arc.mesh);
      arc.mesh.geometry.dispose();
      arc.mat.dispose();
    }
    arcs.length = 0;
  }

  function tickArcs(now) {
    for (let i = arcs.length - 1; i >= 0; i--) {
      const arc = arcs[i];
      const t = (now - arc.born) / arc.life;
      if (t >= 1) {
        artifact.remove(arc.mesh);
        arc.mesh.geometry.dispose();
        arc.mat.dispose();
        arcs.splice(i, 1);
        continue;
      }
      arc.mat.opacity = (1 - t) * (0.35 + 0.65 * Math.abs(Math.sin(now * 0.04 + i)));
    }
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
        if (state.browse) applyFocus();
      }
      return;
    }
    if (type === "reset") {
      resetAssembly();
      if (data.front) state.front = data.front;
      applyHostState({ ...data, type: "state", burst: false });
      return;
    }
    applyHostState(data);
  }

  function applyHostState(msg) {
    if (!msg || msg.type && msg.type !== "state") {
      return;
    }
    if (Array.isArray(msg.faces) && msg.faces.length) {
      state.faces = msg.faces;
    }
    if (typeof msg.yaw === "number") {
      state.yaw = msg.yaw;
    }
    if (typeof msg.pitch === "number") {
      state.pitch = msg.pitch;
    }
    if (typeof msg.restYaw === "number") {
      state.restYaw = msg.restYaw;
    }
    if (typeof msg.restPitch === "number") {
      state.restPitch = msg.restPitch;
    }
    const nextFront = msg.front || state.front;
    const frontChanged = nextFront !== state.front;
    state.front = nextFront;
    state.motion = msg.motion !== false && !reduced;
    const info = faceInfo(state.front);
    state.targetAccent.set(msg.accent || info.accent);
    state.targetSeam.set(msg.seam || info.seam);
    state.targetCore.set(msg.core || info.core);
    if (!state.motion) {
      state.visualYaw = state.yaw;
      state.visualPitch = state.pitch;
      state.yawVel = 0;
      state.pitchVel = 0;
      state.idle = 0;
      state.accent.copy(state.targetAccent);
      state.seam.copy(state.targetSeam);
      state.core.copy(state.targetCore);
      clearArcs();
    }
    if (msg.burst && state.motion) {
      spawnArcs();
    }
    if (frontChanged) {
      rebuildPlateTextures();
    }
    syncCaption();
    setLoop(state.motion);
    if (!state.motion) {
      renderFrame(0);
    }
  }

  function syncCaption() {
    const info = faceInfo(state.front);
    const title = document.getElementById("previewTitle");
    const hint = document.getElementById("previewHint");
    if (title) {
      title.textContent = info.title;
    }
    if (hint) {
      hint.textContent = info.hint || "Arrow keys rotate · Enter opens · drag or click a plate";
    }
  }

  function spring(current, target, vel, dt) {
    const nearest = nearestDeg(current, target);
    const accel = (nearest - current) * SPRING_K - vel * SPRING_D;
    const nvel = vel + accel * dt;
    return { value: current + nvel * dt, vel: nvel };
  }

  function nearestDeg(current, target) {
    let nearest = target;
    while (nearest - current > 180) nearest -= 360;
    while (current - nearest > 180) nearest += 360;
    return nearest;
  }

  function applyRig() {
    restRig.rotation.order = "YXZ";
    restRig.rotation.y = THREE.MathUtils.degToRad(-state.restYaw);
    restRig.rotation.x = THREE.MathUtils.degToRad(state.restPitch);
    spinRig.rotation.order = "YXZ";
    spinRig.rotation.y = THREE.MathUtils.degToRad(-(state.visualYaw + state.idle));
    spinRig.rotation.x = THREE.MathUtils.degToRad(-state.visualPitch);
  }

  function renderFrame(dt) {
    const now = performance.now();
    if (state.opening) {
      tickOpen(dt);
    } else if (state.browse) {
      tickBrowse(dt);
    } else if (state.motion && !state.dragging) {
      const y = spring(state.visualYaw, state.yaw, state.yawVel, dt);
      const p = spring(state.visualPitch, state.pitch, state.pitchVel, dt);
      state.visualYaw = y.value;
      state.yawVel = y.vel;
      state.visualPitch = p.value;
      state.pitchVel = p.vel;
      state.idle = 1.4 * Math.sin(now * 0.00045);
    }
    state.accent.lerp(state.targetAccent, state.motion ? 0.08 : 1);
    state.seam.lerp(state.targetSeam, state.motion ? 0.1 : 1);
    state.core.lerp(state.targetCore, state.motion ? 0.1 : 1);

    const burst = Math.max(0, (state.burstUntil - now) / 520);
    const pulse = state.motion ? 0.86 + 0.14 * Math.sin(now * 0.0018) : 0.84;
    marrowLight.color.set(0xc48a3c);
    marrowLight.intensity = state.opening ? marrowLight.intensity : (state.motion ? 0.32 + burst * 0.5 : 0.26);
    veinLight.color.copy(state.accent);
    veinLight.intensity = state.motion ? 0.14 + pulse * 0.1 : 0.12;
    bruiseKick.intensity = state.motion ? 0.18 + pulse * 0.08 : 0.14;
    cavityGlow.intensity = state.motion ? 2.4 + pulse * 0.3 : 2.2;

    for (const plate of plates) {
      const front = plate.userData.face === state.front;
      if (plate.material && plate.material.emissiveIntensity != null) {
        plate.material.emissiveIntensity = (front ? 0.22 : 0.08) * pulse + burst * 0.05;
      }
    }

    floor.cavity.material.color.copy(new THREE.Color(0xc48a3c));

    const fx = state.motion && !state.browse;
    motes.visible = fx && !state.opening;
    for (const h of haze) h.visible = fx;
    if (state.motion) {
      motes.rotation.y += dt * 0.015;
      for (const h of haze) {
        h.rotation.y += dt * 0.01;
        h.position.y += Math.sin(now * 0.0004 + h.position.x) * 0.00012;
      }
    }

    tickArcs(now);
    applyRig();
    renderer.render(scene, camera);
  }

  let looping = false;
  function setLoop(on) {
    if (on && !looping) {
      looping = true;
      clock.getDelta();
      renderer.setAnimationLoop(() => {
        const dt = Math.min(clock.getDelta(), 0.033);
        renderFrame(dt);
      });
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
    if (!looping) {
      renderFrame(0);
    }
  }

  function send(msg) {
    if (hosted) {
      window.chrome.webview.postMessage(JSON.stringify(msg));
    }
  }

  function localTurn(turn) {
    const pose = snapPose(state.yaw, state.pitch);
    const next = applyTurn(pose, turn);
    state.yaw = next.yaw;
    state.pitch = next.pitch;
    state.front = next.front;
    const info = faceInfo(state.front);
    state.targetAccent.set(info.accent);
    state.targetSeam.set(info.seam);
    state.targetCore.set(info.core);
    rebuildPlateTextures();
    if (state.motion) {
      spawnArcs();
    } else {
      state.visualYaw = state.yaw;
      state.visualPitch = state.pitch;
    }
    syncCaption();
  }

  function yawSteps(yaw) {
    return ((Math.round(yaw / 90) % 4) + 4) % 4;
  }

  function pitchSteps(pitch) {
    return Math.max(-1, Math.min(1, Math.round(Math.max(-90, Math.min(90, pitch)) / 90)));
  }

  function snapPose(yaw, pitch) {
    const ys = yawSteps(yaw);
    const ps = pitchSteps(pitch);
    return { yaw: ys * 90, pitch: ps * 90, yawSteps: ys, pitchSteps: ps, front: frontOf(ys, ps) };
  }

  function frontOf(ys, ps) {
    if (ps < 0) return "Mods";
    if (ps > 0) return "Files";
    return ["Session", "Tools", "Hardware", "Network"][ys] || "Session";
  }

  function applyTurn(pose, turn) {
    let ys = pose.yawSteps;
    let ps = pose.pitchSteps;
    if (turn === "Left") ys -= 1;
    if (turn === "Right") ys += 1;
    if (turn === "Up") ps -= 1;
    if (turn === "Down") ps += 1;
    ys = ((ys % 4) + 4) % 4;
    ps = Math.max(-1, Math.min(1, ps));
    return { yaw: ys * 90, pitch: ps * 90, yawSteps: ys, pitchSteps: ps, front: frontOf(ys, ps) };
  }

  function aimedAt(face) {
    const pose = snapPose(state.yaw, state.pitch);
    if (face === "Mods") return { yaw: pose.yaw, pitch: -90, front: "Mods" };
    if (face === "Files") return { yaw: pose.yaw, pitch: 90, front: "Files" };
    const map = { Session: 0, Tools: 1, Hardware: 2, Network: 3 };
    const ys = map[face] ?? 0;
    return { yaw: ys * 90, pitch: 0, front: face };
  }

  function onPointerDown(ev) {
    if (state.opening) return;
    if (state.browse) {
      pickBrowse(ev.clientX, ev.clientY);
      return;
    }
    state.dragging = true;
    state.moved = false;
    state.pressX = ev.clientX;
    state.pressY = ev.clientY;
    state.lastX = ev.clientX;
    state.lastY = ev.clientY;
    state.lastT = performance.now();
    state.poseYaw = snapPose(state.yaw, state.pitch).yaw;
    state.posePitch = snapPose(state.yaw, state.pitch).pitch;
    canvas.setPointerCapture?.(ev.pointerId);
  }

  function onPointerMove(ev) {
    if (!state.dragging) {
      return;
    }
    const dx = ev.clientX - state.pressX;
    const dy = ev.clientY - state.pressY;
    const now = performance.now();
    const dt = Math.max(0.001, (now - state.lastT) / 1000);
    state.vx = (ev.clientX - state.lastX) / dt;
    state.vy = (ev.clientY - state.lastY) / dt;
    state.lastX = ev.clientX;
    state.lastY = ev.clientY;
    state.lastT = now;
    if (Math.abs(dx) + Math.abs(dy) > 8) {
      state.moved = true;
    }
    if (state.moved) {
      state.yaw = state.poseYaw - (dx / PX_PER_TURN) * 90;
      state.pitch = clamp(state.posePitch - (dy / PX_PER_TURN) * 90, -100, 100);
      if (!state.motion) {
        state.visualYaw = state.yaw;
        state.visualPitch = state.pitch;
      }
    }
  }

  function onPointerUp(ev) {
    if (!state.dragging) {
      return;
    }
    state.dragging = false;
    canvas.releasePointerCapture?.(ev.pointerId);
    const dx = ev.clientX - state.pressX;
    const dy = ev.clientY - state.pressY;
    if (state.moved) {
      send({ v: 1, type: "dragEnd", dx, dy, vx: state.vx, vy: state.vy });
      if (!hosted) {
        const snapped = snapPose(state.yaw, state.pitch);
        const flick = flickTurn(state.vx, state.vy);
        const next = flick ? applyTurn(snapped, flick) : snapped;
        state.yaw = next.yaw;
        state.pitch = next.pitch;
        state.front = next.front;
        const info = faceInfo(state.front);
        state.targetAccent.set(info.accent);
        state.targetSeam.set(info.seam);
        state.targetCore.set(info.core);
        rebuildPlateTextures();
        if (state.motion) spawnArcs();
        syncCaption();
      }
      return;
    }
    pick(ev.clientX, ev.clientY);
  }

  function flickTurn(vx, vy) {
    if (Math.max(Math.abs(vx), Math.abs(vy)) < 640) {
      return null;
    }
    if (Math.abs(vx) >= Math.abs(vy)) {
      return vx < 0 ? "Right" : "Left";
    }
    return vy > 0 ? "Up" : "Down";
  }

  function previewOpen(front) {
    const face = front || state.front;
    const stay = face === "Session" || face === "Tools" || face === "Mods";
    const mode = face === "Session" ? "carousel" : stay ? "mosaic" : "list";
    startOpen({ front: face, motion: state.motion, stay, mode });
  }

  function pick(x, y) {
    if (state.opening) return;
    if (state.browse) {
      pickBrowse(x, y);
      return;
    }
    const rect = canvas.getBoundingClientRect();
    pointer.x = ((x - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((y - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(plates, false);
    if (hits.length) {
      const face = hits[0].object.userData.face;
      if (hosted) {
        send({ v: 1, type: "pick", face });
        return;
      }
      if (face === state.front) {
        previewOpen(face);
        return;
      }
      const next = aimedAt(face);
      state.yaw = next.yaw;
      state.pitch = next.pitch;
      state.front = next.front;
      const info = faceInfo(state.front);
      state.targetAccent.set(info.accent);
      state.targetSeam.set(info.seam);
      state.targetCore.set(info.core);
      rebuildPlateTextures();
      if (state.motion) spawnArcs();
      syncCaption();
      return;
    }
    const nx = pointer.x;
    const ny = -pointer.y;
    if (Math.abs(nx) < 0.32 && Math.abs(ny) < 0.32) {
      if (hosted) send({ v: 1, type: "activate" });
      else previewOpen(state.front);
      return;
    }
    const turn = Math.abs(nx) >= Math.abs(ny) ? (nx < 0 ? "Left" : "Right") : (ny < 0 ? "Up" : "Down");
    if (hosted) {
      send({ v: 1, type: "turn", turn });
    } else {
      localTurn(turn);
    }
  }

  function pickBrowse(x, y) {
    const rect = canvas.getBoundingClientRect();
    pointer.x = ((x - rect.left) / rect.width) * 2 - 1;
    pointer.y = -((y - rect.top) / rect.height) * 2 + 1;
    raycaster.setFromCamera(pointer, camera);
    const hits = raycaster.intersectObjects(playableBricks(), true);
    if (!hits.length) return;
    let obj = hits[0].object;
    while (obj && obj.userData.itemIndex == null) obj = obj.parent;
    const idx = obj && typeof obj.userData.itemIndex === "number" ? obj.userData.itemIndex : -1;
    if (idx < 0) return;
    if (idx === state.focus) {
      selectFocused();
      return;
    }
    state.focus = idx;
    applyFocus();
    send({ v: 1, type: "cycle", index: state.focus, item: state.items[state.focus].id });
  }

  function onKey(ev) {
    if (state.opening) {
      ev.preventDefault();
      return;
    }
    if (state.browse) {
      if (ev.key === "Escape") {
        ev.preventDefault();
        send({ v: 1, type: "back" });
        if (!hosted) resetAssembly();
        return;
      }
      if (ev.key === "Enter" || ev.key === " ") {
        ev.preventDefault();
        selectFocused();
        return;
      }
      if (ev.key === "ArrowLeft" || ev.key === "ArrowUp") {
        ev.preventDefault();
        cycleFocus(-1);
        return;
      }
      if (ev.key === "ArrowRight" || ev.key === "ArrowDown") {
        ev.preventDefault();
        cycleFocus(1);
        return;
      }
      return;
    }
    const map = { ArrowLeft: "Left", ArrowRight: "Right", ArrowUp: "Up", ArrowDown: "Down" };
    if (ev.key === "Enter" || ev.key === " ") {
      ev.preventDefault();
      if (hosted) send({ v: 1, type: "activate" });
      else previewOpen(state.front);
      return;
    }
    const turn = map[ev.key];
    if (!turn) {
      return;
    }
    ev.preventDefault();
    if (hosted) {
      send({ v: 1, type: "turn", turn });
    } else {
      localTurn(turn);
    }
  }

  function onWheel(ev) {
    if (state.opening) return;
    if (state.browse) {
      ev.preventDefault();
      cycleFocus(ev.deltaY < 0 ? -1 : 1);
      return;
    }
    ev.preventDefault();
    const turn = ev.deltaY < 0 ? "Left" : "Right";
    if (hosted) {
      send({ v: 1, type: "turn", turn });
    } else {
      localTurn(turn);
    }
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
      yaw: 0,
      pitch: 0,
      restYaw: 28,
      restPitch: -17,
      front: "Session",
      motion: !reduced,
      burst: false,
      faces: DEFAULT_FACES
    });
  }

  resize();
  setLoop(state.motion);
  if (!state.motion) {
    renderFrame(0);
  }

  function showFallback() {
    if (fallback) {
      fallback.hidden = false;
    }
    if (canvas) {
      canvas.style.display = "none";
    }
  }

  function orientPlate(mesh, dir, up) {
    const n = dir.clone().normalize();
    const r = new THREE.Vector3().crossVectors(up.clone().normalize(), n).normalize();
    const u = new THREE.Vector3().crossVectors(n, r).normalize();
    mesh.quaternion.setFromRotationMatrix(new THREE.Matrix4().makeBasis(r, u, n));
  }

  function makeEnvMap() {
    const colors = ["#2a241c", "#080605", "#1a1418", "#0a0806", "#3a2a18", "#080605"];
    const images = colors.map((color) => {
      const c = document.createElement("canvas");
      c.width = c.height = 16;
      const ctx = c.getContext("2d");
      ctx.fillStyle = color;
      ctx.fillRect(0, 0, 16, 16);
      return c;
    });
    const tex = new THREE.CubeTexture(images);
    tex.needsUpdate = true;
    tex.colorSpace = THREE.SRGBColorSpace;
    return tex;
  }

  function canvasTex(c, srgb) {
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = srgb ? THREE.SRGBColorSpace : (THREE.NoColorSpace || THREE.LinearSRGBColorSpace);
    tex.anisotropy = 8;
    tex.needsUpdate = true;
    return tex;
  }

  function hexAlpha(hex, a) {
    const c = hex.replace("#", "");
    const r = parseInt(c.slice(0, 2), 16);
    const g = parseInt(c.slice(2, 4), 16);
    const b = parseInt(c.slice(4, 6), 16);
    return `rgba(${r}, ${g}, ${b}, ${a})`;
  }

  function hash(s) {
    let h = 2166136261;
    for (let i = 0; i < s.length; i++) {
      h ^= s.charCodeAt(i);
      h = Math.imul(h, 16777619);
    }
    return h >>> 0;
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

  function clamp(v, a, b) {
    return Math.max(a, Math.min(b, v));
  }
})();
