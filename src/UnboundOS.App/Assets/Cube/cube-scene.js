/* UnboundOS Home — original procedural living edge-on galaxy. */
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
  renderer.toneMappingExposure = 1.1;

  const scene = new THREE.Scene();
  const camera = new THREE.PerspectiveCamera(30, 1, 0.08, 80);
  camera.position.set(0, 0.018, 6.35);
  camera.lookAt(0, 0, 0);

  const rig = new THREE.Group();
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

  const farStars = buildHalo(5200, 38, 1.15, 0x8ea0c4, 0x51f, 0.94, 0.0000075);
  const midStars = buildHalo(2800, 20, 1.7, 0xd4dae8, 0x77a, 0.58, -0.000013);
  const nearStars = buildHalo(900, 10, 2.35, 0xf7f1e4, 0x91c, 0.24, 0.00002);
  const diskRings = buildDiskRings();
  const shear = buildShear(1500);
  const orbiters = buildOrbiters(180);
  const diskGlow = buildDiskGlow();
  const dustSheets = buildDustSheets();
  const nodes = buildNodes();
  scene.add(farStars, midStars, nearStars);
  const vignette = buildVignette();
  scene.add(vignette);

  const clock = new THREE.Clock();
  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const nodeMeshes = nodes.map((n) => n.hit);
  const warm = new THREE.Color(0xffe7b8);
  const cool = new THREE.Color(0x8aa4d4);
  const cream = new THREE.Color(0xfff6e0);

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

    oval(w * 0.49, h * 0.07, [
      [0, "rgba(186, 204, 236, 0.2)"],
      [0.45, "rgba(120, 154, 210, 0.08)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.36, h * 0.046, [
      [0, "rgba(255, 226, 186, 0.42)"],
      [0.4, "rgba(214, 176, 128, 0.2)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.2, h * 0.055, [
      [0, "rgba(255, 246, 220, 0.88)"],
      [0.28, "rgba(255, 214, 150, 0.5)"],
      [0.62, "rgba(232, 176, 110, 0.16)"],
      [1, "rgba(0,0,0,0)"]
    ]);
    oval(w * 0.055, h * 0.042, [
      [0, "rgba(255, 252, 242, 1)"],
      [0.35, "rgba(255, 232, 176, 0.7)"],
      [1, "rgba(0,0,0,0)"]
    ]);

    ctx.save();
    ctx.globalCompositeOperation = "lighter";
    ctx.translate(cx, cy);
    ctx.scale(1, 0.045);
    const spike = ctx.createRadialGradient(0, 0, 0, 0, 0, w * 0.42);
    spike.addColorStop(0, "rgba(255,248,230,0.55)");
    spike.addColorStop(0.18, "rgba(255,220,160,0.16)");
    spike.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = spike;
    ctx.fillRect(-w * 0.42, -w * 0.42, w * 0.84, w * 0.84);
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
      const y = cy + gauss(rng) * h * (0.018 + Math.abs(x - cx) / w * 0.03);
      const t = Math.min(1, Math.abs(x - cx) / (w * 0.42));
      const r = 255 - t * 40;
      const g = 236 - t * 50;
      const b = 200 + t * 40;
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
      new THREE.PlaneGeometry(13.4, 5.5),
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
    bar.scale.set(7.6, 1.15, 1);
    bar.position.z = -2.35;
    group.add(bar);

    const nucleus = new THREE.Sprite(new THREE.SpriteMaterial({
      map: glowMap,
      color: 0xfff6dc,
      transparent: true,
      opacity: 0.7,
      depthWrite: false,
      blending: THREE.AdditiveBlending
    }));
    nucleus.scale.set(1.55, 0.72, 1);
    nucleus.position.z = -2.2;
    group.add(nucleus);

    const wings = new THREE.Sprite(new THREE.SpriteMaterial({
      map: barMap,
      color: 0x9ab4dc,
      transparent: true,
      opacity: 0.18,
      depthWrite: false,
      blending: THREE.AdditiveBlending
    }));
    wings.scale.set(12.2, 0.85, 1);
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

  function buildHalo(count, spread, size, color, seed, flatten, spin) {
    const rng = mulberry(seed);
    const pos = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (rng() - 0.5) * spread;
      pos[i * 3 + 1] = (rng() - 0.5) * spread * flatten;
      pos[i * 3 + 2] = (rng() - 0.5) * spread * 0.52 - 2.8;
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
        const c = cream.clone().lerp(warm, Math.min(1, t * 1.1)).lerp(cool, t);
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
      const c = warm.clone().lerp(cool, t);
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

  function buildNodes() {
    const list = [];
    for (let i = 0; i < NODE_IDS.length; i++) {
      const group = new THREE.Group();
      group.position.set(nodeX(i), 0, 0.05);
      const info = faceInfo(NODE_IDS[i]);
      const coreCol = new THREE.Color(info.core || "#FFE9B0");
      const bloom = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: coreCol,
        transparent: true,
        opacity: i === 0 ? 0.2 : 0.1,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      bloom.scale.set(i === 0 ? 2.15 : 1.2, i === 0 ? 0.78 : 0.48, 1);
      bloom.userData.base = bloom.scale.clone();
      group.add(bloom);
      const core = new THREE.Sprite(new THREE.SpriteMaterial({
        map: starMap,
        color: 0xffffff,
        transparent: true,
        opacity: i === 0 ? 0.5 : 0.32,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      core.scale.set(i === 0 ? 0.2 : 0.13, i === 0 ? 0.2 : 0.13, 1);
      group.add(core);
      const dust = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: 0xe8c898,
        transparent: true,
        opacity: 0.04,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      dust.scale.set(i === 0 ? 1.6 : 0.95, i === 0 ? 0.42 : 0.3, 1);
      group.add(dust);
      const label = makeLabel(info.title);
      label.position.y = -0.86;
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
      list.push({ group, bloom, core, dust, label, hit });
    }
    return list;
  }

  function buildVignette() {
    const tex = spriteTex(256, (ctx, s) => {
      const g = ctx.createRadialGradient(s / 2, s / 2, s * 0.3, s / 2, s / 2, s * 0.64);
      g.addColorStop(0, "rgba(0,0,0,0)");
      g.addColorStop(1, "rgba(0,0,0,0.58)");
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
    const focus = typeof msg.focus === "number" ? focus : (origin === "bottom" ? items.length - 1 : 0);
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
    farStars.position.x = -state.visualX * 0.05;
    midStars.position.x = -state.visualX * 0.1;
    nearStars.position.x = -state.visualX * 0.2;
    camera.position.y = state.motion ? 0.018 + Math.sin(now * 0.00022) * 0.01 : 0.018;
    camera.lookAt(0, 0, 0);
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
      const pillW = Math.min(s * 0.86, total + 72);
      const pillH = 78;
      roundRect(ctx, (s - pillW) / 2, (s - pillH) / 2, pillW, pillH, 20);
      ctx.fillStyle = "rgba(0, 2, 8, 0.52)";
      ctx.fill();
      ctx.strokeStyle = "rgba(0,0,0,0.9)";
      ctx.lineWidth = 5;
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
    sprite.scale.set(1.28, 0.26, 1);
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
