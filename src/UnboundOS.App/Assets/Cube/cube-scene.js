/* UnboundOS Home cube — packaged Three.js scene. C# owns pose and navigation. */
(() => {
  "use strict";

  const PX_PER_TURN = 96;
  const SPRING_K = 86;
  const SPRING_D = 15;
  const DEFAULT_FACES = [
    { id: "Session", title: "Session", kicker: "SYS", monogram: "S", meta: "ENTER", hint: "Enter or exit the gaming posture.", accent: "#00F0FF", seam: "#00D0E8", core: "#1E40AF", plate: "#0B121D" },
    { id: "Tools", title: "Tools", kicker: "KIT", monogram: "T", meta: "OPEN", hint: "OBS, Vortex, Discord, Playnite, utilities.", accent: "#00D4F0", seam: "#0098C8", core: "#1E3A8A", plate: "#0A1018" },
    { id: "Hardware", title: "Hardware", kicker: "HW", monogram: "H", meta: "READ", hint: "Inventory from this PC. Optional official HWiNFO.", accent: "#60A5FA", seam: "#1D4ED8", core: "#00B4D8", plate: "#10151E" },
    { id: "Network", title: "Network", kicker: "LINK", monogram: "N", meta: "SPLIT", hint: "Prefer a game NIC. Park bulk traffic.", accent: "#4F7CFF", seam: "#1E40AF", core: "#0EA5E9", plate: "#0B121D" },
    { id: "Mods", title: "Mods", kicker: "MOD", monogram: "M", meta: "OPEN", hint: "Workshop and Vortex discovery. Vortex stays in charge.", accent: "#2EE9D0", seam: "#0891B2", core: "#155E75", plate: "#0A1214" },
    { id: "Files", title: "Files", kicker: "FS", monogram: "F", meta: "BROWSE", hint: "Daily folders. Explorer stays for anticheat.", accent: "#7DD3FC", seam: "#0369A1", core: "#1E40AF", plate: "#0B121D" }
  ];

  const FACE_LAYOUT = {
    Session: { dir: [0, 0, 1], up: [0, 1, 0] },
    Tools: { dir: [1, 0, 0], up: [0, 1, 0] },
    Hardware: { dir: [0, 0, -1], up: [0, 1, 0] },
    Network: { dir: [-1, 0, 0], up: [0, 1, 0] },
    Mods: { dir: [0, 1, 0], up: [0, 0, -1] },
    Files: { dir: [0, -1, 0], up: [0, 0, 1] }
  };

  const MAGENTA = "#C45A8A";
  const AMBER = "#E4B53C";

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
    accent: new THREE.Color("#00F0FF"),
    targetAccent: new THREE.Color("#00F0FF"),
    seam: new THREE.Color("#00D0E8"),
    targetSeam: new THREE.Color("#00D0E8"),
    core: new THREE.Color("#1E40AF"),
    targetCore: new THREE.Color("#1E40AF"),
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
    camZ: 4.55,
    camY: 1.62
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
  renderer.setClearColor(0x030508, 1);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.75));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.05;
  renderer.shadowMap.enabled = false;

  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x030508, 0.078);
  scene.environment = makeEnvMap();

  const camera = new THREE.PerspectiveCamera(28, 1, 0.08, 48);
  camera.position.set(0, 1.62, 4.55);
  camera.lookAt(0, -0.28, 0);

  const restRig = new THREE.Group();
  const spinRig = new THREE.Group();
  scene.add(restRig);
  restRig.add(spinRig);

  const artifact = new THREE.Group();
  spinRig.add(artifact);

  const plates = [];
  const glows = [];
  const arcs = [];
  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const clock = new THREE.Clock();

  const coreLight = new THREE.PointLight(0x00f0ff, 1.8, 8.5, 1.4);
  coreLight.position.set(0, 0.05, 0.2);
  artifact.add(coreLight);

  const bounce = new THREE.PointLight(0x00d0e8, 1.15, 6.5, 1.8);
  bounce.position.set(0, -0.7, 0.4);
  artifact.add(bounce);

  const magentaKick = new THREE.PointLight(0xc45a8a, 0.35, 5.5, 2);
  magentaKick.position.set(0.55, -0.2, 0.7);
  artifact.add(magentaKick);

  const key = new THREE.DirectionalLight(0x9aa8b8, 0.55);
  key.position.set(-2.6, 4.2, 3.4);
  scene.add(key);

  const fill = new THREE.DirectionalLight(0x243044, 0.22);
  fill.position.set(2.2, 0.4, 4.4);
  scene.add(fill);

  const rim = new THREE.DirectionalLight(0x1e40af, 0.7);
  rim.position.set(3.6, 1.1, -2.8);
  scene.add(rim);

  scene.add(new THREE.HemisphereLight(0x1a2436, 0x030508, 0.32));
  scene.add(new THREE.AmbientLight(0x0a1018, 0.18));

  const openSpot = new THREE.SpotLight(0x00f0ff, 0, 16, 0.46, 0.62, 1.3);
  openSpot.position.set(-1.6, 4.6, 3.2);
  openSpot.target.position.set(0, 0, 0);
  scene.add(openSpot);
  scene.add(openSpot.target);

  const creviceCyan = new THREE.PointLight(0x2ee9d0, 0, 3.4, 1.6);
  const creviceMagenta = new THREE.PointLight(0xc45a8a, 0, 3.2, 1.7);
  const creviceAmber = new THREE.PointLight(0xe4b53c, 0, 2.8, 1.8);
  creviceCyan.position.set(0.35, 0.2, 0.4);
  creviceMagenta.position.set(-0.4, -0.15, 0.25);
  creviceAmber.position.set(0.1, 0.45, -0.3);
  artifact.add(creviceCyan, creviceMagenta, creviceAmber);

  const volcanicMap = paintVolcanic();
  const chassis = new THREE.Mesh(
    new THREE.BoxGeometry(1.74, 1.74, 1.74),
    new THREE.MeshBasicMaterial({ map: volcanicMap, color: 0xffffff })
  );
  artifact.add(chassis);

  const hullGroup = new THREE.Group();
  artifact.add(hullGroup);
  hullGroup.add(chassis);

  buildPlates();
  const floor = buildFloor();
  const haze = buildHaze();
  const motes = buildMotes();
  const bricks = buildGreeble();
  const shafts = buildShafts();

  function faceInfo(id) {
    return state.faces.find((f) => f.id === id) || DEFAULT_FACES[0];
  }

  function paintVolcanic() {
    const size = 512;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const ctx = c.getContext("2d");
    const rng = mulberry(0x5e1f0c);
    ctx.fillStyle = "#0C1014";
    ctx.fillRect(0, 0, size, size);
    grit(ctx, size, rng, 5200);
    pits(ctx, size, rng, 40);
    scratches(ctx, size, rng, 80);
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
    tex.anisotropy = 8;
    return tex;
  }

  function paintMetal(info) {
    const size = 1024;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const ctx = c.getContext("2d");
    const rng = mulberry(hash(info.id + "-metal"));

    ctx.fillStyle = "#0A0D11";
    ctx.fillRect(0, 0, size, size);

    const vg = ctx.createRadialGradient(size * 0.42, size * 0.3, 8, size * 0.5, size * 0.55, size * 0.9);
    vg.addColorStop(0, "rgba(28, 34, 40, 0.55)");
    vg.addColorStop(0.45, "rgba(10, 14, 18, 0.2)");
    vg.addColorStop(1, "rgba(4, 5, 7, 0.95)");
    ctx.fillStyle = vg;
    ctx.fillRect(0, 0, size, size);

    grit(ctx, size, rng, 7800);
    pits(ctx, size, rng, 56);
    scratches(ctx, size, rng, 110);

    ctx.save();
    ctx.strokeStyle = "rgba(0, 0, 0, 0.55)";
    ctx.lineWidth = 5;
    for (let g = 0; g < 7; g++) {
      const x = 80 + ((hash(info.id + g) >> 3) % 820);
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x + ((g % 2) ? 18 : -10), size);
      ctx.stroke();
    }
    ctx.restore();

    drawTrenches(ctx, size, mulberry(hash(info.id + "-circuit")), info);

    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = 8;
    return tex;
  }

  function paintGlow(info, isFront) {
    const size = 1024;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const ctx = c.getContext("2d");
    const rng = mulberry(hash(info.id + "-circuit"));
    ctx.clearRect(0, 0, size, size);
    drawTraces(ctx, size, rng, info, isFront);
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = 8;
    return tex;
  }

  function drawTrenches(ctx, size, rng, info) {
    const paths = circuitPaths(info.id, rng);
    ctx.save();
    ctx.lineCap = "square";
    ctx.lineJoin = "miter";
    ctx.strokeStyle = "rgba(2, 3, 4, 0.92)";
    ctx.lineWidth = 11;
    strokePaths(ctx, paths);
    ctx.strokeStyle = "rgba(0, 0, 0, 0.7)";
    ctx.lineWidth = 7;
    strokePaths(ctx, paths);
    ctx.restore();

    ctx.save();
    ctx.fillStyle = "rgba(2, 3, 4, 0.88)";
    for (const pad of circuitPads(info.id, rng)) {
      ctx.fillRect(pad.x - 2, pad.y - 2, pad.w + 4, pad.h + 4);
    }
    ctx.restore();
  }

  function drawTraces(ctx, size, rng, info, isFront) {
    const accent = info.accent || "#00F0FF";
    const cobalt = info.core || "#1E40AF";
    const paths = circuitPaths(info.id, rng);
    const pads = circuitPads(info.id, rng);
    const boost = isFront ? 1 : 0.78;

    ctx.save();
    ctx.lineCap = "square";
    ctx.lineJoin = "miter";
    ctx.shadowBlur = isFront ? 18 : 12;

    paths.forEach((path, i) => {
      const tone = traceTone(i, accent, cobalt);
      ctx.shadowColor = hexAlpha(tone, 0.85);
      ctx.strokeStyle = hexAlpha(tone, 0.22 * boost);
      ctx.lineWidth = 9;
      strokeOne(ctx, path);
      ctx.strokeStyle = hexAlpha(tone, 0.55 * boost);
      ctx.lineWidth = 4.5;
      strokeOne(ctx, path);
      ctx.strokeStyle = hexAlpha(tone, 0.95 * boost);
      ctx.lineWidth = 2;
      strokeOne(ctx, path);
      ctx.strokeStyle = "rgba(220, 245, 255, 0.85)";
      ctx.lineWidth = 0.8;
      strokeOne(ctx, path);
    });

    pads.forEach((pad, i) => {
      const tone = traceTone(i + 3, accent, cobalt);
      ctx.shadowColor = hexAlpha(tone, 0.7);
      ctx.strokeStyle = hexAlpha(tone, 0.8 * boost);
      ctx.lineWidth = 3.2;
      ctx.strokeRect(pad.x, pad.y, pad.w, pad.h);
      if (pad.inner) {
        ctx.strokeRect(pad.x + 16, pad.y + 16, pad.w - 32, pad.h - 32);
      }
      ctx.fillStyle = hexAlpha(tone, 0.12 * boost);
      ctx.fillRect(pad.x, pad.y, pad.w, pad.h);
    });
    ctx.restore();
  }

  function traceTone(i, accent, cobalt) {
    const m = i % 9;
    if (m === 0) return MAGENTA;
    if (m === 4) return AMBER;
    if (m === 2 || m === 6) return cobalt;
    return accent;
  }

  function circuitPaths(id, rng) {
    const seed = hash(id);
    const paths = [];
    const n = 20;
    for (let i = 0; i < n; i++) {
      let x = 70 + Math.floor(rng() * 14) * 64;
      let y = 70 + Math.floor(rng() * 14) * 64;
      const pts = [[x, y]];
      const segs = 3 + ((seed + i) % 6);
      for (let s = 0; s < segs; s++) {
        const step = 48 + ((seed >> (s + i)) & 3) * 32;
        if (((seed + i + s) & 1) === 0) x += rng() > 0.45 ? step : -step;
        else y += rng() > 0.5 ? step : -step;
        x = clamp(x, 48, 976);
        y = clamp(y, 48, 976);
        pts.push([x, y]);
      }
      paths.push(pts);
    }
    return paths;
  }

  function circuitPads(id, rng) {
    const pads = [];
    const count = 5 + (hash(id) % 4);
    for (let i = 0; i < count; i++) {
      const w = 70 + Math.floor(rng() * 5) * 28;
      const h = 54 + Math.floor(rng() * 5) * 24;
      pads.push({
        x: 80 + Math.floor(rng() * 12) * 64,
        y: 80 + Math.floor(rng() * 12) * 64,
        w,
        h,
        inner: i % 2 === 0
      });
    }
    return pads;
  }

  function strokePaths(ctx, paths) {
    for (const path of paths) strokeOne(ctx, path);
  }

  function strokeOne(ctx, path) {
    if (!path.length) return;
    ctx.beginPath();
    ctx.moveTo(path[0][0], path[0][1]);
    for (let i = 1; i < path.length; i++) ctx.lineTo(path[i][0], path[i][1]);
    ctx.stroke();
  }

  function grit(ctx, size, rng, count) {
    ctx.save();
    for (let i = 0; i < count; i++) {
      const n = 5 + rng() * 26;
      ctx.globalAlpha = 0.07 + rng() * 0.16;
      ctx.fillStyle = `rgb(${n},${n + 2},${n + 6})`;
      ctx.fillRect(rng() * size, rng() * size, 1 + rng() * 3, 1);
    }
    ctx.restore();
  }

  function pits(ctx, size, rng, count) {
    ctx.save();
    ctx.globalAlpha = 0.22;
    for (let i = 0; i < count; i++) {
      ctx.fillStyle = i % 3 === 0 ? "rgba(16, 22, 18, 1)" : "rgba(4, 5, 6, 1)";
      ctx.beginPath();
      ctx.ellipse(rng() * size, rng() * size, 18 + rng() * 70, 8 + rng() * 28, rng() * Math.PI, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();
  }

  function scratches(ctx, size, rng, count) {
    ctx.save();
    ctx.strokeStyle = "rgba(170, 186, 198, 0.1)";
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

  function buildPlates() {
    const metalGeom = new THREE.BoxGeometry(1.742, 1.742, 0.034);
    const glowGeom = new THREE.PlaneGeometry(1.73, 1.73);
    for (const info of state.faces) {
      const layout = FACE_LAYOUT[info.id];
      if (!layout) continue;
      const dir = new THREE.Vector3(...layout.dir);
      const up = new THREE.Vector3(...layout.up);
      const metal = new THREE.Mesh(
        metalGeom,
        new THREE.MeshBasicMaterial({ map: paintMetal(info), color: 0xffffff })
      );
      metal.position.copy(dir).multiplyScalar(0.888);
      orientPlate(metal, dir, up);
      metal.userData.face = info.id;
      hullGroup.add(metal);
      plates.push(metal);

      const glow = new THREE.Mesh(
        glowGeom,
        new THREE.MeshBasicMaterial({
          map: paintGlow(info, info.id === state.front),
          transparent: true,
          opacity: 0.92,
          blending: THREE.AdditiveBlending,
          depthWrite: false
        })
      );
      glow.position.copy(dir).multiplyScalar(0.91);
      orientPlate(glow, dir, up);
      glow.userData.face = info.id;
      hullGroup.add(glow);
      glows.push(glow);
    }
  }

  function rebuildPlateTextures() {
    for (const mesh of plates) {
      const info = faceInfo(mesh.userData.face);
      mesh.material.map?.dispose();
      mesh.material.map = paintMetal(info);
      mesh.material.needsUpdate = true;
    }
    for (const mesh of glows) {
      const info = faceInfo(mesh.userData.face);
      mesh.material.map?.dispose();
      mesh.material.map = paintGlow(info, info.id === state.front);
      mesh.material.needsUpdate = true;
    }
  }

  function buildFloor() {
    const size = 1024;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const ctx = c.getContext("2d");
    ctx.fillStyle = "#06080C";
    ctx.fillRect(0, 0, size, size);
    const tiles = 8;
    const cell = size / tiles;
    for (let y = 0; y < tiles; y++) {
      for (let x = 0; x < tiles; x++) {
        const shade = 8 + ((x * 3 + y * 7) % 10);
        ctx.fillStyle = `rgb(${shade},${shade + 1},${shade + 3})`;
        ctx.fillRect(x * cell + 2, y * cell + 2, cell - 4, cell - 4);
      }
    }
    ctx.strokeStyle = "rgba(0,0,0,0.7)";
    ctx.lineWidth = 6;
    for (let i = 0; i <= tiles; i++) {
      ctx.beginPath();
      ctx.moveTo(i * cell, 0);
      ctx.lineTo(i * cell, size);
      ctx.stroke();
      ctx.beginPath();
      ctx.moveTo(0, i * cell);
      ctx.lineTo(size, i * cell);
      ctx.stroke();
    }
    ctx.save();
    ctx.globalAlpha = 0.18;
    ctx.strokeStyle = "rgba(180,200,220,0.35)";
    ctx.lineWidth = 1;
    for (let i = 0; i < 40; i++) {
      ctx.beginPath();
      ctx.moveTo(Math.random() * size, Math.random() * size);
      ctx.lineTo(Math.random() * size, Math.random() * size);
      ctx.stroke();
    }
    ctx.restore();

    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.wrapS = tex.wrapT = THREE.RepeatWrapping;
    tex.repeat.set(2, 2);

    const mesh = new THREE.Mesh(
      new THREE.PlaneGeometry(14, 14),
      new THREE.MeshBasicMaterial({
        map: tex,
        color: 0x8a94a0,
        transparent: true,
        opacity: 1
      })
    );
    mesh.rotation.x = -Math.PI / 2;
    mesh.position.y = -0.88;
    scene.add(mesh);

    const glowTex = (() => {
      const g = document.createElement("canvas");
      g.width = g.height = 256;
      const gx = g.getContext("2d");
      const rad = gx.createRadialGradient(128, 128, 8, 128, 128, 124);
      rad.addColorStop(0, "rgba(0,240,255,0.55)");
      rad.addColorStop(0.35, "rgba(30,64,175,0.28)");
      rad.addColorStop(0.7, "rgba(196,90,138,0.1)");
      rad.addColorStop(1, "rgba(0,0,0,0)");
      gx.fillStyle = rad;
      gx.fillRect(0, 0, 256, 256);
      const t = new THREE.CanvasTexture(g);
      t.colorSpace = THREE.SRGBColorSpace;
      return t;
    })();

    const pool = new THREE.Mesh(
      new THREE.PlaneGeometry(4.6, 4.6),
      new THREE.MeshBasicMaterial({
        map: glowTex,
        transparent: true,
        opacity: 0.72,
        blending: THREE.AdditiveBlending,
        depthWrite: false
      })
    );
    pool.rotation.x = -Math.PI / 2;
    pool.position.y = -0.872;
    scene.add(pool);

    const wet = new THREE.Mesh(
      new THREE.PlaneGeometry(14, 14),
      new THREE.MeshBasicMaterial({
        color: 0x101820,
        transparent: true,
        opacity: 0.28,
        blending: THREE.AdditiveBlending,
        depthWrite: false
      })
    );
    wet.rotation.x = -Math.PI / 2;
    wet.position.y = -0.868;
    scene.add(wet);

    const contact = new THREE.Mesh(
      new THREE.CircleGeometry(1.05, 40),
      new THREE.MeshBasicMaterial({
        color: 0x000000,
        transparent: true,
        opacity: 0.72,
        depthWrite: false
      })
    );
    contact.rotation.x = -Math.PI / 2;
    contact.position.y = -0.869;
    scene.add(contact);

    return { mesh, pool, wet, contact };
  }

  function buildHaze() {
    const tex = (() => {
      const c = document.createElement("canvas");
      c.width = 256;
      c.height = 256;
      const ctx = c.getContext("2d");
      const rng = mulberry(0x0a2e);
      ctx.clearRect(0, 0, 256, 256);
      for (let i = 0; i < 70; i++) {
        const g = ctx.createRadialGradient(rng() * 256, rng() * 256, 4, rng() * 256, rng() * 256, 20 + rng() * 50);
        g.addColorStop(0, `rgba(180,200,220,${0.08 + rng() * 0.12})`);
        g.addColorStop(1, "rgba(0,0,0,0)");
        ctx.fillStyle = g;
        ctx.fillRect(0, 0, 256, 256);
      }
      const t = new THREE.CanvasTexture(c);
      t.colorSpace = THREE.SRGBColorSpace;
      return t;
    })();
    const group = [];
    for (let i = 0; i < 4; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(5.5, 3.4),
        new THREE.MeshBasicMaterial({
          map: tex,
          transparent: true,
          opacity: 0.16,
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
    const count = 140;
    const geo = new THREE.BufferGeometry();
    const pos = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (Math.random() - 0.5) * 7;
      pos[i * 3 + 1] = Math.random() * 3.6 - 0.8;
      pos[i * 3 + 2] = (Math.random() - 0.5) * 7;
    }
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const mat = new THREE.PointsMaterial({
      color: 0x7a8ea4,
      size: 0.014,
      transparent: true,
      opacity: 0.12,
      blending: THREE.AdditiveBlending,
      depthWrite: false
    });
    const pts = new THREE.Points(geo, mat);
    scene.add(pts);
    return pts;
  }

  function gunmetal(hex, roughness) {
    return new THREE.MeshStandardMaterial({
      color: hex,
      metalness: 0.88,
      roughness: roughness,
      envMapIntensity: 1.15
    });
  }

  function makeSteppedBlock(size, shade, rng) {
    const group = new THREE.Group();
    const outer = new THREE.Mesh(
      new THREE.BoxGeometry(size, size, size),
      gunmetal(shade, 0.22 + rng() * 0.12)
    );
    group.add(outer);
    if (size > 0.22) {
      const inset = size * 0.08;
      const inner = size - inset * 2.4;
      const depth = size * 0.08;
      const faces = [
        [0, 0, size / 2 - depth * 0.35],
        [0, 0, -size / 2 + depth * 0.35],
        [size / 2 - depth * 0.35, 0, 0],
        [-size / 2 + depth * 0.35, 0, 0],
        [0, size / 2 - depth * 0.35, 0],
        [0, -size / 2 + depth * 0.35, 0]
      ];
      const innerMat = gunmetal(shade - 0x101010 > 0 ? shade - 0x0a0c10 : 0x2a3038, 0.34);
      for (let i = 0; i < faces.length; i++) {
        const [x, y, z] = faces[i];
        const geo = Math.abs(z) > Math.abs(x) && Math.abs(z) > Math.abs(y)
          ? new THREE.BoxGeometry(inner, inner, depth)
          : Math.abs(x) > Math.abs(y)
            ? new THREE.BoxGeometry(depth, inner, inner)
            : new THREE.BoxGeometry(inner, depth, inner);
        const mesh = new THREE.Mesh(geo, innerMat);
        mesh.position.set(x, y, z);
        group.add(mesh);
      }
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
      const shadePick = rng();
      const shade = shadePick > 0.7 ? 0x8a96a4 : shadePick > 0.4 ? 0x6e7884 : 0x5a646e;
      const group = makeSteppedBlock(size, shade, rng);
      const rest = new THREE.Vector3(x, y, z);
      const out = rest.clone();
      if (out.lengthSq() < 0.01) out.set(0.15, 0.35, 0.4);
      else out.normalize();
      const target = rest.clone().addScaledVector(out, 0.16 + extra + size * 0.12);
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
      artifact.add(group);
      list.push(group);

      if (rng() > 0.55) {
        const glowCol = [0x00f0ff, 0x2ee9d0, 0xc45a8a, 0xe4b53c, 0x4f7cff][(rng() * 5) | 0];
        const slit = new THREE.Mesh(
          new THREE.BoxGeometry(size * 0.08, size * 0.08, size * 1.08),
          new THREE.MeshBasicMaterial({
            color: glowCol,
            transparent: true,
            opacity: 0,
            blending: THREE.AdditiveBlending,
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
          add(cx + (rng() - 0.5) * 0.08, cy + (rng() - 0.5) * 0.08, cz + (rng() - 0.5) * 0.08, size, edge ? 0.08 : 0);
          if (edge && rng() > 0.45) {
            const faceOut = new THREE.Vector3(
              x === 0 ? -1 : x === n - 1 ? 1 : 0,
              y === 0 ? -1 : y === n - 1 ? 1 : 0,
              z === 0 ? -1 : z === n - 1 ? 1 : 0
            );
            add(
              cx + faceOut.x * 0.28,
              cy + faceOut.y * 0.28,
              cz + faceOut.z * 0.28,
              0.16 + rng() * 0.16,
              0.14
            );
          }
        }
      }
    }

    const swirl = new THREE.Mesh(
      new THREE.IcosahedronGeometry(0.34, 2),
      new THREE.MeshBasicMaterial({
        color: 0x1a3040,
        transparent: true,
        opacity: 0,
        blending: THREE.AdditiveBlending,
        depthWrite: false
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
      g.addColorStop(0, "rgba(0,240,255,0)");
      g.addColorStop(0.4, "rgba(0,200,255,0.2)");
      g.addColorStop(1, "rgba(196,90,138,0)");
      ctx.fillStyle = g;
      ctx.fillRect(22, 0, 20, 256);
      const t = new THREE.CanvasTexture(c);
      t.colorSpace = THREE.SRGBColorSpace;
      return t;
    })();
    const group = [];
    for (let i = 0; i < 3; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(0.5, 5.8),
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

  function setHullVisible(on) {
    hullGroup.visible = on;
  }

  function setFloorVisible(opacity) {
    const on = opacity > 0.04;
    floor.mesh.material.opacity = opacity;
    floor.mesh.visible = on;
    floor.pool.material.opacity = 0.72 * opacity;
    floor.wet.material.opacity = 0.28 * opacity;
    floor.contact.material.opacity = 0.72 * opacity;
    floor.pool.visible = on;
    floor.wet.visible = on;
    floor.contact.visible = on;
    for (const h of haze) {
      h.visible = opacity > 0.08;
      h.material.opacity = 0.16 * opacity;
    }
  }

  function startOpen(msg) {
    if (msg && msg.front) state.front = msg.front;
    if (msg && msg.motion === false) state.motion = false;
    if (!state.motion) {
      send({ v: 1, type: "opened", face: state.front });
      return;
    }
    if (state.opening) return;
    state.opening = true;
    state.openT = 0;
    state.dragging = false;
    spawnArcs();
    setHullVisible(false);
    for (const b of bricks) {
      b.visible = true;
      b.position.copy(b.userData.rest);
      b.rotation.set(0, 0, 0);
      b.scale.setScalar(b.userData.restScale || 1);
      if (b.userData.glow && b.material) b.material.opacity = 0;
    }
    for (const s of shafts) s.visible = true;
    setLoop(true);
  }

  function resetAssembly() {
    state.opening = false;
    state.openT = 0;
    renderer.toneMappingExposure = 1.05;
    scene.fog.density = 0.078;
    openSpot.intensity = 0;
    creviceCyan.intensity = 0;
    creviceMagenta.intensity = 0;
    creviceAmber.intensity = 0;
    key.intensity = 0.55;
    fill.intensity = 0.22;
    rim.intensity = 0.7;
    camera.position.set(0, state.camY, state.camZ);
    camera.lookAt(0, -0.28, 0);
    setHullVisible(true);
    setFloorVisible(1);
    for (const b of bricks) {
      b.visible = false;
      b.position.copy(b.userData.rest);
      b.rotation.set(0, 0, 0);
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
      }
    }
    camera.position.z = state.camZ - e * 0.7;
    camera.position.y = state.camY - e * 0.35;
    camera.lookAt(0, -0.05 + e * 0.08, 0);
    renderer.toneMappingExposure = 1.05 + e * 0.55;
    scene.fog.density = 0.078 - e * 0.04;
    setFloorVisible(1 - e);
    key.intensity = 0.55 + e * 1.6;
    fill.intensity = 0.22 + e * 0.7;
    rim.intensity = 0.7 + e * 1.1;
    openSpot.color.copy(state.accent);
    openSpot.intensity = 5.2 * (0.25 + 0.75 * Math.sin(e * Math.PI));
    coreLight.intensity = 2.2 + e * 3.4;
    creviceCyan.intensity = 2.8 * e;
    creviceMagenta.intensity = 1.8 * e;
    creviceAmber.intensity = 1.4 * e;
    for (const s of shafts) {
      s.material.opacity = 0.42 * Math.sin(e * Math.PI);
      s.material.color.copy(state.accent);
      s.rotation.y += dt * 0.12;
    }
    if (t >= 1) {
      send({ v: 1, type: "opened", face: state.front });
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
    const n = 8;
    for (let i = 0; i < n; i++) {
      const a = corners[(Math.random() * corners.length) | 0];
      let b = corners[(Math.random() * corners.length) | 0];
      if (b === a) {
        b = corners[(i + 3) % corners.length];
      }
      const pts = jagged(a, b, 16, 0.2 + Math.random() * 0.12);
      const curve = new THREE.CatmullRomCurve3(pts);
      const tube = new THREE.TubeGeometry(curve, 40, i < 3 ? 0.016 : 0.007, 6, false);
      const col = i % 3 === 0 ? state.accent.clone() : i % 3 === 1 ? state.core.clone() : new THREE.Color(AMBER);
      const mat = new THREE.MeshBasicMaterial({
        color: col,
        transparent: true,
        opacity: 0.92,
        blending: THREE.AdditiveBlending,
        depthWrite: false
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
    } else if (state.motion && !state.dragging) {
      const y = spring(state.visualYaw, state.yaw, state.yawVel, dt);
      const p = spring(state.visualPitch, state.pitch, state.pitchVel, dt);
      state.visualYaw = y.value;
      state.yawVel = y.vel;
      state.visualPitch = p.value;
      state.pitchVel = p.vel;
      state.idle = 2.2 * Math.sin(now * 0.00055);
    }
    state.accent.lerp(state.targetAccent, state.motion ? 0.08 : 1);
    state.seam.lerp(state.targetSeam, state.motion ? 0.1 : 1);
    state.core.lerp(state.targetCore, state.motion ? 0.1 : 1);

    const burst = Math.max(0, (state.burstUntil - now) / 520);
    const pulse = state.motion ? 0.82 + 0.18 * Math.sin(now * 0.0024) : 0.78;
    coreLight.color.copy(state.accent);
    coreLight.intensity = state.opening ? coreLight.intensity : (state.motion ? 1.55 + burst * 2.8 : 0.7);
    bounce.color.copy(state.accent);
    bounce.intensity = state.motion ? 0.9 + pulse * 0.4 : 0.4;
    magentaKick.intensity = state.motion ? 0.28 + pulse * 0.12 : 0.12;

    for (const glow of glows) {
      glow.material.opacity = (glow.userData.face === state.front ? 0.98 : 0.78) * pulse + burst * 0.12;
    }

    floor.pool.material.color.copy(state.accent);

    motes.visible = state.motion && !state.opening;
    if (state.motion) {
      motes.rotation.y += dt * 0.02;
      for (const h of haze) {
        h.rotation.y += dt * 0.015;
        h.position.y += Math.sin(now * 0.0004 + h.position.x) * 0.00015;
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

  function pick(x, y) {
    if (state.opening) return;
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
        startOpen({ front: face, motion: state.motion });
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
      else startOpen({ front: state.front, motion: state.motion });
      return;
    }
    const turn = Math.abs(nx) >= Math.abs(ny) ? (nx < 0 ? "Left" : "Right") : (ny < 0 ? "Up" : "Down");
    if (hosted) {
      send({ v: 1, type: "turn", turn });
    } else {
      localTurn(turn);
    }
  }

  function onKey(ev) {
    if (state.opening) {
      ev.preventDefault();
      return;
    }
    const map = { ArrowLeft: "Left", ArrowRight: "Right", ArrowUp: "Up", ArrowDown: "Down" };
    if (ev.key === "Enter" || ev.key === " ") {
      ev.preventDefault();
      if (hosted) send({ v: 1, type: "activate" });
      else startOpen({ front: state.front, motion: state.motion });
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
    const colors = ["#243044", "#05070a", "#1a3048", "#080c14", "#182844", "#0c1016"];
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
