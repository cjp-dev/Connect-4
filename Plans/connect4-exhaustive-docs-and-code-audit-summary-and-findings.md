# Connect-4 .NET Exhaustive Documentation & Code Audit: Summary & Findings

> **Related Plan:** [`connect4-exhaustive-docs-and-code-audit-plan.md`](connect4-exhaustive-docs-and-code-audit-plan.md)  
> **Repository:** `https://github.com/cjp-dev/Connect-4`  
> **Date:** October 2026  
> **Status:** Completed & Fully Verified (100% Pass)  

---

## 1. Executive Summary

This document presents the findings, defect reconciliations, architectural analyses, and empirical verification results from the execution of the **Exhaustive Documentation, Engine Lineage & Algorithmic Audit Plan** for Connect-4 .NET.

The audit conducted an exhaustive verification across all 16 chapters of [`docs/brain/`](../docs/brain/), the master index [`docs/brain/README.md`](../docs/brain/README.md), the tournament blueprint [`Plans/general-game-gui-specification.md`](general-game-gui-specification.md), and the active .NET 10 solution ([`Connect4.Net.slnx`](../Connect4.Net/Connect4.Net.slnx)).

### Key Audit Accomplishments
- **Automated Snippet Catalog & Linter:** Built an automated node-based linter ([`scratch/audit_connect4_snippets.mjs`](../scratch/audit_connect4_snippets.mjs)) that cataloged all **47 fenced code blocks** and validated every link, symbol, and language tag. Current state: **0 discrepancies detected (100% compliance)**.
- **Mermaid Diagram Syntax Reconciled:** Replaced the unsupported `gantt` diagram in Chapter 10 with a compliant, responsive `flowchart LR` timeline.
- **Option B Full WPF Modernization:** Upgraded [`Connect4.WPF`](../Connect4.Net/Connect4.WPF) from a legacy layout to complete visual and functional parity with [`Connect4.Web`](../Connect4.Net/Connect4.Web) and Chapter 13. Features include the 4-layer dual-card scoreboard, active turn border glow, quick action bar (`Undo`, `Redo`, `Move Now`), crimson modified status badge (`#E74C3C`), modernized telemetry (`Score` and `Speed`), move history card, and Help menu documentation launchers (`F1` / `Shift+F1`).
- **Empirical Opening Book Re-Solve:** Verified all 399,029 positions of the 9-ply opening book; re-solved a random sample of 200 leaf positions using `Connect4.Tools book verify` with **0 errors**.
- **Full Test Suite & Clean Compilation:** All **226 unit tests** (173 Engine + 53 App) pass cleanly; solution builds with **0 warnings and 0 errors**.
- **Online Documentation Link Compliance:** Ensured all repository and source links resolve properly when served via [`BrainDocs.cs`](../Connect4.Net/Connect4.Web/Services/BrainDocs.cs) by using canonical GitHub URLs (`https://github.com/cjp-dev/Connect-4/...`).

---

## 2. Catalog of Cataloged Code Blocks (Layer 1)

The audit registered all **47 fenced code blocks** across the 17 markdown files in the repository:

