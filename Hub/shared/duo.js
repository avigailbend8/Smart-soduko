/* Mind Studio – a small engine shared by the turn-based games against the computer.
   Levels work like the Sudoku: 30 levels in worlds of ten. Beat the computer to open the next level.
   G.L is the level, G.d its world (0, 1, 2 → the AI's search strength), and on early levels the computer
   sometimes plays a random move (G.blunder), so it gets a little sharper every level.

   Duo.run(CFG, {
     tagline, help, count?: 30, worlds?: [names],
     newGame(G) – set up G (return false to cancel), board(G) html (data-act="cell" data-i=…),
     click(G, el) → true when the player moved, ai(G) the computer moves, randomMove?(G) a random legal move,
     turn(G) "me" | "ai", result(G) null | "win" | "lose" | "draw",
     status?(G), tools?(G), act?(G, name, el) for data-act="x-…", resultText?(G, kind), stars?(G) 1..3
   })
*/
(function () {
  "use strict";
  const { $ } = Hub;
  function run(CFG, X) {
    let W, view = "menu", nav = [], G = null, timer = 0;
    const COUNT = X.count || 30;
    const P = () => { const p = W.progress; p.wins = typeof p.wins === "number" ? p.wins : 0; p.draws = p.draws || 0; p.losses = p.losses || 0; return p; };
    CFG.boards = CFG.boards || [{ id: "wins", title: "ניצחונות", bots: X.bots || [["קוואזר", "🛸", 60], ["אוריון", "👾", 25], ["ניצוץ", "🤖", 8]] }];
    CFG.duel = CFG.duel || {
      mode: "match",
      start: r => { if (start([8, 18, 28][r.level], true) !== false) { view = "game"; nav = ["menu"]; render(); } },
      progress: () => G?.over === "win" ? 1 : 0, over: () => !!G?.over, lose: () => {},
      active: () => view === "game" && !!G, menu: () => { clearTimeout(timer); view = "menu"; nav = []; render(); },
    };
    const coinsFor = L => 10 + L * 2;
    function start(L, duel = false) {
      clearTimeout(timer);
      G = { L, d: Math.min(2, Math.floor((L - 1) / 10)), blunder: Math.max(0, .45 - (L - 1) * .016), duel, over: null, msg: "", thinking: false, moves: 0 };
      if (X.newGame(G, G.d) === false) { G = null; return false; }
      if (X.turn(G) === "ai") later();
    }
    function later() {
      G.thinking = true;
      timer = setTimeout(() => {
        if (!G || G.over) return;
        if (X.randomMove && Math.random() < G.blunder) X.randomMove(G); else X.ai(G);
        G.thinking = false; after();
      }, X.aiDelay ?? 520);
    }
    function after() {
      const r = X.result(G);
      if (r) return finish(r);
      if (X.turn(G) === "ai") { render(); return later(); }
      render();
    }
    function finish(kind) {
      G.over = kind; G.thinking = false;
      const p = P(), passed = kind === "win" || kind === "draw";
      const stars = kind === "win" ? (X.stars ? X.stars(G) : 3) : kind === "draw" ? 1 : 0;
      let coins = 0;
      if (kind === "win") { p.wins++; Hub.submitScore(p.wins, "wins"); Hub.sound("win"); Hub.confetti(); }
      else if (kind === "draw") { p.draws++; Hub.sound("good"); }
      else { p.losses++; Hub.sound("lose"); }
      const first = passed && !G.duel ? Hub.levelDone(G.L, stars) : false;
      if (G.coinsWin !== undefined) coins = kind === "win" ? G.coinsWin : kind === "draw" ? G.coinsDraw : 0; // games with their own stakes (21)
      else if (passed) coins = (first || G.duel ? coinsFor(G.L) : Math.ceil(coinsFor(G.L) / 3)) * (kind === "draw" ? .5 : 1) | 0;
      Hub.save(); if (coins) Hub.earn(coins);
      render();
      const sub = X.resultText ? X.resultText(G, kind) : kind === "win" ? "ניצחת את המחשב!" : kind === "draw" ? "תיקו — גם זה עובר את השלב" : "המחשב ניצח הפעם";
      setTimeout(() => G.duel ? Hub.result({ title: kind === "win" ? "ניצחת! 🎉" : kind === "draw" ? "תיקו" : "המחשב ניצח", sub, coins, buttons: [["dmenu", "לתפריט", true], ["dview", "להסתכל על הלוח"]] })
        : Hub.levelResult({ L: G.L, count: COUNT, won: passed, stars, coins, title: kind === "win" ? `ניצחת בשלב ${G.L}! 🎉` : kind === "draw" ? `תיקו בשלב ${G.L}` : `המחשב ניצח בשלב ${G.L}`, sub }), 650);
    }
    function go(v) { nav.push(view); view = v; render(); scrollTo(0, 0); }
    function back() { clearTimeout(timer); view = nav.pop() || "menu"; render(); }
    function render() {
      if (view !== "game") Hub.closeOverlay();
      X.beforeRender?.();
      $("#app").innerHTML = ({ menu: menuView, levels: levelsView, game: gameView, store: () => Hub.store(), help: () => `${Hub.topbar("איך משחקים")}<div class="help card">${X.help}</div>` })[view]();
      X.afterRender?.(G);
    }
    const menuView = () => Hub.menu({ tagline: X.tagline, buttons: [["dlevels", "שלבים", true], ["store", "החנות"], ["help", "איך משחקים"]] });
    const levelsView = () => `${Hub.topbar("שלבים")}${Hub.levelMap({ count: COUNT, names: X.worlds })}`;
    function gameView() {
      const status = G.over ? (G.over === "win" ? "ניצחת! 🎉" : G.over === "draw" ? "תיקו" : "המחשב ניצח") : G.thinking ? `<span class="thinking">${X.thinkingText || "המחשב חושב"}</span>` : G.msg || (X.status ? X.status(G) : "תורך");
      return `${Hub.topbar(`${CFG.title} · שלב ${G.L}`)}
        <div class="turnbar"><span class="chip status" role="status">${status}</span></div>
        ${X.board(G)}
        <div class="tools" style="display:flex;gap:8px;justify-content:center;flex-wrap:wrap;margin-top:16px">${X.tools ? X.tools(G) : ""}${G.over ? `<button class="btn primary" data-act="${G.over !== "lose" && G.L < COUNT && !G.duel ? "lvnext" : "lvagain"}">${G.over !== "lose" && G.L < COUNT && !G.duel ? "לשלב הבא ←" : "שוב"}</button>` : ""}</div>`;
    }
    document.addEventListener("click", e => {
      const b = e.target.closest("[data-act]"); if (!b) return;
      const a = b.dataset.act;
      if (a === "back") { Hub.closeOverlay(); return back(); }
      if (a === "dlevels") return go("levels");
      if (a === "store") { Hub.clearStoreMsg(); return go("store"); }
      if (a === "help") return go("help");
      if (a === "buy") { Hub.buy(b.dataset.item); return render(); }
      if (a === "lvl") { if (start(+b.dataset.level) !== false) go("game"); return; }
      if (a === "cell") { if (!G || G.over || G.thinking || X.turn(G) !== "me") return; G.msg = ""; if (X.click(G, b)) { G.moves++; after(); } else render(); return; }
      if (a.startsWith("x-")) { if (G && X.act) { if (X.act(G, a, b) === "moved") after(); else render(); } return; }
      if (a === "dview") return Hub.closeOverlay();
      if (a === "lvnext") { Hub.closeOverlay(); if (start(Math.min(COUNT, G.L + 1)) !== false) render(); return; }
      if (a === "lvagain") { Hub.closeOverlay(); if (start(G.L, G.duel) !== false) render(); return; }
      if (a === "lvmap") { Hub.closeOverlay(); clearTimeout(timer); view = "levels"; nav = ["menu"]; return render(); }
      if (a === "dmenu") { Hub.closeOverlay(); clearTimeout(timer); view = "menu"; nav = []; return render(); }
    });
    window.DuoDebug = { state: () => G, click: el => { if (X.click(G, el)) after(); else render(); }, start: L => { start(L); view = "game"; render(); } };
    Hub.start(CFG, (u, w) => { W = w; render(); });
  }
  window.Duo = { run };
})();
