/* UnboundOS Home — original linear galaxy. Procedural WebGL only. */
(() => {
  "use strict";

  const NODE_IDS = ["Session", "Tools", "Mods", "Network", "Files", "Hardware"];
  const SPACING = 2.2;
  const DEFAULT_FACES = [
    { id: "Session", title: "Games", kicker: "PLAY", monogram: "G", meta: "PLAY", hint: "Up opens the library over the galaxy.", accent: "#C9D4E8", seam: "#8AA0C8", core: "#E8D7A8", plate: "#07080C", glyph: "games" },
    { id: "Tools", title: "Tools", kicker: "KIT", monogram: "T", meta: "OPEN", hint: "Enter opens Tools.", accent: "#B7C4A8", seam: "#8A9A78", core: "#E2D2B0", plate: "#07080C", glyph: "tools" },
    { id: "Hardware", title: "Hardware", kicker: "HW", monogram: "H", meta: "READ", hint: "Enter opens Hardware.", accent: "#D0B4A8", seam: "#A88880", core: "#E8C8B0", plate: "#07080C", glyph: "hardware" },
    { id: "Network", title: "Network", kicker: "LINK", monogram: "N", meta: "SPLIT", hint: "Enter opens Network.", accent: "#A8B4D8", seam: "#7888B0", core: "#C8D0E8", plate: "#07080C", glyph: "network" },
    { id: "Mods", title: "Mods", kicker: "MOD", monogram: "M", meta: "OPEN", hint: "Enter opens Mods.", accent: "#D8C898", seam: "#A89870", core: "#F0E0B8", plate: "#07080C", glyph: "mods" },
    { id: "Files", title: "Files", kicker: "FS", monogram: "F", meta: "BROWSE", hint: "Enter opens Files.", accent: "#C8C0B0", seam: "#989078", core: "#E8E0D0", plate: "#07080C", glyph: "files" }
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
    dragging: false,
    moved: false,
    pressX: 0,
    pressY: 0,
    lastX: 0,
    lastY: 0,
    lastT: 0,
    vx: 0,
    vy: 0,
    idle: 0
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
  renderer.toneMappingExposure = 1.18;

  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x000000, 0.007);

  const camera = new THREE.PerspectiveCamera(28, 1, 0.08, 140);
  camera.position.set(0, 0.04, 7.35);
  camera.lookAt(0, 0, 0);

  const rig = new THREE.Group();
  scene.add(rig);

  scene.add(new THREE.AmbientLight(0x101018, 0.4));
  const key = new THREE.PointLight(0xf2e6c8, 2.4, 22, 1.4);
  key.position.set(0, 0.05, 1.6);
  rig.add(key);
  const fill = new THREE.PointLight(0x6a7cb0, 0.7, 28, 1.6);
  fill.position.set(-2.5, -0.2, 2.4);
  scene.add(fill);

  const starMap = spriteTex(64, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    g.addColorStop(0, "rgba(255,255,255,1)");
    g.addColorStop(0.12, "rgba(255,248,230,0.95)");
    g.addColorStop(0.32, "rgba(186,204,255,0.28)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
    ctx.globalCompositeOperation = "lighter";
    ctx.strokeStyle = "rgba(255,255,255,0.18)";
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(s / 2, 6);
    ctx.lineTo(s / 2, s - 6);
    ctx.moveTo(6, s / 2);
    ctx.lineTo(s - 6, s / 2);
    ctx.stroke();
  });

  const dustMap = spriteTex(256, (ctx, s) => {
    const rng = mulberry(0x51a7);
    for (let i = 0; i < 70; i++) {
      const x = rng() * s;
      const y = s * 0.5 + (rng() - 0.5) * s * 0.28;
      const r = 8 + rng() * 48;
      const g = ctx.createRadialGradient(x, y, 0, x, y, r);
      g.addColorStop(0, `rgba(${180 + rng() * 60 | 0},${170 + rng() * 50 | 0},${210 + rng() * 40 | 0},${0.08 + rng() * 0.12})`);
      g.addColorStop(1, "rgba(0,0,0,0)");
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, s, s);
    }
  });

  const glowMap = spriteTex(128, (ctx, s) => {
    const g = ctx.createRadialGradient(s / 2, s / 2, 0, s / 2, s / 2, s / 2);
    g.addColorStop(0, "rgba(255,252,240,0.95)");
    g.addColorStop(0.18, "rgba(255,220,160,0.45)");
    g.addColorStop(0.45, "rgba(120,150,220,0.12)");
    g.addColorStop(1, "rgba(0,0,0,0)");
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, s, s);
  });

  const farStars = buildStarField(6000, 48, 1.5, 0x8ea0c4, 0x51f);
  const midStars = buildStarField(3200, 24, 2.2, 0xd8dce8, 0x77a);
  const nearStars = buildStarField(1100, 12, 3.0, 0xf6f0e2, 0x91c);
  const ribbon = buildRibbon(9000);
  const spine = buildSpine(3600);
  const live = buildLive(1800);
  const orbiters = buildOrbiters(360);
  const nodes = buildNodes();
  const shafts = buildShafts();
  const veils = buildVeils();
  const vignette = buildVignette();
  scene.add(vignette);

  const clock = new THREE.Clock();
  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const nodeMeshes = nodes.map((n) => n.hit);

  function nodeX(i) {
    return (wrapNode(i) - (NODE_IDS.length - 1) * 0.5) * SPACING;
  }

  function barY(x) {
    return Math.sin(x * 0.11) * 0.18 + Math.sin(x * 0.29) * 0.05;
  }

  function wrapNode(i) {
    const n = NODE_IDS.length;
    return ((i % n) + n) % n;
  }

  function faceInfo(id) {
    return state.faces.find((f) => f.id === id) || DEFAULT_FACES[0];
  }

  function buildStarField(count, spread, size, color, seed) {
    const rng = mulberry(seed);
    const pos = new Float32Array(count * 3);
    const geo = new THREE.BufferGeometry();
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (rng() - 0.5) * spread;
      pos[i * 3 + 1] = (rng() - 0.5) * spread * 0.45;
      pos[i * 3 + 2] = (rng() - 0.5) * spread * 0.7 - 4;
    }
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
    scene.add(pts);
    pts.userData.parallax = size > 0.02 ? 0.45 : 0.18;
    return pts;
  }

  function buildRibbon(count) {
    const rng = mulberry(0xc0de);
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const orbits = [];
    const cWarm = new THREE.Color(0xf0e2c0);
    const cCool = new THREE.Color(0x8aa0d8);
    const cDust = new THREE.Color(0x3a2a58);
    for (let i = 0; i < count; i++) {
      let node = 0;
      let pick = rng();
      if (pick < 0.22) node = 0;
      else if (pick < 0.4) node = 1;
      else node = (rng() * NODE_IDS.length) | 0;
      if (rng() > 0.62) {
        const u = rng();
        const x = nodeX(0) + u * (nodeX(NODE_IDS.length - 1) - nodeX(0));
        const dens = densityAt(x);
        const y = barY(x) + gauss(rng) * (0.06 + 0.18 * dens);
        const z = gauss(rng) * (0.04 + 0.14 * dens);
        pos[i * 3] = x + gauss(rng) * 0.12;
        pos[i * 3 + 1] = y;
        pos[i * 3 + 2] = z;
        const t = dens;
        const c = cDust.clone().lerp(cWarm, t * 0.7).lerp(cCool, (1 - t) * 0.25);
        col[i * 3] = c.r;
        col[i * 3 + 1] = c.g;
        col[i * 3 + 2] = c.b;
        orbits.push({
          node,
          radius: 0.05 + rng() * (0.18 + dens * 0.35),
          phase: rng() * Math.PI * 2,
          speed: (0.08 + rng() * 0.22) * (rng() > 0.5 ? 1 : -1),
          tilt: (rng() - 0.5) * 0.7,
          along: x,
          restY: y,
          restZ: z,
          bound: rng() > 0.35
        });
      } else {
        const nx = nodeX(node);
        const radius = 0.08 + rng() * 0.55;
        const phase = rng() * Math.PI * 2;
        const tilt = (rng() - 0.5) * 1.1;
        pos[i * 3] = nx + Math.cos(phase) * radius;
        pos[i * 3 + 1] = barY(nx) + Math.sin(phase) * radius * 0.28 * Math.cos(tilt);
        pos[i * 3 + 2] = Math.sin(phase * 0.9) * radius * 0.22;
        const c = cWarm.clone().lerp(cCool, rng() * 0.45);
        col[i * 3] = c.r;
        col[i * 3 + 1] = c.g;
        col[i * 3 + 2] = c.b;
        orbits.push({
          node,
          radius,
          phase,
          speed: (0.12 + rng() * 0.35) * (rng() > 0.5 ? 1 : -1),
          tilt,
          along: nx,
          restY: pos[i * 3 + 1],
          restZ: pos[i * 3 + 2],
          bound: true
        });
      }
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      size: 0.1,
      vertexColors: true,
      transparent: true,
      opacity: 0.95,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: true
    }));
    pts.userData.orbits = orbits;
    rig.add(pts);
    return pts;
  }

  function densityAt(x) {
    let d = 0;
    for (let i = 0; i < NODE_IDS.length; i++) {
      const dx = x - nodeX(i);
      d += Math.exp(-(dx * dx) / 0.9);
    }
    return Math.min(1, d);
  }

  function buildSpine(count) {
    const rng = mulberry(0xa11e);
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const warm = new THREE.Color(0xfff1d0);
    const cool = new THREE.Color(0x9ab0e8);
    const x0 = nodeX(0);
    const x1 = nodeX(NODE_IDS.length - 1);
    for (let i = 0; i < count; i++) {
      const x = x0 + (i / (count - 1)) * (x1 - x0) + gauss(rng) * 0.07;
      const dens = densityAt(x);
      pos[i * 3] = x;
      pos[i * 3 + 1] = barY(x) + gauss(rng) * (0.03 + dens * 0.07);
      pos[i * 3 + 2] = gauss(rng) * (0.025 + dens * 0.05);
      const c = warm.clone().lerp(cool, 0.2 + (1 - dens) * 0.45);
      col[i * 3] = c.r;
      col[i * 3 + 1] = c.g;
      col[i * 3 + 2] = c.b;
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      size: 0.16,
      vertexColors: true,
      transparent: true,
      opacity: 1,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: true
    }));
    rig.add(pts);
    return pts;
  }

  function buildLive(count) {
    const rng = mulberry(0x71fe);
    const pos = new Float32Array(count * 3);
    const col = new Float32Array(count * 3);
    const orbits = [];
    const cWarm = new THREE.Color(0xfff4dc);
    const cCool = new THREE.Color(0xb8c8f0);
    for (let i = 0; i < count; i++) {
      const node = i % NODE_IDS.length;
      const nx = nodeX(node);
      const radius = 0.12 + rng() * 0.62;
      const phase = rng() * Math.PI * 2;
      const tilt = (rng() - 0.5) * 1.05;
      pos[i * 3] = nx + Math.cos(phase) * radius;
      pos[i * 3 + 1] = barY(nx) + Math.sin(phase) * radius * 0.3 * Math.cos(tilt);
      pos[i * 3 + 2] = Math.sin(phase * 0.9) * radius * 0.22;
      const c = cWarm.clone().lerp(cCool, rng() * 0.5);
      col[i * 3] = c.r;
      col[i * 3 + 1] = c.g;
      col[i * 3 + 2] = c.b;
      orbits.push({
        node,
        radius,
        phase,
        speed: (0.14 + rng() * 0.38) * (rng() > 0.5 ? 1 : -1),
        tilt,
        along: nx,
        bound: true
      });
    }
    const geo = new THREE.BufferGeometry();
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    geo.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const pts = new THREE.Points(geo, new THREE.PointsMaterial({
      map: starMap,
      size: 0.14,
      vertexColors: true,
      transparent: true,
      opacity: 1,
      depthWrite: false,
      blending: THREE.AdditiveBlending,
      sizeAttenuation: true
    }));
    pts.userData.orbits = orbits;
    rig.add(pts);
    return pts;
  }

  function buildShafts() {
    const map = spriteTex(256, (ctx, s) => {
      const g = ctx.createLinearGradient(s / 2, 0, s / 2, s);
      g.addColorStop(0, "rgba(255,248,230,0)");
      g.addColorStop(0.42, "rgba(186,198,255,0.08)");
      g.addColorStop(0.5, "rgba(255,236,200,0.42)");
      g.addColorStop(0.58, "rgba(160,180,240,0.1)");
      g.addColorStop(1, "rgba(0,0,0,0)");
      ctx.fillStyle = g;
      ctx.fillRect(s * 0.36, 0, s * 0.28, s);
    });
    const list = [];
    for (let i = 0; i < NODE_IDS.length; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(1.55, 6.8),
        new THREE.MeshBasicMaterial({
          map,
          transparent: true,
          opacity: 0.32,
          depthWrite: false,
          blending: THREE.AdditiveBlending,
          side: THREE.DoubleSide
        })
      );
      const x = nodeX(i);
      mesh.position.set(x, barY(x), 0.08);
      mesh.userData.node = i;
      rig.add(mesh);
      list.push(mesh);
    }
    return list;
  }

  function buildOrbiters(count) {
    const rng = mulberry(0x0b17);
    const list = [];
    for (let i = 0; i < count; i++) {
      const node = i % NODE_IDS.length;
      const mesh = new THREE.Sprite(new THREE.SpriteMaterial({
        map: starMap,
        color: rng() > 0.7 ? 0xc9d8ff : 0xfff4dc,
        transparent: true,
        depthWrite: false,
        blending: THREE.AdditiveBlending,
        opacity: 0.7
      }));
      mesh.scale.setScalar(0.08 + rng() * 0.1);
      mesh.userData = {
        node,
        radius: 0.18 + rng() * 0.7,
        phase: rng() * Math.PI * 2,
        speed: (0.15 + rng() * 0.4) * (rng() > 0.5 ? 1 : -1),
        tilt: (rng() - 0.5) * 0.9
      };
      rig.add(mesh);
      list.push(mesh);
    }
    return list;
  }

  function buildNodes() {
    const list = [];
    for (let i = 0; i < NODE_IDS.length; i++) {
      const group = new THREE.Group();
      group.position.set(nodeX(i), barY(nodeX(i)), 0);
      const info = faceInfo(NODE_IDS[i]);
      const coreCol = new THREE.Color(info.core || "#E8D7A8");
      const bloom = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: coreCol,
        transparent: true,
        opacity: 0.28,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      bloom.scale.set(8.8, 4.6, 1);
      group.add(bloom);
      const halo = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: coreCol,
        transparent: true,
        opacity: 0.55,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      halo.scale.set(4.8, 2.6, 1);
      group.add(halo);
      const mid = new THREE.Sprite(new THREE.SpriteMaterial({
        map: glowMap,
        color: 0xfff8e8,
        transparent: true,
        opacity: 0.8,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      mid.scale.set(1.2, 0.88, 1);
      group.add(mid);
      const core = new THREE.Sprite(new THREE.SpriteMaterial({
        map: starMap,
        color: 0xffffff,
        transparent: true,
        opacity: 1,
        depthWrite: false,
        blending: THREE.AdditiveBlending
      }));
      core.scale.set(0.55, 0.55, 1);
      group.add(core);
      const light = new THREE.PointLight(coreCol, i === 0 ? 3.2 : 1.7, 6.4, 1.4);
      group.add(light);
      const label = makeLabel(info.title);
      label.position.y = -0.78;
      group.add(label);
      group.userData.bloom = bloom;
      group.userData.label = label;
      const hit = new THREE.Mesh(
        new THREE.SphereGeometry(0.42, 12, 10),
        new THREE.MeshBasicMaterial({ visible: false })
      );
      hit.userData.face = NODE_IDS[i];
      hit.userData.node = i;
      group.add(hit);
      group.userData.halo = halo;
      group.userData.mid = mid;
      group.userData.light = light;
      group.userData.node = i;
      rig.add(group);
      list.push({ group, hit, halo, mid, light, bloom, label });
    }
    return list;
  }

  function buildVeils() {
    const list = [];
    for (let i = 0; i < 5; i++) {
      const mesh = new THREE.Mesh(
        new THREE.PlaneGeometry(18, 3.4),
        new THREE.MeshBasicMaterial({
          map: dustMap,
          transparent: true,
          opacity: 0.28,
          depthWrite: false,
          blending: THREE.AdditiveBlending,
          side: THREE.DoubleSide
        })
      );
      mesh.position.set(0, (i - 2) * 0.12 + barY(0), -0.55 - i * 0.18);
      mesh.rotation.z = (i - 2) * 0.028;
      rig.add(mesh);
      list.push(mesh);
    }
    return list;
  }

  function buildVignette() {
    const tex = spriteTex(256, (ctx, s) => {
      const g = ctx.createRadialGradient(s / 2, s / 2, s * 0.18, s / 2, s / 2, s * 0.55);
      g.addColorStop(0, "rgba(0,0,0,0)");
      g.addColorStop(0.62, "rgba(0,0,0,0.12)");
      g.addColorStop(1, "rgba(0,0,0,0.72)");
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

  function gauss(rng) {
    return (rng() + rng() + rng() - 1.5) * 0.85;
  }

  function tickOrbits(dt, now) {
    if (!state.motion) return;
    const pos = live.geometry.attributes.position;
    const orbits = live.userData.orbits;
    const arr = pos.array;
    for (let i = 0; i < orbits.length; i++) {
      const o = orbits[i];
      o.phase += o.speed * dt;
      const nx = nodeX(o.node);
      o.along += (nx - o.along) * 0.12 * dt;
      const ca = Math.cos(o.phase);
      const sa = Math.sin(o.phase);
      arr[i * 3] = o.along + ca * o.radius;
      arr[i * 3 + 1] = barY(o.along) + sa * o.radius * 0.32 * Math.cos(o.tilt);
      arr[i * 3 + 2] = sa * o.radius * 0.24 * Math.sin(o.tilt + 0.4);
    }
    pos.needsUpdate = true;

    for (const s of orbiters) {
      const u = s.userData;
      u.phase += u.speed * dt;
      const nx = nodeX(u.node);
      s.position.set(
        nx + Math.cos(u.phase) * u.radius,
        barY(nx) + Math.sin(u.phase) * u.radius * 0.34 * Math.cos(u.tilt),
        Math.sin(u.phase * 0.85) * u.radius * 0.26
      );
    }
  }

  function setNode(index, instant) {
    state.node = wrapNode(index);
    state.targetX = nodeX(state.node);
    if (instant || !state.motion) state.visualX = state.targetX;
    const info = faceInfo(NODE_IDS[state.node]);
    syncCaption(info.title, info.hint);
  }

  function showOverlay(items, focus, instant) {
    state.overlay = true;
    state.items = items || [];
    state.focus = focus || 0;
    overlayEl.classList.add("show");
    renderTrack();
    applyOverlayFocus(instant);
  }

  function hideOverlay() {
    state.overlay = false;
    state.items = [];
    overlayEl.classList.remove("show");
    trackEl.innerHTML = "";
    const info = faceInfo(NODE_IDS[state.node]);
    syncCaption(info.title, info.hint);
  }

  function renderTrack() {
    trackEl.innerHTML = "";
    state.items.forEach((item, i) => {
      const el = document.createElement("button");
      el.type = "button";
      el.className = "game-card" + (i === state.focus ? " focus" : "");
      el.innerHTML = `<span class="meta">${escapeHtml(item.meta || "")}</span><span class="title">${escapeHtml(item.title || "")}</span><span class="mark">${escapeHtml((item.glyph || "?").slice(0, 1))}</span>`;
      el.addEventListener("click", (ev) => {
        ev.stopPropagation();
        if (i === state.focus) {
          send({ v: 1, type: "select", index: i, item: item.id, face: "Session" });
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
    const cards = trackEl.children;
    for (let i = 0; i < cards.length; i++) {
      cards[i].classList.toggle("focus", i === state.focus);
    }
    const item = state.items[state.focus];
    if (item) syncCaption(item.title, `${item.meta || ""} · arrows cycle · Enter launches · Esc returns`);
    const cardW = 238;
    const x = window.innerWidth / 2 - (state.focus + 0.5) * cardW;
    trackEl.style.transition = instant || !state.motion ? "none" : "transform 0.55s cubic-bezier(0.22, 1, 0.36, 1)";
    trackEl.style.transform = `translate3d(${x}px, 0, 0)`;
  }

  function startOpen(msg) {
    if (msg && typeof msg.node === "number") setNode(msg.node, !state.motion);
    if (msg && msg.front) {
      const idx = NODE_IDS.indexOf(msg.front);
      if (idx >= 0) setNode(idx, !state.motion);
    }
    if (msg && msg.motion === false) state.motion = false;
    const items = Array.isArray(msg && msg.items) ? msg.items : previewItems();
    const stay = msg && (msg.stay === true || String(msg.mode || "").toLowerCase() === "carousel");
    if (stay && items.length) {
      showOverlay(items, typeof msg.focus === "number" ? msg.focus : 0, !state.motion);
    }
    send({ v: 1, type: "opened", face: NODE_IDS[state.node], stay: Boolean(stay) });
    setLoop(true);
  }

  function previewItems() {
    return [
      { id: "session", title: "Session engine", meta: "PAGE", kind: "page", glyph: "G" },
      { id: "steam", title: "Steam", meta: "KIT", kind: "tool", glyph: "S" },
      { id: "playnite", title: "Playnite", meta: "KIT", kind: "tool", glyph: "P" }
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
    setLoop(state.motion || true);
    if (!state.motion) renderFrame(0);
  }

  function syncCaption(title, hint) {
    const t = document.getElementById("previewTitle");
    const h = document.getElementById("previewHint");
    if (t) t.textContent = title || "";
    if (h) h.textContent = hint || "Up opens games · Down opens Settings · Left/Right shift nodes";
  }

  function renderFrame(dt) {
    const now = performance.now();
    const step = Math.min(Math.max(dt, 0), 0.033);
    if (state.motion) {
      state.idle = 0.08 * Math.sin(now * 0.00035);
      const k = 1 - Math.exp(-step * 3.2);
      state.visualX += (state.targetX - state.visualX) * k;
      tickOrbits(step, now);
      farStars.rotation.y = now * 0.00001;
      midStars.rotation.y = -now * 0.000018;
      nearStars.rotation.y = now * 0.00003;
      spine.rotation.z = Math.sin(now * 0.00008) * 0.012;
      veils.forEach((v, i) => {
        v.material.opacity = 0.22 + 0.08 * Math.sin(now * 0.0004 + i);
      });
    }
    const pan = state.visualX;
    camera.position.x = pan * 0.2;
    camera.position.y = 0.04 + state.idle;
    camera.position.z = 7.35 + (state.overlay ? -0.35 : 0);
    camera.lookAt(pan * 0.48, barY(pan) * 0.2, 0);
    farStars.position.x = pan * 0.05;
    midStars.position.x = pan * 0.1;
    nearStars.position.x = pan * 0.22;
    key.position.x = pan * 0.5;
    key.position.y = barY(pan) + 0.2;
    const pulse = state.motion ? 0.88 + 0.12 * Math.sin(now * 0.0016) : 0.9;
    for (const n of nodes) {
      const on = n.group.userData.node === state.node;
      n.bloom.material.opacity = (on ? 0.72 : 0.32) * pulse;
      n.halo.material.opacity = (on ? 0.95 : 0.48) * pulse;
      n.mid.material.opacity = on ? 1 : 0.55;
      n.light.intensity = on ? 3.4 * pulse : 1.2;
      n.label.material.opacity = on ? 0.78 : 0.12;
      const s = on ? 1.1 : 0.9;
      n.group.scale.setScalar(state.motion ? n.group.scale.x + (s - n.group.scale.x) * 0.08 : s);
    }
    shafts.forEach((shaft) => {
      const on = shaft.userData.node === state.node;
      shaft.material.opacity = (on ? 0.55 : 0.22) * pulse;
      shaft.rotation.y = state.motion ? Math.sin(now * 0.00025 + shaft.userData.node) * 0.08 : 0;
    });
    renderer.toneMappingExposure = state.overlay ? 0.92 : 1.18;
    vignette.position.copy(camera.position);
    vignette.quaternion.copy(camera.quaternion);
    vignette.translateZ(-1.2);
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
      if (idx === state.node) previewOpen();
      else {
        const dir = idx > state.node ? "Right" : "Left";
        setNode(idx, false);
        send({ v: 1, type: "turn", turn: dir });
      }
      return;
    }
    const nx = pointer.x;
    const ny = -pointer.y;
    if (Math.abs(ny) > Math.abs(nx) && Math.abs(ny) > 0.28) {
      const turn = ny < 0 ? "Up" : "Down";
      if (hosted) send({ v: 1, type: "turn", turn });
      else localTurn(turn);
      return;
    }
    if (Math.abs(nx) > 0.28) {
      const turn = nx < 0 ? "Left" : "Right";
      if (hosted) send({ v: 1, type: "turn", turn });
      else localTurn(turn);
      return;
    }
    if (hosted) send({ v: 1, type: "activate" });
    else previewOpen();
  }

  function previewOpen() {
    startOpen({ front: "Session", motion: state.motion, stay: true, mode: "carousel" });
  }

  function localTurn(turn) {
    if (turn === "Left") setNode(state.node - 1, !state.motion);
    if (turn === "Right") setNode(state.node + 1, !state.motion);
    if (turn === "Up") previewOpen();
  }

  function onKey(ev) {
    if (state.overlay) {
      if (ev.key === "Escape" || ev.key === "ArrowDown") {
        ev.preventDefault();
        send({ v: 1, type: "back" });
        if (!hosted) hideOverlay();
        return;
      }
      if (ev.key === "Enter" || ev.key === " ") {
        ev.preventDefault();
        const item = state.items[state.focus];
        if (item) send({ v: 1, type: "select", index: state.focus, item: item.id, face: "Session" });
        return;
      }
      if (ev.key === "ArrowLeft") {
        ev.preventDefault();
        state.focus = wrapIndex(state.focus - 1, state.items.length);
        applyOverlayFocus(false);
        send({ v: 1, type: "cycle", index: state.focus, item: state.items[state.focus].id });
        return;
      }
      if (ev.key === "ArrowRight") {
        ev.preventDefault();
        state.focus = wrapIndex(state.focus + 1, state.items.length);
        applyOverlayFocus(false);
        send({ v: 1, type: "cycle", index: state.focus, item: state.items[state.focus].id });
        return;
      }
      return;
    }
    if (ev.key === "Enter" || ev.key === " ") {
      ev.preventDefault();
      if (hosted) send({ v: 1, type: "activate" });
      else previewOpen();
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
      ctx.font = "600 64px 'Segoe UI', sans-serif";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillStyle = "rgba(244,241,234,0.92)";
      ctx.shadowColor = "rgba(0,0,0,0.8)";
      ctx.shadowBlur = 18;
      ctx.fillText(String(text || "").toUpperCase(), s / 2, s / 2);
    });
    const sprite = new THREE.Sprite(new THREE.SpriteMaterial({
      map: tex,
      transparent: true,
      opacity: 0.12,
      depthWrite: false
    }));
    sprite.scale.set(1.7, 0.34, 1);
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
