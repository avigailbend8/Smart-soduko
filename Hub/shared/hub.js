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
  { const l = document.createElement("link"); l.rel = "stylesheet"; l.href = fromHub("shared/neon.css"); document.head.append(l); }

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
    // thinking
    { id: "merge", cat: "think", emo: "🔢", title: "מיזוג", tag: "מספרים · מיזוג", desc: "מחליקים אריחים ומאחדים זוגות זהים עד שמגיעים לאריח היעד.", url: "games/merge.html", color: "#E0913A" },
    { id: "mines", cat: "think", emo: "💣", title: "שדה מוקשים", tag: "הסקה · זהירות", desc: "המספרים מגלים כמה מוקשים מסביב. פתחו את כל המשבצות הבטוחות.", url: "games/mines.html", color: "#4E9A6A" },
    { id: "sort", cat: "think", emo: "🧪", title: "מיון צבעים", tag: "מיון · תכנון", desc: "שופכים צבעים ממבחנה למבחנה עד שכל מבחנה בצבע אחד.", url: "games/sort.html", color: "#D0567A" },
    { id: "slide", cat: "think", emo: "🖼️", title: "פאזל הזזה", tag: "סידור · מרחב", desc: "הזיזו חתיכות אל המשבצת הריקה והחזירו את התמונה לשלמותה.", url: "games/slide.html", color: "#4F6D9A" },
    { id: "towers", cat: "think", emo: "🗼", title: "מגדלים", tag: "רקורסיה · סבלנות", desc: "העבירו מגדל שלם, דיסק אחרי דיסק, בלי להניח גדול על קטן.", url: "games/towers.html", color: "#B4664B" },
    // board and cards
    { id: "connect4", cat: "board", emo: "🔴", title: "ארבע בשורה", tag: "דיסקיות · מול AI", desc: "מפילים דיסקיות לטורים. ארבע ברצף מנצחות.", url: "games/connect4.html", color: "#2F5FD0" },
    { id: "reversi", cat: "board", emo: "⚫", title: "רברסי", tag: "הפיכות · מול AI", desc: "לוכדים את אבני היריב ביניכם והופכים אותן לצבע שלכם.", url: "games/reversi.html", color: "#2E7D5B" },
    { id: "gomoku", cat: "board", emo: "⚪", title: "חמש בשורה", tag: "איומים · מול AI", desc: "חמש אבנים ברצף על לוח 13×13 — תקפו וגם חסמו.", url: "games/gomoku.html", color: "#B07A22" },
    { id: "mancala", cat: "board", emo: "🪨", title: "מנקלה", tag: "גומות · מול AI", desc: "משחק אבנים עתיק: מפזרים, לוכדים וצוברים במחסן.", url: "games/mancala.html", color: "#8B5A3C" },
    { id: "battleship", cat: "board", emo: "🚢", title: "צוללות", tag: "ציים · מול AI", desc: "יורים בתורות על הים של היריב ומטביעים את כל הצי שלו.", url: "games/battleship.html", color: "#1F6F9B" },
    { id: "ultimate", cat: "board", emo: "❌", title: "איקס עיגול על", tag: "9 לוחות · מול AI", desc: "איקס-עיגול בתוך איקס-עיגול. כל מהלך שולח את היריב ללוח אחר.", url: "games/ultimate.html", color: "#5463B0" },
    { id: "blackjack", cat: "board", emo: "🃏", title: "21", tag: "קלפים · מול הדילר", desc: "מתקרבים ל-21 בלי לעבור. ההימור מהארנק, והניצחון כפול.", url: "games/blackjack.html", color: "#BF6677" },
    // action
    { id: "snake", cat: "action", emo: "🐍", title: "נחש הכוכבים", tag: "זחילה · גדילה", desc: "הנחש אוכל כוכבים וגדל. אל תיתקעו בקיר או בזנב.", url: "games/snake.html", color: "#1FA383" },
    { id: "breakout", cat: "action", emo: "🧱", title: "שובר הלבנים", tag: "כדור · מחבט", desc: "מחזירים את הכדור ושוברים קירות של לבנים צבעוניות.", url: "games/breakout.html", color: "#C25C9A" },
    { id: "shooter", cat: "action", emo: "🚀", title: "מגיני הגלקסיה", tag: "חלל · יריות", desc: "גלים של חייזרים יורדים מהכוכבים. החללית יורה, אתם מתחמקים.", url: "games/shooter.html", color: "#6B4FD8" },
    { id: "wings", cat: "action", emo: "🐦", title: "כנפיים", tag: "מעוף · תזמון", desc: "לחיצה = משק כנפיים. עברו בין העמודים בלי לגעת.", url: "games/wings.html", color: "#2F9BD8" },
    { id: "blocks", cat: "action", emo: "🟪", title: "מגדל הבלוקים", tag: "צורות · שורות", desc: "צורות נופלות — סובבו, הזיזו ומלאו שורות שלמות.", url: "games/blocks.html", color: "#8E5CF5" },
    { id: "pong", cat: "action", emo: "🏓", title: "פונג גלקטי", tag: "מחבטים · מול AI", desc: "פינג-פונג ניאון מול ה-AI. הראשון ל-7 מנצח.", url: "games/pong.html", color: "#E0569A" },
    { id: "jumper", cat: "action", emo: "🐸", title: "קפיצה לשמיים", tag: "קפיצות · גובה", desc: "קופצים ממשטח למשטח, מהעננים ועד הגלקסיה.", url: "games/jumper.html", color: "#4CB86B" },
    { id: "catcher", cat: "action", emo: "⭐", title: "גשם כוכבים", tag: "תפיסה · רצפים", desc: "תופסים כוכבים נופלים ומתחמקים משביטים.", url: "games/catcher.html", color: "#D9A03A" },
  ];

  // ---------- Each game's own neon identity ----------
  // n1/n2: two glowing colours · font: a decorative Hebrew display font · btn: the shape of its buttons
  // shop: the name of its store · icon: the store's sign
  const LOOKS = {
    nonogram: { n1: "#4FD1FF", n2: "#7B61FF", font: "Rubik Iso", btn: "cut", shop: "סטודיו הציור", icon: "🖌️" },
    codebreaker: { n1: "#FF4FD8", n2: "#7B2FFF", font: "Rubik Glitch", btn: "glass", shop: "כספת הצפנים", icon: "🔐" },
    lights: { n1: "#FFC94F", n2: "#FF7A2F", font: "Secular One", btn: "pill", shop: "שוק הפנסים", icon: "🏮" },
    memory: { n1: "#4FFFB0", n2: "#2FB4FF", font: "Varela Round", btn: "soft", shop: "ארון הקלפים", icon: "🗄️" },
    chess: { n1: "#FFB347", n2: "#FF4F6D", font: "Suez One", btn: "cut", shop: "אולם המלכים", icon: "♛" },
    checkers: { n1: "#2FE6FF", n2: "#2F7BFF", font: "Rubik Doodle Shadow", btn: "arcade", shop: "נמל הדמקה", icon: "⚓" },
    colors: { n1: "#FF4F81", n2: "#FFD24F", font: "Rubik Bubbles", btn: "arcade", shop: "דוכן הקלפים", icon: "🎴" },
    racer: { n1: "#00F0FF", n2: "#FF2CDF", font: "Karantina", btn: "cut", shop: "מוסך הניאון", icon: "🏎️" },
    runner: { n1: "#FF7A2F", n2: "#FFE14F", font: "Rubik Spray Paint", btn: "arcade", shop: "חנות הספורט", icon: "👟" },
    merge: { n1: "#FFB02F", n2: "#FF4F9B", font: "Fredoka", btn: "soft", shop: "בית המספרים", icon: "🔢" },
    mines: { n1: "#7CFF4F", n2: "#00E0A0", font: "Rubik Maze", btn: "outline", shop: "מחסן הציוד", icon: "🧰" },
    sort: { n1: "#FF5FB0", n2: "#5FD4FF", font: "Rubik Puddles", btn: "pill", shop: "המעבדה", icon: "⚗️" },
    slide: { n1: "#5F9BFF", n2: "#B05FFF", font: "Bellefair", btn: "glass", shop: "הגלריה", icon: "🖼️" },
    towers: { n1: "#FF8A3D", n2: "#FFD23D", font: "Rubik Lines", btn: "cut", shop: "בית המלאכה", icon: "🛠️" },
    connect4: { n1: "#FF3D5A", n2: "#FFD23D", font: "Rubik Moonrocks", btn: "arcade", shop: "חנות הדיסקיות", icon: "🔴" },
    reversi: { n1: "#3DFFB4", n2: "#3D8BFF", font: "Rubik Marker Hatch", btn: "outline", shop: "סלון הרברסי", icon: "♟️" },
    gomoku: { n1: "#FFD23D", n2: "#FF6B3D", font: "Amatic SC", btn: "soft", shop: "בית התה", icon: "🍵" },
    mancala: { n1: "#FF9F43", n2: "#E84393", font: "Rubik Wet Paint", btn: "pill", shop: "שוק האבנים", icon: "🪨" },
    battleship: { n1: "#00D2FF", n2: "#3A7BD5", font: "Rubik Burned", btn: "cut", shop: "הנמל הצבאי", icon: "⚓" },
    ultimate: { n1: "#FF4D6D", n2: "#4D7CFF", font: "Rubik Vinyl", btn: "outline", shop: "מרתף המשחקים", icon: "🎲" },
    blackjack: { n1: "#FFD700", n2: "#FF4757", font: "Noto Serif Hebrew", btn: "glass", shop: "הקזינו", icon: "🎰" },
    snake: { n1: "#39FF14", n2: "#00F0FF", font: "Rubik Glitch", btn: "arcade", shop: "קן הנחשים", icon: "🐍" },
    breakout: { n1: "#FF2CDF", n2: "#FFF01F", font: "Rubik Beastly", btn: "cut", shop: "מפעל הלבנים", icon: "🏭" },
    shooter: { n1: "#7B61FF", n2: "#00F0FF", font: "Rubik Microbe", btn: "glass", shop: "תחנת החלל", icon: "🛰️" },
    wings: { n1: "#4FC3FF", n2: "#FFE14F", font: "Fredoka", btn: "soft", shop: "קן הציפורים", icon: "🪺" },
    blocks: { n1: "#B05FFF", n2: "#FF4FD8", font: "Rubik Iso", btn: "arcade", shop: "מחסן הצורות", icon: "🧩" },
    pong: { n1: "#FF2CDF", n2: "#00F0FF", font: "Karantina", btn: "outline", shop: "מועדון הפונג", icon: "🏓" },
    jumper: { n1: "#4CFF8F", n2: "#4FC3FF", font: "Rubik Bubbles", btn: "pill", shop: "חנות הקופצים", icon: "🌀" },
    catcher: { n1: "#FFD23D", n2: "#B05FFF", font: "Amatic SC", btn: "glass", shop: "חנות הכוכבים", icon: "🌟" },
  };
  const loadedFonts = new Set();
  function loadFont(family) {
    if (!family || loadedFonts.has(family)) return; loadedFonts.add(family);
    const l = document.createElement("link"); l.rel = "stylesheet";
    l.href = `https://fonts.googleapis.com/css2?family=${family.replace(/ /g, "+")}:wght@400;700&display=swap`;
    document.head.append(l);
  }
  function applyLook(id) {
    const lk = LOOKS[id]; if (!lk) return;
    loadFont(lk.font);
    const r = document.documentElement;
    r.style.setProperty("--n1", lk.n1); r.style.setProperty("--n2", lk.n2);
    r.style.setProperty("--gfont", `"${lk.font}", var(--display)`);
    r.dataset.look = lk.btn; r.classList.add("neon");
  }

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
    const g = GAMES.find(x => x.id === id), lk = LOOKS[id];
    if (g?.emo) { // the newer games: their emoji inside a neon ring in the game's two colours, with orbit and sparkles
      const a = lk?.n1 || c, b = lk?.n2 || c, sp = [[42, 30, 5], [160, 38, 4], [150, 112, 6], [52, 106, 3.5], [100, 14, 3]];
      return `<svg ${s}><defs><radialGradient id="glow-${id}"><stop offset="0" stop-color="${a}" stop-opacity=".6"/><stop offset=".7" stop-color="${b}" stop-opacity=".15"/><stop offset="1" stop-color="${b}" stop-opacity="0"/></radialGradient>
        <linearGradient id="ring-${id}" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="${a}"/><stop offset="1" stop-color="${b}"/></linearGradient>
        <filter id="neon-${id}" x="-50%" y="-50%" width="200%" height="200%"><feGaussianBlur stdDeviation="2.4" result="b"/><feMerge><feMergeNode in="b"/><feMergeNode in="SourceGraphic"/></feMerge></filter></defs>
        <circle cx="100" cy="70" r="66" fill="url(#glow-${id})"/>
        <ellipse cx="100" cy="70" rx="76" ry="24" fill="none" stroke="url(#ring-${id})" stroke-width="2" transform="rotate(-14 100 70)" filter="url(#neon-${id})"/>
        <circle cx="100" cy="70" r="40" fill="#0d0a22" fill-opacity=".72" stroke="url(#ring-${id})" stroke-width="3" filter="url(#neon-${id})"/>
        ${sp.map(([x, y, r], k) => `<path d="M${x} ${y - r}L${x + r * .3} ${y - r * .3}L${x + r} ${y}L${x + r * .3} ${y + r * .3}L${x} ${y + r}L${x - r * .3} ${y + r * .3}L${x - r} ${y}L${x - r * .3} ${y - r * .3}Z" fill="${k % 2 ? b : a}" filter="url(#neon-${id})"/>`).join("")}
        <circle cx="170" cy="58" r="5" fill="${b}" filter="url(#neon-${id})"/>
        <text x="100" y="88" text-anchor="middle" font-size="48" font-family="Segoe UI Emoji, Apple Color Emoji, Noto Color Emoji, sans-serif">${g.emo}</text></svg>`;
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
  // A dark background (galaxy, aurora…) turns the page into glass cards over space, keeping the game's accent colour
  const GLASS = { paper: "#0A0918", paper2: "#17122E", card: "rgba(30,25,56,.72)", ink: "#F3EEFF", muted: "#B9AEDB", soft: "rgba(255,255,255,.09)", line: "rgba(255,255,255,.15)", dark: true };
  function applyTheme() {
    const t = equipped("theme"), bg = equipped("bg");
    let p = t?.palette || PALETTES.terracotta;
    if (bg?.dark) p = { ...GLASS, accent: p.dark ? p.accent : `color-mix(in srgb, ${p.accent} 72%, #fff)` };
    applyPalette(p);
    setBackground(bg?.kind || "paper", p);
  }

  // Starts a game page: shows the login card when nobody is logged in, then calls ready(user, wallet)
  function start(cfg, ready) {
    game = cfg;
    applyLook(cfg.id);
    // Every game also sells the shared animated backgrounds
    if (!cfg.items.some(i => i.slot === "bg")) {
      cfg.items.push(...BGS.map(b => ({ ...b, slot: "bg" })));
      cfg.storeGroups.splice(1, 0, { slot: "bg", title: "רקעים חיים" });
    }
    const hasStars = (cfg.boards || []).some(b => b.id === "stars");
    cfg.boards = [...(cfg.boards || []), ...(hasStars ? [] : [{ id: "stars", title: "כוכבים בשלבים", bots: [["קוואזר", "🛸", 84], ["אוריון", "👾", 45], ["ניצוץ", "🤖", 15]] }]), { id: "duels", title: "ניצחונות בדו-קרב", unit: "", bots: [["קוואזר", "🛸", 14], ["אוריון", "👾", 8], ["ניצוץ", "🤖", 3]] }];
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
      <section class="hero-neon">
        <i class="hn-orb a"></i><i class="hn-orb b"></i>
        <div class="menu-art">${art(game.id, LOOKS[game.id] ? "var(--n1)" : "var(--accent)")}</div>
        <p class="eyebrow">${esc(g?.tag || "")}</p>
        <h1 class="display">${esc(game.title)}</h1>
        ${Object.keys(W.progress.lv || {}).length ? `<p class="hn-stars">⭐ ${Object.values(W.progress.lv).reduce((a, b) => a + b, 0)} כוכבים · ${Object.keys(W.progress.lv).length} שלבים</p>` : ""}
      </section>
      <p class="lede">${tagline}</p>
      <p class="hello">${user.avatar} שלום, ${esc(user.name)}</p>
      <div class="menu-buttons">${buttons.map(([act, label, primary]) => `<button class="btn ${primary ? "primary big" : ""}" data-act="${act}">${label}</button>`).join("")}
        <div class="menu-duo">${game.duel ? `<button class="btn duel-btn" data-act="duel">⚔️ דו-קרב מול AI</button>` : ""}<button class="btn" data-act="scores">🏆 טבלת שיאים</button></div>
      </div>
    </main>`;
  }

  // The store: groups = [{ slot, title }] or [{ consumable: true, title }]; preview(item) draws an item
  function store() {
    const groups = game.storeGroups.map(gr => {
      const items = game.items.filter(i => gr.consumable ? i.consumable : i.slot === gr.slot);
      return `<section class="store-group"><h2>${gr.title}</h2><div class="shop">${items.map(it => {
        const own = !it.consumable && owns(it.id), on = !it.consumable && W.equipped[it.slot] === it.id;
        const pv = it.palette ? swatch(it.palette) : it.slot === "bg" ? `<div class="bg-sw" style="background:${it.sw}"></div>` : (game.preview ? game.preview(it) : "");
        const label = it.consumable ? `${coinSvg}${it.price}` : on ? "בשימוש ✓" : own ? "לבחור" : it.price ? `${coinSvg}${it.price}` : "חינם";
        return `<article class="item ${on ? "on" : ""}">
          <div class="pv">${pv}</div>
          <h3>${esc(it.name)}</h3>
          ${it.consumable ? `<p class="small">${esc(it.desc)} · יש לך ${stock(it.stockId)}</p>` : ""}
          <button class="btn small ${on ? "" : own ? "" : "primary"}" data-act="buy" data-item="${it.id}" ${on ? "disabled" : ""}>${label}</button>
        </article>`;
      }).join("")}</div></section>`;
    }).join("");
    const lk = LOOKS[game.id];
    return `${topbar("החנות")}
      <div class="shop-banner"><span class="sb-icon">${lk?.icon || "🛍️"}</span><div><b>${esc(lk?.shop || "החנות")}</b><small>המטבעות והקניות כאן שייכים רק ל${esc(game.title)}</small></div><span class="chip">${coinSvg}<b data-coins>${W.coins}</b></span></div>
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
    const duelNote = D.note ? `<p class="duel-note ${D.noteWin ? "win" : ""}">${D.note}</p>` : ""; D.note = "";
    overlay(`${duelNote}${stars !== null ? `<div class="stars" aria-label="${stars} כוכבים">${[0, 1, 2].map(i => `<span class="${i < stars ? "on" : ""}" style="animation-delay:${.15 + i * .18}s">★</span>`).join("")}</div>` : ""}
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

  // ---------- Animated backgrounds ----------
  // sw = the little swatch shown in the store. dark backgrounds switch the page to glass cards.
  const BGS = [
    { id: "bg-paper", name: "נייר שקט", price: 0, kind: "paper", sw: "linear-gradient(135deg,#F6EEE6,#ECD9C8)" },
    { id: "bg-bokeh", name: "בועות אור", price: 80, kind: "bokeh", sw: "radial-gradient(circle at 30% 40%,#FFD6E0 0 18%,transparent 19%),radial-gradient(circle at 70% 60%,#D6E4FF 0 22%,transparent 23%),#FBF4EF" },
    { id: "bg-petals", name: "פריחת דובדבן", price: 110, kind: "petals", sw: "radial-gradient(ellipse at 30% 30%,#F7B6C8 0 10%,transparent 11%),radial-gradient(ellipse at 65% 70%,#F3A1B8 0 9%,transparent 10%),#FDF1F3" },
    { id: "bg-galaxy", name: "גלקסיה סגולה", price: 150, kind: "galaxy", dark: true, sw: "radial-gradient(circle at 35% 40%,#8B4FD8aa,transparent 45%),radial-gradient(circle at 70% 65%,#D84F9Faa,transparent 40%),#0B0820" },
    { id: "bg-nebula", name: "ערפילית טורקיז", price: 170, kind: "nebula", dark: true, sw: "radial-gradient(circle at 30% 60%,#2EC4B6aa,transparent 45%),radial-gradient(circle at 70% 35%,#E05C8Aaa,transparent 40%),#06101E" },
    { id: "bg-aurora", name: "זוהר צפוני", price: 190, kind: "aurora", dark: true, sw: "linear-gradient(170deg,transparent 30%,#3DDC97aa 45%,#4F86F7aa 60%,transparent 75%),#050B1A" },
    { id: "bg-planets", name: "מערכת שמש", price: 240, kind: "planets", dark: true, sw: "radial-gradient(circle at 30% 70%,#F2A65A 0 16%,transparent 17%),radial-gradient(circle at 75% 30%,#9FB4FF 0 8%,transparent 9%),#07061A" },
  ];
  let bg = { kind: null, cv: null, ctx: null, raf: 0, parts: [], neb: null, t0: 0 };
  const reduced = () => matchMedia("(prefers-reduced-motion: reduce)").matches;
  function setBackground(kind, pal) {
    if (kind === bg.kind && bg.cv) return;
    cancelAnimationFrame(bg.raf); bg.kind = kind;
    document.documentElement.classList.toggle("fx", kind !== "paper");
    if (kind === "paper") { bg.cv?.remove(); bg.cv = null; return; }
    if (!bg.cv) { bg.cv = document.createElement("canvas"); bg.cv.id = "bgfx"; bg.cv.setAttribute("aria-hidden", "true"); document.body.prepend(bg.cv); bg.ctx = bg.cv.getContext("2d"); }
    sizeBg(); seedBg(pal); bg.t0 = performance.now();
    const loop = t => { drawBg(t - bg.t0); if (!reduced()) bg.raf = requestAnimationFrame(loop); };
    bg.raf = requestAnimationFrame(loop);
  }
  function sizeBg() { if (!bg.cv) return; const s = Math.min(devicePixelRatio || 1, 1.5); bg.cv.width = innerWidth * s; bg.cv.height = innerHeight * s; bg.ctx.setTransform(s, 0, 0, s, 0, 0); }
  addEventListener("resize", () => { if (bg.cv) { sizeBg(); seedBg(); } });
  const rnd = (a, b) => a + Math.random() * (b - a);
  function seedBg() {
    const w = innerWidth, h = innerHeight, k = bg.kind;
    if (k === "bokeh") bg.parts = Array.from({ length: 26 }, () => ({ x: rnd(0, w), y: rnd(0, h), r: rnd(20, 90), v: rnd(6, 22), hue: [345, 210, 40, 160, 280][Math.floor(rnd(0, 5))], ph: rnd(0, 6) }));
    else if (k === "petals") bg.parts = Array.from({ length: 34 }, () => ({ x: rnd(0, w), y: rnd(-h, h), s: rnd(5, 11), v: rnd(18, 45), sway: rnd(20, 60), ph: rnd(0, 6), rot: rnd(0, 6), vr: rnd(-1, 1) }));
    else {
      bg.parts = Array.from({ length: Math.round(w * h / 5200) }, () => ({ x: rnd(0, w), y: rnd(0, h), r: rnd(.3, 1.5), tw: rnd(.5, 3), ph: rnd(0, 6), d: rnd(.2, 1) }));
      // The nebula is painted once into an off-screen canvas and then drifts slowly
      const n = document.createElement("canvas"); n.width = w; n.height = h; const c = n.getContext("2d");
      const hues = k === "nebula" ? [175, 330, 200] : k === "aurora" ? [220, 250] : k === "planets" ? [250, 210] : [270, 320, 230];
      for (let i = 0; i < 9; i++) {
        const x = rnd(0, w), y = rnd(0, h), r = rnd(w * .25, w * .6), g = c.createRadialGradient(x, y, 0, x, y, r), hue = hues[i % hues.length];
        g.addColorStop(0, `hsla(${hue},80%,60%,${k === "planets" || k === "aurora" ? .12 : .22})`); g.addColorStop(1, "hsla(0,0%,0%,0)");
        c.fillStyle = g; c.fillRect(0, 0, w, h);
      }
      if (k === "galaxy") { // a soft spiral band of dust across the middle
        c.save(); c.translate(w * .55, h * .45); c.rotate(-.5);
        for (let i = 0; i < 900; i++) { const a = rnd(0, 6.28), rad = rnd(0, 1) ** 1.6 * w * .55, sp = a + rad / (w * .12); c.fillStyle = `hsla(${rnd(260, 330)},90%,${rnd(70, 95)}%,${rnd(.05, .35)})`; c.fillRect(Math.cos(sp) * rad, Math.sin(sp) * rad * .38, 1.4, 1.4); }
        c.restore();
      }
      bg.neb = n;
    }
  }
  function drawBg(t) {
    const c = bg.ctx, w = innerWidth, h = innerHeight, k = bg.kind, s = t / 1000;
    if (k === "bokeh" || k === "petals") {
      const g = c.createLinearGradient(0, 0, w, h);
      g.addColorStop(0, k === "petals" ? "#FDF1F3" : "#FBF4EF"); g.addColorStop(1, k === "petals" ? "#F5E6F0" : "#EEF0FA");
      c.fillStyle = g; c.fillRect(0, 0, w, h);
      for (const p of bg.parts) {
        if (k === "bokeh") {
          const y = ((p.y - s * p.v) % (h + 200) + h + 200) % (h + 200) - 100, x = p.x + Math.sin(s * .3 + p.ph) * 30;
          const gr = c.createRadialGradient(x, y, 0, x, y, p.r); gr.addColorStop(0, `hsla(${p.hue},85%,80%,.55)`); gr.addColorStop(.7, `hsla(${p.hue},85%,82%,.18)`); gr.addColorStop(1, `hsla(${p.hue},85%,85%,0)`);
          c.fillStyle = gr; c.beginPath(); c.arc(x, y, p.r, 0, 6.3); c.fill();
        } else {
          const y = ((p.y + s * p.v) % (h + 40) + h + 40) % (h + 40) - 20, x = p.x + Math.sin(s * .8 + p.ph) * p.sway;
          c.save(); c.translate(x, y); c.rotate(p.rot + s * p.vr); c.fillStyle = "rgba(242,150,178,.75)";
          c.beginPath(); c.ellipse(0, 0, p.s, p.s * .55, 0, 0, 6.3); c.fill(); c.fillStyle = "rgba(255,220,230,.8)"; c.beginPath(); c.ellipse(-p.s * .3, 0, p.s * .4, p.s * .2, 0, 0, 6.3); c.fill(); c.restore();
        }
      }
      return;
    }
    c.fillStyle = k === "nebula" ? "#050D1A" : k === "aurora" ? "#040916" : "#07061A"; c.fillRect(0, 0, w, h);
    if (bg.neb) { c.globalAlpha = .9; c.drawImage(bg.neb, Math.sin(s * .05) * 30, Math.cos(s * .04) * 20); c.globalAlpha = 1; }
    for (const p of bg.parts) { // twinkling stars drifting very slowly
      const a = .35 + .65 * Math.abs(Math.sin(s * p.tw + p.ph)), x = (p.x - s * 4 * p.d + w) % w;
      c.fillStyle = `rgba(255,255,255,${a * p.d})`; c.beginPath(); c.arc(x, p.y, p.r, 0, 6.3); c.fill();
    }
    if (k === "aurora") {
      c.save(); c.globalCompositeOperation = "lighter";
      for (let b = 0; b < 3; b++) {
        const base = h * (.25 + b * .12), hue = [150, 175, 260][b];
        const g = c.createLinearGradient(0, base - 120, 0, base + 160); g.addColorStop(0, `hsla(${hue},90%,60%,0)`); g.addColorStop(.5, `hsla(${hue},90%,60%,.22)`); g.addColorStop(1, `hsla(${hue},90%,60%,0)`);
        c.fillStyle = g; c.beginPath(); c.moveTo(0, h);
        for (let x = 0; x <= w; x += 16) c.lineTo(x, base + Math.sin(x / 180 + s * .5 + b) * 50 + Math.sin(x / 70 + s * .9) * 14);
        c.lineTo(w, h); c.fill();
      }
      c.restore();
    }
    if (k === "planets") {
      const px = w * .18, py = h * .82, pr = Math.min(w, h) * .2;
      const g = c.createRadialGradient(px - pr * .35, py - pr * .35, pr * .1, px, py, pr); g.addColorStop(0, "#FFD39A"); g.addColorStop(.6, "#E58A4E"); g.addColorStop(1, "#5A2A3A");
      c.save(); c.translate(px, py); c.rotate(-.35); c.strokeStyle = "rgba(255,220,180,.35)"; c.lineWidth = pr * .08; c.beginPath(); c.ellipse(0, 0, pr * 1.7, pr * .42, 0, Math.PI, 2 * Math.PI); c.stroke(); c.restore();
      c.fillStyle = g; c.beginPath(); c.arc(px, py, pr, 0, 6.3); c.fill();
      c.save(); c.translate(px, py); c.rotate(-.35); c.strokeStyle = "rgba(255,220,180,.45)"; c.lineWidth = pr * .08; c.beginPath(); c.ellipse(0, 0, pr * 1.7, pr * .42, 0, 0, Math.PI); c.stroke(); c.restore();
      const mx = w * .78 + Math.cos(s * .15) * 40, my = h * .22 + Math.sin(s * .15) * 16, mr = Math.min(w, h) * .05;
      const mg = c.createRadialGradient(mx - mr * .3, my - mr * .3, 1, mx, my, mr); mg.addColorStop(0, "#E6ECFF"); mg.addColorStop(1, "#5C6AA8");
      c.fillStyle = mg; c.beginPath(); c.arc(mx, my, mr, 0, 6.3); c.fill();
    }
    // a shooting star now and then
    const cyc = s % 7;
    if (cyc < .9 && k !== "aurora") {
      const sx = w * (.2 + ((Math.floor(s / 7) * 37) % 60) / 100), sy = h * .15, p = cyc / .9;
      const x = sx + p * 260, y = sy + p * 120, g = c.createLinearGradient(x - 90, y - 42, x, y);
      g.addColorStop(0, "rgba(255,255,255,0)"); g.addColorStop(1, `rgba(255,255,255,${1 - p})`);
      c.strokeStyle = g; c.lineWidth = 2; c.beginPath(); c.moveTo(x - 90, y - 42); c.lineTo(x, y); c.stroke();
    }
  }
  document.addEventListener("visibilitychange", () => {
    if (!bg.cv) return;
    if (document.hidden) cancelAnimationFrame(bg.raf);
    else if (!reduced()) { const loop = t => { drawBg(t - bg.t0); bg.raf = requestAnimationFrame(loop); }; bg.raf = requestAnimationFrame(loop); }
  });

  // ---------- Levels (like the Sudoku: worlds of ten, each level opens the next, 1–3 stars) ----------
  const WORLD_NAMES = ["צעדים ראשונים", "מתחממים", "אתגר אמיתי", "מומחים", "אגדות"];
  const WORLD_ICONS = ["🌱", "🔥", "💎", "👑", "🌌"];
  const lvStars = () => { W.progress.lv = W.progress.lv || {}; return W.progress.lv; };
  const levelOpen = L => L === 1 || lvStars()[L - 1] !== undefined;
  // Saves the stars of a finished level; returns true the first time the level is passed
  function levelDone(L, stars) { const s = lvStars(), first = s[L] === undefined; s[L] = Math.max(s[L] || 0, stars); save(); submitScore(Object.values(s).reduce((a, b) => a + b, 0), "stars"); return first; }
  function levelMap({ count = 30, per = 10, names = [], label = "שלב" } = {}) {
    const s = lvStars(), total = Object.values(s).reduce((a, b) => a + b, 0);
    let html = `<p class="lv-total">${coinSvg.replace("coin-ico", "coin-ico hide")}⭐ <b>${total}</b> מתוך ${count * 3} כוכבים</p>`;
    for (let w = 0; w * per < count; w++) {
      const from = w * per + 1, to = Math.min(count, from + per - 1), got = Array.from({ length: to - from + 1 }, (_, k) => s[from + k] || 0).reduce((a, b) => a + b, 0);
      html += `<section class="world ${levelOpen(from) ? "" : "locked"}" style="--wi:${w}"><div class="world-head"><span class="w-ico">${WORLD_ICONS[w] || "✦"}</span><div><small>עולם ${w + 1}</small><h2>${esc(names[w] || WORLD_NAMES[w] || "")}</h2></div><span class="chip">⭐ ${got}/${(to - from + 1) * 3}</span></div>
        <div class="levels">${Array.from({ length: to - from + 1 }, (_, k) => { const L = from + k, open = levelOpen(L), st = s[L];
          return `<button class="btn lvl ${st !== undefined ? "done" : ""} ${open && st === undefined ? "next" : ""}" data-act="lvl" data-level="${L}" ${open ? "" : "disabled"} aria-label="${label} ${L}"><small>${label}</small><b>${L}</b><i>${!open ? "🔒" : st !== undefined ? "★".repeat(st) + "☆".repeat(3 - st) : "·"}</i></button>`; }).join("")}</div></section>`;
    }
    return html;
  }
  // The usual end-of-level card
  function levelResult({ L, count = 30, won, stars = 0, coins = 0, title, sub = "" }) {
    result({ title: title || (won ? `שלב ${L} הושלם!` : `שלב ${L} — לא הפעם`), sub, stars: won ? stars : null, coins,
      buttons: won ? [L < count ? ["lvnext", "לשלב הבא ←", true] : ["lvmap", "סיימת את כל השלבים! 🏆", true], ["lvagain", "לשחק שוב"], ["lvmap", "לכל השלבים"]] : [["lvagain", "לנסות שוב", true], ["lvmap", "לכל השלבים"]] });
  }

  // ---------- High-score tables ----------
  // Each game declares boards: [{ id, title, unit, lower?, fmt?, bots: [[name, avatar, score]] }].
  // Scores of every account on this device are kept (best per account), and the AI rivals sit in the table too.
  const SKEY = id => `mindstudio.scores.${id}`;
  function boardOf(id) { return (game.boards || []).find(b => b.id === id) || { id, title: "שיאים", unit: "" }; }
  function submitScore(score, boardId = (game.boards?.[0]?.id || "main"), { add = false } = {}) {
    if (!user || !game) return 0;
    const b = boardOf(boardId), all = read(SKEY(game.id), {}), list = all[boardId] || [];
    const better = (a, c) => b.lower ? a < c : a > c;
    let me = list.find(e => e.key === user.key);
    if (!me) { me = { key: user.key, name: user.name, avatar: user.avatar, score: add ? 0 : score, at: Date.now() }; list.push(me); if (add) me.score += score; }
    else if (add) me.score += score;
    else if (better(score, me.score)) { me.score = score; me.at = Date.now(); }
    me.avatar = user.avatar; me.name = user.name;
    list.sort((x, y) => b.lower ? x.score - y.score : y.score - x.score);
    all[boardId] = list.slice(0, 50); write(SKEY(game.id), all);
    return rankedRows(boardId).findIndex(r => r.key === user.key) + 1;
  }
  function rankedRows(boardId) {
    const b = boardOf(boardId), mine = read(SKEY(game.id), {})[boardId] || [];
    const bots = (b.bots || []).map(([name, avatar, score]) => ({ key: "bot:" + name, name, avatar, score, bot: true }));
    return [...mine, ...bots].sort((x, y) => b.lower ? x.score - y.score : y.score - x.score);
  }
  let lbTab = null;
  function showScores(tab) {
    const boards = game.boards || []; lbTab = tab || lbTab || boards[0]?.id;
    if (!boards.some(b => b.id === lbTab)) lbTab = boards[0]?.id;
    const b = boardOf(lbTab), rows = rankedRows(lbTab).slice(0, 15), medal = ["🥇", "🥈", "🥉"];
    const fmt = v => b.fmt === "time" ? fmtTime(v) : `${v}${b.unit ? " " + b.unit : ""}`;
    overlay(`<h2 class="display sm">🏆 טבלת שיאים</h2>
      ${boards.length > 1 ? `<div class="lb-tabs">${boards.map(x => `<button class="toggle ${x.id === lbTab ? "on" : ""}" data-act="lbtab" data-b="${x.id}">${esc(x.title)}</button>`).join("")}</div>` : ""}
      <ol class="lb">${rows.map((r, i) => `<li class="${r.key === user?.key ? "me" : ""} ${r.bot ? "bot" : ""}"><span class="rk">${medal[i] || i + 1}</span><span class="av">${r.avatar}</span><span class="nm">${esc(r.name)}${r.bot ? ` <small>AI</small>` : ""}</span><b>${fmt(r.score)}</b></li>`).join("") || `<li class="empty">עוד אין שיאים — זה הזמן לקבוע את הראשון!</li>`}</ol>
      <p class="fine">כל החשבונות במכשיר הזה, יחד עם יריבי ה-AI של הסטודיו.</p>
      <button class="btn wide primary" data-act="lbclose">סגירה</button>`);
  }

  // ---------- AI duel ----------
  // A game opts in with cfg.duel = { mode, start(rival), progress(), over(), lose(), menu(), seconds?(rival), target?(rival), unit? }
  //  mode "race":  the rival solves the same kind of puzzle in rival.factor × seconds(); you must finish first.
  //  mode "score": the rival has a target score (target(rival)); beat it before your round ends.
  //  mode "match": you play against the rival directly (chess, checkers, cards); rival.level sets the difficulty.
  const RIVALS = [
    { name: "ניצוץ", avatar: "🤖", level: 0, label: "קל", factor: 1.7, bonus: 15, color: "#4FBFA5", line: "בוא נראה מה יש לך!" },
    { name: "אוריון", avatar: "👾", level: 1, label: "בינוני", factor: 1.15, bonus: 35, color: "#8B6FD1", line: "אני לא מתכוון להפסיד." },
    { name: "קוואזר", avatar: "🛸", level: 2, label: "קשה", factor: .8, bonus: 70, color: "#E8735A", line: "אף אחד עוד לא ניצח אותי." },
  ];
  const D = { rival: null, t0: 0, aiTime: 0, raf: 0, state: null, note: "", noteWin: false };
  function duelPicker() {
    overlay(`<h2 class="display sm">⚔️ דו-קרב מול AI</h2>
      <p class="lede">${game.duel.mode === "race" ? "שניכם פותרים בו-זמנית. מי שמסיים ראשון — מנצח." : game.duel.mode === "score" ? "ליריב יש שיא. עקפו אותו בסיבוב אחד." : "משחק ישיר מול היריב. כל יריב ברמה אחרת."}</p>
      <div class="rivals">${RIVALS.map((r, i) => `<button class="rival" data-act="duelgo" data-r="${i}" style="--rc:${r.color}">
        <span class="rv-av">${r.avatar}</span><span class="rv-tx"><b>${r.name}</b><small>${r.label}${game.duel.mode === "score" ? ` · שיא: ${game.duel.target(r)} ${game.duel.unit || ""}` : ""}</small><i>"${r.line}"</i></span><span class="chip">+${r.bonus} ${coinSvg}</span></button>`).join("")}</div>
      <button class="btn wide" data-act="lbclose">ביטול</button>`);
  }
  function duelBegin(r) {
    closeOverlay(); stopDuel();
    D.rival = r; D.state = "on"; D.t0 = performance.now();
    game.duel.start(r);
    D.aiTime = game.duel.mode === "race" ? game.duel.seconds(r) * r.factor * (0.92 + Math.random() * .16) : 0;
    let bar = $("#duelBar");
    if (!bar) { bar = document.createElement("div"); bar.id = "duelBar"; document.body.append(bar); }
    bar.style.setProperty("--rc", r.color);
    bar.innerHTML = `<div class="db-row"><span class="db-av">${user.avatar}</span><div class="db-track"><i id="dbMe"></i></div><b id="dbMeTx">0%</b></div>
      ${game.duel.mode === "match" ? `<div class="db-vs">⚔️ מול ${r.avatar} ${r.name} · ${r.label}</div>` : `<div class="db-row ai"><span class="db-av">${r.avatar}</span><div class="db-track"><i id="dbAi"></i></div><b id="dbAiTx">${game.duel.mode === "score" ? game.duel.target(r) + " " + (game.duel.unit || "") : "0%"}</b></div>`}`;
    bar.classList.toggle("match", game.duel.mode === "match");
    bar.classList.toggle("top", !!game.duel.barTop);
    if (game.duel.mode === "score") $("#dbAi").style.width = "100%"; // the rival's record is the finish line
    sound("hint"); toast(`${r.avatar} ${r.name}: "${r.line}"`);
    const tick = () => {
      if (D.state !== "on") return;
      if (!game.duel.active()) return stopDuel();
      const me = Math.max(0, Math.min(1, game.duel.progress() || 0));
      const meEl = $("#dbMe"); if (meEl) { meEl.style.width = me * 100 + "%"; $("#dbMeTx").textContent = Math.floor(me * 100) + "%"; }
      if (me >= 1) return duelWon();
      if (game.duel.over()) return duelLost(true); // the round ended without a win (out of tries, crashed, lost the match)
      if (game.duel.mode === "race") {
        const el = (performance.now() - D.t0) / 1000, ai = Math.min(1, Math.max(0, el / D.aiTime + Math.sin(el * .7) * .015));
        $("#dbAi").style.width = ai * 100 + "%"; $("#dbAiTx").textContent = Math.floor(ai * 100) + "%";
        if (ai >= 1) return duelLost();
      }
      D.raf = setTimeout(tick, 100);
    };
    D.raf = setTimeout(tick, 100);
  }
  function duelWon() {
    const r = D.rival; D.state = "won"; clearTimeout(D.raf);
    earn(r.bonus); submitScore(1, "duels", { add: true });
    D.note = `⚔️ ניצחת את ${r.avatar} ${r.name}! בונוס +${r.bonus}`; D.noteWin = true;
    toast(`⚔️ ניצחת את ${r.name}!`); setTimeout(() => $("#duelBar")?.remove(), 1600);
  }
  function duelLost(gameShowsResult) {
    const r = D.rival; D.state = "lost"; clearTimeout(D.raf);
    setTimeout(() => $("#duelBar")?.remove(), 1200);
    if (gameShowsResult) { D.note = `${r.avatar} ${r.name} ניצח/ה בדו-קרב הפעם`; D.noteWin = false; return; }
    game.duel.lose(); sound("lose");
    result({ title: `${r.avatar} ${r.name} ניצח/ה`, sub: "היריב סיים לפניך. רוצה הזדמנות נוספת?", buttons: [["duelagain", "דו-קרב חוזר", true], ["duelpick", "יריב אחר"], ["duelmenu", "לתפריט"]] });
  }
  function stopDuel() { clearTimeout(D.raf); if (D.state === "on") D.state = null; $("#duelBar")?.remove(); }
  const inDuel = () => D.state === "on";

  document.addEventListener("click", e => {
    const b = e.target.closest("[data-act]"); if (!b || !game) return;
    const a = b.dataset.act;
    if (a === "scores") return showScores();
    if (a === "lbtab") return showScores(b.dataset.b);
    if (a === "lbclose") return closeOverlay();
    if (a === "duel" || a === "duelpick") return duelPicker();
    if (a === "duelgo") return duelBegin(RIVALS[+b.dataset.r]);
    if (a === "duelagain") return duelBegin(D.rival);
    if (a === "duelmenu") { closeOverlay(); stopDuel(); return game.duel.menu(); }
  });

  const shuffle = a => { for (let i = a.length - 1; i > 0; i--) { const j = Math.floor(Math.random() * (i + 1)); [a[i], a[j]] = [a[j], a[i]]; } return a; };
  const fmtTime = s => `${Math.floor(s / 60)}:${String(s % 60).padStart(2, "0")}`;

  window.Hub = {
    // accounts
    currentUser, logout, setAvatar, loginCard, AVATARS,
    // games and art
    GAMES, CATS, LOOKS, loadFont, art, fromHub, HUB_URL, PALETTES, applyPalette, theme, coinsOf, coinSvg,
    // current game
    start, save, earn, spend, stock, useStock, equipped, owns,
    get wallet() { return W; }, get user() { return user; },
    // UI
    topbar, menu, store, buy, clearStoreMsg, overlay, closeOverlay, overlayOpen, result, toast, confetti,
    sound, soundOn, toggleSound,
    // backgrounds, scores and duels
    levelMap, levelDone, levelOpen, levelResult, levelStars: () => lvStars(),
    BGS, setBackground, submitScore, showScores, RIVALS, inDuel, stopDuel, get duelRival() { return D.rival; },
    // helpers
    esc, $, shuffle, fmtTime,
  };
})();
