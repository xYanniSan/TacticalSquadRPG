# ENVIRONMENT_DESIGN.md

> **Tier 2 — Stable.** Design spec for combat environment and weather. Lives alongside `COMBAT_DESIGN.md` and `LONG_TERM_VISION.md` in `Docs/design/`.
>
> This is a forward-looking design document. The current implementation has no environment or weather systems. This doc defines the target system and the implementation roadmap to get there.

---

## Purpose

This document defines how the **combat environment** affects fights — physically, kinetically, and atmospherically. Combat doesn't happen in a sterile arena; it happens in a *place*, and the place fights back.

The design philosophy here is consistent with the rest of the combat design: **the environment matters because it's there, not because designers labeled invisible regions.** Heroes get physically thrown into rocks. Heroes gain altitude advantage by being on actual high ground. Heroes' line of sight is blocked by actual trees. Weather is a global atmospheric state, visible and felt.

There are **no invisible buff zones**. There are **no fire patches that damage you when you stand in them**. The environment affects combat through *physical interaction* and *global atmospheric state*, not through abstracted regions of effect.

For combat backend systems see `COMBAT_DESIGN.md`. For visual/feel direction see `07_PRESENTATION.md` and `LONG_TERM_VISION.md`.

---

## Design philosophy

Three principles drive every decision in this doc:

**Physical over abstract.** A unit should never take damage because they're "in a fire region." They should take damage because they got *thrown into a fire*. Environment effects are caused by collisions, positions, and physical interactions — not by membership in an invisible volume.

**Global over zoned.** Weather affects the whole battlefield uniformly. No "rain only in this corner." This is what keeps weather *legible* — players see rain everywhere or fire everywhere, and they understand the global combat conditions immediately.

**Cinematic over systemic.** The environment exists primarily to produce *moments* — getting smashed through a wall, leaping off a cliff for a flying kick, the camera framing a hero silhouetted against a lightning storm. If a feature wouldn't be used to produce a memorable moment, it doesn't earn its complexity.

---

## What this design includes

The environment design includes two distinct systems:

1. **Kinetic terrain interactions** — physical collision with terrain features (knockback into walls, altitude advantage, line of sight from foliage)
2. **Weather and atmospheric state** — global modifiers based on current weather conditions, including weather-altering skills

These are independent systems that integrate with combat at different points. They do not depend on each other; either can be implemented before the other.

## What this design explicitly excludes

To keep scope honest, the following are **not** part of this design:

- **Invisible effect zones** — no fire patches, poison clouds, electric pools, sacred ground, or any other "step into this region to gain or lose effects." Combat doesn't reward you for memorizing volumes; it rewards you for being where you are.
- **Destructible meshes** — rocks don't shatter into voxels; trees don't fall down. They're solid features that interact with combat physically but don't get destroyed in the process.
- **Runtime terrain deformation** — no craters from heavy attacks, no carved trenches, no permanent map modification during combat. The map is what it is.
- **Procedural environment generation** — every map is hand-authored.

These exclusions are deliberate scope discipline. Adding any of them is a multi-month commitment that reshapes the entire art and engineering pipeline. They are tracked in `08_ROADMAP.md` as deep-future possibilities, not active design targets.

---

## Kinetic terrain interactions

This section covers the *physical* environment — terrain features that combat collides with and reads from.

### What terrain features matter

Combat reads three properties of the environment:

| Feature | Combat impact |
|---|---|
| **Solid surfaces** (walls, large rocks, trees) | Knockback collision, line of sight blocking |
| **Altitude differential** (cliffs, slopes, raised platforms) | Reach and damage modifiers, fall damage |
| **Foliage / vegetation** | Line of sight reduction, target acquisition obstruction |

These are the only environment features that affect combat. Maps may have many other visual features (water, paths, decorative props) that are purely cosmetic and ignored by the combat system.

### Knockback into terrain

The most important and most cinematic environment interaction.

**Trigger:** When `BattleKnockbackSystem` moves a unit and the unit collides with a marked terrain feature mid-knockback.

**Effects:**

- Additional damage equal to 30% of the original strike's damage
- Knockback velocity reverses briefly (the unit "bounces" off slightly)
- Hit-stop tier is upgraded one level (Light → Medium, Medium → Heavy)
- A new CC effect is applied: brief Knockdown (1.0s, half normal duration)
- Camera shake intensifies
- A visual impact effect plays at the collision point (debris, dust, particles)

