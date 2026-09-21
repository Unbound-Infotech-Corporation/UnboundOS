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
    posePitch: 0
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
  renderer.setClearColor(0x05070a, 1);
  renderer.setPixelRatio(Math.min(window.devicePixelRatio || 1, 1.75));
  renderer.outputColorSpace = THREE.SRGBColorSpace;
  renderer.toneMapping = THREE.ACESFilmicToneMapping;
  renderer.toneMappingExposure = 1.28;
  renderer.shadowMap.enabled = false;

  const scene = new THREE.Scene();
  scene.fog = new THREE.FogExp2(0x05070a, 0.028);
  scene.environment = makeEnvMap();

  const camera = new THREE.PerspectiveCamera(32, 1, 0.1, 40);
  camera.position.set(0, 1.08, 4.7);
  camera.lookAt(0, -0.12, 0);

  const restRig = new THREE.Group();
  const spinRig = new THREE.Group();
  scene.add(restRig);
  restRig.add(spinRig);

  const artifact = new THREE.Group();
  spinRig.add(artifact);

  const plates = [];
  const seams = [];
  const arcs = [];
  const raycaster = new THREE.Raycaster();
  const pointer = new THREE.Vector2();
  const clock = new THREE.Clock();

  const coreLight = new THREE.PointLight(0x00f0ff, 2.1, 7.5, 1.6);
  artifact.add(coreLight);

  const key = new THREE.DirectionalLight(0xe8eef6, 2.35);
  key.position.set(-2.2, 3.4, 4.6);
  scene.add(key);

  const fill = new THREE.DirectionalLight(0x7f93b3, 0.85);
  fill.position.set(1.4, 0.2, 5.2);
  scene.add(fill);

  const rim = new THREE.DirectionalLight(0x3b6bff, 1.05);
  rim.position.set(3.4, 0.6, -3.0);
  scene.add(rim);

  scene.add(new THREE.HemisphereLight(0x3d4f6e, 0x05070a, 0.95));
  scene.add(new THREE.AmbientLight(0x1a2436, 0.55));

  const chassisMat = new THREE.MeshStandardMaterial({
    color: 0x1a2433,
    metalness: 0.62,
    roughness: 0.42,
    envMapIntensity: 0.85
  });
  const chassis = new THREE.Mesh(new THREE.BoxGeometry(1.72, 1.72, 1.72), chassisMat);
  artifact.add(chassis);
  artifact.scale.setScalar(1.02);

  const coreMat = new THREE.MeshBasicMaterial({
    color: 0x00f0ff,
    transparent: true,
    opacity: 0.22,
    blending: THREE.AdditiveBlending,
    depthWrite: false
  });
  const core = new THREE.Mesh(new THREE.IcosahedronGeometry(0.22, 1), coreMat);
  artifact.add(core);

  const coreHalo = new THREE.Mesh(
    new THREE.SphereGeometry(0.38, 24, 24),
    new THREE.MeshBasicMaterial({
      color: 0x1e40af,
      transparent: true,
      opacity: 0.1,
      blending: THREE.AdditiveBlending,
      depthWrite: false
    })
  );
  artifact.add(coreHalo);

  buildPlates();
  buildSeams();
  buildFloor();
  const motes = buildMotes();

  function faceInfo(id) {
    return state.faces.find((f) => f.id === id) || DEFAULT_FACES[0];
  }

  function paintPlate(info, isFront) {
    const size = 1024;
    const c = document.createElement("canvas");
    c.width = c.height = size;
    const ctx = c.getContext("2d");
    const accent = info.accent || "#00F0FF";

    ctx.fillStyle = "#151C28";
    ctx.fillRect(0, 0, size, size);

    const vg = ctx.createRadialGradient(size * 0.38, size * 0.28, 40, size * 0.5, size * 0.55, size * 0.8);
    vg.addColorStop(0, "rgba(0, 240, 255, 0.16)");
    vg.addColorStop(0.4, "rgba(30, 64, 175, 0.12)");
    vg.addColorStop(1, "rgba(11, 18, 29, 0.55)");
    ctx.fillStyle = vg;
    ctx.fillRect(0, 0, size, size);

    ctx.save();
    ctx.globalAlpha = 0.16;
    for (let i = 0; i < 1800; i++) {
      const n = 10 + Math.random() * 30;
      ctx.fillStyle = `rgb(${n},${n + 4},${n + 10})`;
      ctx.fillRect(Math.random() * size, Math.random() * size, 2, 2);
    }
    ctx.restore();

    ctx.strokeStyle = "rgba(148, 163, 184, 0.08)";
    ctx.lineWidth = 1;
    for (let g = 48; g < size; g += 64) {
      ctx.beginPath();
      ctx.moveTo(g, 0);
      ctx.lineTo(g, size);
      ctx.moveTo(0, g);
      ctx.lineTo(size, g);
      ctx.stroke();
    }

    ctx.save();
    ctx.strokeStyle = hexAlpha(accent, isFront ? 0.42 : 0.22);
    ctx.lineWidth = 2;
    ctx.lineCap = "square";
    const seed = hash(info.id);
    for (let i = 0; i < 18; i++) {
      const x0 = 80 + ((seed * (i + 3)) % 860);
      const y0 = 80 + ((seed * (i + 11)) % 860);
      ctx.beginPath();
      ctx.moveTo(x0, y0);
      let x = x0;
      let y = y0;
      const segs = 4 + (i % 4);
      for (let s = 0; s < segs; s++) {
        if (((seed + i + s) & 1) === 0) {
          x += ((seed >> s) & 1) ? 90 : -90;
        } else {
          y += ((seed >> (s + 2)) & 1) ? 80 : -80;
        }
        ctx.lineTo(clamp(x, 48, 976), clamp(y, 48, 976));
      }
      ctx.stroke();
      ctx.fillStyle = hexAlpha(accent, 0.55);
      ctx.fillRect(clamp(x, 48, 976) - 3, clamp(y, 48, 976) - 3, 6, 6);
    }
    ctx.restore();

    ctx.strokeStyle = hexAlpha(accent, 0.7);
    ctx.lineWidth = 3;
    corner(ctx, 36, 36, 1, 1, accent);
    corner(ctx, size - 36, 36, -1, 1, "#E4B53C");
    corner(ctx, 36, size - 36, 1, -1, accent);
    corner(ctx, size - 36, size - 36, -1, -1, accent);

    ctx.fillStyle = hexAlpha(accent, 0.85);
    ctx.font = "600 28px 'Segoe UI', sans-serif";
    ctx.fillText((info.kicker || "").split("").join(" "), 64, 92);

    ctx.fillStyle = "#F8FAFC";
    ctx.font = "650 420px 'Segoe UI Variable', 'Segoe UI', sans-serif";
    ctx.textAlign = "center";
    ctx.fillText(info.monogram || "", size * 0.5, size * 0.58);

    ctx.textAlign = "left";
    ctx.font = "600 72px 'Segoe UI Variable', 'Segoe UI', sans-serif";
    ctx.fillText(info.title || "", 64, size - 110);
    ctx.fillStyle = hexAlpha(accent, 0.9);
    ctx.font = "600 28px 'Segoe UI', sans-serif";
    ctx.fillText((info.meta || "").split("").join(" "), 64, size - 64);

    if (isFront) {
      ctx.fillStyle = hexAlpha(accent, 0.05);
      ctx.fillRect(0, 0, size, size);
    }

    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    tex.anisotropy = 8;
    return tex;
  }

  function corner(ctx, x, y, sx, sy, color) {
    ctx.strokeStyle = color;
    ctx.beginPath();
    ctx.moveTo(x, y + 28 * sy);
    ctx.lineTo(x, y);
    ctx.lineTo(x + 28 * sx, y);
    ctx.stroke();
  }

  function buildPlates() {
    const geom = new THREE.BoxGeometry(1.62, 1.62, 0.055);
    for (const info of state.faces) {
      const layout = FACE_LAYOUT[info.id];
      if (!layout) {
        continue;
      }
      const tex = paintPlate(info, info.id === state.front);
      const mat = new THREE.MeshBasicMaterial({
        map: tex,
        color: 0xffffff
      });
      const mesh = new THREE.Mesh(geom, mat);
      const dir = new THREE.Vector3(...layout.dir);
      mesh.position.copy(dir).multiplyScalar(0.90);
      orientPlate(mesh, dir, new THREE.Vector3(...layout.up));
      mesh.userData.face = info.id;
      artifact.add(mesh);
      plates.push(mesh);
    }
  }

  function rebuildPlateTextures() {
    for (const mesh of plates) {
      const info = faceInfo(mesh.userData.face);
      const tex = paintPlate(info, info.id === state.front);
      mesh.material.map?.dispose();
      mesh.material.emissiveMap?.dispose();
      mesh.material.map = tex;
      mesh.material.needsUpdate = true;
    }
  }

  function buildSeams() {
    const edge = 1.78;
    const h = edge * 0.5;
    const axes = [
      [[-h, -h, -h], [h, -h, -h]],
      [[-h, h, -h], [h, h, -h]],
      [[-h, -h, h], [h, -h, h]],
      [[-h, h, h], [h, h, h]],
      [[-h, -h, -h], [-h, h, -h]],
      [[h, -h, -h], [h, h, -h]],
      [[-h, -h, h], [-h, h, h]],
      [[h, -h, h], [h, h, h]],
      [[-h, -h, -h], [-h, -h, h]],
      [[h, -h, -h], [h, -h, h]],
      [[-h, h, -h], [-h, h, h]],
      [[h, h, -h], [h, h, h]]
    ];
    for (const [a, b] of axes) {
      const start = new THREE.Vector3(...a);
      const end = new THREE.Vector3(...b);
      const dir = end.clone().sub(start);
      const len = dir.length();
      const mid = start.clone().add(end).multiplyScalar(0.5);
      const geo = new THREE.CylinderGeometry(0.018, 0.018, len, 10);
      const mat = new THREE.MeshBasicMaterial({
        color: 0x00f0ff,
        transparent: true,
        opacity: 0.55,
        blending: THREE.AdditiveBlending,
        depthWrite: false
      });
      const mesh = new THREE.Mesh(geo, mat);
      mesh.position.copy(mid);
      mesh.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir.clone().normalize());
      artifact.add(mesh);
      seams.push(mesh);
    }
  }

  function buildFloor() {
    const c = document.createElement("canvas");
    c.width = c.height = 1024;
    const ctx = c.getContext("2d");
    ctx.fillStyle = "#05070A";
    ctx.fillRect(0, 0, 1024, 1024);
    ctx.strokeStyle = "rgba(0, 240, 255, 0.12)";
    ctx.lineWidth = 2;
    for (let i = 1; i <= 6; i++) {
      ctx.beginPath();
      ctx.arc(512, 512, i * 70, 0, Math.PI * 2);
      ctx.stroke();
    }
    ctx.strokeStyle = "rgba(30, 64, 175, 0.16)";
    for (let a = 0; a < 12; a++) {
      const ang = (a / 12) * Math.PI * 2;
      ctx.beginPath();
      ctx.moveTo(512, 512);
      ctx.lineTo(512 + Math.cos(ang) * 480, 512 + Math.sin(ang) * 480);
      ctx.stroke();
    }
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.SRGBColorSpace;
    const floor = new THREE.Mesh(
      new THREE.CircleGeometry(3.2, 64),
      new THREE.MeshBasicMaterial({
        map: tex,
        transparent: true,
        opacity: 0.38,
        depthWrite: false
      })
    );
    floor.rotation.x = -Math.PI / 2;
    floor.position.y = -1.28;
    scene.add(floor);

    const shadow = new THREE.Mesh(
      new THREE.CircleGeometry(1.15, 40),
      new THREE.MeshBasicMaterial({
        color: 0x000000,
        transparent: true,
        opacity: 0.62,
        depthWrite: false
      })
    );
    shadow.rotation.x = -Math.PI / 2;
    shadow.position.y = -1.26;
    scene.add(shadow);
  }

  function buildMotes() {
    const count = 90;
    const geo = new THREE.BufferGeometry();
    const pos = new Float32Array(count * 3);
    for (let i = 0; i < count; i++) {
      pos[i * 3] = (Math.random() - 0.5) * 6;
      pos[i * 3 + 1] = Math.random() * 3.2 - 1.2;
      pos[i * 3 + 2] = (Math.random() - 0.5) * 6;
    }
    geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
    const mat = new THREE.PointsMaterial({
      color: 0x00f0ff,
      size: 0.018,
      transparent: true,
      opacity: 0.18,
      blending: THREE.AdditiveBlending,
      depthWrite: false
    });
    const pts = new THREE.Points(geo, mat);
    scene.add(pts);
    return pts;
  }

  function spawnArcs() {
    if (!state.motion) {
      return;
    }
    clearArcs();
    const corners = [];
    const h = 0.95;
    for (const x of [-h, h]) {
      for (const y of [-h, h]) {
        for (const z of [-h, h]) {
          corners.push(new THREE.Vector3(x, y, z));
        }
      }
    }
    const n = 9;
    for (let i = 0; i < n; i++) {
      const a = corners[(Math.random() * corners.length) | 0];
      let b = corners[(Math.random() * corners.length) | 0];
      if (b === a) {
        b = corners[(i + 3) % corners.length];
      }
      const pts = jagged(a, b, 16, 0.22 + Math.random() * 0.14);
      const curve = new THREE.CatmullRomCurve3(pts);
      const tube = new THREE.TubeGeometry(curve, 40, i < 3 ? 0.02 : 0.008, 6, false);
      const mat = new THREE.MeshBasicMaterial({
        color: i % 2 === 0 ? state.accent.clone() : state.core.clone(),
        transparent: true,
        opacity: 0.95,
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
    // Logical pitch is WinUI/Y-down (Up = Mods = -90). Three.js is Y-up, so negate.
    spinRig.rotation.x = THREE.MathUtils.degToRad(-state.visualPitch);
  }

  function renderFrame(dt) {
    const now = performance.now();
    if (state.motion && !state.dragging) {
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
    coreLight.color.copy(state.accent);
    coreLight.intensity = state.motion ? 1.8 + burst * 3.4 : 0.7;
    coreMat.color.copy(state.accent);
    coreMat.opacity = state.motion ? 0.28 + burst * 0.5 : 0.16;
    coreHalo.material.color.copy(state.core);
    coreHalo.material.opacity = state.motion ? 0.08 + burst * 0.2 : 0.05;
    core.rotation.y += dt * (0.35 + burst * 2.4);
    core.rotation.x += dt * 0.2;

    for (const seam of seams) {
      seam.material.color.copy(state.seam);
      seam.material.opacity = 0.55 + burst * 0.45 + (state.motion ? 0.12 : 0);
    }

    motes.visible = state.motion;
    if (state.motion) {
      motes.rotation.y += dt * 0.03;
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
      send({ v: 1, type: "activate" });
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
    const map = { ArrowLeft: "Left", ArrowRight: "Right", ArrowUp: "Up", ArrowDown: "Down" };
    if (ev.key === "Enter" || ev.key === " ") {
      ev.preventDefault();
      send({ v: 1, type: "activate" });
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
      applyHostState(data);
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
    const colors = ["#1a2740", "#05070a", "#142238", "#05070a", "#182844", "#080c14"];
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

  function clamp(v, a, b) {
    return Math.max(a, Math.min(b, v));
  }
})();
