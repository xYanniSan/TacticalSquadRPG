# MOVES_CATALOG.md

> **Tier 2 — Stable.** The master list of all combat moves and their animation handles. **This file is a contract:** code references these names; clips authored under matching names automatically wire in. Lives alongside `COMBAT_DESIGN.md` in `Docs/Design/`.

---

## Purpose

Combat is move-based (see `COMBAT_DESIGN.md` "Combat engine — move-based, frame-data driven"). Every move has:

- An **animation name** — a string handle the code passes to `UnitAnimationDriver.Play(string)`
- **Frame data** — startup / active / recovery in 50ms ticks
- **Hit / defensive properties** — damage, range, i-frames, super armor
- **Cancel rules** — what other moves can chain off this one
- **Reaction tag** — what defender response this move pairs with

This document is the **animation authoring queue** — when authoring clips, work from this list. The names listed here are stable; new clips bound to these names automatically replace whatever placeholder/fallback was being used.

When a move name isn't in this catalog, code should not reference it. When code adds a new move, add the row here in the same task.

---

## Naming convention

```
<category>_<subcategory>_<descriptor>[_<variant>]
```

- `category`: `locomotion` / `attack` / `defend` / `react` / `cast` / `mobility` / `finisher` / `death` / `idle`
- `subcategory`: short discriminator (`punch`, `kick`, `block`, `dodge`, `hit`, etc.)
- `descriptor`: specific move name
- `variant`: optional direction / element / weight (`_left`, `_heavy`, `_fire`)

Example: `attack_punch_uppercut` / `defend_dodge_side_left` / `react_hit_heavy_back` / `cast_handsign_a`.

---

## Frame-data conventions

Frames are **50ms each (20Hz tick rate)**. A 12-frame move is 600ms.

| Phase | Meaning | Notes |
|---|---|---|
| **Startup** | Committing to the move; no hit, can be interrupted | Long startup = readable telegraph; short startup = fast pressure |
| **Active** | Hitbox is live (for attacks) / i-frames active (for dodges) / armor active (for parries) | Usually 1–4 frames |
| **Recovery** | No hit, no defense, can't act | Last `cancelWindowFrames` accept cancel into a chained move |
| **Cancel window** | Defined as the trailing N frames of recovery during which a chained move can replace this one |

Templates by category:

| Category | Typical startup | Typical active | Typical recovery |
|---|---|---|---|
| Light attack | 2-3 frames | 1 frame | 4-5 frames |
| Heavy attack | 6-12 frames | 1-2 frames | 10-16 frames |
| Cast (sign, focus) | 4-6 frames | 0 (no hit) | 4-6 frames |
| Big cast (Triple Sign, Summoning) | 16-30 frames | 1 frame | 8-12 frames |
| Block | 1 frame | until hit / cancelled | 4 frames |
| Dodge | 1-2 frames | 3-4 frames i-frames | 3-4 frames |
| Parry | 0-1 frames | 3 frames parry window | 8 frames |
| Hit react (light) | n/a | 6 frames stagger | n/a |
| Hit react (heavy) | n/a | 12 frames stagger | n/a |
| Knockdown | n/a | 50 frames (launch + prone + rise) | n/a |
| Stun | n/a | duration-based (CC) | n/a |
| Locomotion | 0 | continuous loop | 0 |
| Finisher (Launch combo) | varies | varies (multi-segment) | varies |

---

## Move catalog

### Locomotion (always playable in neutral)

| Animation name | Notes |
|---|---|
| `idle` | Default neutral pose. Brain falls back here when no plan. |
| `locomotion_walk_forward` | +0/sec speed gain |
| `locomotion_walk_backward` | -2/sec (defensive bleed) |
| `locomotion_strafe_left` | sideways |
| `locomotion_strafe_right` | sideways |
| `locomotion_run` | +8/sec when toward target |
| `locomotion_backstep` | +3/sec, brief back-step (different from `defend_dodge_back`) |
| `locomotion_sprint` | +10/sec, animation-locked briefly |
| `locomotion_dash_forward` | fast straight-line, 0.18s, ends in neutral |
| `locomotion_orbit_clockwise` | sustained circle (Wraith / Tactician signature) |
| `locomotion_orbit_counterclockwise` | reverse |

### Light attacks (cancellable, low damage, fast)

