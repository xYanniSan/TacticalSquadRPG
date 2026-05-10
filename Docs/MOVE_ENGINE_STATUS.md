# Move Engine — Implementation Status

> **Reference doc.** Updated in real time as the move-based combat engine
> lands. Read alongside `Docs/Design/COMBAT_DESIGN.md` (intent), `Docs/Design/MOVES_CATALOG.md`
> (animation contract), and the Tier 1 docs (`03_DATA_MODELS.md`, `04_BATTLE_SYSTEM.md`)
> for the full picture.

## TL;DR (read this first)

The 20Hz move-based combat engine is **operational** and was iterated overnight
(2026-05-06 through 2026-05-07). The user's specific concern — "characters are
stuck 80% of the time doing the hit animation" — is **fixed**: stagger time
per unit dropped from ~80% to ~5–10% across the iteration log below.

A fresh 2v2 60-second run produces:
- All 4 units engage cleanly via the engine (handoff at T+~3s)
- 40 hits land, 38 hit-reactions forced, 58 defensive picks (dodge / block / anchor)
- Cancel chains capped at depth 3 → visible beats between exchanges
- Stance signatures visibly different: Sentinel anchors, Tempest side-dodges,
  Onslaught back-dodges when staggered, Tactician side-dodges
- Battle ends naturally on first death cascade
- Compile clean, no warnings

Open questions / next-session work:
- Animations are still placeholder Mixamo (user owns clip authoring per `MOVES_CATALOG.md`).
- Combat balance: Grunts have 5x player HP, so heroes lose 2v2; that's data, not engine.
- Speed economy / Sharp+ band rarely reached in extended fights (tunable).
- Perception / world entities reserved (Earth Wall-style preemption); not in this slice.

## What this doc is

A running log of the move-based engine's working state: what's implemented,
what's tuned, what to iterate on next. Less polished than Tier 1/2 docs;
think of it as session notes a teammate could scan to know where things stand.

---

## Engine pipeline (1-line per stage)

```
TerrainBattleManager.Awake/Start
  → spawns units, registers with BattleCombatEngine
  → legacy UnitBrainAI handles Backline → Engage → Decide
  → first Decide: hands off to engine (EngineControlled = true)

BattleCombatEngine.Update (every Unity frame)
  → accumulator advances at Time.deltaTime
  → triggers Tick() each 50ms (20Hz fixed)

BattleCombatEngine.Tick (every 50ms)
  → for each engaged unit's UnitMoveExecution:
       ApplyMovement → target-aware fwd/lat × tickSeconds → CC.Move
       UpdatePhase   → Startup / Active / Recovery / CancelWindow / Done
       ResolveHit    → cone check + state-pair resolution if active+IsAttack
       TryPickReaction → 1-frame lookahead, calls IStanceBrain.PickReaction
       PickCancel    → on CancelWindow entry, ask brain.PickCancel once
       framesElapsed++
       UpdatePhase
       if Done → PickPreparation (reserved) → PickNeutral
```

## Files that landed

### Data layer
- `Assets/Scripts/DataModels/MoveDefinition.cs` — frame data + hit cone + i-frames + cancel chains + movement curves + (reserved) entity-spawn slot
- `Assets/Scripts/DataModels/Enums.cs` — `MoveCategory`, `MovePhase`, `ReactionTag`, `FacingPolicy`, `HitResolution`
- `Assets/Resources/Moves/*` — Tier 1 + Tier 2 SOs (idle, locomotion, jab, hook, uppercut, kick_low, kick_crescent, power_strike, dodge_back, dodge_side_left/right, react_hit_light/heavy/sweep, launch_airborne, knockdown_back, death_collapse)

### Engine
- `Assets/Scripts/ThirdPerson/BattleCombatEngine.cs` — 20Hz tick loop, hit resolution, reaction lookahead, cancel-window, force-react, heartbeat logging
- `Assets/Scripts/Systems/Combat/UnitMoveExecution.cs` — per-unit runtime state
- `Assets/Scripts/Systems/Combat/MoveCatalog.cs` — Resources/Moves auto-load + name lookup
- `Assets/Scripts/Systems/Combat/MoveReactionTable.cs` — paired forced-reaction lookup

