# LONG_TERM_VISION.md

> **For the designer.** This is not a working spec. Claude does not read this for daily tasks. This document exists so that, on a hard day in year 2 or year 3, you can reread it and remember what you set out to build.
>
> **Tier 2 — Stable.** Do not modify without deliberate intent. If the vision changes, change it; otherwise leave it alone.

---

## What this game is

This is a long-haul **cultivation-and-legacy game with anime-styled combat**. The player builds a roster of heroes — recruiting them young, training them through use, watching them master techniques, eventually teaching new heroes, aging slowly across thousands of hours of play, and leaving a mark on a reactive world inhabited by other players' squads.

Combat is the lens. Hero progression is the soul. The world is the stage.

It is a single-player game with asynchronous multiplayer texture. It is mission-based but not story-driven. It has no campaign and no scripted plot; it produces its own emergent narratives through systems.

It is the kind of game players play for two thousand hours and remember the names of their dead heroes.

---

## The dual pillars

Every major design decision serves one of two pillars. If something serves neither, it doesn't belong.

### Pillar 1 — Combat that *looks* like anime combat

Bursty. Fast. 1-2 minutes per close fight. Skills that read clearly even at high speed. Dodges that are dramatic. Combos that feel earned. Hits that feel like *hits*. Camera movements that frame the action. Hit-stop, knockback, screen shake — the full anime arsenal applied with restraint.

The combat must look like something a player would record and post. If players don't share clips of their fights, the combat has failed Pillar 1 regardless of how mechanically deep it is.

### Pillar 2 — Hero progression as deep RPG simulation

Heroes are not stat blocks. They are *people* with arcs. They begin as rookies who can barely throw a punch. Through use they master techniques. Through bonds they form relationships with other heroes. Through teaching they pass knowledge forward. Through age they eventually die — but not before leaving students who carry their style.

Every hero has a name the player remembers. Every loss has weight. Every long-lived master is a quiet boast.

Pillar 2 is what makes this game live in the player's memory. Pillar 1 is what makes it live in the moment.

---

## What this game is *not*

Defining the game by negation is more useful than by listing features. These are non-goals. Push back if a design suggestion drifts toward any of them.

- **It is not a fighting game.** No live combat input. The player's agency is in preparation and progression, not execution.
- **It is not a competitive autobattler like TFT.** No shared pool, no rounds, no traits-as-identity, no aggressive seasonal balance.
- **It is not a roguelike.** Runs are not the unit of play. The world persists. Heroes persist. Loss is meaningful but not constant.
- **It is not a story game.** No scripted plot. No protagonist beyond the player's roster. Narrative emerges from systems.
- **It is not a Pokémon collector.** Heroes are invested in, not collected. Quality over quantity. ~20 heroes is the *roster cap*, not the catch goal.
- **It is not a real-time multiplayer game.** No live PvP, no parties, no synchronized state. Multiplayer is asynchronous and ambient.
- **It is not Mount & Blade.** Reactive nations exist but in service of mission variety, not as a self-contained simulation.
- **It is not free-to-play.** Single Steam purchase. No microtransactions ever. No battle pass. No daily login rewards.

Anything that pushes the design toward these is wrong. Cut it.

---

## The combat experience

### Pacing

A close fight runs 1-2 minutes. Faster fights happen when one squad outclasses the other, and that's fine — power fantasy is part of progression. Slower fights happen rarely and indicate either a real tactical puzzle or poor preparation.

Within a fight, action is constant. There are no pauses. Skills cycle, dodges interrupt, knockback resets spacing, combos resolve. The visual texture is *kinetic*, not deliberate.

### What a great fight looks like

The player sees their squad spread across the arena. Kai engages forward, gets two punches in, dodges the counter with a parabolic backflip. Mira begins a Triple Sign cast in the back, hand seals visible. The enemy assassin closes on her — Kai sees it, breaks engagement, Crescent-Kicks the assassin into a cliff face. Mira's combo lands as the assassin recovers. Knockback. Hit-stop. Camera shake. Lightning splashes across two enemies. The third enemy panics, switches targets, runs at Kai. Kai's third combo of the fight — Earth Fist into Earth Fist — lands clean.