| Animation name | Startup | Active | Recovery | Damage | Cancel into | Reaction tag |
|---|---|---|---|---|---|---|
| `attack_punch_jab` | 2 | 1 | 4 | 8 | `attack_punch_hook`, `attack_punch_uppercut`, `attack_power_strike` | LightHit |
| `attack_punch_hook` | 3 | 1 | 5 | 10 | `attack_punch_uppercut`, `attack_kick_crescent` | LightHit |
| `attack_punch_uppercut` | 4 | 1 | 6 | 12 | `attack_kick_crescent`, `attack_kick_axe` | Launch |
| `attack_kick_low` | 3 | 2 | 5 | 9 | `attack_kick_crescent` | Sweep |
| `attack_elbow` | 2 | 1 | 4 | 7 | `attack_punch_hook` | LightHit (close-range only) |
| `attack_knee` | 3 | 1 | 5 | 9 | `attack_punch_uppercut` | LightHit (close-range only) |

### Heavy attacks (commit, high damage, slow)

| Animation name | Startup | Active | Recovery | Damage | Notes | Reaction tag |
|---|---|---|---|---|---|---|
| `attack_power_strike` | 8 | 1 | 12 | 22 | speedScaling 0.5 | Heavy |
| `attack_kick_axe` | 10 | 1 | 14 | 25 | downward overhead | Heavy |
| `attack_slam_ground` | 12 | 2 | 16 | 28 | knockdown on hit | Knockdown |
| `attack_kick_crescent` | 6 | 2 | 10 | 18 | Launch combo entry, gates speed≥30 | Launch |
| `attack_lunge_strike` | 4 | 1 | 8 | 14 | translates forward 2u during startup | Heavy |
| `attack_double_palm` | 8 | 2 | 12 | 24 | knockback on hit | Heavy |

### Sign / cast attacks (rooted, ranged, energy cost)

| Animation name | Startup | Active | Recovery | Notes |
|---|---|---|---|---|
| `cast_handsign_a` | 4 | 0 | 6 | buff move, no damage; energyCost 10 |
| `cast_handsign_b` | 4 | 0 | 6 | same shape, different element |
| `cast_handsign_c` | 4 | 0 | 6 | same shape, different element |
| `cast_focus` | 6 | 0 | 4 | adds powerBoost; energyCost 15 |
| `cast_triple_sign` | 20 | 1 | 8 | rooted, projectile, can be interrupted |
| `cast_orb_ray` | 10 | 1 | 8 | instant ray on each orb |
| `cast_geomagnetic` | 16 | 1 | 8 | mobile cast, Earth element |
| `cast_thunderstorm` | 16 | 1 | 8 | mobile cast, Lightning |
| `cast_mudslide` | 16 | 1 | 8 | mobile cast, Water |

### Entity-spawning moves (place a thing in the world)

These moves' active frame **spawns a `CombatEntity`** in the world — a wall, hazard, summon, trap, or marker. See `COMBAT_DESIGN.md` "Perception, world entities, and predictive reactions" for the full design.

