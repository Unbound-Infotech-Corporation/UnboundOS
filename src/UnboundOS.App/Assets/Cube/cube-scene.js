/* UnboundOS Home — original procedural living galaxy.
   Restrained tilted OS band + dense in-band nebula (not a full-bleed
   wallpaper). Wallpaper Engine / observatory stills are look-dev only. */
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
  renderer.setClearColor(0x000104, 1);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 2));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.05;

  const CAM_Y = 1.62;
  const CAM_Z = 8.95;
  const RIG_PITCH = 0.34;
  const RIG_ROLL = 0.035;
  const lookTarget = new THREE.Vector3(0, 0.03, -0.7);

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(26, 1, 0.08, 160);
  camera.position.set(0, CAM_Y, CAM_Z);
  camera.lookAt(lookTarget);

  const rig = new THREE.Group();
  rig.rotation.x = RIG_PITCH;
  rig.rotation.z = RIG_ROLL;
  scene.add(rig);

  const starMap = spriteTex(64, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s * 0.46);
    g.addColorStop(0, "rgba(255,255,255,1)");
    g.addColorStop(0.16, "rgba(255,246,220,0.9)");
    g.addColorStop(0.4, "rgba(176,198,255,0.24)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  });

  const glowMap = spriteTex(256, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s * 0.46);
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
  const nebulaMap = paintNebulaVolume(1600, 640, 0xb1, "blue");
  const nebulaWarm = paintNebulaVolume(1400, 560, 0xc7, "rose");
  const trailMap = paintEnergyTrail(2048, 512);
  const voidMap = spriteTex(256, paintVoidCore);
  const accretionMap = paintAccretionRing(512, 256);
  const spikeMap = spriteTex(256, paintSpikeStar);
  const filamentMaps = [
    paintFilamentSheet(640, 320, 0xc11, "indigo"),
    paintFilamentSheet(640, 320, 0xc22, "violet"),
    paintFilamentSheet(640, 320, 0xc33, "lavender"),
    paintFilamentSheet(640, 320, 0xc44, "magenta")
  ];

  const warm = new THREE.Color(0xffe2a8);
  const copper = new THREE.Color(0xb86a5a);
  const cool = new THREE.Color(0x6a88c8);
  const cream = new THREE.Color(0xfff4dc);
  const indigo = new THREE.Color(0x24306e);
  const lavender = new THREE.Color(0xa898d0);
  const violet = new THREE.Color(0x5a4a98);
  const magenta = new THREE.Color(0xe07098);

  const farStars = buildHalo(9800, 82, 1.5, 0x8a9ccc, 0x51f, 0.74, 0.0000048, 0);
  const midStars = buildHalo(4400, 30, 1.85, 0xc8d4f0, 0x77a, 0.66, -0.000009, 0);
  const nearStars = buildHalo(1900, 17, 2.2, 0xe8e4f4, 0x91c, 0.58, 0.000016, 2.2);
  const deepField = buildColoredField(11000, 100, 1.42, 0xdef1, 0.72, 0.0000032);
  const diskRings = buildDiskRings();
  const shear = buildShear(1500);
  const diskGlow = buildDiskGlow();
  const dustSheets = buildDustSheets();
  const filaments = buildFilaments();
  const spikes = buildSpikedStars(16);
  const nodes = buildNodes();
  const blackHole = buildBlackHole();
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
    ctx.scale(1, 0.32);
    const g = ctx.createRadialGradient(0, 0, 0, 0, 0, s * 0.44);
    g.addColorStop(0, "rgba(255,248,236,0.82)");
    g.addColorStop(0.16, "rgba(230,190,200,0.32)");
    g.addColorStop(0.42, "rgba(120,110,180,0.12)");
    g.addColorStop(0.7, "rgba(60,80,160,0.05)");
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

    oval(w * 0.48, h * 0.22, [
      [0, "rgba(40, 52, 130, 0.55)"],
      [0.4, "rgba(24, 30, 86, 0.26)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.42, h * 0.16, [
      [0, "rgba(96, 70, 158, 0.42)"],
      [0.48, "rgba(46, 38, 104, 0.18)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.34, h * 0.11, [
      [0, "rgba(168, 96, 140, 0.34)"],
      [0.42, "rgba(90, 70, 130, 0.14)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.16, h * 0.065, [
      [0, "rgba(255, 236, 210, 0.78)"],
      [0.3, "rgba(255, 200, 150, 0.36)"],
      [0.7, "rgba(180, 120, 110, 0.1)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.048, h * 0.042, [
      [0, "rgba(255, 250, 240, 0.95)"],
      [0.4, "rgba(255, 226, 176, 0.48)"],
      [1, "rgba(0,0,0,0)"]
    ]);

    ctx.save();
    ctx.globalCompositeOperation = "lighter";
    ctx.translate(cx, cy);
    ctx.scale(1, 0.04);
    const spike = ctx.createRadialGradient(0, 0, 0, 0, 0, w * 0.46);
    spike.addColorStop(0, "rgba(255,248,230,0.5)");
    spike.addColorStop(0.2, "rgba(255,220,160,0.12)");
    spike.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = spike;
    ctx.fillRect(-w * 0.46, -w * 0.46, w * 0.92, w * 0.92);
    ctx.restore();

    const img = ctx.getImageData(0, 0, w, h);
    const d = img.data;
    for (let y = 0; y < h; y++) {
      const ny = (y - cy) / h;
      const band = Math.exp(-ny * ny * 52);
      if (band < 0.02) continue;
      for (let x = 0; x < w; x++) {
        const nx = (x - cx) / w;
        const n1 = valueNoise(nx * 20 + 2.1, ny * 30, 11);
        const n2 = valueNoise(nx * 8 - 1.4, ny * 16, 29);
        const lane = band * (0.32 * n1 + 0.68 * n2);
        if (lane < 0.13) continue;
        const i = (y * w + x) * 4;
        if (!d[i + 3]) continue;
        const k = 1 - Math.min(0.62, (lane - 0.13) * 1.3);
        d[i] = Math.round(d[i] * k * 0.72);
        d[i + 1] = Math.round(d[i + 1] * k * 0.68);
        d[i + 2] = Math.round(d[i + 2] * (0.85 + k * 0.2));
        d[i + 3] = Math.round(d[i + 3] * (0.78 + k * 0.22));
      }
    }
    ctx.putImageData(img, 0, 0);

    const rng = mulberry(0xc0de);
    ctx.globalCompositeOperation = "lighter";
    for (let i = 0; i < 14000; i++) {
      const x = cx + gauss(rng) * w * 0.42;
      const y = cy + gauss(rng) * h * (0.034 + Math.abs(x - cx) / w * 0.045);
      const t = Math.min(1, Math.abs(x - cx) / (w * 0.42));
      const r = 210 - t * 70;
      const g = 200 - t * 50;
      const b = 230 + t * 20;
      const a = 0.1 + rng() * 0.4;
      const s = rng() < 0.07 ? 1.35 : 0.5 + rng() * 0.7;
      ctx.fillStyle = `rgba(${r | 0},${g | 0},${b | 0},${a})`;
      ctx.fillRect(x, y, s, s);
    }

    return finishSoftTexture(c, 72);
  }

  function paintDustSheet(w, h) {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d");
    ctx.clearRect(0, 0, w, h);
    for (let i = 0; i < 160; i++) {
      const x = w * 0.12 + (i / 160) * w * 0.76 + (valueNoise(i * 0.2, 0.4, 3) - 0.5) * 28;
      const y = h * 0.5 + (valueNoise(i * 0.31, 1.2, 5) - 0.5) * h * 0.22;
      const rw = 20 + valueNoise(i, 2, 7) * 80;
      const rh = 6 + valueNoise(i, 3, 8) * 18;
      const g = ctx.createRadialGradient(x, y, 0, x, y, rw);
      g.addColorStop(0, "rgba(16, 12, 32, 0.5)");
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
    return finishSoftTexture(c, 48);
  }

  function paintNebulaVolume(w, h, seed, kind) {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d");
    const img = ctx.createImageData(w, h);
    const d = img.data;
    const cx = w * 0.5;
    const cy = h * 0.5;
    const rose = kind === "rose";
    const s0 = seed || 0xb1;
    for (let y = 0; y < h; y++) {
      const ny = (y - cy) / h;
      const band = Math.exp(-ny * ny * 26);
      if (band < 0.03) continue;
      for (let x = 0; x < w; x++) {
        const nx = (x - cx) / w;
        const edgeX = Math.exp(-nx * nx * 7.2);
        const n1 = valueNoise(nx * 3.4 + 0.4, ny * 6.8, s0);
        const n2 = valueNoise(nx * 8.0 - 1.1, ny * 13, s0 + 17);
        const n3 = valueNoise(nx * 15, ny * 21, s0 + 31);
        const hue = valueNoise(nx * 2.1 + 0.6, ny * 3.4, s0 + 53);
        const cloud = n1 * 0.52 + n2 * 0.33 + n3 * 0.15;
        const a = band * edgeX * cloud * 0.74;
        if (a < 0.03) continue;
        const t = Math.min(1, cloud * 1.2);
        const i = (y * w + x) * 4;
        if (rose) {
          d[i] = (70 + t * 130 + hue * 50) | 0;
          d[i + 1] = (22 + t * 48) | 0;
          d[i + 2] = (68 + t * 90 - hue * 20) | 0;
        } else {
          d[i] = (28 + t * 90 + hue * 80) | 0;
          d[i + 1] = (24 + t * 72 + (1 - hue) * 30) | 0;
          d[i + 2] = (86 + t * 130 - hue * 36) | 0;
        }
        d[i + 3] = (a * 225) | 0;
      }
    }
    ctx.putImageData(img, 0, 0);
    return finishSoftTexture(c, 64);
  }

  function paintEnergyTrail(w, h) {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d");
    ctx.clearRect(0, 0, w, h);
    const cx = w * 0.5;
    const cy = h * 0.5;
    ctx.globalCompositeOperation = "lighter";
    const trailY = (x) => {
      const t = (x - cx) / w;
      return cy
        + Math.sin(t * 4.1 + 0.35) * h * 0.18
        + Math.sin(t * 9.4 + 1.6) * h * 0.05
        + (valueNoise(t * 7.2, 0.22, 0xee) - 0.5) * h * 0.07;
    };
    const fade = (x, y) => {
      const ux = Math.abs((x - cx) / (w * 0.4));
      const uy = Math.abs((y - cy) / (h * 0.36));
      return Math.max(0, 1 - ux * ux) * Math.max(0, 1 - uy * uy);
    };
    const passes = [
      { width: 56, color: [90, 36, 120], alpha: 0.08 },
      { width: 28, color: [170, 70, 140], alpha: 0.12 },
      { width: 12, color: [230, 140, 180], alpha: 0.16 },
      { width: 4.5, color: [255, 220, 230], alpha: 0.22 }
    ];
    for (const pass of passes) {
      ctx.lineCap = "round";
      ctx.lineJoin = "round";
      ctx.lineWidth = pass.width;
      ctx.beginPath();
      let started = false;
      for (let x = w * 0.12; x <= w * 0.88; x += 2) {
        const y = trailY(x);
        if (!started) {
          ctx.moveTo(x, y);
          started = true;
        } else ctx.lineTo(x, y);
      }
      ctx.strokeStyle = `rgba(${pass.color[0]},${pass.color[1]},${pass.color[2]},${pass.alpha})`;
      ctx.stroke();
    }
    const veil = ctx.getImageData(0, 0, w, h);
    const d = veil.data;
    for (let y = 0; y < h; y++) {
      for (let x = 0; x < w; x++) {
        const i = (y * w + x) * 4;
        if (!d[i + 3]) continue;
        d[i + 3] = Math.round(d[i + 3] * fade(x, y));
      }
    }
    ctx.putImageData(veil, 0, 0);
    return finishSoftTexture(c, 56);
  }

  function paintVoidCore(ctx, s) {
    ctx.clearRect(0, 0, s, s);
    const c = s / 2;
    const g = ctx.createRadialGradient(c, c, 0, c, c, s * 0.42);
    g.addColorStop(0, "rgba(0,0,0,0.88)");
    g.addColorStop(0.42, "rgba(0,0,2,0.5)");
    g.addColorStop(0.72, "rgba(8,6,14,0.16)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  }

  function paintAccretionRing(w, h) {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    const ctx = c.getContext("2d");
    ctx.clearRect(0, 0, w, h);
    ctx.globalCompositeOperation = "lighter";
    const cx = w * 0.5;
    const cy = h * 0.5;
    ctx.save();
    ctx.translate(cx, cy);
    ctx.scale(1, 0.28);
    const ring = ctx.createRadialGradient(0, 0, w * 0.18, 0, 0, w * 0.38);
    ring.addColorStop(0, "rgba(0,0,0,0)");
    ring.addColorStop(0.62, "rgba(0,0,0,0)");
    ring.addColorStop(0.78, "rgba(196, 120, 82, 0.16)");
    ring.addColorStop(0.88, "rgba(255, 196, 150, 0.1)");
    ring.addColorStop(0.96, "rgba(80, 50, 90, 0.04)");
    ring.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = ring;
    ctx.beginPath();
    ctx.arc(0, 0, w * 0.38, 0, Math.PI * 2);
    ctx.fill();
    ctx.restore();
    return finishSoftTexture(c, 36);
  }

  function buildBlackHole() {
    const group = new THREE.Group();
    group.position.set(-11.4, 3.7, -36);
    const shadow = new THREE.Sprite(new THREE.SpriteMaterial({
      map: voidMap,
      color: 0x04040a,
      transparent: true,
      premultipliedAlpha: true,
      opacity: 0.4,
      depthWrite: false,
      blending: THREE.NormalBlending
    }));
    shadow.scale.set(2.55, 2.1, 1);
    group.add(shadow);
    const ring = new THREE.Mesh(
      new THREE.PlaneGeometry(3.35, 0.68),
      softMat(accretionMap, { opacity: 0.16, blending: THREE.AdditiveBlending })
    );
    ring.rotation.x = 1.05;
    ring.rotation.z = 0.38;
    group.add(ring);
    group.userData = { shadow, ring };
    scene.add(group);
    return group;
  }

  function buildDiskGlow() {
    const group = new THREE.Group();
    const plate = new THREE.Mesh(
      new THREE.PlaneGeometry(14.6, 5.15),
      softMat(diskMap)
    );
    plate.position.z = -0.1;
    group.add(plate);

    const nebula = new THREE.Mesh(
      new THREE.PlaneGeometry(13.8, 3.7),
      softMat(nebulaMap, { opacity: 0.94 })
    );
    nebula.position.z = -0.18;
    group.add(nebula);

    const nebula2 = new THREE.Mesh(
      new THREE.PlaneGeometry(11.4, 2.85),
      softMat(nebulaWarm, { opacity: 0.4 })
    );
    nebula2.position.set(1.15, -0.14, -0.3);
    nebula2.rotation.z = 0.07;
    group.add(nebula2);

    const trail = new THREE.Mesh(
      new THREE.PlaneGeometry(12.6, 2.4),
      softMat(trailMap, { opacity: 0.38, blending: THREE.AdditiveBlending })
    );
    trail.position.set(0.12, 0.03, 0.01);
    trail.rotation.z = -0.05;
    group.add(trail);

    const bar = new THREE.Sprite(softSprite(barMap, { color: 0xffe8d0, opacity: 0.2 }));
    bar.scale.set(6.2, 0.92, 1);
    bar.position.z = 0.02;
    group.add(bar);

    const nucleus = new THREE.Sprite(softSprite(glowMap, { color: 0xfff6dc, opacity: 0.4 }));
    nucleus.scale.set(1.7, 0.92, 1);
    nucleus.position.z = 0.06;
    group.add(nucleus);

    const wings = new THREE.Sprite(softSprite(barMap, { color: 0x6a78c8, opacity: 0.2 }));
    wings.scale.set(12.2, 1.15, 1);
    wings.position.z = -0.22;
    group.add(wings);

    group.userData = { plate, nebula, nebula2, trail, bar, nucleus, wings };
    rig.add(group);
    return group;
  }

  function buildDustSheets() {
    const list = [];
    const rng = mulberry(0xd05);
    for (let i = 0; i < 2; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(8.2 + i * 0.7, 0.88 + i * 0.1),
        softMat(dustMap, { color: 0xb8b0d8, opacity: 0.26 - i * 0.06 })
      );
      mesh.position.set((rng() - 0.5) * 0.7, (rng() - 0.5) * 0.05, -0.14 - i * 0.04);
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
      indigo: [[70, 96, 176], [36, 48, 110], [12, 16, 40]],
      violet: [[110, 72, 168], [56, 40, 110], [18, 16, 42]],
      lavender: [[176, 150, 220], [96, 80, 150], [28, 32, 68]],
      magenta: [[214, 96, 150], [140, 52, 110], [40, 20, 48]]
    }[kind] || [[120, 100, 180], [50, 40, 90], [12, 14, 32]];
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
        const cliff = Math.exp(-Math.pow((nx * 0.7 + ny * 0.55 - 0.55) / 0.38, 2));
        const edge = Math.pow(Math.sin(nx * Math.PI), 1.35) * Math.pow(Math.sin(ny * Math.PI), 1.35);
        const a = Math.min(1, (0.18 + veil * 0.62) * (1 - ridge * 0.55) * (0.35 + cliff) * edge);
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
    return finishSoftTexture(c, 36);
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
      premultipliedAlpha: true,
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
      { count: 1100, r0: 0.2, r1: 1.15, omega: 0.018, seed: 0xa01, flatten: 0.2 },
      { count: 1400, r0: 1.0, r1: 2.4, omega: 0.012, seed: 0xa02, flatten: 0.17 },
      { count: 1400, r0: 2.1, r1: 3.8, omega: 0.008, seed: 0xa03, flatten: 0.14 },
      { count: 1100, r0: 3.4, r1: 5.6, omega: 0.0052, seed: 0xa04, flatten: 0.12 }
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
        const y = gauss(rng) * (spec.flatten + radius * 0.014);
        const z = Math.sin(phase) * radius * 0.14 + gauss(rng) * 0.07;
        pos[wrote * 3] = Math.cos(phase) * radius;
        pos[wrote * 3 + 1] = y;
        pos[wrote * 3 + 2] = z;
        const t = Math.min(1, radius / 5.2);
        const c = cream.clone().lerp(warm, (1 - t) * 0.35).lerp(violet, t * 0.4).lerp(cool, t * t * 1.05);
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
        premultipliedAlpha: true,
        opacity: 0.64,
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
      const y = gauss(rng) * (0.11 + radius * 0.022);
      const z = gauss(rng) * 0.14;
      pos[i * 3] = Math.cos(phase) * radius;
      pos[i * 3 + 1] = y;
      pos[i * 3 + 2] = Math.sin(phase) * radius * 0.08 + z;
      const t = Math.min(1, radius / 5.2);
      const c = warm.clone().lerp(magenta, t * 0.18).lerp(cool, t * 0.9);
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
      premultipliedAlpha: true,
      opacity: 0.8,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: true
    }));
    pts.userData.orbits = orbits;
    rig.add(pts);
    return pts;
  }

  function poseSystem(n, dt) {
    for (const p of n.planets) {
      const u = p.userData;
      if (dt) u.phase += u.omega * dt;
      p.position.set(
        Math.cos(u.phase) * u.radius,
        Math.sin(u.phase) * u.radius * u.tilt,
        Math.sin(u.phase) * u.radius * 0.42
      );
    }
  }

  function buildColoredField(count, spread, size, seed, flatten, spin) {
    const rng = mulberry(seed);
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const palette = [indigo, cool, cream, violet, lavender, magenta];
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (rng() - 0.5) * spread;
      pos[i * 3 + 1] = (rng() - 0.5) * spread * flatten;
      pos[i * 3 + 2] = (rng() - 0.5) * spread * 0.9 - 8;
      const roll = rng();
      const c = roll < 0.7 ? palette[0].clone().lerp(palette[1], rng())
        : roll < 0.86 ? palette[2].clone().lerp(palette[1], rng())
          : roll < 0.94 ? palette[3].clone()
            : palette[4].clone().lerp(palette[5], rng());
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
      premultipliedAlpha: true,
      opacity: 0.9,
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
      { map: 0, x: -3.2, y: 0.38, z: -3.4, sx: 10.4, sy: 2.8, rx: 0.1, rz: -0.06, op: 0.16, drift: 0.003 },
      { map: 3, x: 0.3, y: 0.05, z: -2.1, sx: 11.2, sy: 1.9, rx: -0.04, rz: -0.04, op: 0.15, drift: -0.0018 }
    ];
    for (const spec of specs) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(1, 1),
        softMat(filamentMaps[spec.map], { opacity: spec.op })
      );
      mesh.position.set(spec.x, spec.y, spec.z);
      mesh.scale.set(spec.sx, spec.sy, 1);
      mesh.rotation.x = spec.rx;
      mesh.rotation.z = spec.rz;
      mesh.userData.drift = spec.drift;
      mesh.userData.baseX = spec.x;
      rig.add(mesh);
      list.push(mesh);
    }
    return list;
  }

  function buildSpikedStars(count) {
    const rng = mulberry(0x5b1);
    const list = [];
    for (let i = 0; i < count; i++) {
      const sprite = new THREE.Sprite(softSprite(spikeMap, {
        color: rng() > 0.45 ? 0xe8d8ff : 0xc8d4ff,
        opacity: 0.14 + rng() * 0.16
      }));
      const y = (rng() > 0.5 ? 1 : -1) * (2.4 + rng() * 8);
      sprite.position.set((rng() - 0.5) * 20, y, -8 - rng() * 24);
      const s = 0.08 + rng() * 0.14;
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
    const planetPal = [0xc4b49a, 0x8a98b4, 0xb07e68, 0xd0c4ae, 0x6a768c];
    for (let i = 0; i < NODE_IDS.length; i++) {
      const group = new THREE.Group();
      group.position.set(nodeX(i), 0.04, 0.12);
      const rng = mulberry(0x5100 + i * 97);
      const core = i === 0;
      const sun = new THREE.Sprite(softSprite(starMap, {
        color: core ? 0xfff0d4 : 0xeee6dc,
        opacity: 0.36
      }));
      const sunS = core ? 0.14 : 0.108;
      sun.scale.set(sunS, sunS, 1);
      group.add(sun);
      const corona = new THREE.Sprite(softSprite(glowMap, {
        color: core ? 0xffe0b4 : 0xe4d6c8,
        opacity: 0.06
      }));
      const corS = core ? 0.46 : 0.36;
      corona.scale.set(corS, corS * 0.82, 1);
      corona.userData.base = corona.scale.clone();
      group.add(corona);
      const planets = [];
      const count = 3 + (i % 3);
      for (let p = 0; p < count; p++) {
        const spr = new THREE.Sprite(softSprite(starMap, {
          color: planetPal[p % planetPal.length],
          opacity: 0.26
        }));
        const ps = 0.028 + rng() * 0.016;
        spr.scale.set(ps, ps, 1);
        spr.userData = {
          radius: 0.11 + p * 0.055 + rng() * 0.012,
          phase: rng() * Math.PI * 2,
          omega: (0.2 / (0.65 + p * 0.55)) * (rng() > 0.4 ? 1 : -1),
          tilt: 0.26 + rng() * 0.2,
          baseOpacity: 0.32 + rng() * 0.12
        };
        group.add(spr);
        planets.push(spr);
      }
      for (let k = 0; k < 7; k++) {
        const spr = new THREE.Sprite(softSprite(starMap, {
          color: rng() > 0.5 ? 0xd4dcec : 0xf0e8d8,
          opacity: 0.14 + rng() * 0.1
        }));
        const cs = 0.011 + rng() * 0.01;
        spr.scale.set(cs, cs, 1);
        spr.position.set((rng() - 0.5) * 0.2, (rng() - 0.5) * 0.07, (rng() - 0.5) * 0.09);
        group.add(spr);
      }
      const hit = new THREE.Mesh(
        new THREE.SphereGeometry(0.48, 10, 8),
        new THREE.MeshBasicMaterial({ visible: false })
      );
      hit.userData.face = NODE_IDS[i];
      hit.userData.node = i;
      group.add(hit);
      group.userData.node = i;
      rig.add(group);
      const sys = { group, sun, corona, planets, hit };
      poseSystem(sys, 0);
      list.push(sys);
    }
    return list;
  }

  function buildVignette() {
    const c = document.createElement("canvas");
    c.width = c.height = 256;
    const ctx = c.getContext("2d");
    const g = ctx.createRadialGradient(128, 128, 56, 128, 128, 168);
    g.addColorStop(0, "rgba(0,0,0,0)");
    g.addColorStop(0.55, "rgba(0,0,0,0.18)");
    g.addColorStop(1, "rgba(0,0,0,0.62)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, 256, 256);
    const tex = new THREE.CanvasTexture(c);
    tex.minFilter = THREE.LinearFilter;
    tex.magFilter = THREE.LinearFilter;
    tex.generateMipmaps = false;
    const mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(2, 2),
      new THREE.MeshBasicMaterial({
        map: tex,
        transparent: true,
        depthTest: false,
        depthWrite: false
      })
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
      diskGlow.userData.bar.material.opacity = 0.18 + Math.sin(now * 0.00016) * 0.02;
      diskGlow.userData.nucleus.material.opacity = 0.38 + Math.sin(now * 0.0002) * 0.03;
    }
    if (diskGlow.userData.trail) {
      diskGlow.userData.trail.material.opacity = 0.34 + Math.sin(now * 0.00028) * 0.05;
      diskGlow.userData.trail.position.x = 0.12 + Math.sin(now * 0.00012) * 0.1;
    }
    if (diskGlow.userData.nebula) {
      diskGlow.userData.nebula.position.x = Math.sin(now * 0.00008) * 0.18;
    }
    if (diskGlow.userData.nebula2) {
      diskGlow.userData.nebula2.position.x = 1.15 + Math.sin(now * 0.00007) * 0.14;
    }

    for (const n of nodes) poseSystem(n, dt);

    if (blackHole && blackHole.userData.ring) {
      blackHole.userData.ring.material.opacity = 0.14 + Math.sin(now * 0.00014) * 0.025;
      blackHole.rotation.z = Math.sin(now * 0.00005) * 0.04;
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

  let hideTimer = 0;

  function cancelHide() {
    if (hideTimer) {
      clearTimeout(hideTimer);
      hideTimer = 0;
    }
  }

  function showOverlay(items, focus, origin, instant) {
    cancelHide();
    state.overlay = true;
    state.items = items || [];
    state.focus = typeof focus === "number" ? focus : 0;
    state.origin = origin === "bottom" ? "bottom" : "top";
    overlayEl.classList.remove("show", "ready", "motion");
    if (listHeadEl) listHeadEl.textContent = faceInfo(NODE_IDS[state.node]).title || "";
    if (!instant && state.motion) overlayEl.classList.add("motion");
    renderTrack();
    applyOverlayFocus(true);
    overlayEl.classList.add("show");
    syncNodeLabel();
    const arm = () => overlayEl.classList.add("ready");
    if (instant || !state.motion) arm();
    else requestAnimationFrame(() => requestAnimationFrame(arm));
  }

  function hideOverlay() {
    cancelHide();
    const fade = state.motion && overlayEl.classList.contains("ready");
    state.overlay = false;
    state.items = [];
    overlayEl.classList.remove("show");
    const finish = () => {
      hideTimer = 0;
      overlayEl.classList.remove("ready", "motion");
      trackEl.style.removeProperty("--track-y");
      trackEl.style.removeProperty("transition");
      trackEl.innerHTML = "";
      if (listHeadEl) listHeadEl.textContent = "";
    };
    if (fade) {
      overlayEl.classList.add("motion");
      overlayEl.classList.remove("ready");
      hideTimer = setTimeout(finish, 720);
    } else {
      finish();
    }
    const info = faceInfo(NODE_IDS[state.node]);
    syncCaption(info.title, info.hint);
    syncNodeLabel();
  }

  function renderTrack() {
    trackEl.innerHTML = "";
    state.items.forEach((item, i) => {
      const el = document.createElement("button");
      el.type = "button";
      el.className = "row" + (state.motion ? " motion" : "") + (i === state.focus ? " focus" : "");
      el.textContent = item.title || "";
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

  function rowStride() {
    const rows = trackEl.children;
    if (rows.length >= 2) return rows[1].offsetTop - rows[0].offsetTop;
    if (rows[0]) return rows[0].offsetHeight + 2;
    return 56;
  }

  function applyOverlayFocus(instant) {
    const rows = trackEl.children;
    for (let i = 0; i < rows.length; i++) {
      rows[i].classList.toggle("focus", i === state.focus);
    }
    const item = state.items[state.focus];
    if (item) syncCaption(item.title, "Up/Down move · Enter opens · Esc returns");
    const y = (state.items.length * 0.5 - state.focus - 0.5) * rowStride();
    if (instant || !state.motion) trackEl.style.transition = "none";
    else trackEl.style.transition = "";
    trackEl.style.setProperty("--track-y", `${y}px`);
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

  const labelNdc = new THREE.Vector3();

  function syncNodeLabel() {
    if (!nodeLabelEl) return;
    if (state.overlay) {
      nodeLabelEl.hidden = true;
      return;
    }
    const n = nodes[state.node];
    if (!n) {
      nodeLabelEl.hidden = true;
      return;
    }
    const title = faceInfo(NODE_IDS[state.node]).title || NODE_IDS[state.node];
    if (nodeLabelEl.textContent !== title) nodeLabelEl.textContent = title;
    n.group.updateWorldMatrix(true, false);
    n.group.getWorldPosition(labelNdc);
    camera.updateMatrixWorld();
    labelNdc.project(camera);
    if (labelNdc.z < -1 || labelNdc.z > 1) {
      nodeLabelEl.hidden = true;
      return;
    }
    const w = canvas.clientWidth || window.innerWidth;
    const h = canvas.clientHeight || window.innerHeight;
    const x = (labelNdc.x * 0.5 + 0.5) * w;
    const y = (-labelNdc.y * 0.5 + 0.5) * h + Math.max(30, h * 0.04);
    const pad = 36;
    const clampedX = Math.min(Math.max(x, pad), w - pad);
    const clampedY = Math.min(Math.max(y, pad), h - pad - 40);
    nodeLabelEl.hidden = false;
    nodeLabelEl.style.transform = `translate3d(${clampedX}px, ${clampedY}px, 0) translate(-50%, 0)`;
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
      const base = n.corona.userData.base;
      n.sun.material.opacity = (on ? 0.52 : 0.3) * breathe;
      n.corona.material.opacity = (on ? 0.14 : 0.055) * breathe;
      n.corona.scale.set(base.x * (on ? 1.12 : 1), base.y * (on ? 1.08 : 1), 1);
      for (const p of n.planets) {
        p.material.opacity = p.userData.baseOpacity * (on ? 1.15 : 0.85);
      }
      if (!state.motion) poseSystem(n, 0);
    }
    syncNodeLabel();
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

  function spriteTex(size, draw) {
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const ctx = c.getContext("2d");
    draw(ctx, size);
    return finishSoftTexture(c, Math.max(6, size * 0.06));
  }

  function smooth01(t) {
    const x = Math.min(1, Math.max(0, t));
    return x * x * (3 - 2 * x);
  }

  function featherPremul(ctx, w, h, margin) {
    const img = ctx.getImageData(0, 0, w, h);
    const d = img.data;
    const mx = Math.max(8, margin | 0);
    const my = Math.max(8, Math.round(margin * (h / Math.max(1, w))));
    for (let y = 0; y < h; y++) {
      const ey = smooth01(y < my ? y / my : y > h - 1 - my ? (h - 1 - y) / my : 1);
      for (let x = 0; x < w; x++) {
        const i = (y * w + x) * 4;
        const a0 = d[i + 3];
        if (!a0) {
          d[i] = d[i + 1] = d[i + 2] = 0;
          continue;
        }
        const ex = smooth01(x < mx ? x / mx : x > w - 1 - mx ? (w - 1 - x) / mx : 1);
        const a = a0 * ex * ey;
        if (a < 1.4) {
          d[i] = d[i + 1] = d[i + 2] = d[i + 3] = 0;
          continue;
        }
        const pa = a / 255;
        d[i] = Math.round(d[i] * pa);
        d[i + 1] = Math.round(d[i + 1] * pa);
        d[i + 2] = Math.round(d[i + 2] * pa);
        d[i + 3] = Math.round(a);
      }
    }
    ctx.putImageData(img, 0, 0);
  }

  function finishSoftTexture(canvas, margin) {
    const ctx = canvas.getContext("2d", { willReadFrequently: true }) || canvas.getContext("2d");
    featherPremul(ctx, canvas.width, canvas.height, margin);
    const tex = new THREE.CanvasTexture(canvas);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.wrapS = THREE.ClampToEdgeWrapping;
    tex.wrapT = THREE.ClampToEdgeWrapping;
    tex.minFilter = THREE.LinearFilter;
    tex.magFilter = THREE.LinearFilter;
    tex.generateMipmaps = false;
    return tex;
  }

  function softMat(map, extra) {
    return new THREE.MeshBasicMaterial(Object.assign({
      map,
      transparent: true,
      premultipliedAlpha: true,
      depthWrite: false,
      blending: THREE.NormalBlending
    }, extra || {}));
  }

  function softSprite(map, extra) {
    return new THREE.SpriteMaterial(Object.assign({
      map,
      transparent: true,
      premultipliedAlpha: true,
      depthWrite: false,
      blending: THREE.AdditiveBlending
    }, extra || {}));
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