This is the texture. **The player did not control any of this.** They configured the squad. They chose the loadouts. They watched the plan they designed come to life.

### What a bad fight looks like

Two squads stand near each other and trade hits in place. Animations play but feel disconnected from outcomes. Hits look the same whether they crit or graze. The player can't tell why their hero just died. The screen is too busy to read.

The single most important rule of combat presentation: **the player must always understand why something just happened.** If a hero dies and the player can't articulate why, the system has failed.

### The anime aesthetic, with restraint

References for *feel*: Naruto Storm, Dragon Ball FighterZ, Demon Slayer, Jujutsu Kaisen. **Inspiration only — original IP, no copying.** No copyrighted characters, no clan names lifted, no signature techniques transplanted. The game lives in adjacent space.

What "anime feel" actually means in implementation:

- Every landed hit gets hit-stop sized to its severity
- Heavy attacks knock targets through space, not just stagger them in place
- Special techniques have visible windups long enough to read but short enough not to bore
- The camera is alive — not cinematic per-fight, but reactive, framing important moments
- Particles and trail effects are generous on skills, restrained on basic attacks
- Slow-motion exists, used sparingly — the killing blow on a boss, the moment a hero dies
- Sound carries impact more than music does

What anime feel does *not* mean:

- Excessive screen-fill effects that hide the action
- Three-second freeze-frames on every hit
- Cinematic ultimate cutscenes that interrupt every battle (use them only on rare, earned moments)
- Voice barks in every action

Restraint is what separates a game that *uses* anime aesthetics from a game that *is* anime-flavored. Aim for the latter.

---

## Hero progression — the long form

This is the deepest system in the game. It must be designed as a single coherent experience, not a stack of mechanics.

### The shape of a hero's life

A hero enters your roster as a **rookie** — low stats, few unlocked actions, no mastery in anything. They join missions, fight battles, throw punches, attempt hand signs.

Through use, they level up individual actions. **Punch** climbs from level 1 to level 50 over hundreds of fights. At certain thresholds, the hero unlocks a new combo — but the player doesn't see in advance what it will be. The mastery system is *discovery-flavored*: you know the hero is progressing, you can see the percentage filling, but the unlock itself is a surprise.

Heroes also have **attributes** that grow more slowly: HP, Speed, Perception, Dexterity, Defense, Wisdom, Loyalty. These mostly grow through usage patterns — heroes who dodge a lot grow Speed, heroes who land critical timing grow Perception, heroes who survive bad situations grow Loyalty. **Direct stat allocation is not a feature.** The hero shapes themselves through how they're used.

Over hundreds of hours, a hero accumulates mastery, unlocked actions, equipped items, formed relationships with other heroes, and a personal history. They become *someone*.

Eventually — if they live long enough — they age. Real-time-equivalent thousands of in-game-hours later, they become *masters* who have seen everything, taught dozens of students, and earned their place in the village's history.

Then they die. Or, with deep enough cultivation progression, they become near-immortal.

The player remembers them either way.

### Action XP and discovery-flavored unlocks

Every action type has its own XP track. Punch, Kick, Hand Sign A, Hand Sign B, Hand Sign C, Focus, and every action type added later — each one levels independently per hero. A hero who never uses kicks never levels Kick. A hero who specializes in elemental signs becomes a sign-specialist naturally.

XP comes from use. Throwing a punch in combat = 1 XP. Throwing a punch that lands = 2 XP. Throwing a punch that lands as part of a successful combo = 3 XP. Numbers approximate; the principle is *use grants progression, success grants more*.

At certain thresholds (level 5, 10, 20, 35, 50 — approximate), the hero unlocks **something new related to that action**. The player sees a notification: "Kai unlocked something through Punch mastery." They open the hero's sheet. There's a new combo, a new variant of an existing combo, a passive bonus, a stat boost. The unlock is revealed.

The player did not choose what would unlock. The system did, based on the hero's history, the hero's other masteries, and the hero's attributes. **Two players who both train Punch to 20 on the same hero get different unlocks.**

This is the discovery layer. It works because:

