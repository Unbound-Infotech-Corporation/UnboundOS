/* UnboundOS Home — original procedural living galaxy.
   Slightly pitched horizontal OS bar + JWST-class deep field.
   Observatory photos are look-dev only and are never loaded. */
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
  renderer.setClearColor(0x010208, 1);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.06;

  const CAM_Y = 1.16;
  const CAM_Z = 8.55;
  const RIG_PITCH = 0.22;
  const RIG_ROLL = 0.042;
  const lookTarget = new THREE.Vector3(0, 0.05, -0.85);

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(26, 1, 0.08, 160);
  camera.position.set(0, CAM_Y, CAM_Z);
  camera.lookAt(lookTarget);

  const rig = new THREE.Group();
  rig.rotation.x = RIG_PITCH;
  rig.rotation.z = RIG_ROLL;
  scene.add(rig);

  const starMap = spriteTex(64, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    g.addColorStop(0, "rgba(255,255,255,1)");
    g.addColorStop(0.16, "rgba(255,246,220,0.9)");
    g.addColorStop(0.4, "rgba(176,198,255,0.24)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  });

  const glowMap = spriteTex(256, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    g.addColorStop(0, "rgba(255,252,236,0.98)");
    g.addColorStop(0.18, "rgba(255,226,168,0.42)");
    g.addColorStop(0.48, "rgba(150,176,230,0.1)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  });

  const barMap = spriteTex(512, paintBarGlow);
  const diskMap = paintDiskTexture(2560, 1024);
  const dustMap = paintDustSheet(1024, 256);
  const spikeMap = spriteTex(256, paintSpikeStar);
  const filamentMaps = [
    paintFilamentSheet(640, 320, 0xc11, "copper"),
    paintFilamentSheet(640, 320, 0xc22, "gold"),
    paintFilamentSheet(640, 320, 0xc33, "lavender"),
    paintFilamentSheet(640, 320, 0xc44, "indigo")
  ];

  const warm = new THREE.Color(0xffe2a8);
  const copper = new THREE.Color(0xd4844a);
  const cool = new THREE.Color(0x7a96d0);
  const cream = new THREE.Color(0xfff4dc);
  const indigo = new THREE.Color(0x4a5c92);
  const lavender = new THREE.Color(0xc4b4e0);

  const farStars = buildHalo(8200, 78, 0.74, 0x6e82b8, 0x51f, 0.94, 0.0000048, 0);
  const midStars = buildHalo(3800, 28, 0.98, 0xc8d4ec, 0x77a, 0.78, -0.000009, -1.2);
  const nearStars = buildHalo(1400, 13, 1.18, 0xf4ead8, 0x91c, 0.52, 0.000016, 1.4);
  const deepField = buildColoredField(7200, 92, 0.62, 0xdef1, 0.98, 0.0000032);
  const diskRings = buildDiskRings();
  const shear = buildShear(1500);
  const orbiters = buildOrbiters(180);
  const diskGlow = buildDiskGlow();
  const dustSheets = buildDustSheets();
  const filaments = buildFilaments();
  const spikes = buildSpikedStars(42);
  const nodes = buildNodes();
  scene.add(farStars, midStars, nearStars, deepField);
  const vignette = buildVignette();
  scene.add(vignette);

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

  function paintBarGlow(ctx, s) {
    ctx.clearRect(0, 0, s, s);
    ctx.translate(s / 2, s / 2);
    ctx.scale(1, 0.22);
    const g = ctx.createRadialGradient(0, 0, 0, 0, 0, s / 2);
    g.addColorStop(0, "rgba(255,252,236,0.95)");
    g.addColorStop(0.16, "rgba(255,226,168,0.5)");
    g.addColorStop(0.42, "rgba(210,170,120,0.14)");
    g.addColorStop(0.7, "rgba(120,150,210,0.05)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(-s / 2, -s / 2, s, s);
  }

  function paintDiskTexture(w, h) {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d", { willReadFrequently: true });
    ctx.clearRect(0, 0, w, h);
    const cx = w * 0.5;
    const cy = h * 0.5;

    function oval(rx, ry, stops) {
      ctx.save();
      ctx.translate(cx, cy);
      ctx.scale(rx, ry);
      const g = ctx.createRadialGradient(0, 0, 0, 0, 0, 1);
      for (const [t, col] of stops) g.addColorStop(t, col);
      ctx.fillStyle = g;
      ctx.fillRect(-1, -1, 2, 2);
      ctx.restore();
    }

    oval(w * 0.5, h * 0.042, [
      [0, "rgba(90, 118, 186, 0.2)"],
      [0.55, "rgba(58, 78, 140, 0.08)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.38, h * 0.03, [
      [0, "rgba(212, 132, 74, 0.22)"],
      [0.5, "rgba(168, 96, 52, 0.08)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.34, h * 0.026, [
      [0, "rgba(255, 226, 176, 0.4)"],
      [0.42, "rgba(214, 154, 88, 0.16)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.16, h * 0.032, [
      [0, "rgba(255, 246, 222, 0.84)"],
      [0.3, "rgba(255, 208, 140, 0.44)"],
      [0.68, "rgba(228, 160, 88, 0.12)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.04, h * 0.024, [
      [0, "rgba(255, 252, 244, 0.98)"],
      [0.4, "rgba(255, 230, 176, 0.55)"],
      [1, "rgba(0,0,0,0)"]
    ]);

    ctx.save();
    ctx.globalCompositeOperation = "lighter";
    ctx.translate(cx, cy);
    ctx.scale(1, 0.022);
    const spike = ctx.createRadialGradient(0, 0, 0, 0, 0, w * 0.46);
    spike.addColorStop(0, "rgba(255,248,230,0.42)");
    spike.addColorStop(0.2, "rgba(255,220,160,0.1)");
    spike.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = spike;
    ctx.fillRect(-w * 0.46, -w * 0.46, w * 0.92, w * 0.92);
    ctx.restore();

    const img = ctx.getImageData(0, 0, w, h);
    const d = img.data;
    for (let y = 0; y < h; y++) {
      const ny = (y - cy) / h;
      const band = Math.exp(-ny * ny * 220);
      if (band < 0.02) continue;
      for (let x = 0; x < w; x++) {
        const nx = (x - cx) / w;
        const n1 = valueNoise(nx * 22 + 2.1, ny * 54, 11);
        const n2 = valueNoise(nx * 9 - 1.4, ny * 28, 29);
        const lane = band * (0.35 * n1 + 0.65 * n2);
        if (lane < 0.16) continue;
        const i = (y * w + x) * 4;
        if (!d[i + 3]) continue;
        const k = 1 - Math.min(0.62, (lane - 0.16) * 1.35);
        d[i] = Math.round(d[i] * k * 0.92);
        d[i + 1] = Math.round(d[i + 1] * k * 0.78);
        d[i + 2] = Math.round(d[i + 2] * k * 0.48);
        d[i + 3] = Math.round(d[i + 3] * (0.72 + k * 0.28));
      }
    }
    ctx.putImageData(img, 0, 0);

    const rng = mulberry(0xc0de);
    ctx.globalCompositeOperation = "lighter";
    for (let i = 0; i < 9000; i++) {
      const x = cx + gauss(rng) * w * 0.42;
      const y = cy + gauss(rng) * h * (0.012 + Math.abs(x - cx) / w * 0.022);
      const t = Math.min(1, Math.abs(x - cx) / (w * 0.42));
      const r = 255 - t * 28;
      const g = 228 - t * 70;
      const b = 186 + t * 56;
      const a = 0.12 + rng() * 0.45;
      const s = rng() < 0.08 ? 1.35 : 0.55 + rng() * 0.7;
      ctx.fillStyle = `rgba(${r | 0},${g | 0},${b | 0},${a})`;
      ctx.fillRect(x, y, s, s);
    }

    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = renderer.capabilities.getMaxAnisotropy();
    tex.minFilter = THREE.LinearFilter;
    tex.magFilter = THREE.LinearFilter;
    return tex;
  }

  function paintDustSheet(w, h) {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d");
    ctx.clearRect(0, 0, w, h);
    for (let i = 0; i < 140; i++) {
      const x = (i / 140) * w + (valueNoise(i * 0.2, 0.4, 3) - 0.5) * 40;
      const y = h * 0.5 + (valueNoise(i * 0.31, 1.2, 5) - 0.5) * h * 0.28;
      const rw = 18 + valueNoise(i, 2, 7) * 70;
      const rh = 4 + valueNoise(i, 3, 8) * 14;
      const g = ctx.createRadialGradient(x, y, 0, x, y, rw);
      g.addColorStop(0, "rgba(28, 18, 12, 0.42)");
      g.addColorStop(1, "rgba(0,0,0,0)");
      ctx.fillStyle = g;
      ctx.save();
      ctx.translate(x, y);
      ctx.scale(1, rh / rw);
      ctx.beginPath();
      ctx.arc(0, 0, rw, 0, Math.PI * 2);
      ctx.fill();
      ctx.restore();
    }
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    return tex;
  }

  function buildDiskGlow() {
    const group = new THREE.Group();
    const plate = new THREE.Mesh(
      new THREE.PlaneGeometry(14.2, 4.4),
      new THREE.MeshBasicMaterial({
        map: diskMap,
        transparent: true,
        depthWrite: false,
        blending: THREE.NormalBlending
      })
    );
    plate.position.z = -2.7;
    group.add(plate);

    const bar = new THREE.Sprite(new THREE.SpriteMaterial({
      map: barMap,
      color: 0xfff1d4,
      transparent: true,
      opacity: 0.55,
      depthWrite: false,
      blending: THREE.AdditiveBlending
    }));
    bar.scale.set(6.4, 0.52, 1);
    bar.position.z = -2.35;
    group.add(bar);

    const nucleus = new THREE.Sprite(new THREE.SpriteMaterial({
      map: glowMap,
      color: 0xfff6dc,
      transparent: true,
      opacity: 0.58,
      depthWrite: false,
      blending: THREE.AdditiveBlending
    }));
    nucleus.scale.set(0.95, 0.38, 1);
    nucleus.position.z = -2.2;
    group.add(nucleus);

    const wings = new THREE.Sprite(new THREE.SpriteMaterial({
      map: barMap,
      color: 0x8aa6d4,
      transparent: true,
      opacity: 0.2,
      depthWrite: false,
      blending: THREE.AdditiveBlending
    }));
    wings.scale.set(13.4, 0.42, 1);
    wings.position.z = -2.55;
    group.add(wings);

    group.userData = { plate, bar, nucleus, wings };
    rig.add(group);
    return group;
  }

  function buildDustSheets() {
    const list = [];
    const rng = mulberry(0xd05);
    for (let i = 0; i < 3; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(8.8 + i * 1.1, 0.42 + i * 0.08),
        new THREE.MeshBasicMaterial({
          map: dustMap,
          transparent: true,
          opacity: 0.16 - i * 0.03,
          depthWrite: false,
          blending: THREE.NormalBlending
        })
      );
      mesh.position.set((rng() - 0.5) * 0.8, (rng() - 0.5) * 0.04, -2.45 - i * 0.08);
      mesh.userData.drift = (rng() - 0.5) * 0.012;
      rig.add(mesh);
      list.push(mesh);
    }
    return list;
  }

  function paintSpikeStar(ctx, s) {
    ctx.clearRect(0, 0, s, s);
    const c = s / 2;
    ctx.save();
    ctx.translate(c, c);
    ctx.globalCompositeOperation = "lighter";
    const spike = ctx.createLinearGradient(0, -c, 0, c);
    spike.addColorStop(0, "rgba(255,246,230,0)");
    spike.addColorStop(0.46, "rgba(255,236,210,0.55)");
    spike.addColorStop(0.5, "rgba(255,255,255,0.95)");
    spike.addColorStop(0.54, "rgba(255,236,210,0.55)");
    spike.addColorStop(1, "rgba(255,246,230,0)");
    ctx.fillStyle = spike;
    ctx.fillRect(-1.1, -c, 2.2, s);
    ctx.rotate(Math.PI / 2);
    ctx.fillRect(-1.1, -c, 2.2, s);
    ctx.rotate(Math.PI / 4);
    ctx.globalAlpha = 0.35;
    ctx.fillRect(-0.6, -c * 0.72, 1.2, s * 0.72);
    ctx.rotate(Math.PI / 2);
    ctx.fillRect(-0.6, -c * 0.72, 1.2, s * 0.72);
    ctx.restore();
    const g = ctx.createRadialGradient(c, c, 0, c, c, s * 0.22);
    g.addColorStop(0, "rgba(255,252,244,1)");
    g.addColorStop(0.35, "rgba(255,226,176,0.55)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  }

  function paintFilamentSheet(w, h, seed, kind) {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d");
    ctx.clearRect(0, 0, w, h);
    const pal = {
      copper: [[210, 112, 58], [140, 72, 36], [70, 42, 88]],
      gold: [[255, 214, 140], [214, 154, 78], [90, 70, 120]],
      lavender: [[196, 168, 230], [120, 96, 168], [40, 50, 96]],
      indigo: [[90, 118, 186], [48, 64, 120], [20, 24, 48]]
    }[kind] || [[180, 140, 200], [80, 70, 120], [20, 24, 48]];
    const img = ctx.createImageData(w, h);
    const d = img.data;
    for (let y = 0; y < h; y++) {
      const ny = y / h;
      for (let x = 0; x < w; x++) {
        const nx = x / w;
        const n1 = valueNoise(nx * 3.2 + seed * 0.01, ny * 5.4, seed);
        const n2 = valueNoise(nx * 8.5, ny * 11.2, seed + 17);
        const ridge = Math.pow(Math.abs(n1 - 0.48), 0.55);
        const veil = n2 * (0.35 + n1 * 0.65);
        const band = Math.exp(-Math.pow((ny - 0.5) / 0.42, 2));
        const a = Math.min(1, (0.22 + veil * 0.55) * (1 - ridge * 0.7) * band);
        if (a < 0.03) continue;
        const t = Math.min(1, veil * 1.15);
        const c0 = pal[0];
        const c1 = pal[1];
        const mix = t;
        const i = (y * w + x) * 4;
        d[i] = (c0[0] + (c1[0] - c0[0]) * mix) | 0;
        d[i + 1] = (c0[1] + (c1[1] - c0[1]) * mix) | 0;
        d[i + 2] = (c0[2] + (c1[2] - c0[2]) * mix) | 0;
        d[i + 3] = (a * 165) | 0;
      }
    }
    ctx.putImageData(img, 0, 0);
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.minFilter = THREE.LinearFilter;
    tex.magFilter = THREE.LinearFilter;
    return tex;
  }

  function buildHalo(count, spread, size, color, seed, flatten, spin, zBias) {
    const rng = mulberry(seed);
    const pos = new Float32Array(count * 3);
    const bias = zBias || 0;
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (rng() - 0.5) * spread;
      pos[i * 3 + 1] = (rng() - 0.5) * spread * flatten;
      pos[i * 3 + 2] = (rng() - 0.5) * spread * 0.88 + bias;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      color,
      size,
      transparent: true,
      opacity: 0.9,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: false
    }));
    pts.userData.spin = spin;
    return pts;
  }

  function buildDiskRings() {
    const rings = [
      { count: 900, r0: 0.2, r1: 1.15, omega: 0.018, seed: 0xa01, flatten: 0.055 },
      { count: 1100, r0: 1.0, r1: 2.4, omega: 0.012, seed: 0xa02, flatten: 0.048 },
      { count: 1200, r0: 2.1, r1: 3.8, omega: 0.008, seed: 0xa03, flatten: 0.042 },
      { count: 900, r0: 3.4, r1: 5.6, omega: 0.0052, seed: 0xa04, flatten: 0.036 }
    ];
    return rings.map((spec) => {
      const rng = mulberry(spec.seed);
      const pos = new Float32Array(spec.count * 3);
      const col = new Float32Array(spec.count * 3);
      let wrote = 0;
      let guard = 0;
      while (wrote < spec.count && guard < spec.count * 6) {
        guard += 1;
        const radius = spec.r0 + rng() * (spec.r1 - spec.r0);
        const phase = rng() * Math.PI * 2;
        const lane = Math.abs(Math.sin(phase * 2.2 + radius * 1.15));
        if (lane < 0.2 && rng() < 0.72) continue;
        const y = gauss(rng) * (spec.flatten + radius * 0.01);
        const z = Math.sin(phase) * radius * 0.085 + gauss(rng) * 0.05;
        pos[wrote * 3] = Math.cos(phase) * radius;
        pos[wrote * 3 + 1] = y;
        pos[wrote * 3 + 2] = z;
        const t = Math.min(1, radius / 5.2);
        const c = cream.clone().lerp(warm, Math.min(1, t * 0.7)).lerp(copper, t * 0.35).lerp(cool, t);
        col[wrote * 3] = c.r;
        col[wrote * 3 + 1] = c.g;
        col[wrote * 3 + 2] = c.b;
        wrote += 1;
      }
      const geo = new THREE.BufferGeometry();
      geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
      geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
      const pts = new THREE.Points(geo, new THREE.PointsMaterial({
        map: starMap,
        size: 0.09,
        vertexColors: true,
        transparent: true,
        opacity: 0.82,
        depthWrite: false,
        blending: THREE.AdditiveBlending,
        sizeAttenuation: true
      }));
      pts.userData.omega = spec.omega;
      rig.add(pts);
      return pts;
    });
  }

  function buildShear(count) {
    const rng = mulberry(0xa11e);
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const orbits = [];
    for (let i = 0; i < count; i++) {
      const radius = 0.28 + rng() * 5.5;
      const phase = rng() * Math.PI * 2;
      const y = gauss(rng) * (0.07 + radius * 0.016);
      const z = gauss(rng) * 0.1;
      pos[i * 3] = Math.cos(phase) * radius;
      pos[i * 3 + 1] = y;
      pos[i * 3 + 2] = Math.sin(phase) * radius * 0.08 + z;
      const t = Math.min(1, radius / 5.2);
      const c = warm.clone().lerp(copper, t * 0.28).lerp(cool, t);
      col[i * 3] = c.r;
      col[i * 3 + 1] = c.g;
      col[i * 3 + 2] = c.b;
      orbits.push({
        radius,
        phase,
        y,
        z,
        omega: (0.046 / (0.5 + radius)) * (rng() > 0.5 ? 1 : -1) * (0.75 + rng() * 0.5)
      });
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      size: 0.082,
      vertexColors: true,
      transparent: true,
      opacity: 0.8,
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
        opacity: 0.5
      }));
      sprite.scale.setScalar(0.04 + rng() * 0.036);
      sprite.userData = {
        node,
        radius: 0.1 + rng() * 0.4,
        phase: rng() * Math.PI * 2,
        omega: (0.1 + rng() * 0.2) * (rng() > 0.5 ? 1 : -1),
        tilt: (rng() - 0.5) * 0.32,
        baseOpacity: 0.42 + rng() * 0.18
      };
      rig.add(sprite);
      list.push(sprite);
    }
    return list;
  }

  function buildColoredField(count, spread, size, seed, flatten, spin) {
    const rng = mulberry(seed);
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const palette = [indigo, cool, cream, copper, lavender, warm];
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (rng() - 0.5) * spread;
      pos[i * 3 + 1] = (rng() - 0.5) * spread * flatten;
      pos[i * 3 + 2] = (rng() - 0.5) * spread * 0.9 - 8;
      const roll = rng();
      const c = roll < 0.62 ? palette[0].clone().lerp(palette[1], rng())
        : roll < 0.82 ? palette[2].clone().lerp(palette[5], rng())
          : roll < 0.93 ? palette[3].clone()
            : palette[4].clone();
      col[i * 3] = c.r;
      col[i * 3 + 1] = c.g;
      col[i * 3 + 2] = c.b;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      size,
      vertexColors: true,
      transparent: true,
      opacity: 0.78,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: false
    }));
    pts.userData.spin = spin;
    return pts;
  }

  function buildFilaments() {
    const list = [];
    const specs = [
      { map: 0, x: -6, y: 4.6, z: -22, sx: 28, sy: 10, rz: -0.18, op: 0.13, drift: 0.004 },
      { map: 1, x: 7, y: -4.2, z: -20, sx: 24, sy: 9, rz: 0.14, op: 0.11, drift: -0.0035 },
      { map: 2, x: -2, y: 6.4, z: -26, sx: 22, sy: 8, rz: 0.08, op: 0.09, drift: 0.0028 },
      { map: 3, x: 3, y: -6.1, z: -24, sx: 30, sy: 12, rz: -0.06, op: 0.1, drift: -0.0022 }
    ];
    for (const spec of specs) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(1, 1),
        new THREE.MeshBasicMaterial({
          map: filamentMaps[spec.map],
          transparent: true,
          opacity: spec.op,
          depthWrite: false,
          blending: THREE.NormalBlending
        })
      );
      mesh.position.set(spec.x, spec.y, spec.z);
      mesh.scale.set(spec.sx, spec.sy, 1);
      mesh.rotation.z = spec.rz;
      mesh.userData.drift = spec.drift;
      mesh.userData.baseX = spec.x;
      scene.add(mesh);
      list.push(mesh);
    }
    return list;
  }

  function buildSpikedStars(count) {
    const rng = mulberry(0x5b1);
    const list = [];
    for (let i = 0; i < count; i++) {
      const sprite = new THREE.Sprite(new THREE.SpriteMaterial({
        map: spikeMap,
        color: rng() > 0.35 ? 0xfff1dc : 0xc8d4ff,
        transparent: true,
        opacity: 0.22 + rng() * 0.28,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      const y = (rng() > 0.5 ? 1 : -1) * (1.6 + rng() * 9);
      sprite.position.set((rng() - 0.5) * 22, y, -6 - rng() * 28);
      const s = 0.12 + rng() * 0.22;
      sprite.scale.set(s, s, 1);
      sprite.userData.base = sprite.material.opacity;
      sprite.userData.phase = rng() * Math.PI * 2;
      scene.add(sprite);
      list.push(sprite);
    }
    return list;
  }

  function buildNodes() {
    const list = [];
    for (let i = 0; i < NODE_IDS.length; i++) {
      const group = new THREE.Group();
      group.position.set(nodeX(i), 0.04, 0.12);
      const info = faceInfo(NODE_IDS[i]);
      const coreCol = new THREE.Color(info.core || "#FFE9B0");
      const bloom = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: coreCol,
        transparent: true,
        opacity: i === 0 ? 0.26 : 0.14,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      bloom.scale.set(i === 0 ? 3.15 : 2.05, i === 0 ? 1.18 : 0.82, 1);
      bloom.userData.base = bloom.scale.clone();
      group.add(bloom);
      const core = new THREE.Sprite(new THREE.SpriteMaterial({
        map: starMap,
        color: 0xffffff,
        transparent: true,
        opacity: i === 0 ? 0.58 : 0.4,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      core.scale.set(i === 0 ? 0.3 : 0.2, i === 0 ? 0.3 : 0.2, 1);
      group.add(core);
      const dust = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: 0xe8c898,
        transparent: true,
        opacity: 0.05,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      dust.scale.set(i === 0 ? 2.15 : 1.35, i === 0 ? 0.58 : 0.42, 1);
      group.add(dust);
      const label = makeLabel(info.title);
      label.position.y = -0.72;
      group.add(label);
      const hit = new THREE.Mesh(
        new THREE.SphereGeometry(0.52, 10, 8),
        new THREE.MeshBasicMaterial({ visible: false })
      );
      hit.userData.face = NODE_IDS[i];
      hit.userData.node = i;
      group.add(hit);
      group.userData.node = i;
      rig.add(group);
      list.push({ group, bloom, core, dust, label, hit });
    }
    return list;
  }

  function buildVignette() {
    const tex = spriteTex(256, (ctx, s) => {
      const g = ctx.createRadialGradient(s / 2, s / 2, s * 0.3, s / 2, s / 2, s * 0.64);
      g.addColorStop(0, "rgba(0,0,0,0)");
      g.addColorStop(1, "rgba(0,0,0,0.32)");
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

  function tickLiving(dt, now) {
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

    for (const ring of diskRings) {
      ring.rotation.y += ring.userData.omega * dt;
    }

    for (const sheet of dustSheets) {
      sheet.position.x += sheet.userData.drift * dt;
      if (sheet.position.x > 0.7) sheet.userData.drift = -Math.abs(sheet.userData.drift);
      if (sheet.position.x < -0.7) sheet.userData.drift = Math.abs(sheet.userData.drift);
    }

    if (diskGlow.userData.bar) {
      diskGlow.userData.bar.material.opacity = 0.5 + Math.sin(now * 0.00018) * 0.04;
      diskGlow.userData.nucleus.material.opacity = 0.66 + Math.sin(now * 0.00022) * 0.04;
    }

    for (const s of orbiters) {
      const u = s.userData;
      u.phase += u.omega * dt;
      const nx = nodeX(u.node);
      const pull = 0.08;
      s.position.x += (nx + Math.cos(u.phase) * u.radius - s.position.x) * pull;
      s.position.y = Math.sin(u.phase) * u.radius * 0.22 * Math.cos(u.tilt);
      s.position.z = Math.sin(u.phase * 0.7) * u.radius * 0.12;
      const on = u.node === state.node;
      s.material.opacity = u.baseOpacity * (on ? 0.85 : 0.55);
    }

    for (const sheet of filaments) {
      sheet.position.x += sheet.userData.drift * dt * 4;
      if (sheet.position.x > sheet.userData.baseX + 1.4) sheet.userData.drift = -Math.abs(sheet.userData.drift);
      if (sheet.position.x < sheet.userData.baseX - 1.4) sheet.userData.drift = Math.abs(sheet.userData.drift);
      sheet.rotation.z += sheet.userData.drift * dt * 0.08;
    }

    for (const spike of spikes) {
      spike.userData.phase += dt * 0.35;
      spike.material.opacity = spike.userData.base * (0.82 + Math.sin(spike.userData.phase) * 0.18);
    }

    if (deepField.userData.spin) {
      deepField.rotation.y = now * deepField.userData.spin;
      deepField.rotation.z = Math.sin(now * 0.00003) * 0.01;
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
    overlayEl.classList.remove("show", "ready", "motion");
    trackEl.classList.remove("from-top", "from-bottom");
    trackEl.classList.add(state.origin === "bottom" ? "from-bottom" : "from-top");
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
    overlayEl.classList.remove("show", "ready", "motion");
    trackEl.classList.remove("from-top", "from-bottom");
    trackEl.style.removeProperty("--track-y");
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
    trackEl.style.setProperty("--track-y", `${y}px`);
    if (instant || !state.motion) {
      trackEl.style.transition = "none";
    } else {
      trackEl.style.transition = "";
    }
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
      tickLiving(step, now);
      farStars.rotation.y = now * (farStars.userData.spin || 0);
      midStars.rotation.y = now * (midStars.userData.spin || 0);
      nearStars.rotation.y = now * (nearStars.userData.spin || 0);
      farStars.rotation.z = Math.sin(now * 0.00004) * 0.012;
      midStars.rotation.z = Math.sin(now * 0.00006) * 0.018;
    }
    rig.position.x = -state.visualX * 0.82;
    farStars.position.x = -state.visualX * 0.03;
    midStars.position.x = -state.visualX * 0.07;
    nearStars.position.x = -state.visualX * 0.16;
    deepField.position.x = -state.visualX * 0.02;
    camera.position.set(
      0,
      CAM_Y + (state.motion ? Math.sin(now * 0.00018) * 0.028 : 0),
      CAM_Z
    );
    camera.lookAt(lookTarget);
    const breathe = state.motion ? 0.96 + 0.04 * Math.sin(now * 0.0007) : 1;
    for (const n of nodes) {
      const on = n.group.userData.node === state.node;
      const base = n.bloom.userData.base;
      n.bloom.material.opacity = (on ? 0.3 : 0.11) * breathe;
      n.core.material.opacity = on ? 0.62 : 0.28;
      n.dust.material.opacity = on ? 0.11 : 0.035;
      n.bloom.scale.set(base.x * (on ? 1.08 : 1), base.y * (on ? 1.06 : 1), 1);
      n.label.material.opacity = on ? 0.94 : 0.4;
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
    const tex = spriteTex(1024, (ctx, s) => {
      ctx.clearRect(0, 0, s, s);
      const label = String(text || "").toUpperCase();
      ctx.font = "600 54px 'Segoe UI Variable', 'Segoe UI', sans-serif";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      const tracking = 7;
      const chars = label.split("");
      const widths = chars.map((ch) => ctx.measureText(ch).width);
      const total = widths.reduce((a, b) => a + b, 0) + tracking * Math.max(0, chars.length - 1);
      const pillW = Math.min(s * 0.82, total + 56);
      const pillH = 68;
      roundRect(ctx, (s - pillW) / 2, (s - pillH) / 2, pillW, pillH, 16);
      ctx.fillStyle = "rgba(0, 2, 8, 0.34)";
      ctx.fill();
      ctx.strokeStyle = "rgba(0,0,0,0.82)";
      ctx.lineWidth = 4.5;
      ctx.lineJoin = "round";
      drawTracked(ctx, chars, widths, tracking, s / 2, s / 2, "stroke");
      ctx.fillStyle = "rgba(244,239,226,0.94)";
      drawTracked(ctx, chars, widths, tracking, s / 2, s / 2, "fill");
    });
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({
      map: tex,
      transparent: true,
      opacity: 0.4,
      depthWrite: false
    }));
    sprite.scale.set(1.36, 0.26, 1);
    return sprite;
  }

  function drawTracked(ctx, chars, widths, tracking, x, y, mode) {
    const total = widths.reduce((a, b) => a + b, 0) + tracking * Math.max(0, chars.length - 1);
    let cx = x - total / 2;
    for (let i = 0; i < chars.length; i++) {
      const at = cx + widths[i] / 2;
      if (mode === "stroke") ctx.strokeText(chars[i], at, y);
      else ctx.fillText(chars[i], at, y);
      cx += widths[i] + tracking;
    }
  }

  function roundRect(ctx, x, y, w, h, r) {
    const rad = Math.min(r, w / 2, h / 2);
    ctx.beginPath();
    ctx.moveTo(x + rad, y);
    ctx.arcTo(x + w, y, x + w, y + h, rad);
    ctx.arcTo(x + w, y + h, x, y + h, rad);
    ctx.arcTo(x, y + h, x, y, rad);
    ctx.arcTo(x, y, x + w, y, rad);
    ctx.closePath();
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

  function valueNoise(x, y, seed) {
    const x0 = Math.floor(x);
    const y0 = Math.floor(y);
    const fx = x - x0;
    const fy = y - y0;
    const sx = fx * fx * (3 - 2 * fx);
    const sy = fy * fy * (3 - 2 * fy);
    const a = hash2(x0, y0, seed);
    const b = hash2(x0 + 1, y0, seed);
    const c = hash2(x0, y0 + 1, seed);
    const d = hash2(x0 + 1, y0 + 1, seed);
    return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
  }

  function hash2(ix, iy, seed) {
    let n = Math.imul(ix, 374761393) + Math.imul(iy, 668265263) + seed * 1013904223 | 0;
    n = Math.imul(n ^ n >>> 13, 1274126177);
    return ((n ^ n >>> 16) >>> 0) / 4294967296;
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
