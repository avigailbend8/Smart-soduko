/* Mind Studio – a small engine shared by the canvas action games.
   It owns the page views (menu, game, store, help), the canvas and its size, the game loop, pause,
   the ready card with power-ups, arrow pad + keyboard + swipe input, the end-of-round card, the score
   tables and the "beat the rival's score" AI duel. A game only describes its rules and drawing:

   Arcade.run(CFG, {
     tagline, help (html), play: "label", ratio: height/width, pad: [[name, symbol, label], …] | null,
     powers: [{ id, label }]   – consumables you can switch on before a round (stock ids in the wallet)
     init(S, w, h)             – set up a new round on S (S.power[id] tells which power-ups are on)
     update(S, dt, w, h)       – advance; set S.over = true to end the round
     draw(ctx, S, w, h, t)
     input(S, name, down)      – name: left/right/up/down/action, or "tap" with S.tapX/S.tapY
     hud: S => [[icon, value], [icon, value]]
     score: S => number, coins: S => number, unit: "נק׳", resultTitle?: S => string
   })
*/
(function () {
  "use strict";
  const { $ } = Hub;
  const DEFAULT_KEYS = { ArrowLeft: "left", a: "left", A: "left", ArrowRight: "right", d: "right", D: "right", ArrowUp: "up", w: "up", W: "up", ArrowDown: "down", s: "down", S: "down", Enter: "action", x: "action", X: "action", " ": "action" };

  function run(CFG, G) {
    let W, view = "menu", nav = [], S = null, raf = 0, cv, ctx, dims = { w: 0, h: 0 }, power = {};
    const P = () => { const p = W.progress; p.best = p.best || 0; p.runs = p.runs || 0; return p; };
    const unit = G.unit || "נק׳";
    CFG.boards = CFG.boards || [{ id: "best", title: "השיא", unit, bots: G.bots || [["קוואזר", "🛸", 400], ["אוריון", "👾", 200], ["ניצוץ", "🤖", 80]] }];
    CFG.duel = CFG.duel || {
      mode: "score", unit, barTop: true,
      target: r => G.targets[r.level],
      start: () => { mode = "free"; view = "game"; nav = ["menu"]; render(); },
      progress: () => S && S.running ? G.score(S) / G.targets[Hub.duelRival.level] : 0,
      over: () => !!S?.over, lose: () => {},
      active: () => view === "game", menu: () => { stop(); view = "menu"; nav = []; render(); },
    };

    function measure() {
      const stage = $("#stage"); if (!stage) return;
      const w = stage.clientWidth, h = Math.round(Math.min(innerHeight * .66, w * (G.ratio || 1.35), 700));
      stage.style.height = h + "px";
      const dpr = Math.min(devicePixelRatio || 1, 2);
      cv.width = w * dpr; cv.height = h * dpr; ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
      dims = { w, h };
      if (S && G.resize) G.resize(S, w, h);
    }
    // Levels: a target score that grows each level, and the whole game runs a little faster on later levels
    const COUNT = 30;
    let mode = "free", L = 0;
    const goalOf = n => G.goal ? G.goal(n) : Math.max(1, Math.round(G.targets[0] * (.25 + (n - 1) * .09)));
    const speedOf = () => mode === "level" ? .85 + .5 * (L - 1) / (COUNT - 1) : 1;
    function newRound() {
      S = { running: false, paused: false, over: false, count: 0, t: 0, power: { ...power }, time: 0, L: mode === "level" ? L : 0, goal: mode === "level" ? goalOf(L) : 0 };
      G.init(S, dims.w, dims.h);
      S.lives0 = S.lives;
    }
    function step(dt) {
      const k = speedOf(); S.t += dt * k; G.update(S, dt * k, dims.w, dims.h);
      if (mode === "level" && !S.over && G.score(S) >= S.goal) { S.over = true; S.cleared = true; }
      if (S.over) end();
    }
    function frame(t) {
      if (view !== "game" || !S) return;
      const dt = Math.min(.05, (t - (S.time || t)) / 1000); S.time = t;
      if (S.running && !S.paused && !S.over) {
        if (S.count > 0) { S.count -= dt; if (S.count <= 0) Hub.sound("good"); }
        else step(dt);
      }
      ctx.clearRect(0, 0, dims.w, dims.h);
      G.draw(ctx, S, dims.w, dims.h, t);
      if (S.running && S.count > 0) {
        ctx.fillStyle = "rgba(0,0,0,.28)"; ctx.fillRect(0, 0, dims.w, dims.h);
        ctx.fillStyle = "#fff"; ctx.textAlign = "center"; ctx.textBaseline = "middle";
        ctx.font = `500 ${Math.round(dims.w * .24)}px "Frank Ruhl Libre", serif`; ctx.fillText(Math.min(3, Math.ceil(S.count / .6)), dims.w / 2, dims.h * .45);
      }
      hud();
      raf = requestAnimationFrame(frame);
    }
    function hud() { const el = $("#hud"); if (!el || !S) return; const v = [...G.hud(S), ...(S.goal ? [["🎯", `${Math.min(S.goal, Math.floor(G.score(S)))}/${S.goal}`]] : [])].map(([i, x]) => `<span class="chip">${i} <b>${x}</b></span>`).join(""); if (el._v !== v) { el.innerHTML = v; el._v = v; } }
    function stop() { cancelAnimationFrame(raf); raf = 0; }
    function end() {
      if (mode === "level") return endLevel();
      Hub.sound("crash");
      const st = $("#stage"); st.classList.remove("shake"); void st.offsetWidth; st.classList.add("shake");
      const p = P(), score = Math.floor(G.score(S)), record = score > p.best;
      p.runs++; if (record) p.best = score;
      const coins = Math.max(0, Math.floor(G.coins(S)));
      Hub.submitScore(score, "best");
      Hub.save(); Hub.earn(coins);
      setTimeout(() => {
        if (record) Hub.confetti();
        Hub.result({ title: record ? "שיא חדש! 🏆" : (G.resultTitle ? G.resultTitle(S) : "נגמר הסיבוב"), sub: `התוצאה: <b>${score}</b> ${unit}${record ? "" : ` · השיא שלך: ${p.best}`}`, coins, buttons: [["again", "עוד סיבוב", true], ["tomenu", "לתפריט"]] });
      }, 700);
    }
    function endLevel() {
      const won = !!S.cleared, score = Math.floor(G.score(S));
      const lost = S.lives0 !== undefined && S.lives !== undefined ? Math.max(0, S.lives0 - S.lives) : 0;
      const stars = won ? (lost === 0 ? 3 : lost === 1 ? 2 : 1) : 0;
      const p = P(); if (score > p.best) p.best = score;
      let coins = Math.max(0, Math.floor(G.coins(S) * .5));
      if (won) { const first = Hub.levelDone(L, stars); coins += first ? 10 + L * 2 : 4; Hub.sound("win"); Hub.confetti(); }
      else { Hub.sound("crash"); const st = $("#stage"); st.classList.remove("shake"); void st.offsetWidth; st.classList.add("shake"); }
      Hub.save(); if (coins) Hub.earn(coins);
      setTimeout(() => Hub.levelResult({ L, count: COUNT, won, stars, coins, sub: won ? `הגעת ליעד: ${S.goal} ${unit}` : `הגעת ל-${score} מתוך ${S.goal} ${unit}` }), 650);
    }
    function readyCard() {
      const pw = G.powers || [];
      power = {}; pw.forEach(x => { power[x.id] = false; });
      Hub.overlay(`<div class="ready"><h2 class="display sm">${mode === "level" ? `שלב ${L}` : G.readyTitle || "מוכנים?"}</h2>${mode === "level" ? `<p class="earned">🎯 ${goalOf(L)} ${unit}</p>` : ""}
        <p class="lede">${G.readyText || ""}</p>
        ${pw.length ? `<div class="toggles">${pw.map(x => { const n = Hub.stock(x.id); return `<button class="toggle" data-act="apow" data-p="${x.id}" ${n ? "" : "disabled"}>${x.label} · יש ${n}</button>`; }).join("")}</div>` : ""}
        <button class="btn primary big" data-act="ago">${G.go || "יאללה!"}</button>
        <p class="small">${G.keysText || "חצים / WASD במקלדת · כפתורים או החלקה בטלפון · רווח לעצירה"}</p></div>`);
    }
    function pause(on) {
      if (!S || !S.running || S.over) return;
      S.paused = on;
      if (on) Hub.overlay(`<h2 class="display sm">הפסקה</h2><div class="stack"><button class="btn wide primary" data-act="aresume">להמשיך</button><button class="btn wide" data-act="aquit">לסיים את הסיבוב</button></div>`);
      else Hub.closeOverlay();
    }

    function go(v) { nav.push(view); view = v; render(); scrollTo(0, 0); }
    function back() { stop(); view = nav.pop() || "menu"; render(); }
    function render() {
      Hub.closeOverlay();
      $("#app").innerHTML = ({ menu: menuView, levels: levelsView, game: gameView, store: () => Hub.store(), help: helpView })[view]();
      if (view === "game") {
        cv = $("#cv"); ctx = cv.getContext("2d");
        measure(); newRound(); stop(); raf = requestAnimationFrame(frame); readyCard();
      }
    }
    const menuView = () => { const p = P(); return Hub.menu({ tagline: `${G.tagline}${p.best ? `<br><b>השיא שלך: ${p.best} ${unit}</b>` : ""}`, buttons: [["alevels", "שלבים", true], ["aplay", "♾ משחק חופשי"], ["store", "החנות"], ["help", "איך משחקים"]] }); };
    const levelsView = () => `${Hub.topbar("שלבים")}${Hub.levelMap({ count: COUNT, names: G.worlds })}`;
    const gameView = () => `${Hub.topbar(CFG.title)}
      <div class="stage" id="stage" style="background:${G.stageBg || "#111"}"><canvas id="cv"></canvas>
        <div class="hud-top"><span id="hud" style="display:flex;gap:8px"></span><button class="icon-btn" data-act="apause" aria-label="הפסקה">⏸</button></div></div>
      ${G.pad === null ? "" : `<div class="pad" style="grid-template-columns:repeat(${(G.pad || []).length || 4},1fr)">${(G.pad || [["left", "◀", "שמאלה"], ["up", "▲", "למעלה"], ["down", "▼", "למטה"], ["right", "▶", "ימינה"]]).map(([n, s, l]) => `<button data-pad="${n}" aria-label="${l}">${s}<small>${l}</small></button>`).join("")}</div>`}
      <p class="small" style="text-align:center;margin-top:10px">${G.keysText || "חצים / WASD במקלדת · כפתורים או החלקה בטלפון"}</p>`;
    const helpView = () => `${Hub.topbar("איך משחקים")}<div class="help card">${G.help}</div>`;

    // ---------- Input ----------
    const live = () => S && S.running && !S.paused && !S.over && S.count <= 0;
    const send = (name, down) => { if (live()) G.input(S, name, down); };
    document.addEventListener("keydown", e => {
      if (view !== "game" || !S) return;
      if (e.key === "Escape" || e.key === "p" || (e.key === " " && !G.spaceIsAction)) { e.preventDefault(); if (!Hub.overlayOpen() || S.paused) pause(!S.paused); return; }
      const m = (G.keys || DEFAULT_KEYS)[e.key]; if (!m) return;
      e.preventDefault();
      if (Hub.overlayOpen() || (e.repeat && !G.repeatKeys)) return;
      send(m, true);
    });
    document.addEventListener("keyup", e => { const m = (G.keys || DEFAULT_KEYS)[e.key]; if (m && S) send(m, false); });
    let sw = null;
    document.addEventListener("pointerdown", e => {
      const b = e.target.closest("[data-pad]");
      if (b) { e.preventDefault(); b.classList.add("held"); b._p = b.dataset.pad; send(b.dataset.pad, true); return; }
      if (e.target.id === "cv") { const r = cv.getBoundingClientRect(); sw = { x: e.clientX, y: e.clientY, t: performance.now() }; if (S) { S.tapX = e.clientX - r.left; S.tapY = e.clientY - r.top; S.pointer = true; } if (G.pointerDown) send("tap", true); }
    });
    document.addEventListener("pointermove", e => { if (sw && S && G.pointerMove) { const r = cv.getBoundingClientRect(); S.tapX = e.clientX - r.left; S.tapY = e.clientY - r.top; } });
    const up = e => {
      document.querySelectorAll(".pad .held").forEach(x => { x.classList.remove("held"); send(x._p, false); });
      if (sw && e.type === "pointerup") {
        const dx = e.clientX - sw.x, dy = e.clientY - sw.y;
        if (Math.max(Math.abs(dx), Math.abs(dy)) > 26) send(Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? "right" : "left") : (dy < 0 ? "up" : "down"), true);
        else if (!G.pointerDown) send("tap", true);
        if (S) S.pointer = false;
      }
      sw = null;
    };
    document.addEventListener("pointerup", up);
    document.addEventListener("pointercancel", up);
    document.addEventListener("visibilitychange", () => { if (document.hidden && S && S.running && !S.over && !S.paused) pause(true); });
    addEventListener("resize", () => { if (view === "game" && cv) measure(); });

    document.addEventListener("click", e => {
      const b = e.target.closest("[data-act]"); if (!b) return;
      const a = b.dataset.act;
      if (a === "back") return back();
      if (a === "aplay") { mode = "free"; return go("game"); }
      if (a === "alevels") return go("levels");
      if (a === "lvl") { mode = "level"; L = +b.dataset.level; return go("game"); }
      if (a === "lvnext") { Hub.closeOverlay(); L = Math.min(COUNT, L + 1); newRound(); readyCard(); return; }
      if (a === "lvagain") { Hub.closeOverlay(); newRound(); readyCard(); return; }
      if (a === "lvmap") { Hub.closeOverlay(); stop(); view = "levels"; nav = ["menu"]; return render(); }
      if (a === "store") { Hub.clearStoreMsg(); return go("store"); }
      if (a === "help") return go("help");
      if (a === "buy") { Hub.buy(b.dataset.item); G.onBuy?.(); return render(); }
      if (a === "apow") { const id = b.dataset.p; power[id] = !power[id]; b.classList.toggle("on", power[id]); return; }
      if (a === "ago") {
        Hub.closeOverlay();
        for (const id of Object.keys(power)) if (power[id] && !Hub.useStock(id)) power[id] = false;
        newRound(); S.running = true; S.count = 1.8; Hub.sound("tap");
        return;
      }
      if (a === "apause") return pause(true);
      if (a === "aresume") return pause(false);
      if (a === "aquit") { S.paused = false; Hub.closeOverlay(); S.over = true; return end(); }
      if (a === "again") { Hub.closeOverlay(); newRound(); readyCard(); return; }
      if (a === "tomenu") { stop(); view = "menu"; nav = []; return render(); }
    });

    // For testing from the console: step the game without waiting for animation frames
    window.ArcadeDebug = {
      state: () => S,
      step(dt = 1 / 60, n = 1) { for (let i = 0; i < n && S && !S.over; i++) { if (S.count > 0) S.count = 0; step(dt); } ctx.clearRect(0, 0, dims.w, dims.h); G.draw(ctx, S, dims.w, dims.h, performance.now()); hud(); return S; },
      input: (name, down = true) => G.input(S, name, down),
      level: n => { mode = "level"; L = n; view = "game"; render(); },
    };

    Hub.start(CFG, (u, w) => { W = w; render(); });
  }

  // Small drawing helpers shared by the games
  const rr = (c, x, y, w, h, r) => { c.beginPath(); c.roundRect ? c.roundRect(x, y, w, h, r) : c.rect(x, y, w, h); };
  function glow(c, x, y, r, color, a = .5) { const g = c.createRadialGradient(x, y, 0, x, y, r); g.addColorStop(0, color.replace(")", `,${a})`).replace("rgb(", "rgba(")); g.addColorStop(1, "rgba(0,0,0,0)"); c.fillStyle = g; c.beginPath(); c.arc(x, y, r, 0, 7); c.fill(); }
  // A star field for space-themed stages (seeded once per size)
  function stars(n, w, h) { return Array.from({ length: n }, () => ({ x: Math.random() * w, y: Math.random() * h, r: Math.random() * 1.4 + .3, v: Math.random() * .8 + .2 })); }
  function drawStars(c, list, w, h, speed = 0, t = 0) {
    for (const s of list) { const y = (s.y + speed * s.v) % h; c.globalAlpha = .4 + .6 * Math.abs(Math.sin(t / 900 + s.x)); c.fillStyle = "#fff"; c.fillRect(s.x, y, s.r * 1.4, s.r * 1.4); }
    c.globalAlpha = 1;
  }
  function spaceBg(c, w, h, hue = 260) {
    const g = c.createLinearGradient(0, 0, 0, h); g.addColorStop(0, `hsl(${hue},55%,9%)`); g.addColorStop(1, `hsl(${hue + 30},60%,16%)`);
    c.fillStyle = g; c.fillRect(0, 0, w, h);
    const n = c.createRadialGradient(w * .7, h * .3, 0, w * .7, h * .3, w * .8); n.addColorStop(0, `hsla(${hue + 50},80%,55%,.22)`); n.addColorStop(1, "rgba(0,0,0,0)");
    c.fillStyle = n; c.fillRect(0, 0, w, h);
  }

  window.Arcade = { run, rr, glow, stars, drawStars, spaceBg };
})();