- Progress is visible (mastery percentage shown)
- The unlock is the surprise, not the progression
- Different heroes unlock different things — each playthrough produces unique characters
- Community discussion thrives on it ("did you ever see what Punch level 35 does on a high-Perception hero?")
- Min-maxers can't fully optimize, but they can pursue strategies (build for Perception, hope for surprise unlocks aligned with that)

### Per-hero skill trees

In addition to action mastery, each hero has a personal skill tree unlocked through level-up points. Skill trees are *per-hero* — not shared across the roster. This is deliberate. It means each hero is uniquely yours. It means losing a hero means losing their tree.

Tree nodes include:

- Stat bonuses (HP, Speed, etc.)
- Passive abilities tied to the hero's specialty
- Action variants (a Punch that pushes back further, a Focus with shorter cast)
- New combo unlocks not tied to mastery thresholds
- Behavioral options ("this hero can block while casting")

Trees branch — players cannot unlock everything. Choices matter. Respec is **rare and expensive**, not free, because investment is the point.

### Attributes and how they grow

Heroes have these attributes:

- **HP** — health pool
- **Speed** — affects dodge chance, movement speed, action speed
- **Perception** — affects critical timing, dodge precision, target acquisition (lower-HP enemies prioritized)
- **Dexterity** — affects combo execution, action chaining, animation smoothness
- **Defense** — affects damage reduction, block chance
- **Wisdom** — affects teaching effectiveness, mission insight, learning rate
- **Loyalty** — affects how committed the hero is to your village (factors into desertion, morale, late-game faction events)
- **Charisma** — affects diplomatic missions, faction interactions

Combat-relevant attributes are HP, Speed, Perception, Dexterity, Defense. The rest are non-combat dimensions that make heroes *people*, not units. **A hero's full attribute spread is what makes them feel three-dimensional.**

Attributes grow slowly through *related activity*. A hero who spends a hundred fights dodging develops Speed. A hero who teaches students gains Wisdom. A hero who survives long campaigns gains Loyalty. **There is no level-up screen where you assign points.** Heroes shape themselves.

This means:

- Two heroes who started identical can diverge significantly
- The player's playstyle shapes their roster's identity
- Min-maxing requires intentional play patterns, not menu optimization
- Heroes have *flavor* that emerges from their history

### Teaching — the legacy mechanic

This is the spine of the long game.

**Heroes can teach other heroes.** Two forms exist; both are part of the system.

**Form 1 — Specific combo transfer.** A hero who has mastered a particular combo can teach it to another hero. The teaching takes in-game time — days to weeks depending on the complexity of the combo, the teacher's Wisdom, and the student's Dexterity. During teaching, both heroes are unavailable for missions.

The student doesn't get the combo for free. They unlock the *capacity* to learn it. They must then practice it through use to bring it to combat-effective levels. But the teaching shortens the discovery curve dramatically — they don't have to discover the unlock through random mastery progression; the teacher *gives them the seed*.

**Form 2 — Mentor-student bond.** A hero can take on another hero as their student in a longer-term mentorship. While bonded, the student gains XP faster across all actions, has a small bonus to attribute growth, and may inherit some of the mentor's quirks. The mentor gains slow Wisdom growth.

A mentor can have only one student at a time. A student can have only one mentor at a time. Bonds last until either hero retires, dies, or the mentorship is dissolved.

**Why teaching matters at the design level:**

- It makes long-lived heroes more valuable than just "old units." A hero who has trained five students has *legacy* even after they die.
- It creates dependency between heroes — losing your main teacher hurts in ways losing a generic veteran doesn't.
- It produces emergent narratives. "Mira learned Triple Sign from old Kazuo, who learned it from Ren in the founding years."
- It makes the roster feel like a *lineage*, not a list.

When a hero dies, their students reflect them. When a teacher dies before their student matures, that's a story. When a student surpasses their teacher, that's a story.

### Aging and immortality

Heroes age slowly. Real-time-equivalent **2000+ hours** of active use before age becomes a meaningful pressure. For the vast majority of players, aging is a *late-game flavor mechanic*, not a constant fear.

The Tale of Immortal model is the reference here. Aging works because:

- It's slow enough that early- and mid-game players don't think about it
- It only affects heroes who *survived long enough to deserve a long arc*
- There are progression paths that extend lifespan (cultivation breakthroughs, rare items, deep mastery)
- True immortality is achievable but extremely rare and expensive — a multi-thousand-hour goal