The spawned entity is independent of the caster, has its own HP and lifetime, and interacts with subsequent hit resolution (e.g. walls block projectiles before they reach the caster's allies).

| Animation name | Startup | Active | Recovery | Spawned entity | Notes |
|---|---|---|---|---|---|
| `cast_earth_wall` | 4 | 1 | 6 | `EarthWallEntity` | 1.5u in front of caster, faces target; blocks projectiles & melee from one side; HP 60; despawns after 8s |
| `cast_ice_wall` | 5 | 1 | 6 | `IceWallEntity` | Same shape as Earth Wall, slows on contact when broken |
| `cast_stone_pillar` | 6 | 1 | 8 | `StonePillarEntity` | At target's feet; blocks movement and projectiles; HP 80 |
| `cast_fire_zone` | 8 | 1 | 6 | `FireZoneEntity` | At target's feet; ticks 4 dmg/sec to units within bounds; lasts 4s |
| `cast_ice_floor` | 8 | 1 | 6 | `IceFloorEntity` | Reduces movement speed of units crossing; lasts 6s |
| `cast_thunder_pillar` | 10 | 1 | 8 | `ThunderPillarEntity` | Periodic AOE pulse at fixed point; lasts 5s |
| `cast_poison_cloud` | 10 | 1 | 8 | `PoisonCloudEntity` | DOT for units inside; lasts 8s |
| `cast_summoning` | 30 | 0 | 10 | `GuardianSummonEntity` | Rooted long cast; spawns ally guardian unit; one per caster |
| `cast_orb_summon` | 20 | 0 | 6 | `OrbSummonEntity` | Spawns N orbiting projectile entities |
| `cast_mark_target` | 6 | 1 | 4 | `MarkEntity` | Attaches to target; ally moves consume mark for damage bonus |
| `cast_trap_rune` | 12 | 1 | 8 | `TrapEntity` | Placed at caster's feet; proximity-triggers on enemy entry; one-shot |

**Implementation note**: the move's `MoveDefinition.spawnsEntity` field references an entity prefab; the engine spawns it on the move's active frame at the configured offset. Entity behaviour after spawn is owned by its `CombatEntity` subclass — moves are agnostic to what each entity does.

**Authoring**: animation for each spawn move is the *caster's gesture*, not the entity itself. The entity (wall, fire zone, etc.) has its own visual asset bound separately. So `cast_earth_wall` the move only needs a hand-sign + ground-strike animation; the wall mesh / VFX is a separate asset paired with `EarthWallEntity` directly.

### Combo finishers (cinematic, gated by speed)

| Animation name | Notes |
|---|---|
| `finisher_launch_combo` | Reference 3 — Crescent Kick + aerial flurry + KnockbackFar. Multi-segment, ~1.5s total. |
| `finisher_fade_strike_chain` | Reference 1 — TeleportFlank ×4 with strikes between. Wraith Primed only. |
| `finisher_pose_attack` | Reference 5 — Zuko-style frozen pose at impact. Camera holds. |
| `finisher_domain_expansion` | JJK locked-mode mini-cinematic. Gates 1v1 exchange briefly. |
| `finisher_air_clash` | Naruto/Sasuke aerial collision. Both units launched into mutual mid-air strike. |
| `finisher_blast_off` | Cinematic killing blow — KnockbackFar to walls/horizon. |

### Defensive (reactive)

| Animation name | Startup | Active | Recovery | Notes |
|---|---|---|---|---|
| `defend_block_idle` | 0 | passive | 0 | Stance-default for Stalwart/Sentinel; converts to `defend_block_react` on hit |
| `defend_block_react` | 1 | until hit + 4 | 4 | -50% damage, +5 energy on hit |
| `defend_dodge_back` | 2 | 4 i-frames | 4 | parabolic backflip, 3.5m back |
| `defend_dodge_side_left` | 2 | 3 i-frames | 3 | sidestep, stays in range |
| `defend_dodge_side_right` | 2 | 3 i-frames | 3 | sidestep, stays in range |
| `defend_bob_weave` | 1 | 2 i-frames | 2 | in-place narrow window (Sharp+ band) |
| `defend_parry` | 0 | 3 parry-window | 8 | catches incoming attack, opens punish-window |
| `defend_static_anchor` | 0 | passive | 0 | -75% damage, no movement (Sentinel/Stalwart) |
| `defend_fade_out` | 1 | 4 i-frames | 2 | Wraith Primed teleport with `GhostTrail`, reappear at Mid range |

### Hit reactions (forced, no startup — instant on hit)

| Animation name | Active frames | Notes |
|---|---|---|
| `react_hit_light` | 6 | brief stagger |
| `react_hit_heavy` | 12 | longer stagger |
| `react_hit_sweep` | 8 | recovery from being swept low |
| `react_launch_airborne` | varies | launched; takes 1.5× damage during; ends with `react_knockdown_back` if HP > 0 |
| `react_knockdown_back` | 50 (launch + prone + rise) | knocked back, prone for ~1.5s, vulnerable to follow-up |
| `react_knockdown_forward` | 50 | knocked forward, prone |
| `react_recoil_blocked` | 4 | brief shove backward after blocking |
| `react_stunned` | duration-based | CC state, cannot act |
| `react_dazed` | 10 | partial action lockout (post-Interrupt) |
| `react_burn` | 4 | fire-status flinch |
| `react_freeze` | 8 | ice-status hesitation |

### Movement abilities (skill-driven movement)

| Animation name | Notes |
|---|---|
| `mobility_dash_through` | passes through opponent, ends behind |
| `mobility_teleport_flank` | Wraith — teleport to flank with `GhostTrail` |
| `mobility_air_jump` | jump-cancel mid-combo to airborne |
| `mobility_wallbounce` | hit a wall, bounce off (environment interaction) |
| `mobility_slide` | low-profile slide; goes under high attacks |

### Death & defeat

| Animation name | Notes |
|---|---|
| `death_collapse` | fall forward; default |
| `death_blast` | blown back by killing strike; pairs with `KnockbackFar` |
| `death_kneel` | slow defeat, dignified |
| `death_air` | killed mid-air; ragdoll fall |

---

## Per-stance default move pools

Each stance picks moves preferentially from its pool. Moves not in a stance's pool are still pickable when the move's `cancelInto` chain leads there (you can finish a combo even if your stance "shouldn't" pick the finisher).

| Stance | Neutral pool | Cancel preferences | Reaction picks |
|---|---|---|---|
| **Onslaught** | `locomotion_run`, `attack_punch_jab`, `attack_punch_hook` | always cancel on hit | `defend_block_react`, light dodges |
| **Tempest** | `locomotion_dash_forward`, `attack_lunge_strike`, `attack_kick_crescent` | cancels into Launch | `defend_dodge_back`, `defend_dodge_side_*` |
| **Stalwart** | `idle`, `defend_block_idle` | rare; only follow-throughs | `defend_block_react`, `defend_static_anchor` |
| **Tactician** | `locomotion_orbit_*`, `attack_punch_jab` | cancels into counter after parry | `defend_parry`, `defend_dodge_side_*` |
| **Wraith** | `locomotion_orbit_*`, `mobility_teleport_flank` | rarely cancels | `defend_fade_out`, `defend_dodge_side_*` |
| **Sentinel** | `idle`, `defend_static_anchor` | never cancels | `defend_static_anchor` only |
| **Conduit** | `locomotion_walk_backward`, `cast_handsign_*`, `cast_triple_sign` | cancels sign chains | `defend_fade_out`, `defend_dodge_back` |

---

## Authoring priorities

If you have a finite amount of time to author clips, do them in this order. Each tier produces a meaningful gameplay improvement.

### Tier 1 — Core fight loop (without these, combat is mute)

- `idle`
- `locomotion_run`
- `locomotion_walk_forward`
- `attack_punch_jab`
- `attack_punch_hook`
- `defend_block_react`
- `defend_dodge_back`
- `react_hit_light`
- `react_hit_heavy`
- `death_collapse`

10 clips. Bare-minimum playable combat.

### Tier 2 — Combo identity

- `attack_punch_uppercut`
- `attack_kick_low`
- `attack_kick_crescent` (the Launch entry)
- `attack_power_strike`
- `react_launch_airborne`
- `react_knockdown_back`
- `defend_dodge_side_left`
- `defend_dodge_side_right`

8 more clips. Crescent Kick / Power Strike now feel distinct from jab/hook. Aerial combo readable.

### Tier 3 — Stance signatures

- `defend_static_anchor` — Sentinel / Stalwart identity
- `defend_fade_out` — Wraith identity
- `defend_parry` — Tactician identity
- `mobility_teleport_flank` — Wraith fade-strike
- `locomotion_orbit_clockwise` / `_counterclockwise` — Tactician/Wraith preludes
- `locomotion_dash_forward` — Tempest entry
- `defend_bob_weave` — Sharp band evasion

7 more clips. The 7 stances now visibly differ from each other.

### Tier 4 — Casts and signs

- `cast_handsign_a`, `_b`, `_c`
- `cast_focus`
- `cast_triple_sign`
- `cast_orb_summon`
- `cast_summoning`

6 clips. Conduit / caster archetypes become real.

### Tier 5 — Finishers and cinematics

- `finisher_launch_combo` (multi-segment cinematic)
- `finisher_pose_attack` (frozen-pose moment)
- `finisher_blast_off` (killing blow KnockbackFar)
- `finisher_fade_strike_chain` (Wraith ultimate)

4 set-pieces. Each fight now has potential moments.

### Tier 6 — Polish

Everything else — direction-specific hit reacts, element-specific casts, environment interactions, secondary CC visuals.

---

## How code references these

```csharp
// Engine triggers a move:
animDriver.Play(move.animationName);
//   move.animationName = "attack_punch_jab"

// Driver looks up:
//   1. Animancer transition registered under that name?
//   2. Animator state with that name?
//   3. Fallback (e.g. "attack_punch_*" → fallback to "anim_punch")?
//   4. No-op (combat continues regardless)
```

The lookup is cached per name. First hit on a missing name logs once at info level; combat doesn't error.

---

## Maintenance rules

- **Adding a move**: add the row to this catalog FIRST, then write the `MoveDefinition` SO using that name. Don't reference a name in code that isn't here.
- **Renaming a move**: update this catalog, the `MoveDefinition` SO, and any cancel-into references at the same time.
- **Removing a move**: deprecate it in this catalog (move under a "Removed" section with the reason) before deleting the asset, so future-you knows why it's gone.
- **The catalog is the source of truth.** If reality diverges from this list, the list is wrong and needs updating, OR the code is wrong and needs fixing.

---

## Frame-data tuning notes

The frame counts above are **starting values**, intended to be tuned during integration. Anchors:

- **Light attack total ≈ 350ms (7 frames)** — fast enough to chain into a 5-hit combo at ~1.75s
- **Heavy attack total ≈ 1000ms (20 frames)** — slow enough to be punishable on whiff, fast enough to land in time when committed
- **Triple Sign cast ≈ 1500ms (30 frames)** — long enough that Conduit needs protection, short enough not to feel oppressive in soloplay
- **Dodge i-frames ≈ 200ms (4 frames)** — enough to consistently avoid one strike, not enough to stack-dodge a 5-hit combo without retreat

When tuning, change the catalog row first, then re-validate downstream balance (combo damage totals, exchange duration distribution).