### Brain layer
- `Assets/Scripts/Systems/Combat/IStanceBrain.cs` — 4-hook interface (PickNeutral / PickReaction / PickPreparation / PickCancel)
- `Assets/Scripts/Systems/Combat/StanceBrainBase.cs` — shared affordability / locomotion helpers
- `Assets/Scripts/Systems/Combat/StanceBrainRegistry.cs` — StanceId → IStanceBrain
- `Assets/Scripts/Systems/Combat/Brains/{Onslaught,Tempest,Stalwart,Tactician,Wraith,Sentinel,Conduit}Brain.cs` — all 7 stances

### Animation contract
- `Assets/Scripts/ThirdPerson/UnitAnimationDriver.cs` — `PlayMove(string)` with Inspector overrides + name-prefix builtin map + no-op fallback. First miss logged once.

### Wiring
- `Assets/Scripts/ThirdPerson/TerrainBattleManager.cs` — adds `BattleCombatEngine`, exposes `useMoveBasedEngine` toggle (default true), `IsBattleOver` flag
- `Assets/Scripts/ThirdPerson/TerrainBattleUnit.cs` — registers with engine in Initialize; OnDestroy unregisters; legacy anim events drop silently when engine controls
- `Assets/Scripts/ThirdPerson/UnitBrainAI.cs` — `EngineControlled` flag; legacy state-machine no-ops past Decide
- `Assets/Scripts/ThirdPerson/UnitMovementController.cs` — `EngineMove(delta, blendSpeed)` for fixed-tick integration

### Editor tooling
- `Assets/Scripts/Editor/Tier1MoveCatalogCreator.cs` — `TacticalRPG → Combat → Create Tier 1 Move Catalog` menu (idempotent — re-runs update existing assets) + `Dump Move Catalog` debug menu

## Iteration log

### Iter 0 — first fight
- Engine ticks, brain hands off, locomotion runs forward.
- Issue: units run in circles — locomotion direction was unit.transform.forward (Slerped lazily), missing the moving target.
- Fix: target-aware basis in ApplyMovement (use direction-toward-target as forward when FaceTarget).
- Fix: knockback distance was 1/3 of intended — `MoveDirection` scales by `Time.deltaTime` (~16ms) but engine ticks at 50ms. Added `EngineMove(delta, blend)` that takes a pre-scaled world delta.

### Iter 1 — combo-pin (user reported "stuck 80% in hit anim")
- Defender caught in repeating react_hit_light because cancel chain hit lands every ~250ms while react is 300ms.
- Fix: i-frames on back half of `react_hit_light` (frames 3-5 of 6 active) so the second swing whiffs in-cone.
- Fix: Onslaught.PickReaction returns `defend_dodge_back` when self is currently in HitReact.
- Fix: stop-short threshold 1.6 → 1.9 so locomotion ends just outside swing reach.
- Result: Whiffs now happen, dodge_back fires occasionally, but defenders re-trigger react when re-hit during their own attack startup.

### Iter 2 — re-trigger noise + brain agency
- Added `UnitMoveExecution.consecutiveHitsTaken`. Increment on FullHit, reset on landing own hit OR on starting a Dodge/Block move.
- Onslaught.PickNeutral returns `defend_dodge_back` when `consecutiveHitsTaken >= 2 && rangeBand != Far`.
- `ForceMove` no longer restarts the same react if it's already playing (damage applies, but the move's frame counter keeps progressing toward exit — visually one hit pose, not five).
- Result: 1 dodge_back → 3 dodge_backs per 30s. React stagger time roughly halved.

### Iter 3 — Tier 2 catalog
- Added: `attack_punch_uppercut`, `attack_kick_low`, `attack_kick_crescent`, `attack_power_strike`, `defend_dodge_side_left/right`, `react_hit_sweep`, `react_launch_airborne`, `react_knockdown_back`.
- Cancel ladder: `jab → hook → uppercut → kick_crescent` (Launch finisher).
- Speed gates work: `power_strike (40)` and `kick_crescent (30)` only fire when speed allows.
- 1v1 Onslaught (Kai) vs Tempest (Grunt A) shows visibly different brains: Tempest side-dodges light hits; Onslaught only back-dodges when staggered.
- Heartbeat now includes hp / speed / pos / move id / consecutive hits.
- Result: 8 react_hit_lights / 30s with 60s of game time. ~10% time staggered per unit.