Aging mechanics:

- Heroes have a hidden **biological age** that ticks up over real-time-equivalent in-game time
- At certain age thresholds, attributes decline slightly (Speed first, then Dexterity, then HP)
- Aged heroes gain Wisdom faster, become better teachers, can unlock late-game perks
- Eventually, heroes die of old age — a **dignified death**, not a punishment, with proper memorialization
- Late-game cultivation paths can pause or reverse aging
- A handful of heroes per playthrough may achieve effective immortality through deep enough investment

**The death of an old, beloved hero should be an event the player remembers.** A small in-world memorial. Their students gather. Their items can be passed to a successor. Their grave sits in the village.

This is not a system for grinding past. It's a system that gives the long arc *meaning*.

### What heroes do between missions

Even when not on missions, heroes have lives. In the village, you can see them in different states:

- **Resting** — recovering from injury or fatigue
- **Training** (a specific action) — gaining slow XP in that action without combat
- **Teaching** — engaged in either form of teaching
- **Studying** — solo time gaining Wisdom and slow attribute growth
- **Patrolling** — low-stakes nearby missions, generating intel
- **Idle** — simply present

The player sets their roster's between-mission activities. This is the *village simulation* layer. Even when the player is not in combat, the world ticks. Heroes grow. Time passes.

This layer is what makes the village feel like a *place* and not a menu.

---

## The world

### The map

A hand-crafted map of the region — not procedurally generated, not infinite. A Naruto-flavored world of villages, frontier zones, hidden valleys, mountain ranges, and contested borders. **Persistent and fixed.** Every player sees the same map.

The map shows nations (factions), regions, current control, current conflicts, and available missions. The player's home village is one node on the map.

### Reactive nations — in layers

Faction simulation is implemented progressively. Future-you needs to be honest about what's achievable and when. The vision is layered:

**Layer 1 (foundation):**

- Each region has a controlling faction
- Mission availability per region depends on current control
- Some regions are at war with others; these states change based on world events including your missions
- Visible faction strength values per region

**Layer 2 (mid-game systems):**

- Factions launch attacks against each other independently of the player
- Faction strength shifts visibly over time
- Your village's standing with each faction tracks separately
- Factions remember your actions

**Layer 3 (deep-cut, may not ship):**

- Faction leaders with personalities and goals
- Diplomatic missions that meaningfully alter the political map
- Emergent stories from faction conflict
- Player can be allied, neutral, or enemy with each faction independently

Layer 1 is essential. Layer 2 is the goal. Layer 3 is a stretch goal that may live as a "future expansion" in perpetuity. **That's fine.** Many great games shipped at Layer 1 of their reactive systems and were better for not biting off more.

### Mission types

Combat is not the only thing your roster does. Mission variety prevents the gameplay loop from collapsing into "fight, fight, fight, level up, fight."

Mission types include:

- **Combat missions** — kill the enemies, survive the wave, defeat the boss
- **Patrols** — low-stakes, generate intel, low XP, helpful for newer heroes
- **Diplomacy** — non-combat or partial-combat, leverages high-Charisma or high-Wisdom heroes
- **Rescue/escort** — combat-adjacent, protect a friendly NPC
- **Training expeditions** — controlled combat against weaker enemies, more XP than usual
- **Reconnaissance** — stealth-like, leverages high-Perception heroes
- **Defense** — your village is attacked, you defend
- **Boss raids** — high-difficulty fights against unique enemies with named loot

Each mission type stresses different parts of your roster. A Speed-heavy roster excels at reconnaissance but struggles at boss raids. A Defense-heavy roster owns village defense but is poor at patrols. **This makes hero variety matter.**

### Dungeons

