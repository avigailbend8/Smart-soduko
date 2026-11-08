/* Mind Studio – shared core.
   The hub and every game load this file. It holds:
   - accounts and ONE shared login for the hub and all games (kept in this browser's localStorage),
   - a separate wallet for every game (its own coins, store items and progress),
   - the small UI pieces every game uses: login card, top bar, store, result card, toast, confetti and sounds. */
(function () {
  "use strict";

  const KEY = {
    users: "mindstudio.users",
    session: "mindstudio.session",
    sound: "mindstudio.sound",
    save: (user, game) => `mindstudio.save.${user}.${game}`,
  };
  const read = (k, fallback) => { try { const v = localStorage.getItem(k); return v ? JSON.parse(v) : fallback; } catch (e) { return fallback; } };
  const write = (k, v) => { try { localStorage.setItem(k, JSON.stringify(v)); } catch (e) {} };
  const esc = s => String(s).replace(/[&<>"']/g, c => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c]));
  const $ = (s, r = document) => r.querySelector(s);

  // The hub is index.html; games live in games/, so they reach the hub one folder up
  const inGame = /\/games\//.test(location.pathname);
  const HUB_URL = inGame ? "../index.html" : "index.html";
  const fromHub = path => (inGame ? "../" : "") + path;

  // ---------- Palettes (each game has its own signature one, and sells the others in its store) ----------
  const PALETTES = {
    terracotta: { paper: "#F6EEE6", paper2: "#ECD9C8", card: "#FFFBF6", ink: "#33251D", muted: "#8E7867", accent: "#B4664B", soft: "#F0DACB" },
    slate:      { paper: "#F2F1EC", paper2: "#DFE4E3", card: "#FFFFFD", ink: "#24313A", muted: "#76848B", accent: "#4F6D7A", soft: "#D8E2E5" },
    plum:       { paper: "#F4EFF3", paper2: "#E6DAE6", card: "#FFFCFE", ink: "#2F2433", muted: "#86748C", accent: "#7A5C99", soft: "#E6DAEE" },
    amber:      { paper: "#F7F0E3", paper2: "#EEDDBE", card: "#FFFBF2", ink: "#33291C", muted: "#8C7A5F", accent: "#B07A22", soft: "#F2E1BC" },
    sage:       { paper: "#F1F3EC", paper2: "#DCE4D5", card: "#FDFEFA", ink: "#25302A", muted: "#76867A", accent: "#5B8767", soft: "#DAE7DA" },
    rose:       { paper: "#F8EFEF", paper2: "#EFD8DB", card: "#FFFBFB", ink: "#3A2629", muted: "#917679", accent: "#BF6677", soft: "#F3D9DD" },
    ocean:      { paper: "#EEF3F5", paper2: "#D4E3E9", card: "#FBFDFE", ink: "#1F2E36", muted: "#6B838E", accent: "#2C7A91", soft: "#D3E7ED" },
    indigo:     { paper: "#EFF0F7", paper2: "#DADDF0", card: "#FCFCFF", ink: "#22253A", muted: "#737894", accent: "#5463B0", soft: "#DCE0F4" },
    coral:      { paper: "#FBF0EC", paper2: "#F3D9CF", card: "#FFFBF9", ink: "#3A2621", muted: "#937770", accent: "#D9654A", soft: "#F8DDD3" },
    night:      { paper: "#1B1A21", paper2: "#2B2734", card: "#25232D", ink: "#EEE8E0", muted: "#A39BAB", accent: "#D9A85B", soft: "#3A3446", line: "rgba(255,255,255,.13)", dark: true },
  };
  const PALETTE_NAMES = { terracotta: "טרקוטה", slate: "צפחה", plum: "שזיף", amber: "ענבר", sage: "מרווה", rose: "ורד עתיק", ocean: "אוקיינוס", indigo: "אינדיגו", coral: "אלמוג", night: "לילה" };
  function applyPalette(p) {
    const root = document.documentElement.style;
    root.setProperty("--paper", p.paper); root.setProperty("--paper-2", p.paper2); root.setProperty("--card", p.card);
    root.setProperty("--ink", p.ink); root.setProperty("--muted", p.muted); root.setProperty("--accent", p.accent);
    root.setProperty("--soft", p.soft); root.setProperty("--line", p.line || "rgba(40,30,25,.11)");
    root.setProperty("color-scheme", p.dark ? "dark" : "light");
    document.documentElement.classList.toggle("dark", !!p.dark);
  }
  // A store item for a palette: theme(id, price)
  const theme = (id, price) => ({ id: "theme-" + id, slot: "theme", name: PALETTE_NAMES[id], price, palette: PALETTES[id] });

  // ---------- The games in the studio ----------
  // cat: think = thinking games, board = board and card games, action = quick-reflex games
  const CATS = [
    { id: "think", title: "משחקי חשיבה", sub: "שקט, היגיון וסבלנות" },
    { id: "board", title: "לוח וקלפים", sub: "מול המחשב, בכמה רמות קושי" },
    { id: "action", title: "פעולה", sub: "תגובה מהירה ואצבעות זריזות" },
  ];
  const GAMES = [
    { id: "sudoku", cat: "think", title: "סודוקו חכם", tag: "מספרים · היגיון", desc: "המשחק הראשון של הסטודיו: 80 שלבים, ערכות נושא וחנות משלו.", url: "../Web/index.html", color: "#C9708F" },
    { id: "nonogram", cat: "think", title: "ציור נסתר", tag: "רמזים · ציור", desc: "פענחו את המספרים שבשוליים וגלו את הציור שמסתתר בלוח.", url: "games/nonogram.html", color: PALETTES.slate.accent },
    { id: "codebreaker", cat: "think", title: "פיצוח הצופן", tag: "הסקה · צבעים", desc: "נחשו את הצופן הסודי של אבני החן לפי הרמזים שמקבלים.", url: "games/codebreaker.html", color: PALETTES.plum.accent },
    { id: "lights", cat: "think", title: "גן הפנסים", tag: "תכנון · מהלכים", desc: "כל לחיצה מדליקה ומכבה גם את השכנים. הדליקו את כל הגן.", url: "games/lights.html", color: PALETTES.amber.accent },
    { id: "memory", cat: "think", title: "זיכרון", tag: "ריכוז · זוגות", desc: "הפכו קלפים ומצאו את כל הזוגות בכמה שפחות מהלכים.", url: "games/memory.html", color: PALETTES.sage.accent },
    { id: "chess", cat: "board", title: "שחמט", tag: "אסטרטגיה · לוח", desc: "שחמט מלא מול המחשב בשלוש רמות, עם הצרחה, הכאה דרך הילוכו והכתרה.", url: "games/chess.html", color: PALETTES.terracotta.accent },
    { id: "checkers", cat: "board", title: "דמקה", tag: "קפיצות · לוח", desc: "דמקה ישראלית: אכילה חובה, אכילה גם אחורה ומלכים שעפים לאורך האלכסון.", url: "games/checkers.html", color: PALETTES.ocean.accent },
    { id: "colors", cat: "board", title: "ארבעה צבעים", tag: "קלפים · התאמה", desc: "משחק קלפים מול 1–3 יריבים: התאימו צבע או מספר, ושחקו +2, עצור, שנה כיוון ופלוס.", url: "games/colors.html", color: PALETTES.rose.accent },
    { id: "racer", cat: "action", title: "כביש מהיר", tag: "מהירות · התחמקות", desc: "דהרו בכביש, התחמקו ממכוניות וממכשולים ואספו מטבעות. כמה רחוק תגיעו?", url: "games/racer.html", color: PALETTES.indigo.accent },
    { id: "runner", cat: "action", title: "ריצת הנתיבים", tag: "קפיצה · החלקה", desc: "שלושה נתיבים ודמות שלא עוצרת: קפצו, החליקו והחליפו נתיב כדי לעבור מכשולים.", url: "games/runner.html", color: PALETTES.coral.accent },
  ];

  // Cover art for the hub cards and the game menus. c = the game's color.
  function art(id, c = "var(--accent)") {
    const s = `xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 140" class="art" direction="ltr" aria-hidden="true"`;
    if (id === "sudoku") {
      const nums = { 0: 5, 4: 3, 8: 9, 2: 7, 6: 1 };
      return `<svg ${s}>${Array.from({ length: 9 }, (_, i) => { const x = 55 + (i % 3) * 32, y = 22 + Math.floor(i / 3) * 32; return `<rect x="${x}" y="${y}" width="28" height="28" rx="7" fill="${i % 2 ? c : "#fff"}" fill-opacity="${i % 2 ? .18 : .9}" stroke="${c}" stroke-opacity=".35"/>${nums[i] ? `<text x="${x + 14}" y="${y + 20}" text-anchor="middle" font-family="Frank Ruhl Libre, serif" font-size="17" fill="${c}">${nums[i]}</text>` : ""}`; }).join("")}</svg>`;
    }
    if (id === "nonogram") {
      const pic = [".#.#.", "#####", "#####", ".###.", "..#.."];
      return `<svg ${s}>${pic.map((row, r) => [...row].map((ch, k) => `<rect x="${70 + k * 18}" y="${30 + r * 18}" width="15" height="15" rx="3" fill="${ch === "#" ? c : "#fff"}" fill-opacity="${ch === "#" ? .85 : .8}" stroke="${c}" stroke-opacity=".25"/>`).join("")).join("")}
        ${["1 1", "5", "5", "3", "1"].map((t, r) => `<text x="62" y="${42 + r * 18}" text-anchor="end" font-size="10" font-family="Heebo, sans-serif" fill="${c}" opacity=".8">${t}</text>`).join("")}
        ${["2", "4", "4", "4", "2"].map((t, k) => `<text x="${77 + k * 18}" y="24" text-anchor="middle" font-size="10" font-family="Heebo, sans-serif" fill="${c}" opacity=".8">${t}</text>`).join("")}</svg>`;
    }
    if (id === "codebreaker") {
      const gems = ["#C2414B", "#3466B0", "#E0A030", "#3E9A6B"];
      return `<svg ${s}><rect x="30" y="38" width="140" height="64" rx="32" fill="#fff" fill-opacity=".85" stroke="${c}" stroke-opacity=".25"/>${gems.map((g, i) => `<circle cx="${62 + i * 28}" cy="70" r="11" fill="${g}"/><circle cx="${59 + i * 28}" cy="66" r="3.5" fill="#fff" opacity=".55"/>`).join("")}
        ${[0, 1, 2, 3].map(i => `<circle cx="${80 + i * 13}" cy="118" r="4.5" fill="${i < 2 ? c : "none"}" stroke="${c}" stroke-width="1.6"/>`).join("")}
        <path d="M100 18l8 10h-16z" fill="${c}" opacity=".6"/></svg>`;
    }
    if (id === "lights") {
      const on = [1, 0, 1, 1, 1, 0, 0, 1, 1];
      return `<svg ${s}><defs><radialGradient id="glow-${id}"><stop offset="0" stop-color="#FFE7A8"/><stop offset=".55" stop-color="${c}"/><stop offset="1" stop-color="${c}" stop-opacity="0"/></radialGradient></defs>
        ${on.map((v, i) => { const x = 70 + (i % 3) * 30, y = 40 + Math.floor(i / 3) * 30; return v ? `<circle cx="${x}" cy="${y}" r="16" fill="url(#glow-${id})" opacity=".55"/><circle cx="${x}" cy="${y}" r="8" fill="#FFE3A0" stroke="${c}"/>` : `<circle cx="${x}" cy="${y}" r="8" fill="#fff" stroke="${c}" stroke-opacity=".4"/>`; }).join("")}</svg>`;
    }
    if (id === "memory") {
      return `<svg ${s}><g transform="rotate(-9 80 70)"><rect x="52" y="26" width="58" height="84" rx="10" fill="${c}" fill-opacity=".85"/><path d="M60 34h42v68H60z" fill="none" stroke="#fff" stroke-opacity=".45" stroke-dasharray="3 4"/></g>
        <g transform="rotate(8 125 70)"><rect x="96" y="26" width="58" height="84" rx="10" fill="#fff" stroke="${c}" stroke-opacity=".3"/><text x="125" y="78" text-anchor="middle" font-size="30">🌸</text></g></svg>`;
    }
    if (id === "chess") {
      const cells = Array.from({ length: 12 }, (_, i) => { const col = i % 6, row = Math.floor(i / 6); return `<rect x="${40 + col * 20}" y="${96 + row * 16}" width="20" height="16" fill="${(col + row) % 2 ? c : "#fff"}" fill-opacity="${(col + row) % 2 ? .55 : .9}"/>`; }).join("");
      const glyph = (x, ch, fill) => `<text x="${x}" y="100" text-anchor="middle" font-size="62" font-family="Segoe UI Symbol, Noto Sans Symbols 2, DejaVu Sans, serif" fill="${fill}" stroke="${c}" stroke-width="1.6">${ch}︎</text>`;
      return `<svg ${s}>${cells}<rect x="40" y="96" width="120" height="32" fill="none" stroke="${c}" stroke-opacity=".45"/>${glyph(80, "♚", c)}${glyph(122, "♞", "#fff")}</svg>`;
    }
    if (id === "checkers") {
      let out = "";
      for (let r = 0; r < 4; r++) for (let k = 0; k < 6; k++) {
        const x = 40 + k * 20, y = 30 + r * 20, dark = (r + k) % 2;
        out += `<rect x="${x}" y="${y}" width="20" height="20" fill="${dark ? c : "#fff"}" fill-opacity="${dark ? .35 : .9}"/>`;
        if (dark && (r === 0 || r === 3 || (r === 2 && k === 1))) out += `<circle cx="${x + 10}" cy="${y + 10}" r="7.5" fill="${r < 2 ? c : "#fff"}" stroke="${c}" stroke-width="1.3"/><circle cx="${x + 10}" cy="${y + 10}" r="4.2" fill="none" stroke="${r < 2 ? "#fff" : c}" stroke-opacity=".6"/>`;
      }
      return `<svg ${s}>${out}<rect x="40" y="30" width="120" height="80" rx="2" fill="none" stroke="${c}" stroke-opacity=".45"/></svg>`;
    }
    if (id === "colors") {
      const card = (x, rot, col, txt) => `<g transform="rotate(${rot} ${x + 25} 118)"><rect x="${x}" y="30" width="50" height="76" rx="9" fill="${col}"/><rect x="${x + 5}" y="35" width="40" height="66" rx="6" fill="none" stroke="#fff" stroke-opacity=".5"/><text x="${x + 25}" y="80" text-anchor="middle" font-family="Frank Ruhl Libre, serif" font-size="28" fill="#fff">${txt}</text></g>`;
      return `<svg ${s}>${card(48, -16, "#3C78B0", "7")}${card(75, 0, "#E0A43A", "+2")}${card(102, 16, c, "⇄")}</svg>`;
    }
    if (id === "racer") {
      return `<svg ${s}><path d="M80 6h40l42 128H38z" fill="${c}" fill-opacity=".88"/><path d="M80 6L38 134M120 6l42 128" stroke="#fff" stroke-opacity=".75" stroke-width="2"/>
        ${[0, 1, 2, 3, 4].map(i => `<rect x="${99 - i * .3}" y="${12 + i * 25}" width="${2 + i * .6}" height="${8 + i * 3}" fill="#fff" opacity=".8"/>`).join("")}
        <rect x="70" y="42" width="15" height="24" rx="4" fill="#E0A43A"/><rect x="73" y="46" width="9" height="5" rx="2" fill="#fff" opacity=".6"/>
        <rect x="112" y="88" width="26" height="42" rx="8" fill="#fff"/><rect x="116" y="96" width="18" height="10" rx="3" fill="${c}" opacity=".7"/><rect x="116" y="118" width="18" height="6" rx="2" fill="${c}" opacity=".35"/></svg>`;
    }
    if (id === "runner") {
      return `<svg ${s}><path d="M88 16h24l58 118H30z" fill="${c}" fill-opacity=".2"/><path d="M96 16L72 134M104 16l24 118" stroke="${c}" stroke-opacity=".5" stroke-width="2"/><path d="M88 16L30 134M112 16l58 118" stroke="${c}" stroke-width="2.5"/>
        ${[[100, 34, 3], [100, 50, 4], [100, 70, 5.5]].map(([x, y, r]) => `<circle cx="${x}" cy="${y}" r="${r}" fill="#D9B154" stroke="#B48A2C"/>`).join("")}
        <rect x="58" y="66" width="24" height="16" rx="3" fill="${c}" opacity=".8"/><rect x="124" y="50" width="16" height="11" rx="2" fill="${c}" opacity=".6"/>
        <ellipse cx="100" cy="128" rx="17" ry="4" fill="${c}" opacity=".25"/><circle cx="100" cy="106" r="15" fill="${c}"/><circle cx="95" cy="102" r="2.4" fill="#fff"/><circle cx="105" cy="102" r="2.4" fill="#fff"/></svg>`;
    }
    return "";
  }

  // ---------- Accounts and the shared session ----------
  async function digest(text) {
    if (window.crypto?.subtle) {
      const buf = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(text));
      return [...new Uint8Array(buf)].map(b => b.toString(16).padStart(2, "0")).join("");
    }
    let h = 2166136261; for (const ch of text) { h ^= ch.codePointAt(0); h = Math.imul(h, 16777619) >>> 0; }
    return "f" + h.toString(16);
  }
  const userKey = name => name.trim().toLowerCase();
  const allUsers = () => read(KEY.users, {});
  const AVATARS = ["🦊", "🦉", "🐼", "🐢", "🦋", "🌸", "🌙", "⭐", "🐱", "🐰", "🦄", "🐳"];

  async function register(name, pass, avatar) {
    name = name.trim();
    if (name.length < 2) return "השם צריך להיות לפחות 2 תווים";
    if (pass.length < 4) return "הסיסמה צריכה להיות לפחות 4 תווים";
    const users = allUsers();
    if (users[userKey(name)]) return "השם הזה כבר רשום — אפשר להתחבר אליו";
    const salt = Math.random().toString(36).slice(2, 12);
    users[userKey(name)] = { name, avatar: avatar || AVATARS[0], salt, hash: await digest(salt + pass), created: Date.now() };
    write(KEY.users, users);
    startSession(users[userKey(name)]);
    return "";
  }
  async function login(name, pass) {
    const u = allUsers()[userKey(name)];
    if (!u || u.hash !== await digest(u.salt + pass)) return "השם או הסיסמה לא נכונים";
    startSession(u);
    return "";
  }
  function startSession(u) { write(KEY.session, { key: userKey(u.name), name: u.name, avatar: u.avatar, at: Date.now() }); }
  function currentUser() {
    const s = read(KEY.session, null);
    const u = s && allUsers()[s.key];
    return u ? { key: s.key, name: u.name, avatar: u.avatar } : null;
  }
  function logout() { try { localStorage.removeItem(KEY.session); } catch (e) {} }
  function setAvatar(a) {
    const s = currentUser(); if (!s) return;
    const users = allUsers(); users[s.key].avatar = a; write(KEY.users, users); startSession(users[s.key]);
  }
  // Logging in or out in another tab (the hub, another game) is followed here too
  window.addEventListener("storage", e => { if (e.key === KEY.session) location.reload(); });

  // The login card, used by the hub and by any game opened on its own
  function loginCard(container, { title, subtitle, artId, color }, done) {
    let mode = "login", avatar = AVATARS[Math.floor(Math.random() * AVATARS.length)];
    const draw = (err = "") => {
      container.innerHTML = `<main class="login-wrap rise">
        ${artId ? `<div class="login-art">${art(artId, color)}</div>` : ""}
        <p class="eyebrow">Mind Studio · סטודיו המשחקים</p>
        <h1 class="display">${esc(title)}</h1>
        <p class="lede">${esc(subtitle)}</p>
        <form class="card stack" id="loginForm" novalidate>
          <div class="seg" role="tablist">
            <button type="button" role="tab" class="${mode === "login" ? "on" : ""}" data-mode="login" aria-selected="${mode === "login"}">התחברות</button>
            <button type="button" role="tab" class="${mode === "register" ? "on" : ""}" data-mode="register" aria-selected="${mode === "register"}">חשבון חדש</button>
          </div>
          <label class="field"><span>שם משתמש</span><input id="lgName" type="text" maxlength="20" autocomplete="username" required></label>
          <label class="field"><span>סיסמה</span><input id="lgPass" type="password" maxlength="40" autocomplete="${mode === "login" ? "current-password" : "new-password"}" required></label>
          ${mode === "register" ? `<div class="field"><span>הדמות שלך</span><div class="avatars">${AVATARS.map(a => `<button type="button" class="avatar-pick ${a === avatar ? "on" : ""}" data-avatar="${a}" aria-label="${a}">${a}</button>`).join("")}</div></div>` : ""}
          <p class="err" role="alert">${esc(err)}</p>
          <button class="btn primary wide" type="submit">${mode === "login" ? "כניסה" : "יצירת חשבון"}</button>
        </form>
        <p class="fine">חשבון אחד לכל המשחקים בסטודיו. הוא נשמר בדפדפן הזה בלבד.</p>
      </main>`;
      $("#lgName", container).focus();
    };
    container.onclick = e => {
      const m = e.target.closest("[data-mode]"); if (m) { mode = m.dataset.mode; return draw(); }
      const a = e.target.closest("[data-avatar]"); if (a) { avatar = a.dataset.avatar; container.querySelectorAll(".avatar-pick").forEach(b => b.classList.toggle("on", b === a)); }
    };
    container.onsubmit = async e => {
      e.preventDefault();
      const name = $("#lgName", container).value, pass = $("#lgPass", container).value;
      const err = mode === "login" ? await login(name, pass) : await register(name, pass, avatar);
      if (err) { const keep = name; draw(err); $("#lgName", container).value = keep; $("#lgPass", container).focus(); return; }
      container.onclick = container.onsubmit = null;
      done(currentUser());
    };
    draw();
  }

  // ---------- The current game and its wallet ----------
  let game = null, user = null, W = null, storeMsg = "";
  const coinSvg = `<svg class="coin-ico" viewBox="0 0 20 20" aria-hidden="true"><circle cx="10" cy="10" r="9" fill="#D9B154"/><circle cx="10" cy="10" r="6.4" fill="none" stroke="#B48A2C" stroke-width="1.3"/><path d="M10 6.2l1.1 2.4 2.6.3-1.9 1.8.5 2.6L10 12l-2.3 1.3.5-2.6-1.9-1.8 2.6-.3z" fill="#FFF3CF"/></svg>`;

  function freshWallet(cfg) {
    const equipped = {};
    for (const it of cfg.items) if (it.slot && !(it.slot in equipped)) equipped[it.slot] = it.id; // first item of a slot is the free default
    return {
      coins: cfg.startCoins ?? 60,
      owned: Object.values(equipped),
      equipped,
      stock: { ...(cfg.startStock || {}) },
      progress: {},
    };
  }
  function loadWallet(u, cfg) {
    const fresh = freshWallet(cfg), w = read(KEY.save(u.key, cfg.id), null) || fresh;
    for (const k of Object.keys(fresh)) if (w[k] === undefined) w[k] = fresh[k];
    for (const [slot, id] of Object.entries(fresh.equipped)) if (!w.equipped[slot]) w.equipped[slot] = id;
    return w;
  }
  const save = () => { if (user && game) write(KEY.save(user.key, game.id), W); };
  const coinsOf = (u, gameId) => {
    if (gameId === "sudoku") { const s = read("smartSudoku.v1", null); return s?.profiles?.[u.name]?.coins ?? null; }
    return read(KEY.save(u.key, gameId), null)?.coins ?? null;
  };
  const refreshCoins = () => document.querySelectorAll("[data-coins]").forEach(el => { el.textContent = W.coins; });

  function earn(n) { if (n <= 0) return; W.coins += n; save(); refreshCoins(); sound("coin"); }
  function spend(n) { if (W.coins < n) return false; W.coins -= n; save(); refreshCoins(); return true; }
  const stock = id => W.stock[id] || 0;
  function useStock(id) { if (!W.stock[id]) return false; W.stock[id]--; save(); return true; }
  const equipped = slot => game.items.find(i => i.id === W.equipped[slot]);
  const owns = id => W.owned.includes(id);
  function applyTheme() { const t = equipped("theme"); applyPalette(t?.palette || PALETTES.terracotta); }

  // Starts a game page: shows the login card when nobody is logged in, then calls ready(user, wallet)
  function start(cfg, ready) {
    game = cfg;
    document.title = cfg.title + " · Mind Studio";
    applyPalette(cfg.items.find(i => i.slot === "theme")?.palette || PALETTES.terracotta);
    const go = u => { user = u; W = loadWallet(u, cfg); save(); applyTheme(); ready(u, W); };
    const u = currentUser();
    if (u) return go(u);
    const g = GAMES.find(x => x.id === cfg.id);
    loginCard($("#app"), { title: cfg.title, subtitle: "כדי לשחק צריך להתחבר לחשבון הסטודיו", artId: cfg.id, color: g?.color }, go);
  }

  // ---------- Shared UI ----------
  function topbar(title, { back = true } = {}) {
    return `<header class="topbar">
      ${back ? `<button class="icon-btn" data-act="back" aria-label="חזרה">→</button>` : `<a class="icon-btn" href="${HUB_URL}" aria-label="חזרה לסטודיו" title="חזרה לסטודיו">⌂</a>`}
      <h1>${title}</h1>
      <span class="chip" title="המטבעות של המשחק הזה">${coinSvg}<b data-coins>${W.coins}</b></span>
    </header>`;
  }

  // The game's front page: cover art, greeting and the main buttons [act, label, primary?]
  function menu({ tagline, buttons }) {
    const g = GAMES.find(x => x.id === game.id);
    return `<main class="menu rise">
      <div class="menu-top"><a class="pill-link" href="${HUB_URL}">⌂ לסטודיו</a><span class="chip">${coinSvg}<b data-coins>${W.coins}</b></span></div>
      <div class="menu-art">${art(game.id, "var(--accent)")}</div>
      <p class="eyebrow">${esc(g?.tag || "")}</p>
      <h1 class="display">${esc(game.title)}</h1>
      <p class="lede">${tagline}</p>
      <p class="hello">${user.avatar} שלום, ${esc(user.name)}</p>
      <div class="menu-buttons">${buttons.map(([act, label, primary]) => `<button class="btn ${primary ? "primary big" : ""}" data-act="${act}">${label}</button>`).join("")}</div>
    </main>`;
  }

  // The store: groups = [{ slot, title }] or [{ consumable: true, title }]; preview(item) draws an item
  function store() {
    const groups = game.storeGroups.map(gr => {
      const items = game.items.filter(i => gr.consumable ? i.consumable : i.slot === gr.slot);
      return `<section class="store-group"><h2>${gr.title}</h2><div class="shop">${items.map(it => {
        const own = !it.consumable && owns(it.id), on = !it.consumable && W.equipped[it.slot] === it.id;
        const pv = it.palette ? swatch(it.palette) : (game.preview ? game.preview(it) : "");
        const label = it.consumable ? `${coinSvg}${it.price}` : on ? "בשימוש ✓" : own ? "לבחור" : it.price ? `${coinSvg}${it.price}` : "חינם";
        return `<article class="item ${on ? "on" : ""}">
          <div class="pv">${pv}</div>
          <h3>${esc(it.name)}</h3>
          ${it.consumable ? `<p class="small">${esc(it.desc)} · יש לך ${stock(it.stockId)}</p>` : ""}
          <button class="btn small ${on ? "" : own ? "" : "primary"}" data-act="buy" data-item="${it.id}" ${on ? "disabled" : ""}>${label}</button>
        </article>`;
      }).join("")}</div></section>`;
    }).join("");
    return `${topbar("החנות")}
      <p class="lede center">כל משחק בסטודיו הוא עולם בפני עצמו — המטבעות והקניות כאן שייכים רק ל${esc(game.title)}.</p>
      <p class="store-msg" role="status">${esc(storeMsg)}</p>${groups}`;
  }
  const swatch = p => `<div class="swatch" style="background:${p.paper}"><i style="background:${p.accent}"></i><i style="background:${p.soft}"></i><i style="background:${p.ink}"></i></div>`;
  function buy(id) {
    const it = game.items.find(i => i.id === id); if (!it) return;
    if (it.consumable) {
      if (!spend(it.price)) { storeMsg = "אין מספיק מטבעות — אפשר להרוויח עוד במשחק"; sound("wrong"); return; }
      W.stock[it.stockId] = stock(it.stockId) + it.amount; storeMsg = `קנית ${it.name}!`;
    } else {
      if (!owns(it.id)) {
        if (!spend(it.price)) { storeMsg = "אין מספיק מטבעות — אפשר להרוויח עוד במשחק"; sound("wrong"); return; }
        W.owned.push(it.id); storeMsg = `"${it.name}" שלך עכשיו ✨`;
      } else storeMsg = `עברת ל"${it.name}"`;
      W.equipped[it.slot] = it.id;
    }
    sound("coin"); save(); applyTheme();
  }
  const clearStoreMsg = () => { storeMsg = ""; };

  // A modal card on top of the page
  function overlay(html) {
    let layer = $("#layer"); if (!layer) { layer = document.createElement("div"); layer.id = "layer"; document.body.append(layer); }
    layer.innerHTML = `<div class="overlay" role="dialog" aria-modal="true"><div class="card modal rise">${html}</div></div>`;
    layer.querySelector("button")?.focus();
  }
  const closeOverlay = () => { const l = $("#layer"); if (l) l.innerHTML = ""; };
  const overlayOpen = () => !!$("#layer")?.innerHTML;

  // The end-of-round card. buttons = [[act, label, primary?]]
  function result({ title, sub = "", stars = null, coins = 0, buttons }) {
    overlay(`${stars !== null ? `<div class="stars" aria-label="${stars} כוכבים">${[0, 1, 2].map(i => `<span class="${i < stars ? "on" : ""}" style="animation-delay:${.15 + i * .18}s">★</span>`).join("")}</div>` : ""}
      <h2 class="display sm">${title}</h2>
      ${sub ? `<div class="lede">${sub}</div>` : ""}
      ${coins ? `<p class="earned">+${coins} ${coinSvg}</p>` : ""}
      <div class="stack">${buttons.map(([act, label, primary]) => `<button class="btn wide ${primary ? "primary" : ""}" data-act="${act}">${label}</button>`).join("")}</div>`);
  }

  function toast(msg) {
    let t = $("#toast"); if (!t) { t = document.createElement("div"); t.id = "toast"; t.setAttribute("role", "status"); document.body.append(t); }
    t.textContent = msg; t.classList.remove("show"); void t.offsetWidth; t.classList.add("show");
    clearTimeout(t._h); t._h = setTimeout(() => t.classList.remove("show"), 2200);
  }

  function confetti() {
    if (matchMedia("(prefers-reduced-motion: reduce)").matches) return;
    const cv = document.createElement("canvas"); cv.className = "confetti"; document.body.append(cv);
    const ctx = cv.getContext("2d"), dpr = devicePixelRatio || 1;
    cv.width = innerWidth * dpr; cv.height = innerHeight * dpr; ctx.scale(dpr, dpr);
    const css = getComputedStyle(document.documentElement);
    const colors = [css.getPropertyValue("--accent"), "#D9B154", css.getPropertyValue("--soft"), "#ffffff"];
    const parts = Array.from({ length: 130 }, () => ({ x: innerWidth / 2 + (Math.random() - .5) * 200, y: innerHeight * .35, vx: (Math.random() - .5) * 11, vy: -Math.random() * 11 - 3, r: Math.random() * 6 + 3, a: Math.random() * 6, va: (Math.random() - .5) * .3, c: colors[Math.floor(Math.random() * colors.length)] }));
    const t0 = performance.now();
    (function frame(t) {
      ctx.clearRect(0, 0, innerWidth, innerHeight);
      for (const p of parts) { p.vy += .28; p.vx *= .99; p.x += p.vx; p.y += p.vy; p.a += p.va; ctx.save(); ctx.translate(p.x, p.y); ctx.rotate(p.a); ctx.fillStyle = p.c; ctx.fillRect(-p.r / 2, -p.r / 4, p.r, p.r / 2); ctx.restore(); }
      if (t - t0 < 2600) requestAnimationFrame(frame); else cv.remove();
    })(t0);
  }

  // Sounds made in the browser (no files). One on/off switch for the whole studio.
  let audio = null;
  const soundOn = () => read(KEY.sound, true);
  const toggleSound = () => { write(KEY.sound, !soundOn()); return soundOn(); };
  function sound(kind) {
    if (!soundOn()) return;
    try {
      audio = audio || new (window.AudioContext || window.webkitAudioContext)();
      const notes = {
        tap: [[880, 0, .06]], flip: [[620, 0, .07], [930, .03, .07]],
        good: [[1046.5, 0, .16], [1568, .06, .2]], wrong: [[311, 0, .16], [233, .09, .22]],
        coin: [[1975.5, 0, .09], [2637, .06, .26]], hint: [[1318.5, 0, .13], [1760, .07, .13], [2093, .14, .22]],
        move: [[440, 0, .07], [660, .03, .07]], card: [[1400, 0, .035], [900, .02, .05]],
        jump: [[523.25, 0, .1], [880, .05, .14]], crash: [[196, 0, .3], [131, .06, .45], [98, .12, .5]],
        lose: [[392, 0, .28], [311, .18, .3], [261.6, .36, .5]],
        win: [[523.25, 0, .3], [659.25, .12, .3], [783.99, .24, .3], [1046.5, .36, .6], [1318.5, .36, .6]],
      }[kind];
      const t0 = audio.currentTime;
      for (const [f, at, len] of notes) {
        const o = audio.createOscillator(), g = audio.createGain();
        o.type = "sine"; o.frequency.value = f;
        g.gain.setValueAtTime(0, t0 + at); g.gain.linearRampToValueAtTime(.14, t0 + at + .005); g.gain.exponentialRampToValueAtTime(.001, t0 + at + len);
        o.connect(g).connect(audio.destination); o.start(t0 + at); o.stop(t0 + at + len + .05);
      }
    } catch (e) {}
  }

  const shuffle = a => { for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; };
  const fmtTime = s => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;

  window.Hub = {
    // accounts
    currentUser, logout, setAvatar, loginCard, AVATARS,
    // games and art
    GAMES, CATS, art, fromHub, HUB_URL, PALETTES, applyPalette, theme, coinsOf, coinSvg,
    // current game
    start, save, earn, spend, stock, useStock, equipped, owns,
    get wallet() { return W; }, get user() { return user; },
    // UI
    topbar, menu, store, buy, clearStoreMsg, overlay, closeOverlay, overlayOpen, result, toast, confetti,
    sound, soundOn, toggleSound,
    // helpers
    esc, $, shuffle, fmtTime,
  };
})();