### Iter 4 — 60s 1v1 outcome
- Combat continues 60s without finishing — Grunt has 500 HP vs Kai's 100. Severe imbalance.
- Speed system depletes to 0 fast (Tier 2 attack costs > locomotion gain). Speed-gated moves rarely fire.
- 16 hits per 60s, 16 react_hit_lights — react time / unit ≈ 5%.

### Iter 5 — 2v2 multi-stance via execute_code scene-edit
- Used `mcp__unityMCP__execute_code` to add Hero_Mira (Tactician) and Enemy_Grunt_B (Sentinel) to the scene's playerHeroes / enemyTeam lists via reflection on the private fields. Saved in-place.
- Result: 4-unit fight; all four hand off correctly to engine.
- **Issue:** Sentinel sat at idle the whole fight (per design "no chase") but contributed nothing — Mira and Kai had to come to it, but they fought Grunt A and ignored Grunt B.
- **Issue:** consecutive `defend_dodge_side_left` reactions each tick (multiple attackers in lookahead → engine called PickReaction multiple times per Unity frame; each call reset the dodge to frame 0).
- Mira (80 HP) died around T+25-30. Kai survived to T+50.

### Iter 6 — Sentinel + Stalwart engagement; engine reaction-gate
- **Sentinel/Stalwart neutral** now picks `WalkForward` at Far/Mid range and `PunchJab` at Close/Locked, so they actually participate. Identity preserved (no orbit, no run, no chase past walk speed).
- **Engine `TryPickReaction`** now skips when the defender is already in Dodge / Block / Parry / Knockdown / Stun / Death (HitReact still reactable so the escape-dodge can fire).
- **Heartbeat** skips dead units so post-mortem ticks don't pollute the log.
- **CombatOverlayUI** now shows the engine's current move + frame + phase when `EngineControlled`, falling back to legacy `CombatState` only before handoff.

### Iter 7 — cancel-chain depth cap (combat rhythm)
- `UnitMoveExecution.cancelChainDepth` increments on cancel-into-attack, resets on non-attack move, starts at 1 on fresh attack from neutral.
- `StanceBrainBase.PickCancel` returns null when `cancelChainDepth >= MaxChainDepth` (default 3) — current move plays full recovery before next pick. Visible "beat" between exchanges.
- 2v2 60s with cap: 57 hits, 35 cancels (instead of unbounded chains), 54 react_hit_lights, battle ended in Defeat at T+49.1s — first natural finish.

### Iter 8 — speed gates lowered
- Tier 2 heavies (`attack_kick_crescent`, `attack_power_strike`) had gates of 30 / 40 — never fired because units hovered at 0–30 speed.
- Lowered to 20 / 25; speed costs reduced to 15 / 20.
- Result: 35 hits / 43s, 15 heavy / special moves fired (uppercut + crescent + power_strike).

### Iter 9 — seeded RNG for determinism
- `EngineRandom` (xorshift32) — owned by `BattleCombatEngine`, exposed via `BrainContext.rng`.
- `BattleCombatEngine.randomSeed` field: 0 → derive from `Time.frameCount` (variety); non-zero → fixed seed (reproducible).
- `OnslaughtBrain.PickNeutral` uses `ctx.rng.NextBool(0.4f)` to mix jab/hook (replaces frame-parity hack).
- Architecture in place; other brains can opt in incrementally.