Within the map, certain locations are **dungeons** — multi-fight challenges with escalating difficulty, unique boss fights, and meaningful loot. Dungeons respawn over real-world time but with variation each time. Some dungeons unlock only after specific world conditions (a faction is weakened enough; the player has reached a reputation threshold; a certain item is in the player's possession).

Dungeons are the *test* of a player's roster. Combat missions are bread-and-butter; dungeons are the meal.

### The village

The player's home village is a place. Not just a menu. It has:

- A roster screen showing all heroes and their current state
- A barracks (rest area)
- A training ground (action XP grind area)
- A library (Wisdom growth, teaching coordination)
- A cemetery (dead heroes are remembered here)
- A trophy hall (notable items, mission completion records, named victories)
- A council chamber (faction relations, diplomatic missions, world map view)
- A map room (mission selection, world overview)

These don't have to be 3D explorable spaces — they can be UI panels with strong visual identity. But they should *feel* like places, not menus. Each one has its own background, music cue, signature interactions.

The village is where the player spends time *not* fighting. It needs to feel inhabited.

---

## Items and economy

### The loot loop

Items come from missions. Bosses drop named items. Dungeons drop higher-tier items. Standard missions drop mundane gear and crafting materials. **Loot is the spine of progression that lives outside hero progression.**

Items are *invested* — not constantly upgraded. A weapon found in hour 20 might still be in use at hour 200, leveled up alongside its wielder. Players form relationships with their gear.

Item categories:

- **Weapons** — primary combat impact, modify damage and unlock action variants
- **Armor** — defensive, modifies HP and Defense
- **Accessories** — flavor and synergy, often passive effects
- **Consumables** — situational use in missions
- **Crafting materials** — for the (eventually-implemented) crafting system

### Item progression

Items themselves can level up through use. A weapon used in a hundred fights becomes a *named weapon* with its own bonuses earned through that history. This pairs beautifully with hero progression — your hero's sword is *their* sword, marked by the same fights they survived.

Players name notable items the same way they name notable heroes.

### Crafting (later)

Crafting exists as a "future expansion" target. Not in the initial design. The loot loop is the spine; crafting is a *mid-development* addition that adds depth without replacing the core. When crafting comes online, it lets players combine materials and lower-tier items into higher-tier items, but it does not replace the satisfaction of finding a great weapon as a drop.

Crafting is a feature; loot is the system.

### Inheritance and item bonds

When a hero dies, their items can be inherited by another hero. A weapon passed down from a dead master to a living student is *the same weapon* — same level, same history, same name. The new wielder may have different proficiencies; the weapon adapts subtly.

This makes loss productive. Your master's weapon now lives on with their student. The cycle continues.

---

## Multiplayer — asynchronous and ambient

### The snapshot model

Players upload **squad snapshots** — frozen states of their roster, items, loadouts, and behavior settings. Other players download these snapshots and run battles against them locally. The player whose snapshot was used does not need to be online. The result is reported back; ladder ratings update.

This is the entirety of the multiplayer infrastructure. There is no live PvP. There is no synchronization. There is no chat in the gameplay loop.

Why this is the right model:

- It costs almost nothing to implement compared to real-time multiplayer
- It works with the game's deterministic AI-vs-AI combat naturally
- It scales to a small player base or a large one without infrastructure changes
- It produces a competitive experience without the toxicity of synchronous play
- It survives if the player base is small — old snapshots still feel like opponents

### The ladder

A simple competitive ladder. Players who opt in to ranked mode submit a snapshot. The system matches them against other snapshots of similar rating. Wins push rating up; losses push it down. Seasonal resets every 3-6 months.

**Balance is not pursued aggressively.** The metagame is part of the fun. Players will find degenerate squads. Other players will find counters. The cycle continues. We will not chase patch-by-patch perfect balance — that's a treadmill that destroys small studios.

What we *will* do: **publish weekly snapshot stats** (most-used heroes, most-used combos, win rates). The community will diagnose the meta itself. We adjust only when something is genuinely broken.

### NPC integration — the killer feature

Snapshots also feed back into the single-player world. NPC squads in enemy villages, in faction armies, in hostile dungeons — these can be drawn from the snapshot pool. **You're not fighting AI opponents. You're fighting other players' rosters, frozen at moments in time.**

This means:

- Every enemy squad has texture (someone built it; someone leveled those heroes; someone chose those loadouts)
- The world feels populated even with zero live multiplayer activity
- Old snapshots remain useful (they become "lower-level NPC" content as ladder ratings climb)
- The player's own snapshot may appear in another player's world

This is the design leverage of the snapshot model. It transforms a tiny multiplayer feature into a quietly pervasive social presence across the entire game.

### What multiplayer does NOT include

Explicitly out of scope:

- Live combat synchronization
- Voice chat or in-game chat (links to Discord/external community welcome)
- Guilds or clans
- Trading items between players
- Cooperative missions
- Spectator mode for live fights
- Any feature that requires both players online simultaneously

These can be revisited if the game becomes successful enough to warrant it. They are not part of the vision.

---

## Endgame and replayability

### What hour 100 looks like

The player has a roster of 15-20 heroes. Several have died of old age — their successors carry their styles. Three or four heroes are masters of their domains, on the cusp of cultivation breakthroughs. A few rookies are being trained by a beloved old veteran. The village has a cemetery the player walks past in their hub view.

The player is hunting a rare boss in a dungeon that respawned with a unique modifier. They've been chasing a specific named weapon for ten missions. Their current snapshot sits at rank 12 on the regional ladder.

A faction has invaded a neighboring region. The player needs to decide: defend it (good for diplomacy, costly), let it fall (the world shifts, new mission types unlock).

A young hero in their roster — third generation, taught by a hero taught by a hero — just unlocked a Punch mastery they've never seen before. The player saves the moment, posts it, looks it up later, finds out it was a 1-in-1000 unlock conditioned on a specific attribute spread.

That is what hour 100 looks like. The game is alive.

### What hour 1000 looks like

The player has buried twenty heroes. They have one near-immortal grandmaster who has personally taught a third of their current roster. Their snapshot is in the regional top-50. They've cleared every dungeon at least three times, and certain dungeons they return to repeatedly because the variants always surprise them.

They've started a new playthrough on a different "save world" while keeping their main one. Their main world is comfortable; their new world is harsh. They alternate.

This is the kind of game that has 1000-hour players. It earns those hours through depth, not through grind.

### Replayability without resetting

Single-save-slot replayability comes from:

- **Variant playthroughs** through hidden mastery unlocks
- **Faction-state divergence** between worlds
- **Snapshot ladder seasons** keeping the metagame alive
- **Dungeon respawn variants** keeping high-difficulty content fresh
- **Long-arc legacy mechanics** (dynasties of heroes that span decades of in-game time)

Players don't *need* to start over. The game's depth means they shouldn't *want* to until they've mined the surface deeply.

But for players who do want to start over, the game supports multiple save worlds — different worlds with different histories, different rosters, different faction landscapes.

---

## The texture of narrative without a story

There is no scripted story. There is no protagonist beyond the player's roster. There are no dialogue trees, no cutscenes, no NPCs with arcs.

There **is** narrative texture, generated by systems:

- **Heroes have names, traits, and backstories** chosen at recruitment from large pools
- **Heroes form bonds** through fighting together, training together, teaching each other
- **Notable moments are remembered** — biggest hit, longest combo, first kill, narrow survival
- **Deaths are mourned** — visited cemetery entries, passing-down of items, students reflecting their teacher
- **Faction events generate news** — "The Northern Kingdom invaded the Iron Peaks" — without dialogue, just headlines and consequences
- **Long-running heroes accumulate epithets** — "Kai the Vigilant," "Mira of Three Storms" — earned through play patterns

This is what gives the game soul without needing writers, voice actors, or narrative designers. Dwarf Fortress, Rimworld, Mount & Blade, Wildermyth — all of these earned their fans through *generated narrative texture*. This game does the same.

---

## Risks and hard choices

This section is for future-you to remember the things that can derail this project. Each is a real risk; each has a recommended approach.

### Risk: Scope. Three years is honest but tight.

The vision is large. Combat + 20-hero progression + per-hero skill trees + items + map + reactive nations + aging + teaching + multiplayer is genuinely several full systems' worth of design.

The mitigation is sequential commitment, not parallel ambition. Build *one* system end-to-end before starting the next. Resist the temptation to half-build five things. A combat system that's done > five systems that are 60%.

The Claude-as-multiplier factor is real but not infinite. Code velocity goes up; design judgment, art, audio, testing, and tuning do not multiply the same way. **Plan systems for an optimistic timeline. Plan polish for a realistic one.**

### Risk: Balance death-spiral.

PvP-with-progression is one of the hardest balance environments in game design. As you add more actions, more combos, more items, more skill tree nodes, the combinatorial space explodes. Some combinations *will* be broken.

The mitigation is the embrace approach. Don't chase perfect balance. Make the metagame visible. Let the community do the work. Adjust only egregious outliers.

If you start patching weekly, the player base resents the treadmill and so do you. Pick a cadence (monthly, quarterly) and stick to it.

### Risk: Hidden mastery being mistaken for randomness.

The discovery-flavored mastery system depends on players *understanding that there is a system* even when they can't see all of it. If players think it's random, they disengage from progression entirely.

The mitigation is **visible progress, hidden outcomes**. Always show the player how close they are to the next unlock. Always confirm when an unlock happened. Never let progression feel random — only let outcomes feel surprising.

### Risk: Aging anxiety.

Even with the Tale of Immortal-style long lifespans, some players will obsessively track their heroes' age and play under self-imposed pressure. This is fine and even healthy for the game's identity, but the system needs to *not punish* casual players who never engage with aging optimization.

The mitigation is *long lifespans* and *clear telegraphing*. A hero approaching old age should be visibly aging — graying, wrinkles, slower combat animations even before mechanical impact — so the player knows the arc is winding down. No surprise deaths.

### Risk: Reactive nations becoming a black hole.

Faction simulation is the kind of system that absorbs unbounded development time. Layer 1 is achievable. Layer 2 is challenging. Layer 3 is heroic.

The mitigation is honesty about layering. **Decide before each year of development which layer is the goal for that year.** Don't promise yourself Layer 3 in year 1. Don't promise yourself Layer 4 ever, unless you've shipped Layer 3 and have time to spare.

### Risk: Combat readability under feature creep.

As more skills, more elements, more effects, more particles get added, fights can become unreadable. The "player must always understand why something just happened" rule erodes one feature at a time.

The mitigation is a periodic **readability audit**. Every six months, watch a recorded battle and ask: can I narrate what's happening? If not, cut. Restraint is a feature.

### Risk: Multiplayer infrastructure complexity.

Even async multiplayer requires *some* backend. Snapshots have to live somewhere. Ladder ratings need a database. This is real work and real cost, even if minimal.

The mitigation is to use a managed service (PlayFab, Nakama, Supabase, or similar) rather than building backend from scratch. The savings in maintenance time are worth the per-month cost.

Don't build multiplayer until single-player is genuinely solid. Multiplayer is **late** in the schedule.

### Risk: Solo development burnout.

Three years on a project of this scope, even with Claude as multiplier, is a long arc. Burnout kills more games than bugs do.

The mitigation is to ship *something playable* early, even if minimal. A 30-minute demo where players can build a squad and fight one battle. Showing it to people. Getting feedback. Getting validation. This sustains the long haul better than any internal motivation.

If by month 12 there is no public-facing version of the game, reassess.

---

## What I (the future designer) am building, in one paragraph

I am building a long-haul cultivation-and-legacy game where players raise a roster of anime-styled combat heroes through deep RPG progression — mastery from use, bonds between teammates, knowledge passed down through teaching, lives that age slowly across thousands of hours, and items that become heirlooms. Heroes fight in fast, dynamic, anime-styled 3D battles that resolve automatically based on the player's pre-fight preparation. The world is a reactive map of factions in conflict, populated by other players' squad snapshots that fight in single-player and compete on a global ladder. There is no scripted story; the systems generate narrative texture. The game is sold once on Steam, has no microtransactions, and is built to be remembered.

If, on a hard day, this paragraph still sounds like the game I want to build — I'm on the right track.

If it doesn't — I update this document deliberately, not by accident, and align the rest accordingly.

---

## Final note

This document is a North Star, not a roadmap. The specific systems and decisions live in the working docs (`Docs/CLAUDE.md`, `Docs/01_VISION.md` through `Docs/08_ROADMAP.md`). When the working docs and this vision drift apart, that is the moment to ask: *did the game change, or did I lose sight of it?*

Both are valid answers. Be honest about which.

The hardest part of a multi-year project is remembering what it was for. This document exists so I don't have to remember. I just have to read.
