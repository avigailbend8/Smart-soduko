/* Mind Studio – a small engine shared by the single-player puzzles.
   Levels work like the Sudoku: 30 levels in worlds of ten, each one opens the next, 1–3 stars each.
   The engine owns the views, the level map, timer, moves, stars, coins, the tables and the race duel.

   Puzzle.run(CFG, {
     tagline, help, count?: 30, worlds?: [names],
     newGame(G) – G.L is the level (1…count) and G.d its world (0, 1, 2…); set up the puzzle from them
     par(G) seconds for the duel rival, goal?(G) short text shown above the board,
     board(G) html (clickable parts use data-act="cell" data-i=…), click(G, el) → true when something changed,
     progress(G) 0..1, solved(G), failed?(G), stars?(G) 1..3, tools?(G), act?(G, name, el), key?(G, e),
     afterRender?(G), resultText?(G), score?(G) + extraBoards
   })
*/
(function () {
  "use strict";
  const { $ } = Hub;
  function run(CFG, X) {
    let W, view = "menu", nav = [], G = null, tick = 0;
    const COUNT = X.count || 30;
    const P = () => { const p = W.progress; p.solved = p.solved || 0; return p; };
    CFG.boards = CFG.boards || [{ id: "stars", title: "כוכבים בשלבים", bots: [["קוואזר", "🛸", 84], ["אוריון", "👾", 45], ["ניצוץ", "🤖", 15]] }, { id: "solved", title: X.solvedTitle || "פאזלים שנפתרו", bots: X.bots || [["קוואזר", "🛸", 120], ["אוריון", "👾", 50], ["ניצוץ", "🤖", 12]] }, ...(X.extraBoards || [])];
    CFG.duel = CFG.duel || {
      mode: "race",
      start: r => { start([6, 16, 26][r.level], true); view = "game"; nav = ["menu"]; render(); },
      seconds: () => X.par(G),
      progress: () => !G || G.lost ? 0 : G.done ? 1 : Math.min(.97, Math.max(0, X.progress(G))),
      over: () => !!G && (G.lost || (G.failed && !G.done)), lose: () => { G.lost = true; G.done = true; stopTimer(); render(); },
      active: () => view === "game", menu: () => { stopTimer(); view = "menu"; nav = []; render(); },
    };
    const coinsFor = L => 10 + L * 2;
    function start(L, duel = false) { stopTimer(); G = { L, d: Math.min(2, Math.floor((L - 1) / 10)), duel, secs: 0, moves: 0, done: false, failed: false, msg: "" }; X.newGame(G, G.d); }
    function startTimer() { stopTimer(); tick = setInterval(() => { if (!G || G.done || G.failed) return; G.secs++; const t = $("#ptime"); if (t) t.textContent = Hub.fmtTime(G.secs); }, 1000); }
    function stopTimer() { clearInterval(tick); tick = 0; }
    function check() {
      if (X.solved(G)) return win();
      if (X.failed && X.failed(G)) {
        G.failed = true; stopTimer(); render(); Hub.sound("lose");
        return setTimeout(() => G.duel ? Hub.result({ title: X.failTitle || "אוי, לא הפעם", sub: X.resultText ? X.resultText(G) : "", buttons: [["lvagain", "לנסות שוב", true], ["pmenu", "לתפריט"]] })
          : Hub.levelResult({ L: G.L, count: COUNT, won: false, title: X.failTitle, sub: X.resultText ? X.resultText(G) : "" }), 500);
      }
      render();
    }
    function win() {
      G.done = true; stopTimer();
      const p = P(), stars = X.stars ? X.stars(G) : 3;
      p.solved++; Hub.submitScore(p.solved, "solved"); if (X.score) Hub.submitScore(X.score(G), X.extraBoards[0].id);
      const first = G.duel ? false : Hub.levelDone(G.L, stars);
      const coins = first || G.duel ? coinsFor(G.L) + (stars - 1) * 4 : Math.ceil(coinsFor(G.L) / 2);
      Hub.save(); Hub.earn(coins);
      render();
      const sub = `${X.resultText ? X.resultText(G) + " · " : ""}⏱ ${Hub.fmtTime(G.secs)}`;
      setTimeout(() => { Hub.sound("win"); Hub.confetti(); G.duel ? Hub.result({ title: X.winTitle || "פתרת! 🎉", sub, stars, coins, buttons: [["pmenu", "לתפריט", true]] }) : Hub.levelResult({ L: G.L, count: COUNT, won: true, stars, coins, title: `${X.winTitle || "פתרת!"} · שלב ${G.L}`, sub }); }, 500);
    }
    function go(v) { nav.push(view); view = v; render(); scrollTo(0, 0); }
    function back() { stopTimer(); view = nav.pop() || "menu"; render(); }
    function render() {
      if (view !== "game") Hub.closeOverlay();
      X.beforeRender?.();
      $("#app").innerHTML = ({ menu: menuView, levels: levelsView, game: gameView, store: () => Hub.store(), help: () => `${Hub.topbar("איך משחקים")}<div class="help card">${X.help}</div>` })[view]();
      if (view === "game") { X.afterRender?.(G); if (!tick && !G.done && !G.failed) startTimer(); }
    }
    const menuView = () => Hub.menu({ tagline: X.tagline, buttons: [["plevels", "שלבים", true], ["store", "החנות"], ["help", "איך משחקים"]] });
    const levelsView = () => `${Hub.topbar("שלבים")}${Hub.levelMap({ count: COUNT, names: X.worlds })}`;
    const gameView = () => `${Hub.topbar(`${CFG.title} · שלב ${G.L}`)}
      ${X.goal ? `<p class="lvl-goal small">${X.goal(G)}</p>` : ""}
      <div class="turnbar"><span class="chip">⏱ <b id="ptime">${Hub.fmtTime(G.secs)}</b></span><span class="chip">מהלכים <b>${G.moves}</b></span>${G.msg ? `<span class="chip status">${G.msg}</span>` : ""}</div>
      ${X.board(G)}
      <div class="tools" style="display:flex;gap:8px;justify-content:center;flex-wrap:wrap;margin-top:16px">${X.tools ? X.tools(G) : ""}${G.done || G.failed ? `<button class="btn primary" data-act="${G.done && G.L < COUNT && !G.duel ? "lvnext" : "lvagain"}">${G.done && G.L < COUNT && !G.duel ? "לשלב הבא ←" : "שוב"}</button>` : ""}</div>`;
    document.addEventListener("click", e => {
      const b = e.target.closest("[data-act]"); if (!b) return;
      const a = b.dataset.act;
      if (a === "back") { Hub.closeOverlay(); return back(); }
      if (a === "plevels") return go("levels");
      if (a === "store") { Hub.clearStoreMsg(); return go("store"); }
      if (a === "help") return go("help");
      if (a === "buy") { Hub.buy(b.dataset.item); return render(); }
      if (a === "lvl") { start(+b.dataset.level); return go("game"); }
      if (a === "cell") { if (!G || G.done || G.failed) return; G.msg = ""; if (X.click(G, b, e)) check(); else render(); return; }
      if (a.startsWith("x-")) { if (G && X.act) { const r = X.act(G, a, b); if (r === "moved") check(); else render(); } return; }
      if (a === "lvnext") { Hub.closeOverlay(); start(Math.min(COUNT, G.L + 1)); return render(); }
      if (a === "lvagain") { Hub.closeOverlay(); start(G.L, G.duel); return render(); }
      if (a === "lvmap") { Hub.closeOverlay(); stopTimer(); view = "levels"; nav = ["menu"]; return render(); }
      if (a === "pmenu") { Hub.closeOverlay(); stopTimer(); view = "menu"; nav = []; return render(); }
    });
    document.addEventListener("contextmenu", e => { const b = e.target.closest("[data-act=cell]"); if (b && X.rightClick && G && !G.done && !G.failed) { e.preventDefault(); if (X.rightClick(G, b)) check(); else render(); } });
    document.addEventListener("keydown", e => { if (view === "game" && G && !G.done && !G.failed && X.key && !Hub.overlayOpen()) { const r = X.key(G, e); if (r) { e.preventDefault(); check(); } } });
    document.addEventListener("visibilitychange", () => { if (view !== "game" || !G || G.done) return; document.hidden ? stopTimer() : startTimer(); });
    window.PuzzleDebug = { state: () => G, check, start: L => { start(L); view = "game"; render(); } };
    Hub.start(CFG, (u, w) => { W = w; render(); });
  }
  window.Puzzle = { run };
})();