**Why this matters mechanically:**

- It rewards positioning play — drive enemies toward walls; avoid being driven into them
- It rewards heavy-knockback skills against melee-heavy enemies (more likely to be near terrain)
- It's a setup-and-payoff opportunity — Hero A drives the enemy back; Hero B finishes against the wall

**Why this matters cinematically:**

- It's the most reliably anime moment in any fight: enemy goes flying, smashes through into the rock face, slumps down. Players record this. Players share this.

### Knockback into trees and small props

Smaller solid features (trees, small rocks, fences) work similarly but with reduced effects:

- 15% bonus damage (vs. 30% for walls/large rocks)
- Brief stagger, no knockdown
- The feature itself is unaffected — trees don't fall, rocks don't break

Trees blocking knockback create *dynamic* combat positioning — a forested map fights differently than an open one because there are more knockback-collision points.

### Knockback into water or pits

If knockback drives a unit off a cliff or into water:

- Unit takes fall damage proportional to the height differential
- Unit re-enters combat after a 2-3s recovery (climbs out of water; teleports back from fall)
- During recovery, unit is treated as Knocked Down (vulnerable, can't act)
- 4.0s grace period before they can be knocked off again (anti-spam)

This is a *premium* environment moment. Maps with cliff edges or water features create real positional risk.

### Altitude advantage

Units fighting from higher ground gain meaningful combat advantages:

| Property | Modifier |
|---|---|
| Attack range (melee) | +20% on downward strikes |
| Attack damage | +10% on downward strikes |
| Dodge chance against upward attackers | +15% |
| Speed-gain rate from movement | +20% (running downhill is easier) |

Conversely, attackers striking *upward* take penalties:

- Range -10%, damage -5%
- Speed-gain from uphill movement -25%

**Implementation:** A height differential of more than ~1.5 units between attacker and target qualifies as "altitude advantage." Below that threshold, no modifier applies. This avoids constant ±5% noise on slightly uneven terrain.

**AI awareness:** The brain reads altitude differential when picking targets and movement intents. Aggressive stances on high ground hold position to maintain advantage; Defensive stances retreat upward when wounded; Wraith-style hit-and-runs leverage altitude for hit-and-run patterns.

This is what makes maps with vertical structure interesting. A flat arena and a multi-tiered ridge produce visibly different combat without requiring different rules — the same systems just have more inputs.

### Line of sight and foliage

Some skills (ranged casts, projectile attacks) require line of sight to fire effectively:

- **Solid walls and large rocks** completely block line of sight; a target behind cover cannot be hit by ranged casts
- **Trees and dense foliage** reduce target acquisition range (the targeting system treats foliage-obscured enemies as further away than they are)
- **Sparse vegetation** (grass, bushes) has no combat effect

Melee skills are not affected by line of sight — if a unit can reach the target physically, they can hit them.

**Caster archetype implication:** A caster facing terrain-rich maps must reposition to maintain line of sight, which slows their offensive cadence. This is a fair tradeoff — casters are powerful in open arenas, less so in forests. Players adapt their roster to the map.

### Map authoring guidelines

For maps to leverage these systems, they must be authored with combat-relevant features in mind. Guidelines for the (eventual) map team:

- **Mark solid surfaces** (walls, large rocks, trees) with a `KnockbackCollidable` component or layer
- **Mark altitude regions** clearly — don't make terrain ambiguous in height
- **Provide at least one knockback-friendly wall** per arena — players should always have *some* opportunity to use the system
- **Don't over-clutter** — too many trees and the combat reads as chaotic. Aim for 5-10 significant terrain interactions per map.
- **Place at least one cliff or water hazard** on at least 30% of maps — these are premium moments worth designing around

These are guidelines, not rules. Some maps will be deliberately open (gladiator arenas, training grounds) and won't leverage terrain at all. That's fine — those maps test pure squad-vs-squad combat.

### Subsystem: `BattleEnvironmentSystem`

A new MonoBehaviour subsystem manages environment interaction queries.

Owns:

- Per-frame collision queries between units and `KnockbackCollidable` features
- Altitude differential calculation between unit pairs
- Line-of-sight queries (ray-casts) between units

Public API:

- `CheckKnockbackCollision(unit, velocity) → CollisionResult | null`
- `GetAltitudeDifferential(attacker, defender) → float`
- `HasLineOfSight(attacker, defender) → bool`
- `GetFoliageObscureFactor(attacker, defender) → float` (returns 0-1; 0 = clear, 1 = fully obscured)

Other subsystems (`BattleKnockbackSystem`, `BattleCombatResolver`, `BattleTargetFinder`) call into this system as needed. The environment system itself doesn't apply effects — it answers questions.

---

## Weather and atmospheric state

Weather is a **global ambient state** that applies battlefield-wide modifiers to combat. There is exactly one weather state at any given moment per battle, visible to every unit equally.

### Weather types

The game ships with these primary weather states:

| Weather | Atmospheric character |
|---|---|
| **Clear** | Default, no modifiers |
| **Rain** | Wet conditions; Water-element synergy |
| **Storm** | Severe rain with lightning; Water and Lightning synergy |
| **Fog** | Reduced visibility; ranged disadvantage |
| **Snow** | Cold and slippery; movement penalties |
| **Heat** | Hot and dry; Fire synergy, energy drains faster |
| **Sandstorm** | Severe heat with wind; Fire and Wind synergy, severe visibility penalty |

Seven states is the catalog. Don't add more without deliberate design intent — each weather state needs its own visual identity, particle systems, lighting tint, and balance considerations.

### Weather severity bands

Each weather state has three severity bands (except Clear):

| Severity | Characteristics |
|---|---|
| **Light** | Visible but mild; modest combat modifiers (~5-10%) |
| **Moderate** | Clearly active; meaningful combat modifiers (~15-20%) |
| **Severe** | Dramatic; strong combat modifiers (~25-35%); may carry secondary effects |

A "Light Rain" might apply +5% Water damage. A "Severe Rain" (storm) applies +25% Water damage, +15% Lightning damage, -20% Fire damage, and reduces movement speed by 10% for non-Wind units.

Severity is not just a number tweak — it's a *visual escalation*. Light Rain is a few drops; Severe Rain is sheets of water with poor visibility. The player sees severity, not just number changes.

### Weather modifier table

| Weather | Element boosts | Element penalties | Other effects |
|---|---|---|---|
| Clear | None | None | None |
| Rain (Light/Mod/Severe) | Water +5/+15/+25%, Lightning +0/+5/+15% | Fire -5/-10/-20% | Severe: ranged target acquisition -20% |
| Storm (only Severe) | Water +30%, Lightning +30% | Fire -25% | Visibility severely reduced; non-Wind movement -15%; periodic ambient lightning strikes |
| Fog (Light/Mod/Severe) | None | Ranged accuracy reduced | Severe: target acquisition range halved |
| Snow (Light/Mod/Severe) | None | None | Movement speed -5/-10/-15% for non-Wind units; speed gain from movement -20% at Severe |
| Heat (Light/Mod/Severe) | Fire +5/+15/+25% | Water -5/-10/-20% | Energy drains 5/10/15% faster |
| Sandstorm (only Severe) | Fire +20%, Wind +20% | Water -25% | Visibility halved; chip damage to non-Wind units |

These are starting values for tuning. The shape is what matters: weather creates *element-conditional* combat advantages without breaking the game.

### Storm and Sandstorm — the severe-only states

Storm and Sandstorm are not just "Severe Rain" and "Severe Heat" — they're qualitatively different states with their own dramatic identity. They:

- Have dedicated visuals (lightning flashes for Storm; sand walls for Sandstorm)
- Are reachable only via skills, never as a starting weather
- Cannot be "downgraded" to lower severity (they end abruptly when their duration expires, returning to Clear)
- Apply their full effects without intermediate steps

These are the *premium* weather states — the dramatic moments. Players don't accidentally encounter them; they're summoned deliberately.

### Weather as starting condition

When a battle begins, the map specifies the *starting weather*. Some maps are always Clear (gladiator arenas, training grounds). Others have a fixed weather (a desert region map starts at Light Heat). Others randomize within a range (forest map: Clear, Light Rain, or Light Fog).

Starting weather is a property of the map, not the squad. Players see the weather before the fight and can configure their squad accordingly — bringing a Water-heavy team to a Light Rain map is a smart play; bringing it to Clear is fine but suboptimal.

### Weather transitions

Weather changes during a battle in three ways:

1. **Skill-driven** (most common): a player or AI hero casts a weather-altering skill
2. **Time-driven** (rare): some maps have scripted weather changes (e.g., a map starts Clear and shifts to Rain after 60 seconds — for narrative atmosphere)
3. **Severity decay**: high-severity weather summoned by skills naturally decays toward lower severity over time (Severe Rain → Moderate Rain → Light Rain → Clear, over ~30s if not refreshed)

Transitions take 2-3 seconds, with visible visual interpolation (rain particles fading in, lighting tint shifting). Combat math snaps to the new weather instantly at the midpoint of the transition — no half-step modifiers.

### Weather-altering skills

This is where weather becomes a *strategic mechanic* rather than just ambient flavor.

**Design principle:** weather skills are **expensive, slow, and high-impact.** They're not common cantrips — they're tactical commitments comparable to summoning a guardian or dropping a screen-clearing combo.

Properties of a weather skill:

- High energy cost (typically 60-80 energy — 60-80% of the 100 max)
- Long cast time (1.5-3.0s, typically Rooted)
- Long cooldown (effectively, the unit has to spend the energy and rebuild — there's no short-cooldown weather skill)
- Substantial visible windup (telegraphing — opponents see it coming and may interrupt)
- Effect lasts 20-45s before decaying

Examples:

| Skill name | Trigger sequence | Effect |
|---|---|---|
| Rain Calling | Sign C → Sign C → Focus | Sets weather to Moderate Rain for 30s |
| Storm Summoning | Sign C → Sign C → Sign C → Focus | Sets weather to Severe Storm for 20s; high cost |
| Heat Wave | Sign A → Sign A → Focus | Sets weather to Moderate Heat for 30s |
| Sandstorm | Sign A → Sign A → Sign A → Focus | Sets weather to Severe Sandstorm for 20s |
| Mist | Sign C → Sign C | Sets weather to Light Fog for 25s; cheaper |
| Clear Skies | (Wind-element combo, future) | Resets weather to Clear; counters opposing weather setups |

These are designed to fit the action-chain combo system. Most are 3-action recipes — a meaningful cast but not the entire skill catalog.

### Weather counter-play

One of the most interesting design opportunities: **weather can be counter-summoned.** A Fire team facing a Rain-summoning enemy can pre-emptively summon Heat to neutralize. A Wind hero in development can counter both with Clear Skies.

This creates **weather meta-game** without requiring complex rules:

- Squads can be built for specific weather (Fire-team in Heat is devastating; in Rain is gimped)
- Weather skills become **opening moves** (priority casts at battle start to seize atmospheric advantage)
- Weather-counter skills become **reactive plays** (cast in response to enemy weather setup)
- Weather *resilient* squads (mixed elements, Wind-element flexibility) play differently from *weather-dependent* squads (single-element specialists)

This is the depth that justifies weather's existence in the design.

### Subsystem: `BattleWeatherSystem`

A new MonoBehaviour subsystem manages the global weather state.

Owns:

- Current weather state (type and severity)
- Active weather effects on combat (modifiers applied at battle level)
- Severity decay timer
- Visual layer coordination (cues `BattleAnimancerDriver` and presentation systems to update particles, lighting, audio)

Public API:

- `GetCurrentWeather() → WeatherState`
- `SetWeather(weatherType, severity, duration) → void`
- `GetElementModifier(element) → float`
- `GetMovementModifier(unit) → float`
- `GetVisibilityModifier() → float`

`BattleCombatResolver` queries weather modifiers when resolving damage. `BattleSpeedSystem` queries movement modifiers for speed-gain calculations. `BattleTargetFinder` queries visibility modifiers for target acquisition.

The weather system is *queried by* combat subsystems — it doesn't push state changes into them. This keeps the dependency direction clean.

---

## Integration points with combat

A summary of where environment and weather plug into the existing combat backend defined in `COMBAT_DESIGN.md`.

| Combat system | Environment hook |
|---|---|
| `BattleCombatResolver` | Queries weather modifiers; checks knockback collision before applying knockback |
| `BattleKnockbackSystem` | Routes through `BattleEnvironmentSystem.CheckKnockbackCollision`; applies bonus effects on collision |
| `BattleSpeedSystem` | Queries weather movement modifier; queries altitude differential for speed gain |
| `BattleTargetFinder` | Queries `BattleEnvironmentSystem.HasLineOfSight`; queries weather visibility modifier |
| `BattleAIBrain` | Reads weather state when choosing skills (prefer Water skills in Rain); reads altitude when picking movement intent |
| `BattleStatusEffectSystem` | Knockback-into-terrain triggers a Knockdown effect through this system |
| `BattleAnimancerDriver` | Coordinates weather visual transitions; plays terrain-impact animations on knockback collision |

These hooks are minimal — environment is *additive* to combat, not foundational. Combat works without environment; environment makes combat better.

---

## Implementation roadmap

The environment system is **independent of combat work** and can be implemented in parallel with combat phases (per `COMBAT_DESIGN.md` roadmap), or sequentially after combat foundations are solid. Recommended: start environment work after Phase 6 (AI brain core) of the combat roadmap, since the AI brain needs to read environment state.

### Phase E1 — Knockback collision (1-2 weeks)

- Create `BattleEnvironmentSystem` MonoBehaviour
- Add `KnockbackCollidable` component / layer system for terrain features
- Implement `CheckKnockbackCollision` query
- Wire collision into `BattleKnockbackSystem` — apply bonus damage, hit-stop upgrade, brief Knockdown
- Add visual impact effect at collision point
- Author one test map with marked walls

Deliverable: enemies knocked into walls take bonus damage, get knocked down briefly. The first cinematic environment moment works.

### Phase E2 — Altitude advantage (1 week)

- Implement `GetAltitudeDifferential` query
- Add altitude modifier to `BattleCombatResolver` damage formula
- Add altitude modifier to dodge chance
- Add altitude modifier to `BattleSpeedSystem` gain rate
- Tune thresholds (1.5-unit minimum differential)

Deliverable: high ground matters in combat. Maps with vertical structure play differently.

### Phase E3 — Line of sight and foliage (1-2 weeks)

- Implement `HasLineOfSight` and `GetFoliageObscureFactor` queries
- Wire line of sight into `BattleTargetFinder` for ranged casts
- Wire foliage into target acquisition range
- Add `Foliage` component / tag for trees and dense vegetation
- Author one map with foliage and walls to test

Deliverable: caster archetypes must reposition for line of sight. Forest maps fight differently than open arenas.

### Phase E4 — Weather foundation (2 weeks)

- Create `BattleWeatherSystem` MonoBehaviour
- Implement weather state and severity tracking
- Implement weather modifier API
- Add starting weather as a map property
- Wire element modifier into `BattleCombatResolver`
- Wire movement modifier into `BattleSpeedSystem`
- Wire visibility modifier into `BattleTargetFinder`
- Build visual representation for at least Clear, Rain, and Heat (particles, lighting tint)

Deliverable: maps can have weather. Element-based skills feel different in different weather. Basic atmospheric feel.

### Phase E5 — Weather severity bands (1 week)

- Implement three severity bands per weather type
- Tune the modifier table for each band
- Add severity-driven visual escalation (more particles at Severe, etc.)
- Add severity decay over time

Deliverable: weather feels like a spectrum, not a binary. Severe weather is dramatic.

### Phase E6 — Weather-altering skills (2 weeks)

- Add `weatherEffect` field to `ComboRecipeDefinition`
- Implement weather-summoning skills (Rain Calling, Storm Summoning, Heat Wave, Sandstorm, Mist, Clear Skies)
- Wire into `BattleCombatResolver` so weather skills change `BattleWeatherSystem` state
- Tune skill costs and durations
- Update `05_SKILL_SYSTEM.md` with new combo recipes

Deliverable: heroes can change the weather. Tactical weather meta-game emerges.

### Phase E7 — Storm and Sandstorm — the severe-only states (1 week)

- Implement Storm as a distinct state (not just Severe Rain)
- Implement Sandstorm as a distinct state (not just Severe Heat)
- Add unique visuals (lightning strikes, sand walls)
- Add periodic ambient effects (Storm: occasional lightning chip damage to all units; Sandstorm: chip damage to non-Wind)
- Tune cinematic moments

Deliverable: the premium weather states feel premium. Casting Storm Summoning is a *moment*.

### Phase E8 — Polish and balance (ongoing)

- Tune all environment and weather numbers
- Add map-design tools to mark terrain features
- Iterate on visual transitions
- Add additional weather types as new map biomes are designed (Snow for cold regions, etc.)

Total estimate: 9-13 weeks of focused work for the environment system, parallelizable with combat work.

---

## What changes in the existing docs

When implementing each phase, the corresponding Tier 1 docs need updates.

**`03_DATA_MODELS.md`:**
- Add `weatherEffect` field to `ComboRecipeDefinition`
- Add `WeatherType`, `WeatherSeverity` enums
- Add `WeatherState` runtime class

**`04_BATTLE_SYSTEM.md`:**
- Add `BattleEnvironmentSystem` and `BattleWeatherSystem` to subsystem list
- Add a section on terrain interaction (knockback collision, altitude, line of sight)
- Add a section on weather and combat
- Update damage formula to include weather modifier
- Update dodge formula to include altitude modifier

**`05_SKILL_SYSTEM.md`:**
- Add weather-altering skill recipes
- Add `weatherEffect` field documentation

**`07_PRESENTATION.md`:**
- Add weather visual coordination notes
- Add knockback-collision impact effect specification
- Add weather transition timing rules

**`CLAUDE.md`:**
- Add `BattleEnvironmentSystem` and `BattleWeatherSystem` to the list of valid subsystem locations for combat behavior

---

## Risks and watch-points

**Risk: Knockback collision becomes annoying instead of cinematic.** If every fight involves enemies being slammed into walls constantly, the moment loses meaning. Mitigation: knockback collision bonuses should feel like a *highlight* (~10-20% of fights have a notable wall slam), not a *constant*. If playtesting shows wall slams happening multiple times per fight, reduce knockback ranges or design more open maps.

**Risk: Altitude advantage trivializes some encounters.** A team that always fights from high ground will dominate. Mitigation: enemy AI should aggressively pursue altitude when wounded; some maps should be deliberately flat to neutralize altitude as a factor; bosses should counter altitude with ranged or vertical-mobility attacks.

**Risk: Line of sight punishes casters too hard.** If most maps have heavy foliage, caster archetypes become weak. Mitigation: balance map foliage density across the map roster — some forest maps are caster-hostile (intentional), but most maps should have at least some open sightlines.

**Risk: Weather becomes a "cast at start" routine.** If weather skills are clearly optimal at battle start, every fight begins with a weather-cast race. Mitigation: weather skills should be expensive enough that committing to one *costs* the early game. The trade-off "spend my opening on weather, but I'm down energy for combat" should be real.

**Risk: Severe weather (Storm/Sandstorm) becomes the only viable weather.** If Severe is dramatically better than Moderate, players ignore Moderate options. Mitigation: Severe weather should be *expensive* to summon and *short-lived*. The Moderate/Light bands are useful for cheaper, longer-lasting buffs that pair with extended fights.

**Risk: Weather visuals don't read.** If players can't tell at a glance "it's Severe Rain," weather effects feel arbitrary. Mitigation: weather severity should be *unmistakable*. Severe Rain should be sheets of water; Severe Heat should be visible heat haze. Cosmetic clarity is non-negotiable.

**Risk: Environment becomes the main thing instead of combat.** If half the design conversations become "let's add a new terrain feature," focus has shifted away from combat. Mitigation: respect the scope discipline. Tier 2 only. No invisible zones. No destructibles. The list of features in this doc is the list. New features require explicit design discussion.

---

## Final note

The environment is the stage on which combat happens. Done well, it makes every fight feel located in a *place* — a windswept ridge, a fog-bound forest, a desert in a sandstorm. Done poorly, it becomes a confusing layer of invisible rules that obscure combat instead of enhancing it.

The discipline this design enforces — physical over abstract, global over zoned, cinematic over systemic — is what keeps environment in service of combat, not in competition with it.

When this system is working, players will remember specific fights for *where they happened*, not just for *who fought them*.