| # | Document | Lines | Language | Description / Verified Content |
|---|---|---|:---:|---|
| **01** | `docs/brain/01-overview.md` | L21–L29 | `mermaid` | Clean MVVM Architecture flowchart (`Engine` $\leftarrow$ `App` $\leftarrow$ `WPF`/`Web`) |
| **02** | `docs/brain/02-board-and-bitboard.md` | L13–L21 | `text` | 49-bit bitboard bit index map with sentinel top row (bits 6, 13, ..., 48) |
| **03** | `docs/brain/02-board-and-bitboard.md` | L27–L31 | `text` | Column order and bit indices table ($c \times 7 + r$) |
| **04** | `docs/brain/02-board-and-bitboard.md` | L50–L52 | `csharp` | `Position.Play` constructor call verbatim check |
| **05** | `docs/brain/02-board-and-bitboard.md` | L68–L72 | `text` | Mirror symmetry column swap diagram |
| **06** | `docs/brain/02-board-and-bitboard.md` | L78–L82 | `text` | Canonical key formula illustration |
| **07** | `docs/brain/03-rules-and-move-generation.md` | L24–L28 | `text` | Directional broadword bit shifts ($s=1, 7, 6, 8$) |
| **08** | `docs/brain/03-rules-and-move-generation.md` | L66–L72 | `text` | Vertical threat wrap-around prevention via sentinel row |
| **09** | `docs/brain/03-rules-and-move-generation.md` | L74–L83 | `mermaid` | Non-losing move filter decision tree (Win $\rightarrow$ Block $\rightarrow$ Prune) |
| **10** | `docs/brain/04-game-record.md` | L27–L33 | `text` | 1-based digit string notation format (`4453`) |
| **11** | `docs/brain/05-evaluation.md` | L30–L37 | `text` | Allis odd/even row parity zugzwang masks (`RedGoodRows`, `YellowGoodRows`) |
| **12** | `docs/brain/05-evaluation.md` | L43–L48 | `text` | Centre weight distribution across 69 winning lines |
| **13** | `docs/brain/07-search.md` | L22–L26 | `text` | Score ranges ($\pm 1\,000$ heuristic, $10\,000 - n$ mate score) |
| **14** | `docs/brain/07-search.md` | L34–L51 | `mermaid` | Life-of-a-search flowchart (Book $\rightarrow$ Immediate $\rightarrow$ PVS $\rightarrow$ Solver) |
| **15** | `docs/brain/07-search.md` | L62–L70 | `mermaid` | Alpha-beta cutoff diagram |
| **16** | `docs/brain/08-transposition-table.md` | L24–L29 | `text` | 64-bit packed entry bit layout (Col, Bound, Depth, Value, Age, Check) |
| **17** | `docs/brain/08-transposition-table.md` | L41–L44 | `text` | Bijective hash key multiplication formula ($h = (\text{key} \cdot K) \bmod 2^{49}$) |
| **18** | `docs/brain/09-endgame-solver.md` | L81–L83 | `text` | Pons score convention ($22 - n$) and conversion formula |
| **19** | `docs/brain/09-endgame-solver.md` | L100–L108 | `mermaid` | Endgame threshold solve-time comparison chart (`xychart-beta`) |
| **20** | `docs/brain/10-time-control.md` | L40–L47 | `mermaid` | Iterative deepening timeline (**Reconciled from gantt to flowchart LR**) |
| **21** | `docs/brain/11-opening-book.md` | L13–L18 | `text` | Breadth-first canonical enumeration node count progression |
| **22** | `docs/brain/11-opening-book.md` | L28–L33 | `mermaid` | Book generation pipeline flowchart |
| **23** | `docs/brain/11-opening-book.md` | L47–L57 | `mermaid` | Bottom-up Negamax score backup tree |
| **24** | `docs/brain/11-opening-book.md` | L78–L81 | `text` | `Connect4.Tools book generate` / `verify` CLI syntax |
| **25** | `docs/brain/11-opening-book.md` | L88–L99 | `text` | `OpeningBook.txt` file format sample |
| **26** | `docs/brain/12-app-integration.md` | L11–L26 | `mermaid` | Clean Architecture layer dependency graph |
| **27** | `docs/brain/12-app-integration.md` | L67–L85 | `mermaid` | Web Worker async dispatch & SAB cooperative cancellation sequence |
| **28** | `docs/brain/13-gui-and-settings.md` | L17–L35 | `mermaid` | GUI component wireframe layout flowchart |
| **29** | `docs/brain/13-gui-and-settings.md` | L104–L118 | `text` | Settings dialog parameters and options summary |
| **30** | `docs/brain/13-gui-and-settings.md` | L162–L168 | `mermaid` | Fair-play competitive integrity state diagram |
| **31** | `docs/brain/13-gui-and-settings.md` | L190–L202 | `text` | Dual-card scoreboard layout specification |
| **32** | `docs/brain/13-gui-and-settings.md` | L227–L235 | `text` | Analysis card telemetry field specifications |
| **33** | `docs/brain/README.md` | L17–L34 | `mermaid` | "The brain on one page" decision flowchart |
| **34–47** | `Plans/general-game-gui-specification.md` | Various | Multi | 14 universal tournament GUI specification blocks (Mermaid, text, ini) |