### Iter 10 — Tier 3 stance signature moves
- Added `defend_parry` (Tactician), `defend_fade_out` (Wraith — speedGate 50, costs 25), `defend_static_anchor` (Sentinel — incomingMult 0.25), `defend_bob_weave` (Sharp+ in-place evasion).
- Verified in 2v2: Sentinel (Grunt B) fires `defend_static_anchor` 12 times in 44 s; Tempest (Grunt A) fires `defend_dodge_side_left` 37 times; Onslaught (Kai) back-dodges 9 times when consecutive hits ≥ 2; Tactician (Mira) side-dodges (parry rare since Tactician's parry gates on Sharp+ band which units rarely reach in extended fights — fallthrough is intentional).
- Stance differentiation now visibly reads: each unit's defensive choices match its archetype signature, not just its attack flavor.

### Final state — what combat looks like

A 60s 2v2 (Onslaught + Tactician vs Tempest + Sentinel) produces:
- All 4 units hand off at T+3, run toward each other
- First contact at T+5–6s
- 25–35 hits over 30–60s
- 25–55 react_hit_lights total = ~5–10 % stagger time per unit (was the 80 % the user observed before iter 1)
- Cancel chains cap at 3 → visible beats between exchanges
- Defenders escape pin via back-dodge when consecutive hits taken ≥ 2
- Side dodges from Tempest, parries from Tactician (when in Sharp+ band), block-anchor from Sentinel
- Heavies (uppercut, power_strike, kick_crescent) fire when speed permits
- Battle ends naturally on first death cascade

### Known gaps (out of engine scope)

- **Animation clips.** PlayMove fires animator triggers; whatever clip is bound (placeholder Mixamo) plays. The user owns the authoring queue per `MOVES_CATALOG.md`; engine's animation contract is in place and waits for clips.
- **Combat balance.** Grunts have 500 HP each, heroes 80–100. Heroes lose 2v2 unless data is rebalanced. Unit-stat tuning lives in `Assets/Data/*.asset`, not engine code.
- **Speed economy.** Units hover at 0–30 speed in extended fights because Hold-intent during attack drains faster than locomotion gains. Sharp/Primed band rarely reached. Tunable via `BattleSpeedSystem` rates.
- **Perception + world entities.** Reserved slots in `MoveDefinition.spawnsEntityPrefab` and `BrainContext` (`nearbyEntities`, `threats`) are present but no concrete entities (Earth Wall, Fire Zone) shipped yet — per `COMBAT_DESIGN.md` "When to land it", post-stable-engine work.
- **Multi-unit (3v3+) stress test.** Engine has run cleanly 2v2; not yet exercised at 5v5.

---

## How to verify after the night's iteration

1. Open `GameScene`.
2. `TacticalRPG → Combat → Create Tier 1 Move Catalog` (re-creates the 19 moves under `Assets/Resources/Moves/`).
3. `TacticalRPG → Combat Test → Run 60s Battle` — auto-runs 2v2.
4. Watch the play-mode camera; both teams should:
   - Run toward each other for ~3s
   - Engage with mixed punches / cancels / heavies
   - Defenders should dodge (back / side) and not loop in stagger
   - Battle ends with a side dying or 60 s timeout
5. After exit, read `Logs/combat-current.log`. Expected pattern:
   - `engine] handoff` for all units at T+~3s
   - `engine] next → ...` on neutral picks (varied moves)
   - `engine] cancel → ...` on combo extensions (capped at depth 3)
   - `engine] reaction → ...` on defensive picks
   - `engine] forced-reaction → ...` on hit (mostly `react_hit_light`)
   - `DMG ... FullHit` / `WHIFF` lines distributed roughly evenly across units
   - `SUMMARY BATTLE` outcome line at end
6. Visual: characters move smoothly, hit reactions slide them backward, occasional dodges create space, no unit pinned in stagger for >0.5s.

If any of the above is broken, the iteration log above gives the relevant fixes to revisit.

---

## Known issues / next iteration targets

1. **Speed system depletes fast.** Tier 2 moves have costs (uppercut 10, crescent 20, power 25) but Onslaught units spend more than they gain (close-range fighting → Hold intent → drain). Speed-gated moves rarely fire. Tunable via `BattleSpeedSystem` rates or by giving Onslaught some Disengage/Circle bias.

2. **No speed-band visual.** SpeedBarUI exists (legacy) but isn't visibly tied to band. Could add overlay rings / glow at Sharp / Primed thresholds.

3. **Animator triggers fire but clips may not exist.** PlayMove falls back to no-op if clip missing — so visually still placeholder Mixamo animations. This is the user's "animations are a disaster" caveat — they own the authoring queue per `MOVES_CATALOG.md`.

4. **Determinism not yet wired.** Brains use frame-parity / runtimeId for "random" picks instead of a per-battle seeded RNG. Sufficient for replay but not formalized as a `Random` singleton.

5. **Engine state not surfaced to UI.** `CombatOverlayUI` reads `unit.CombatState` which stays at `Decide` after handoff. Should expose engine state separately.

6. **Multi-unit (3v3+) not yet stress-tested.** Engagement system + target finder still gate frontline slots, but engine + brains haven't been exercised at scale.

7. **Perception / world entities reserved.** `MoveDefinition.spawnsEntityPrefab` slot exists; `BrainContext.PickPreparation` hook exists; concrete entities (Earth Wall, Fire Zone) not built. Per `COMBAT_DESIGN.md` "When to land it", this is post-stable-engine work.

---

## Test commands

- `TacticalRPG → Combat → Create Tier 1 Move Catalog` — re-create move SOs
- `TacticalRPG → Combat → Dump Move Catalog` — verify catalog state
- `TacticalRPG → Combat Test → Run 30s Battle` — auto-run 1v1 + log to `Logs/combat-current.log`
- `TacticalRPG → Combat Test → Run 60s Battle` — longer for outcome-watching
- `TacticalRPG → Combat Test → Stop Auto-Test` — abort

Combat log is `Logs/combat-current.log`; each run snapshots to `Logs/combat-<timestamp>.log`.

---

## Kubold animation bring-up (2026-05-07)

Phase 2 deliverable: prove the Kubold MovementAnimsetPro + FightingAnimsetPro
clips retarget cleanly onto the Mixamo `Ch24_nonPBR` skeleton via Animancer,
without manual avatar T-pose surgery.

### Outcome

Retargeting works **out of the box**. Both Kubold packs and the Mixamo
character import as humanoid (`animationType: 3`); Unity's muscle-space
retargeting handles the A-pose → T-pose offset cleanly. Idle, walk, and run
visually verified in Play mode — no shrugging, no droopy arms, no wrist
distortion. T-pose enforcement utility was scoped but **not needed**.

### What landed

- `Assets/Scripts/Systems/Combat/BattleAnimancerClipLibrary.cs` —
  generic `name → TransitionAsset` ScriptableObject. Reusable for any future
  clip-pack bring-up.
- `Assets/Scripts/Editor/KuboldClipLibrarySetup.cs` — editor menu
  `TacticalRPG → Kubold → Setup Test Clip Library`. Wraps 8 hand-picked
  Kubold clips in `TransitionAsset`s under `Assets/Data/AnimancerClips/Kubold/`,
  populates `Kubold_TestClipLibrary.asset`. Re-runnable.
- `Assets/Scripts/ThirdPerson/KuboldClipTester.cs` — runtime tester with
  on-screen legend. Press number keys 1–8 in Play mode to play each clip.
  Auto-plays `idle` on Start so the prefab doesn't sit in T-pose.
- `Assets/Scenes/KuboldClipTest.unity` — self-contained test scene. Spawns
  a `HeroPrefab` instance, disables battle-only components (so the prefab
  doesn't reach for `TerrainBattleManager.Instance`), attaches the tester,
  wires the library.

### The 8 wired clips

| # | Library id     | Source FBX                                       | Clip                       |
|---|----------------|--------------------------------------------------|----------------------------|
| 1 | `idle`         | `KB_Movement.fbx`                                | `KB_Idle_1`                |
| 2 | `walk_forward` | `KB_Movement.fbx`                                | `KB_WalkFwd1`              |
| 3 | `run_forward`  | `MovementAnimsetPro.fbx`                         | `RunFwdLoop`               |
| 4 | `punch`        | `KB_Punches.fbx`                                 | `KB_p_Jab_R_1`             |
| 5 | `kick`         | `KB_Kicks.fbx`                                   | `KB_p_MidKick_R_1`         |
| 6 | `block`        | `KB_Blocks.fbx`                                  | `KB_Block_Single`          |
| 7 | `dodge`        | `KB_Movement.fbx`                                | `KB_Dodge_R`               |
| 8 | `hit_react`    | `KB_Hits.fbx`                                    | `KB_Hit_m_HighFront_Med`   |

### How to run the bring-up test

1. Run `TacticalRPG → Kubold → Setup Test Clip Library` once
   (idempotent — re-points existing entries).
2. Open `Assets/Scenes/KuboldClipTest.unity`.
3. Press Play. Hero spawns in boxing-guard idle.
4. Number keys 1–8 cycle through the clip list. Hold previous frame on
   one-shots; press 1 to return to idle.

### Out of scope for Phase 2

- No wiring to the move engine yet (`MoveCatalog` still uses placeholder
  Mixamo). The library exists; binding move ids → library ids is the next
  task.
- Per-skill TransitionAssets (with named events for impact/recovery) are
  the longer-term home for combat clips per `07_PRESENTATION.md`. The
  test library is intentionally event-free — its job is to prove the rig
  works.

---

## TrainingDummy test scene (2026-05-07)

A focused button-driven bench for verifying clip playback on the prefab,
without any AI / engine in the loop. The Subject only does what its
buttons say; the Dummy stands in idle and ignores everything.

### Scene

`Assets/Scenes/Tests/TrainingDummy.unity`

- **TestSubject** — `HeroPrefab` instance at `(0, 0, 0)` facing +Z. Battle
  components (`UnitBrainAI`, `TerrainBattleUnit`, etc.) disabled so the
  prefab doesn't reach for `TerrainBattleManager.Instance`. Has a
  `TrainingDummyController` driving `AnimancerComponent` from the
  `Kubold_TestClipLibrary` asset.
- **Dummy** — same prefab at `(0, 0, 5)` facing −Z. Auto-plays `idle`,
  reacts to nothing.
- A thin red strip on the ground marks the 5m gap.

### UI

Screen-space overlay Canvas with two panels:

- **Left** — 8 vertical buttons (`1 — Idle` … `8 — Hit React`) each wired
  via persistent `UnityEvent` to the matching `TrainingDummyController.Play*`
  method on TestSubject. Survives scene save / Play-mode reload.
- **Top right** — debug overlay with three lines:
  - `State:` — legacy `TerrainBattleUnit.CombatState` if battle components
    are enabled; otherwise `TEST MODE (no AI)`. Will report meaningfully
    once we layer AI on this scene.
  - `Action:` — last button pressed (logical id from the library).
  - `Anim:` — the underlying clip name on Animancer's current state.

### What's wired

- `Assets/Scripts/ThirdPerson/TrainingDummyController.cs` — 8 public
  `Play*` methods + a generic `PlayClip(id)`. No AI hooks, no engine
  hooks. Reports `CurrentAction` / `CurrentAnimation` for the HUD.
- `Assets/Scripts/UI/TrainingDummyUI.cs` — refreshes the three labels
  every Update from the controller + battle component (when present).

### Verified before commit

Programmatic click on every button updated both labels and switched
the active Animancer state to the expected clip:

| Button | Action label    | Animation label                |
|--------|-----------------|--------------------------------|
| 1      | `idle`          | `KB_Idle_1`                    |
| 2      | `walk_forward`  | `KB_WalkFwd1`                  |
| 3      | `run_forward`   | `RunFwdLoop`                   |
| 4      | `punch`         | `KB_p_Jab_R_1`                 |
| 5      | `kick`          | `KB_p_MidKick_R_1`             |
| 6      | `block`         | `KB_Block_Single`              |
| 7      | `dodge`         | `KB_Dodge_R`                   |
| 8      | `hit_react`     | `KB_Hit_m_HighFront_Med`       |

### Phase 3: 3D environment + WASD locomotion

The bench was upgraded from "buttons-only on a small plane" to a
fully-3D playable scene with WASD on the Subject. The Dummy still
stands still and ignores everything.

**New components on the Subject:**
- `TrainingPlayerController` — WASD-driven `CharacterController` movement,
  Shift to run, character rotates to face movement direction, cursor
  locked on Start (Escape toggles).
- `KuboldLocomotionDriver` — watches `CharacterController.velocity` and
  picks `idle` / `walk_forward` / `run_forward` from the library. When a
  one-shot fires (button click), `TrainingDummyController.PlayClip`
  calls `KuboldLocomotionDriver.SuppressFor(clipLength)` so the punch /
  kick / hit-react can play through without locomotion stomping it.
- `ThirdPersonCamera` (re-used from GameScene) — orbits the Subject on
  mouse delta when the cursor is locked. The camera has a static
  fallback position so the scene reads even before the user takes
  control.

**Scene changes:**
- 60×60 lit ground plane (URP/Lit, dark grey) instead of the original 10×10.
- Camera repositioned at `(0, 2.2, -4.5)` looking down at +Z.
- Help panel bottom-left lists the controls.

**How to drive it:**
1. Open `Assets/Scenes/Tests/TrainingDummy.unity`, press Play.
2. Cursor is locked → mouse orbits camera, WASD walks (Shift to run).
3. Press Escape to free cursor for clicking the 8 buttons; press Escape
   again to relock and resume orbiting.
4. Buttons fire one-shots and hold the last frame; press WASD or **1 — Idle**
   to return to locomotion.

### What's next on this bench

- **AI takeover** — re-enable `UnitBrainAI` on the Subject, connect the
  legacy state machine to the move engine, and watch the State / Action
  labels here become live. This is what turns the "TEST MODE" line into
  a real AI debug surface.

---

## Phase 4: Full clip library (2026-05-07)

The Kubold library was expanded from the 8-clip bring-up to the **full
movement + combat set** drawn exclusively from `MovementAnimsetPro` and
`FightingAnimsetPro`. `Assets/Scripts/Editor/KuboldClipLibrarySetup.cs`
now wires **156 logical ids** backed by **134 unique TransitionAssets**
under `Assets/Data/AnimancerClips/Kubold/`.

### What's covered

- All 23 ids in the existing move catalog (`Resources/Moves/`):
  `idle`, `locomotion_walk_forward`, `locomotion_run`, `attack_punch_*`,
  `attack_kick_*`, `attack_power_strike`, `defend_*`, `react_hit_*`,
  `react_launch_airborne`, `react_knockdown_back`, `death_collapse`. So
  the move engine can resolve `MoveDefinition.animationName` against the
  library directly when we wire the Animancer bridge.
- 6 idle variants (`idle_combat_2..6`, `idle_relaxed_2..3`) for variation pools.
- Directional locomotion: `walk_back`, `walk_strafe_L/R`,
  `walk_back_left/right`, `sidestep_L/R`, `skip_forward/back`,
  `turn_R/L_90/180`, `sprint_forward`, run / walk lean variants.
- Punch attack variants: jab L/R, hook L/R, uppercut L/R, one-two,
  one-two-three, double-jab, double-hooks, elbow L/R, overhand L/R,
  backfist L/R, rabbit punch, super-punch.
- Kick attack variants: low/mid/high L/R, axe, roundhouse, side, knee
  L/R, kick-uppercut, mid-back, low-round.
- Defense set: block start/loop/end + crouch variants, mid-block L/R,
  duck L/R + parry-duck, dodge L/R, fade-out.
- Hit-react spread: 9-direction `hit_*_weak`, 7-direction `hit_*_med`,
  4-direction `hit_*_stagger`, plus on-ground hits.
- KO / death set: 12 KO variants from `KB_KOs.fbx` (HighKO L/R/Air/
  Powerful, MidKO + Back + Powerful, LowKO L/R, KO_Head, UpperKO + 2 +
  Flip, TopKO).
- Get-up clips for ragdoll recovery: `getup_back`, `getup_back_180`,
  `getup_face`, `getup_face_180`, `layback_roll`.

### TransitionAssets are deduplicated

When two ids share the same underlying clip (e.g. `idle` and `walk_forward`
overlap with `locomotion_walk_forward`), the setup script reuses the
same `TransitionAsset` rather than creating duplicates — 156 ids → 134
files. Library entries are aliases over a smaller pool of clips.

### Engine bridge — not yet wired

The library contains the clips; the move engine still uses
`UnitAnimationDriver`'s legacy Animator-trigger path. Connecting the
two means either (a) extending `BattleAnimancerDriver` to `Play(libraryId)`
on a unit's `AnimancerComponent`, or (b) extending
`UnitAnimationDriver.PlayMove` to look the id up in the library and play
through Animancer when an `AnimancerComponent` is present. Either is a
~50-line addition; deferred to the engine-integration task.

---

## Phase 5: Ragdoll system (2026-05-07)

Skill-driven ragdoll for "knockdown" effects, using the standard
Kubold/Mixamo Humanoid skeleton. Two pieces:

### `Assets/Scripts/Editor/RagdollBaker.cs`

`TacticalRPG → Ragdoll → Bake On Selected` (or `RagdollBaker.Bake(go)`
via code) adds physics components to the 12 ragdoll bones of a Humanoid
Animator:

- **Rigidbody** with anatomical mass distribution (Hips 12kg, Spine 8kg,
  Chest 8kg, Head 4kg, UpperArms 3kg, ForeArms 2kg, UpperLegs 8kg,
  LowerLegs 4kg — total ~70kg).
- **Collider** sized to bone length: `BoxCollider` on torso, `SphereCollider`
  on head, `CapsuleCollider` on limbs (axis auto-detected from bone vs
  child position).
- **`ConfigurableJoint`** on every bone except Hips, parented to the
  parent bone's rigidbody. Translation locked at the joint anchor;
  angular limits anatomical (elbows/knees ~150° hinge, shoulders ~90/90/60,
  spine ±25°, head ±30/35/50, hips ±90/45/30).
- `slerpDrive` with per-bone spring/damper baked from the spec (Spine
  4800/240, Chest 4800/240, Head 2400/120, UpperArms 1600/78, ForeArms
  1200/60, UpperLegs 5600/270, LowerLegs 3600/180 after the post-bake
  ×0.4 softening).
- Pairwise `Physics.IgnoreCollision` between all 12 ragdoll bones so
  the body doesn't punch itself.
- An `ActiveRagdollDriver` is added to the root with the bone list
  populated; the driver becomes the API surface.

Re-runnable: prior physics components on the spec bones are stripped
before re-baking.

### `Assets/Scripts/ThirdPerson/ActiveRagdollDriver.cs`

Two-mode driver. State persists in serialized fields so the Inspector
shows what's bone-by-bone.

**Animation-driven (default).** All ragdoll rigidbodies are *kinematic*;
the Animator writes bone transforms unfought. Physics overhead is
near-zero. This is the resting state during normal combat and idle.

**Ragdoll mode** (`ActivateRagdoll()`):
- All bones flip to non-kinematic, gravity on, joint drives go to zero.
- The Animator is disabled so it doesn't fight physics.
- The `CharacterController` on root is disabled so the body isn't anchored
  in mid-air.
- Bones fall under gravity, react to impulses via `ApplyImpulse(boneName,
  force)` and `ApplyImpulseAll(force)`, tangle realistically.

`DeactivateRagdoll()` flips back: kinematic bones, springs restored,
Animator + CC re-enabled, root teleported to current hips position so
the Animator's next pose snaps cleanly.

### How a "knockdown skill" uses it

```csharp
ragdoll.ActivateRagdoll();
ragdoll.ApplyImpulse("Chest", -attacker.forward * 200f);
// … wait for ground contact / timer …
ragdoll.DeactivateRagdoll();
animancer.Play(library.Get("getup_back"));   // recovery clip from the get-up set in Phase 4
```

### What the user can test in TrainingDummy

Three new buttons on the left panel (red-tinted to distinguish from the
animation-test buttons):

| Button             | What it does |
|--------------------|--------------|
| `9 — Punch Chest`  | Activates ragdoll + applies a back-and-up impulse on the Chest bone. Body tumbles backward. |
| `0 — Go Limp`      | Activates ragdoll without an impulse. Body collapses straight down. |
| `R — Recover`      | Restores animation-driven mode. Animator pose snaps back to current root. |

Verified end-to-end: clean idle pose under animation, clean collapse on
Go Limp, clean recovery, plausible knockback on Punch Chest. No mass
explosion or jittering at rest.

### Why Tier 1+2 instead of Tier 3 active drive

The plan was Tier 3 (active ragdoll — every-hit physical deflection
while standing). The single-skeleton approach hit the expected wall:
the Animator writes bone transforms each frame, and on a non-kinematic
rigidbody Unity reads transform → rigidbody.position implicitly,
overriding any physics velocity from impulses. Result: bones snap
straight back to animation pose, no visible deflection.

The remaining path to true Tier 3 is **dual skeleton**: clone the
Mixamo bone hierarchy, animator drives the source clone, physics
drives the target clone, mesh skinned to the target. Each `FixedUpdate`
copy source-bone rotations to target-joint targets via `slerpDrive`.
That's a separate task — the bake here lays the groundwork (bones,
joints, colliders, mass) and the driver leaves room for it.

For the user's stated need ("skills with chance to ragdoll") Tier 1+2
covers the case end-to-end.