---

## 3. Findings & Defect Reconciliations

### Finding 1: Unsupported Mermaid `gantt` Diagram in Chapter 10
- **Location:** [`docs/brain/10-time-control.md:40-54`](../docs/brain/10-time-control.md#L40)
- **Defect:** Chapter 10 utilized a `gantt` block to represent iterative deepening depth progression. The project's Mermaid rendering configuration does not support `gantt` syntax, causing rendering failures in strict markdown viewers.
- **Resolution:** Re-architected the diagram into a compliant, horizontally scrolling directed flowchart (`flowchart LR`):
  ```mermaid
  flowchart LR
      D1["Depth 1<br/>(&lt;1 ms)"] --> D2["Depth 2<br/>(&lt;1 ms)"]
      D2 --> D3["Depth 3<br/>(&lt;1 ms)"]
      D3 --> D4["Depth 4<br/>(2 ms)"]
      D4 --> D5["Depth 5<br/>(10 ms)"]
      D5 --> D6["Depth 6<br/>(60 ms)"]
      D6 --> D7["Depth 7<br/>(350 ms)"]
      D7 -. "Time check (&gt; 2/3 limit)" .-> Stop["Stop: return depth 7 move"]
  ```

### Finding 2: Empirical Test Suite Baseline Missing from Chapter 01
- **Location:** [`docs/brain/01-overview.md:18, 75`](../docs/brain/01-overview.md)
- **Defect:** Chapter 01 referenced the presence of automated tests but omitted empirical test metrics and breakdown counts.
- **Resolution:** Added explicit test suite statistics: **173 Engine tests** + **53 App tests** = **226 passing tests** across both desktop and web targets.

### Finding 3: Broken Relative Links to Relocated Plan Files
- **Location:** [`docs/brain/15-references.md:21-22`](../docs/brain/15-references.md#L21) and [`docs/brain/README.md:11`](../docs/brain/README.md#L11)
- **Defect:** Links targeted `../../Connect%204%20porting%20documentation.md` and `../../general-game-gui-specification.md`. Because these files reside under `Plans/`, these links failed to resolve.
- **Resolution:** Updated links to canonical GitHub URLs:
  - `https://github.com/cjp-dev/Connect-4/blob/main/Plans/Connect%204%20porting%20documentation.md`
  - `https://github.com/cjp-dev/Connect-4/blob/main/Plans/general-game-gui-specification.md`
  - `https://github.com/cjp-dev/Connect-4/tree/main/Connect4.Net`

### Finding 4: Online Documentation Link Architecture (`BrainDocs.cs`)
- **Discovery:** The web application serves documentation via [`BrainDocs.cs`](../Connect4.Net/Connect4.Web/Services/BrainDocs.cs) using a GitHub-backed base URI (`https://github.com/cjp-dev/Connect-4/blob/main/docs/brain/`).
- **Rule Established:** Markdown links referencing source code or repository files outside `docs/brain/` must use explicit GitHub URLs (`https://github.com/cjp-dev/...`). `BrainDocs.cs` identifies these URLs, adds `target="_blank"` and `rel="noopener"`, allowing them to open cleanly in external tabs without attempting to navigate within the web app's SPA router.

### Finding 5: Asymmetric WPF Desktop GUI vs Web / Chapter 13 Specifications
- **Location:** [`Connect4.WPF/MainWindow.xaml`](../Connect4.Net/Connect4.WPF/MainWindow.xaml) and [`MainWindow.xaml.cs`](../Connect4.Net/Connect4.WPF/MainWindow.xaml.cs)
- **Defect:** While `Connect4.Web` had been upgraded to tournament specifications, `Connect4.WPF` still used legacy controls:
  - Simple `GroupBox` status controls without dual-player scorecards.
  - Bound to deprecated `Value` rather than `Score`.
  - Missing engine throughput (`Speed` in `nps`).
  - No Help menu shortcuts for documentation (`F1` / `Shift+F1`).
  - No visual indication when competitive integrity was compromised (`IsStatusModified`).
- **Resolution:** Executed **Option B Full WPF Modernization** (detailed below).

---

## 4. Option B: Full WPF Modernization Summary

The desktop WPF application ([`Connect4.WPF`](../Connect4.Net/Connect4.WPF)) has been upgraded to full tournament parity with [`Connect4.Web`](../Connect4.Net/Connect4.Web) and the Chapter 13 specifications.

```mermaid
flowchart TD
    subgraph WPF ["Modernized Connect4.WPF Architecture"]
        Menu["Main Menu Bar<br/>(File, Game with Checkmarks, Moves, View, Help F1/Shift+F1)"]
        TopBanner["Tournament Header Banner<br/>(RULES: [STANDARD], SETTINGS: [Summary], CLOCK)"]
        Board["7x6 Grid BoardView<br/>(Drop Previews, Column Buttons, Winning Rings, Last Move)"]
        
        subgraph Sidebar ["Right Sidebar Controls (Auto ScrollViewer)"]
            StatusCard["Status Card<br/>(Turn Indicator, Move Count, AI Thinking Bar, Crimson MODIFIED Badge)"]
            Scoreboard["Dual-Card Scoreboard<br/>(Red & Yellow Cards with Active Turn Border Glow)"]
            QuickActions["Quick Action Bar<br/>([Undo], [Redo], [Move Now])"]
            AnalysisCard["Real-Time Analysis Panel<br/>(Score, Move, Depth, BestMove, Nodes, Speed, Time)"]
            HistoryCard["Move History Panel<br/>(Scrollable Move List with Ply Numbering)"]
        end
    end
    Menu --> TopBanner
    TopBanner --> Board
    Board --> Sidebar
```

### Modernized WPF Features
1. **Help Menu & Documentation Integration:**
   - Bound `F1` to `User guide` (`https://github.com/cjp-dev/Connect-4/blob/main/docs/brain/13-gui-and-settings.md`).
   - Bound `Shift+F1` to `Rules & Documentation` (`https://github.com/cjp-dev/Connect-4/blob/main/docs/brain/README.md`).
   - Implemented via `RoutedUICommand` and `Process.Start` with `UseShellExecute = true`.
2. **Tournament Header Banner:**
   - Centered above the 7×6 board with soft card styling and drop shadow.
   - Emerald `[STANDARD]` variant badge (`#16A085`).
   - Sky-blue `[Settings summary]` badge (`#2980B9`).
   - Purple chess clock badge (`#8E44AD`) displayed during timed games.
3. **Competitive Integrity & Status Card:**
   - Prominent turn indicator (`Red's Turn — Drop a disc` / `GameOverText`).
   - When competitive integrity is broken (`IsStatusModified = true`), header changes to `#E74C3C` and displays a crimson `MODIFIED` badge.
   - Indeterminate progress bar active during computer search (`IsAiThinking`).
4. **4-Layer Dual-Card Scoreboard:**
   - **Active Turn Highlight:** Dynamic border glow (`#D32F2F` for Red, `#F57F17` for Yellow) driven by `IsRedActiveTurn` / `IsYellowActiveTurn`.
   - **Player Identity:** 14×14 disc swatch, bold player name, and individual chess clock.
   - **Role:** Human vs Computer labeling.
   - **Metrics:** Placed disc count (18pt bold) and remaining discs in reserve (`X left`).
5. **Quick Action Bar:**
   - `Undo` (`Ctrl+Z`, bound to `UndoCommand`, enabled when `CanUndo`).
   - `Redo` (`Ctrl+Y`, bound to `RedoCommand`, enabled when `CanRedo`).
   - `Move Now` (`Ctrl+M`, bound to `MoveNowCommand`, active during AI search).
6. **Telemetry & Real-Time Analysis Panel:**
   - Scores bound to `Score` (formatted from Red's perspective, e.g. `+0.00` or `#+14`).
   - Engine search speed bound to `Speed` (`nps` / `knps`).
   - Complete telemetry: `Move`, `Depth`, `Score`, `BestMove`, `Nodes`, `Speed`, `Time`.
7. **Move History Card:**
   - Scrollable card bound to `MoveHistoryList`, displaying round numbers and algebraic drop columns.

---

## 5. Lineage & Algorithmic Attribution Truth (Layer 4)

The codebase's algorithms and historical lineages were audited against original papers and source repositories:

```mermaid
flowchart TD
    Pons["Pascal Pons (connect4-master, 2017–2019)<br/>• 49-bit bitboard & sentinel row<br/>• Broadword threat detection<br/>• Non-losing move pruning<br/>• 1:1 C++ Endgame solver & CRT table<br/>• Opening book leaf enumeration"]
    Stello["Stello (Author's Othello Engine)<br/>• Negamax Iterative Deepening<br/>• PVS null-window scouting<br/>• 64-bit bijective hash table<br/>• Time controls (soft/hard limits)<br/>• Clean MVVM architecture"]
    Allis["Victor Allis (1988 Thesis)<br/>• Mathematical first-player win proof<br/>• Zugzwang odd/even row parity bonuses<br/>• Red odd (1,3,5) / Yellow even (2,4,6)"]
    Tromp["John Tromp (Fhourstones, 1995)<br/>• 49-bit bitboard representation<br/>• Board state count 4.53 x 10^12"]
    Knuth["Donald Knuth & Ronald Moore (1975)<br/>• Alpha-beta pruning foundation"]

    Pons --> Engine["Connect4.Engine"]
    Stello --> Engine
    Allis --> Engine
    Tromp --> Engine
    Knuth --> Engine
```

### Lineage Reconciliations
1. **Pascal Pons' Move Ordering Formulation:**
   - In `connect4-master/MoveSorter.hpp`, a comment states column sequence `3, 4, 2, 5, 1, 6, 0`.
   - The actual evaluation formula `WIDTH/2 + (1 - 2*(i%2)) * (i+1)/2` generates:
     $$i=0 \rightarrow 3, \quad i=1 \rightarrow 2, \quad i=2 \rightarrow 4, \quad i=3 \rightarrow 1, \quad i=4 \rightarrow 5, \quad i=5 \rightarrow 0, \quad i=6 \rightarrow 6$$
   - The C# engine faithfully implements the exact formula sequence (`3, 2, 4, 1, 5, 0, 6`), and Chapter 06 accurately clarifies this historical discrepancy.
2. **Chinese Remainder Theorem in `EndgameTable`:**
   - Table size is prime: $16\,777\,259$ (first prime above $2^{24}$).
   - Stored key check is $\text{key} \bmod 2^{32}$.
   - Because $\gcd(16\,777\,259, 2^{32}) = 1$, the Chinese Remainder Theorem guarantees that keys under $16\,777\,259 \times 2^{32} \approx 7.2 \times 10^{16}$ have a unique representation, completely eliminating false hash collisions.
3. **Bijective Hash Table Multiplication:**
   - Multiplier $K = \texttt{0x9E3779B97F4A7C15}$ is coprime to $2^{49}$ (odd number).
   - Multiplication modulo $2^{49}$ forms a permutation of the 49-bit key space, guaranteeing no mathematical key collisions.

---

## 6. Empirical Verification & Test Results (Phase 9)

All test suites and verification tools were executed and verified:

```text
================================================================
Connect-4 Documentation & Snippet Linter
Scanning 17 markdown documents...
================================================================
Found 47 fenced code blocks across documents.
Discrepancy & Linter Report (0 finding(s)):
SUCCESS: 0 discrepancies detected. All documents in 100% compliance!
================================================================
```

### Detailed Verification Log
1. **Unit Test Execution:**
   ```text
   dotnet test Connect4.Net/Connect4.Net.slnx
   Passed! - Failed: 0, Passed:  53, Skipped: 0, Total:  53, Duration: 548 ms - Connect4.App.Tests.dll (net10.0)
   Passed! - Failed: 0, Passed: 173, Skipped: 0, Total: 173, Duration: 2.0 s  - Connect4.Engine.Tests.dll (net10.0)
   Total: 226 tests passed.
   ```
2. **Opening Book Verification (200 Leaves Re-Solved):**
   ```text
   dotnet run --project Connect4.Net/Connect4.Tools -- book verify --book Connect4.Net/Connect4.Engine/Book/OpeningBook.txt --sample 200
   Connect4.Net/Connect4.Engine/Book/OpeningBook.txt: 399.029 positions, depth 9.
   Empty board: 1; Red's first moves, columns 1-7: -2 -1 0 1 0 -1 -2
   Known first-move scores: OK
   Back-up of the shallower positions: OK
   Solving 200 random positions with at least 3 discs again (seed 20631921).
   200 / 200 (100,0 %), 5,37 positions/s, 0:37 used, 0:00 left
   Solved again: OK
   The book is correct.
   ```
3. **Full Solution Build:**
   ```text
   dotnet build Connect4.Net/Connect4.Net.slnx
   Build succeeded.
       0 Warning(s)
       0 Error(s)
   ```

---

## 7. Traceability Matrix & File Locations

| Component | Path / Location | Verification Outcome |
|---|---|:---:|
| **Audit Plan** | [`Plans/connect4-exhaustive-docs-and-code-audit-plan.md`](connect4-exhaustive-docs-and-code-audit-plan.md) | Verified Complete |
| **Audit Summary & Findings** | [`Plans/connect4-exhaustive-docs-and-code-audit-summary-and-findings.md`](connect4-exhaustive-docs-and-code-audit-summary-and-findings.md) | Published |
| **Automated Linter** | [`scratch/audit_connect4_snippets.mjs`](../scratch/audit_connect4_snippets.mjs) | 0 Findings |
| **WPF Main View XAML** | [`Connect4.Net/Connect4.WPF/MainWindow.xaml`](../Connect4.Net/Connect4.WPF/MainWindow.xaml) | Modernized |
| **WPF Code-Behind** | [`Connect4.Net/Connect4.WPF/MainWindow.xaml.cs`](../Connect4.Net/Connect4.WPF/MainWindow.xaml.cs) | Modernized |
| **Engine Tests** | [`Connect4.Net/Connect4.Engine.Tests`](../Connect4.Net/Connect4.Engine.Tests) | 173/173 Pass |
| **App Tests** | [`Connect4.Net/Connect4.App.Tests`](../Connect4.Net/Connect4.App.Tests) | 53/53 Pass |
| **Opening Book File** | [`Connect4.Net/Connect4.Engine/Book/OpeningBook.txt`](../Connect4.Net/Connect4.Engine/Book/OpeningBook.txt) | 200-Sample Pass |
| **Web Docs Reader** | [`Connect4.Net/Connect4.Web/Services/BrainDocs.cs`](../Connect4.Net/Connect4.Web/Services/BrainDocs.cs) | Compliant |

