# ANIMATION_CATALOG.md

> **Tier 3 — Inspiration / planning artifact.** Not a working spec. This is a creative reference for what the full animation library should eventually contain. Use it as a brainstorming source, prioritize ruthlessly when actually building.

---

## Purpose

This is a wishlist-style catalog of animations the game will eventually want, organized for reference when designing skills, building maps, and recording reference video for animation work.

The numbers are aspirational. **You will not animate all of these in Year 1.** A realistic Year 1 ships 80-120 of these; Year 2 expands to 200; Year 3 polishes and adds the signature stuff. This is the long-term vision.

References used throughout: Naruto / Naruto Storm games, Bleach (anime + Soul Reaper game series), Jujutsu Kaisen (anime + Cursed Clash game), Attack on Titan, Avatar: The Last Airbender. Inspiration only — original IP, no copies.

---

## How to read this catalog

Each entry has:

- **Name** — proposed naming-scheme-compatible name (matches the convention from `ANIMATION_NAMING.md` when that doc exists)
- **Description** — what the animation looks like
- **Inspiration** — reference source (where applicable)
- **Priority** — Year 1 / Year 2 / Year 3 / Polish (rough estimate of when to build)

---

## 1. Locomotion (~20 animations)

The foundation. Every hero uses these constantly. Get them right early — bad locomotion is the most visible flaw in any combat game.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Loco_Idle_Combat_01` | Combat-ready stance: knees slightly bent, weight forward, hands relaxed but raised | Generic anime fighter | Y1 |
| `Loco_Idle_Combat_02` | Variant idle with subtle weight shift; plays after a few seconds of Idle_01 to avoid stiffness | — | Y1 |
| `Loco_Idle_Backline_01` | Calmer non-combat idle for backline units; relaxed shoulders, looking around | — | Y1 |
| `Loco_Walk_Forward` | Steady combat walk, eyes on target | — | Y1 |
| `Loco_Walk_Backward` | Defensive backstep; shorter strides, weight back | — | Y1 |
| `Loco_Strafe_Left` | Sideways shuffle, body still facing forward | — | Y1 |
| `Loco_Strafe_Right` | Mirror of strafe left | — | Y1 |
| `Loco_Run_Forward` | Combat run; not a sprint, controlled | — | Y1 |
| `Loco_Sprint_Forward` | Full-tilt sprint; arms swinging, body leaned forward | Naruto-style ninja run optional | Y1 |
| `Loco_Sprint_NinjaRun` | Naruto-style ninja sprint with arms trailing behind | Naruto | Y2 |
| `Loco_Stop_Sharp` | Sharp stop from sprint; brief skid | — | Y1 |
| `Loco_Turn_Left_90` | In-place 90° left turn | — | Y1 |
| `Loco_Turn_Right_90` | In-place 90° right turn | — | Y1 |
| `Loco_Turn_180` | In-place 180° turn (defensive pivot) | — | Y1 |
| `Loco_Circle_Left` | Sideways shuffle around a target, body angled inward | Boxing / fighting games | Y1 |
| `Loco_Circle_Right` | Mirror of circle left | — | Y1 |
| `Loco_Climb_Up` | Climbing up a vertical surface (tree, wall) | AoT (with hands, not gear) | Y2 |
| `Loco_Climb_Sideways` | Climbing horizontally along a wall | — | Y2 |
| `Loco_Drop_Down` | Controlled drop from height; landing crouch | AoT, Naruto | Y2 |
| `Loco_TreeRun_Forward` | Running up the side of a tree | Naruto | Y2 |

---

## 2. Reactions and getting hit (~25 animations)

These animations sell *impact*. Getting hit needs to read clearly so the player understands what just happened. Every CC effect needs its corresponding reaction.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `React_Hit_Light_Front` | Slight head jerk back, brief stagger; body otherwise stable | — | Y1 |
| `React_Hit_Light_Back` | Stagger forward, arms briefly out for balance | — | Y1 |
| `React_Hit_Light_Left` | Twist right, brief weight shift | — | Y1 |
| `React_Hit_Light_Right` | Twist left | — | Y1 |
| `React_Hit_Heavy_Front` | Major recoil; full-body backward bend, arms thrown wide | DBZ / Naruto Storm | Y1 |
| `React_Hit_Heavy_Back` | Arched forward; body wrapped around the impact | — | Y1 |
| `React_Hit_Heavy_Left` | Body whipped sideways; nearly off-balance | — | Y1 |
| `React_Hit_Heavy_Right` | Mirror of left | — | Y1 |
| `React_Block_Idle` | Defensive guard pose — forearms crossed in front, body lowered slightly | — | Y1 |
| `React_Block_Hit` | Block holds, body absorbs impact and slides back slightly | Bleach | Y1 |
| `React_Block_Break` | Block fails dramatically — guard knocked open, exposed for follow-up | Sekiro-style posture break | Y2 |
| `React_Stagger_Forward` | Brief unsteady forward shuffle; arms try to recover balance | — | Y1 |
| `React_Stagger_Backward` | Brief backward shuffle | — | Y1 |
| `React_Stun_Loop` | Stunned loop animation: head down, body swaying, stars/birds optional | Anime cliché in a good way | Y1 |
| `React_Knockback_Slide` | Forced slide backward, feet planted, body braced | Naruto Storm | Y1 |
| `React_Knockback_Tumble` | Tumbling backward roll; feet over head, recovers standing | DBZ | Y2 |
| `React_Knockdown_Launch` | Launched into low arc by heavy attack | Bleach / DBZ | Y1 |
| `React_Knockdown_Land_Front` | Lands face-down after knockdown launch | — | Y1 |
| `React_Knockdown_Land_Back` | Lands on back after knockdown launch | — | Y1 |
| `React_Knockdown_Prone` | Idle loop while lying on the ground after knockdown | — | Y1 |
| `React_Knockdown_StandUp` | Rises from prone, briefly staggers, returns to Idle_Combat | — | Y1 |
| `React_Ragdoll_Recovery` | Transition from physics ragdoll back to standing pose | — | Y2 |
| `React_Death_Front` | Knocked back, falls onto back; brief twitch | — | Y1 |
| `React_Death_Back` | Falls forward onto face | — | Y1 |
| `React_Death_Dramatic` | Slow, cinematic fall to knees, then forward; for boss kills or named hero deaths | Anime moment | Y2 |

---

## 3. Dodges and evasions (~12 animations)

Dodge animations are the player's "I survived that" moment. Make them feel impactful.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Dodge_Backflip` | Parabolic backward flip — current implementation; clean and dramatic | Anime classic | Y1 |
| `Dodge_SidestepLeft` | Quick sideways step, body leans away from attack | Boxing | Y1 |
| `Dodge_SidestepRight` | Mirror of left | — | Y1 |
| `Dodge_Roll_Forward` | Forward roll under or past an attack | — | Y2 |
| `Dodge_Roll_Backward` | Backward roll, ends in low ready stance | — | Y2 |
| `Dodge_Slide_Low` | Drops to the ground, slides forward under an attack | Naruto | Y2 |
| `Dodge_Phase_AfterImage` | Disappears in a flash, reappears 2-3m away; afterimage briefly persists | Naruto Body Flicker / Bleach Shunpo | Y2 |
| `Dodge_Aerial_Twist` | Mid-air evasion: twists body sideways while falling/jumping | DBZ aerial | Y3 |
| `Dodge_Crouch_Duck` | Drops into a low crouch, head below sweep height | Boxing | Y2 |
| `Dodge_LeanBack` | Body leans dramatically back (Matrix/AoT-style) without moving feet | AoT (Levi-style) | Y2 |
| `Dodge_Spin_Away` | Pivots around the attacker; ends facing them from a different angle | JJK | Y2 |
| `Dodge_Vanish_Smoke` | Vanishes in puff of smoke, reappears nearby | Naruto substitution | Y3 |

---

## 4. Punches (~25 animations)

The bread and butter. You need variety here because punches happen *constantly* in combat. Don't ship Year 1 with one punch animation.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Attack_Punch_Light_01` | Fast jab forward, snappy retract | — | Y1 |
| `Attack_Punch_Light_02` | Cross punch from rear hand | — | Y1 |
| `Attack_Punch_Light_03` | Hook punch from the side | Boxing | Y1 |
| `Attack_Punch_Heavy_01` | Wound-up power punch with full hip rotation; clear anticipation | Naruto Storm | Y1 |
| `Attack_Punch_Heavy_02` | Overhead drop punch; arm comes from above | DBZ Goku-style hammer punch | Y2 |
| `Attack_Punch_Uppercut` | Rising uppercut from the hip | — | Y1 |
| `Attack_Punch_HighJump` | Jumping uppercut, briefly airborne | DBZ | Y2 |
| `Attack_Punch_Backhand` | Spinning backhand from a 180° turn | JJK | Y2 |
| `Attack_Punch_Spear` | Straight spear-fingers thrust (knife hand) | Bleach Hisagi-style | Y2 |
| `Attack_Punch_Palm_Heel` | Open-palm strike; pushes target back (knockback-ready) | Avatar TLA waterbending pose | Y1 |
| `Attack_Punch_DoubleFist` | Both fists hammer down together | DBZ ground slam | Y2 |
| `Attack_Punch_Combo_01` | Two-hit combo: jab + cross | — | Y1 |
| `Attack_Punch_Combo_02` | Three-hit combo: jab + cross + hook | — | Y1 |
| `Attack_Punch_Combo_03` | Four-hit combo: jab + cross + hook + uppercut | Boxing | Y2 |
| `Attack_Punch_Flurry` | Rapid-fire 8-10 punch volley; arms blur | Bleach / DBZ rush | Y2 |
| `Attack_Punch_Rush_Charge` | Rushing forward punch — covers distance with the punch | Naruto | Y1 |
| `Attack_Punch_DashIn` | Dashes forward then punches at end of dash | — | Y1 |
| `Attack_Punch_Roundhouse` | Spinning full-body punch | Bleach | Y2 |
| `Attack_Punch_Hammerfist_Drop` | Aerial drop, both fists down on landing | DBZ | Y2 |
| `Attack_Punch_Whirlwind` | Spinning multi-punch as the attacker rotates 360° | DBZ | Y3 |
| `Attack_Punch_Counter` | Defensive counter: deflects then punches in one motion | Bleach Byakuya-style | Y2 |
| `Attack_Punch_Iaido_Style` | Slow draw, instant strike, slow re-sheath of fist | Bleach / samurai aesthetic | Y3 |
| `Attack_Punch_GroundSmash` | Both fists into the ground; sends shockwave | Avatar TLA earthbending | Y2 |
| `Attack_Punch_AirRising` | Rising punch that launches the user off the ground | DBZ | Y2 |
| `Attack_Punch_Finisher` | Slow-windup massive punch with cinematic camera; for kills | Naruto Storm finishing moves | Y2 |

---

## 5. Kicks (~20 animations)

Kicks tend to be more dramatic than punches in anime — leverage that. They're often the "moment" attacks.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Attack_Kick_Light_Front` | Fast snap kick to the midsection | Karate | Y1 |
| `Attack_Kick_Light_Side` | Side kick; foot pivot, leg extends laterally | — | Y1 |
| `Attack_Kick_Heavy_Roundhouse` | Full-rotation roundhouse to the head | Naruto | Y1 |
| `Attack_Kick_Crescent` | Sweeping crescent kick from below; arc upward | Bleach | Y1 |
| `Attack_Kick_Axe_Down` | Raised leg axe-kick down on target's shoulder/head | — | Y2 |
| `Attack_Kick_Spin_Heel` | 540° spinning heel kick | DBZ | Y2 |
| `Attack_Kick_FlyingFront` | Forward-flying kick; body horizontal, leg extended | Bruce Lee / DBZ classic | Y1 |
| `Attack_Kick_FlyingSide` | Sideways-flying kick mid-air | DBZ | Y2 |
| `Attack_Kick_Aerial_Drop` | Drops from height, both feet land on target | Bleach | Y2 |
| `Attack_Kick_Sweep_Low` | Low sweeping kick, knocks target off feet | Naruto | Y1 |
| `Attack_Kick_Rising` | Rising kick from low stance; uppercut-like with foot | Bleach | Y2 |
| `Attack_Kick_DoubleAerial` | Two kicks in mid-air on same target | DBZ | Y2 |
| `Attack_Kick_Spinning_Heel_Aerial` | Aerial 360° spinning heel | JJK | Y2 |
| `Attack_Kick_Tornado` | Multi-kick tornado spin (3-4 kicks while rotating) | Capoeira / DBZ | Y3 |
| `Attack_Kick_GroundPound` | Aerial drop with ground impact; small shockwave | Naruto | Y2 |
| `Attack_Kick_Ricochet` | Kicks one target, ricochets to a second nearby | Naruto Storm signature | Y3 |
| `Attack_Kick_LotusOpen` | Opening lotus from Rock Lee — wraps target in cloth-like motion | Naruto (Rock Lee inspiration) | Y3 |
| `Attack_Kick_LotusReverse` | Reverse lotus — multi-kick chain culminating in slam | Naruto (Rock Lee inspiration) | Y3 |
| `Attack_Kick_DragonRise` | Rising dragon kick with elemental trail | DBZ | Y2 |
| `Attack_Kick_Finisher` | Slow-windup massive kick with cinematic camera | Naruto Storm | Y2 |

---

## 6. Hand signs and casts (~35 animations — the *core mechanic*)

These are the actions in your skill slots. Per the design, you wanted ~30 different hand signs. Each sign is short (~0.4-0.8s) but they need to be *visually distinct* so players can read which sign is being formed.

The Naruto reference is intentional but the visual design should be original. Each sign should have a clear hand pose at the moment of "completion" — the snap-shot frame where the sign locks in.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Cast_Sign_A_Earth` | Both hands form a triangular shape at chest level; brief earth-glow at completion | Naruto-style | Y1 |
| `Cast_Sign_B_Lightning` | One hand vertical, other clasped over fist; lightning flicker | — | Y1 |
| `Cast_Sign_C_Water` | Hands flowing in wave pattern, ending palm-up | — | Y1 |
| `Cast_Sign_D_Fire` | Hands clap with brief flame burst between palms | — | Y2 |
| `Cast_Sign_E_Wind` | Hands sweep apart in a fan pattern | Avatar TLA airbending | Y2 |
| `Cast_Sign_F_Ice` | Hands cross at wrists, frost crackle | — | Y2 |
| `Cast_Sign_G_Light` | Hands form a circle, briefly luminous | — | Y3 |
| `Cast_Sign_H_Dark` | Hands form a downward-pointing shape, dark wisp | — | Y3 |
| `Cast_Sign_I_Wood` | Hands form a tree-branching gesture | Naruto | Y3 |
| `Cast_Sign_J_Steel` | Fists clash, ringing pose | Bleach | Y3 |
| `Cast_Sign_K_Sand` | Open palms, dust trickling between fingers | Naruto Gaara-style | Y3 |
| `Cast_Sign_L_Sound` | Hands cup one ear; resonance ripple | — | Y3 |
| `Cast_Sign_M_Time` | Slow hand sweep across the face | JJK | Y3 |
| `Cast_Sign_N_Space` | Hands describe an outward expanding bubble | JJK domain expansion-flavor | Y3 |
| `Cast_Sign_O_Lifeforce` | Both hands at solar plexus, glow upward | DBZ | Y3 |
| `Cast_Sign_P_Death` | Hand sweeps downward, fingers like a closing trap | Bleach Soul Reaper | Y3 |
| `Cast_Sign_Q_Bind` | Hands cross at chest, fists clench | — | Y3 |
| `Cast_Sign_R_Release` | Arms thrown wide outward; chains breaking gesture | Bleach Bankai | Y3 |
| `Cast_Sign_S_Mirror` | Hands form mirror-frame square in front of face | — | Y3 |
| `Cast_Sign_T_Storm` | Both arms raised, fingers spread; sky-pulling gesture | Naruto / Avatar TLA | Y3 |
| `Cast_Sign_U_Flame` | Single fist extended; fire sparking from knuckles | — | Y3 |
| `Cast_Sign_V_Thunder` | Two-finger point upward; lightning fork | — | Y3 |
| `Cast_Sign_W_Tide` | Sweeping arm from low to high; water curl trail | Avatar TLA | Y3 |
| `Cast_Sign_X_Forge` | Hammer-fist downward into open palm | — | Y3 |
| `Cast_Sign_Y_Veil` | Hand brushes across own face; obscuring gesture | — | Y3 |
| `Cast_Sign_Z_Awakening` | Both hands clasped at chest; sudden release outward | Bleach Bankai release | Y3 |
| `Cast_Sign_Combo_AB` | Two-sign sequence A>B; flowing motion between them | — | Y2 |
| `Cast_Sign_Combo_ABC` | Three-sign sequence A>B>C; faster pace, end pose | — | Y2 |
| `Cast_Sign_Focus` | Hands together at chest, brief meditative pose | — | Y1 |
| `Cast_Sign_Focus_Deep` | Longer focus version; eyes close briefly | — | Y2 |
| `Cast_Sign_Generic_Quick` | Generic fast sign (placeholder for new signs) | — | Y1 |
| `Cast_Sign_Generic_Medium` | Generic mid-pace sign | — | Y1 |
| `Cast_Sign_Generic_Slow` | Generic slow sign for high-power techniques | — | Y2 |
| `Cast_Sign_Loop_Charge` | Looping idle while charging a multi-sign cast | — | Y1 |
| `Cast_Sign_Release_Burst` | Final release pose at end of multi-sign chain | — | Y1 |

> **Design note:** You don't need 26 fully unique signs in Year 1. Ship with 6-8 (one per element you're using initially) and the four Generic placeholders. Add more as new elements come online. The "Cast_Sign_Generic_*" clips let new signs be designed without animations.

---

## 7. Skill cast poses (~25 animations)

Beyond the hand signs themselves, skills have *casting poses* — the body language while a skill resolves. Rooted casts especially need impressive poses; this is where players spend visible seconds watching.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Cast_Rooted_TripleSign_Hold` | Standing firm, hands held in final sign pose; body slightly leaned forward | Naruto | Y1 |
| `Cast_Rooted_Channeling_Loop` | Looping cast pose: arms extended, energy gathering | DBZ Kamehameha charge | Y1 |
| `Cast_Rooted_GroundDraw` | Kneels, hand on ground; runic glow spreads around | Bleach Kido | Y2 |
| `Cast_Rooted_Pillar` | Standing tall, arms raised; pillar of energy descends | Avatar TLA | Y2 |
| `Cast_Rooted_GreatBurst_Charge` | Both hands cupped at side, gathering energy; shoulders shake | DBZ | Y2 |
| `Cast_Rooted_GreatBurst_Release` | Hands thrust forward releasing the gathered energy | DBZ | Y2 |
| `Cast_Rooted_Summoning` | Slams palm to ground; smoke erupts; summon appears | Naruto | Y1 |
| `Cast_Rooted_Domain` | Arms thrown wide; dome of effect expands from caster | JJK domain expansion | Y3 |
| `Cast_Rooted_Meditate` | Cross-legged, hands on knees, eyes closed | — | Y2 |
| `Cast_Rooted_Bankai_Release` | Sword/weapon raised; transformation aura erupts | Bleach Bankai | Y3 |
| `Cast_Mobile_RunCast_Loop` | Running while making hand signs — body angled forward | Naruto | Y1 |
| `Cast_Mobile_DashCast` | Dash-while-casting; one hand outstretched mid-dash | — | Y1 |
| `Cast_Mobile_LeapCast` | Aerial cast while jumping | DBZ | Y2 |
| `Cast_Mobile_SlideStop_Cast` | Slides to a halt with cast finishing on stop | Naruto Storm | Y2 |
| `Cast_AfterImage_Burst` | Speed burst — afterimage trail forms behind caster | Naruto Body Flicker / Bleach Shunpo | Y2 |
| `Cast_Float_Idle` | Floating in place, slight hover; for advanced casters | DBZ / JJK | Y3 |
| `Cast_Float_DriftLeft` | Floating drift sideways | — | Y3 |
| `Cast_Float_AscendStrong` | Rising upward with energy aura | DBZ Super Saiyan ascent | Y3 |
| `Cast_Aura_Ignite` | Aura erupts around caster; brief power pose | DBZ aura | Y2 |
| `Cast_Aura_Sustained_Loop` | Looping aura idle pose | DBZ | Y2 |
| `Cast_Healing_Hands` | Hands cupped together, soft glow; healing pose | Naruto medical-nin | Y2 |
| `Cast_Healing_Channel_Loop` | Sustained healing pose with looping particle hands | — | Y2 |
| `Cast_Buff_SelfFist` | Punches own palm; brief confidence pose | Naruto / Avatar TLA | Y1 |
| `Cast_Buff_AllyTouch` | Reaches forward to ally; brief touch | — | Y2 |
| `Cast_Weather_RainCalling` | Hands raised, palms up; gathering rain | Avatar TLA | Y2 |

---

## 8. Speed and mobility moves (~20 animations)

These are the kinetic/speed-resource animations — the visual texture of speed gain and high-speed-state actions.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Speed_Sluggish_Idle` | Visibly tired idle: shoulders slump slightly, weight uneven | — | Y1 |
| `Speed_Engaged_Idle` | Default Idle_Combat | — | Y1 |
| `Speed_Sharp_Idle` | Sharp idle: posture upright, slight bounce, ready stance | — | Y1 |
| `Speed_Primed_Idle` | Primed: visible energy, slight aura tremor, body almost vibrating | DBZ | Y2 |
| `Speed_Burst_Vanish` | Instant vanish in place; afterimage stays briefly | Naruto Body Flicker | Y2 |
| `Speed_Burst_Reappear` | Snaps into existence at new location | — | Y2 |
| `Speed_Sprint_AfterImage` | Running with afterimage trail | Bleach Shunpo | Y2 |
| `Speed_DashStart` | Crouches briefly then explodes forward | DBZ | Y1 |
| `Speed_DashEnd_Slide` | Slides to a stop with body lean | — | Y1 |
| `Speed_Zigzag_Dodge` | Quick lateral dash; fakes one direction, goes the other | Naruto Storm | Y2 |
| `Speed_AerialDash` | Mid-air burst forward | DBZ | Y2 |
| `Speed_AerialBrake` | Stops mid-air, briefly hovers | DBZ | Y3 |
| `Speed_GroundSlide` | Slides along ground; body low | — | Y2 |
| `Speed_WallDash_Up` | Dashes up a vertical surface | AoT, Naruto | Y2 |
| `Speed_WallKick_Pivot` | Kicks off wall, redirects in air | Naruto | Y2 |
| `Speed_TripleDash_Combo` | Three quick dashes in succession; afterimages chain | Naruto Storm | Y3 |
| `Speed_MachSpeed_Loop` | Looping high-speed run animation; legs barely visible | DBZ | Y3 |
| `Speed_Teleport_Chain` | Multiple sequential teleports; afterimages | Bleach | Y3 |
| `Speed_GroundShockwave_Liftoff` | Crouches, then explodes upward; ground cracks | DBZ | Y3 |
| `Speed_Feint_Slide` | Slides one way, then explodes the other | Naruto | Y3 |

---

## 9. Aerial moves (~15 animations)

Combat in the air. Rare in early game (no jumping by default), but becomes vital with airborne states later.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Aerial_Jump_Up` | Vertical leap, knees up | — | Y1 |
| `Aerial_Jump_Forward` | Forward leap | — | Y1 |
| `Aerial_HighJump_Tree` | Massive vertical jump; multiple stories high | Naruto | Y2 |
| `Aerial_Hover_Idle` | Floating in place; arms relaxed | DBZ | Y3 |
| `Aerial_Falling_Slow` | Slowly descending; arms slightly out | — | Y2 |
| `Aerial_Falling_Combat` | Falling with active body language; ready to attack on landing | — | Y2 |
| `Aerial_Land_Soft` | Crouching landing; absorbs impact | — | Y1 |
| `Aerial_Land_Hard` | Heavy landing; ground crack effect; brief recovery | DBZ | Y2 |
| `Aerial_Land_Roll` | Lands and rolls forward | — | Y2 |
| `Aerial_Combo_Knockup` | Launching attack that sends self and target into air | Naruto Storm | Y2 |
| `Aerial_Combo_Hold_Loop` | Looping mid-air combo idle while juggling target | DBZ | Y2 |
| `Aerial_Combo_Drop_Strike` | Aerial finisher; smashes target into ground | Bleach | Y2 |
| `Aerial_Wallrun_Forward` | Running along a vertical wall | AoT-flavored, no gear | Y3 |
| `Aerial_TreeBranch_Crouch` | Crouched on a tree branch | Naruto | Y2 |
| `Aerial_TreeBranch_Leap` | Leaping from one tree branch to another | Naruto | Y2 |

---

## 10. Environment interaction (~15 animations)

Tier 2 environment per `ENVIRONMENT_DESIGN.md`. These are the animations that respond to physical environment features.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Env_KnockbackImpact_Wall` | Slammed into wall; body presses against surface, slumps down | DBZ / Naruto Storm | Y2 |
| `Env_KnockbackImpact_Tree` | Hit tree, briefly wraps around it, falls forward | — | Y2 |
| `Env_KnockbackImpact_Rock` | Smashes against rock; debris kicks up | — | Y2 |
| `Env_Fall_Cliff` | Falls off a cliff edge; flailing, then recovers far below | — | Y3 |
| `Env_Splash_WaterEntry` | Hits water; splash and submerge | — | Y3 |
| `Env_WaterEmerge_Climbout` | Climbing back out of water onto land | — | Y3 |
| `Env_Climb_TreeUp` | Climbing up a tree trunk | Naruto | Y2 |
| `Env_Climb_TreeDown` | Descending a tree trunk | — | Y2 |
| `Env_TreeBranch_Stand` | Standing on a thin branch; balance pose | Naruto | Y2 |
| `Env_HighGround_Pose` | Slightly raised stance from elevated terrain; chest out, looking down | — | Y2 |
| `Env_LowGround_Pose` | Slightly defensive low stance from below | — | Y2 |
| `Env_GroundCrack_Stomp` | Stomps the ground; cracks spread | Avatar TLA earthbending | Y2 |
| `Env_DustCloud_Idle` | Dust kicked up at feet from intense aura/movement | DBZ | Y2 |
| `Env_FoliageEmerge` | Emerges from bushes / tall grass | Naruto stealth | Y3 |
| `Env_RubbleBurst_FromGround` | Rises from beneath rubble, debris falling off | — | Y3 |

---

## 11. Special/signature techniques (~30 animations)

These are the *moments*. The signature techniques that define heroes. Most heroes get 1-3 of these — totaling roughly 30 across a 20-hero roster, with some overlap (multiple heroes share archetype-tier signatures, only top heroes get fully unique ones).

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Sig_Rasengan_Charge_Loop` | Spinning energy ball forming in palm | Naruto-style swirling sphere | Y2 |
| `Sig_Rasengan_Strike` | Rushes target with the spinning sphere; impact slam | Naruto | Y2 |
| `Sig_LightningBlade_Charge` | Crouched stance, arm crackling with energy | Chidori-flavor | Y2 |
| `Sig_LightningBlade_Pierce` | Sprint forward with arm extended; piercing strike | Chidori-flavor | Y2 |
| `Sig_LotusOpen_Wrap` | Wraps target with cloth-like energy bands; spinning kick chain | Rock Lee inspiration | Y3 |
| `Sig_LotusReverse_Combo` | Multi-hit airborne combo culminating in slam | Rock Lee inspiration | Y3 |
| `Sig_Bankai_Release` | Massive aura erupts; weapon transforms; pose holds | Bleach Bankai | Y3 |
| `Sig_Getsuga_Slash` | Sword sweep releases crescent energy wave | Bleach Ichigo | Y3 |
| `Sig_Senbonzakura` | Petal storm bursts outward from caster | Bleach Byakuya | Y3 |
| `Sig_Domain_Expansion` | Massive AOE expansion from caster; reality-warping pose | JJK | Y3 |
| `Sig_Reverse_Curse` | Self-healing pose; aura inverts | JJK Reverse Cursed Technique | Y3 |
| `Sig_BlackFlash` | Single ultra-fast strike with afterimage; brief slow-mo | JJK Black Flash | Y3 |
| `Sig_Avatar_State_Trigger` | Eyes glow; aura surges; element shift visible | Avatar TLA Avatar State | Y3 |
| `Sig_Earthbend_Pillar` | Foot stomps; pillar of stone rises from ground | Avatar TLA | Y3 |
| `Sig_Waterbend_Whip` | Water tendril extends from caster; whip motion | Avatar TLA | Y3 |
| `Sig_Firebend_Stream` | Continuous flame stream from extended fist | Avatar TLA | Y3 |
| `Sig_Airbend_Sphere` | Air sphere around caster; deflects projectiles | Avatar TLA | Y3 |
| `Sig_ODM_Manuever` | High-speed aerial movement (no gear, pure mobility) | AoT spirit | Y3 |
| `Sig_Titan_Fist_Slam` | Massive overhead two-fisted slam | AoT | Y3 |
| `Sig_FinalForm_Pose` | Standing transformation pose; aura at maximum | DBZ Super Saiyan | Y3 |
| `Sig_Kamehameha_Wave` | Two-handed beam release | DBZ | Y3 |
| `Sig_SpiritBomb_Gather` | Hands raised overhead; gathering energy | DBZ | Polish |
| `Sig_Multi_Shadow_Clone` | Clones burst into existence around caster | Naruto | Y2 |
| `Sig_Substitution_Vanish` | Caster swaps with log/ally at moment of impact | Naruto Substitution | Y3 |
| `Sig_PuppetMaster_Loop` | Caster controlling unseen strings; hands gestural | Naruto Sasori-flavor | Y3 |
| `Sig_TimeStop_Pose` | Hand raised, eyes glowing; world freezes briefly | JJK / DBZ Hit | Polish |
| `Sig_BloodlineAwaken` | Eyes change color; aura shifts; brief power surge | Naruto Sharingan/Byakugan | Y3 |
| `Sig_Telekinetic_Lift` | Hand raised, target lifted | Avatar TLA bloodbending-flavor | Polish |
| `Sig_DarkSummon_Loop` | Dark figure forms near caster; ominous pose | Bleach Hollow | Polish |
| `Sig_Absolute_Defense` | Full-body stance; impenetrable barrier forms | Bleach Captain Yamamoto-flavor | Polish |

---

## 12. Stance idles and combat-ready variants (~14 animations)

Per `COMBAT_DESIGN.md`, stances change combat behavior. Each stance gets a distinctive idle pose so the player can read which stance a hero is in at a glance.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Stance_Onslaught_Idle` | Aggressive forward lean; fists raised; weight on front foot | — | Y1 |
| `Stance_Tempest_Idle` | Lower stance; weight balanced; coiled energy | — | Y2 |
| `Stance_Stalwart_Idle` | Wide grounded stance; arms ready to block | — | Y2 |
| `Stance_Tactician_Idle` | Neutral observant stance; weight even, ready to react | — | Y2 |
| `Stance_Wraith_Idle` | Sideways profile stance; smaller target; ready to dash | — | Y2 |
| `Stance_Sentinel_Idle` | Heavy planted stance; immovable | — | Y2 |
| `Stance_Conduit_Idle` | Upright; hands relaxed at sides; meditative | — | Y2 |
| `Stance_Switch_Onslaught` | Transition into Onslaught from neutral | — | Y2 |
| `Stance_Switch_Stalwart` | Transition into Stalwart | — | Y2 |
| `Stance_Switch_Wraith` | Transition into Wraith with brief blur | — | Y2 |
| `Stance_Switch_Conduit` | Transition into Conduit; brief meditation | — | Y2 |
| `Stance_PowerPose_Win` | Victory pose at battle end | — | Y1 |
| `Stance_PowerPose_Bow` | Respectful bow; pre-fight or post-victory | Anime classic | Y2 |
| `Stance_PowerPose_Taunt` | Taunting gesture toward enemy | — | Y2 |

---

## 13. Death and defeat (~10 animations)

End-of-fight moments. Make them readable and varied.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `Death_Collapse_Forward` | Falls forward face-first | — | Y1 |
| `Death_Collapse_Backward` | Falls backward onto back | — | Y1 |
| `Death_Collapse_Slow` | Drops to knees, then forward; cinematic | Anime moment | Y2 |
| `Death_Vanish_Particles` | Body dissipates into particles | Bleach hollow death | Y3 |
| `Death_Petals_Scatter` | Body scatters as petals | Bleach Senbonzakura death | Polish |
| `Death_Shatter_Crystal` | Body crystallizes and shatters | — | Polish |
| `Defeat_KneelHold` | Kneeling defeated pose; not dead, just incapacitated | — | Y2 |
| `Defeat_HandUp` | Conscious but unable to fight; one hand raised | — | Y2 |
| `Defeat_Carry_Pose` | Being carried by ally (if rescue mechanic exists) | — | Polish |
| `Death_Aged_Peaceful` | Old hero, dies of age, peaceful sit-down | LONG_TERM_VISION ref | Polish |

---

## 14. Hit-stop and impact frames (~10 animations)

These are *frames*, not full animations — single-frame poses for hit-stop windows. Each is a "moment frozen in time" pose held briefly.

| Name | Description | Inspiration | Priority |
|---|---|---|---|
| `HitStop_PunchConnect_Frame` | Held single frame: fist pressed into target body, target deformed by impact | Anime classic | Y2 |
| `HitStop_KickConnect_Frame` | Held frame: foot into target torso | — | Y2 |
| `HitStop_BlockHold_Frame` | Held frame: blocking arm braced at moment of impact | — | Y2 |
| `HitStop_DodgeDuck_Frame` | Held frame: head tilted, attack passes overhead | — | Y2 |
| `HitStop_Counter_Frame` | Held frame: counter strike connecting | — | Y2 |
| `HitStop_Killing_Blow_Frame` | Held frame: final hit landing; cinematic | DBZ / Naruto Storm | Y2 |
| `HitStop_Block_Break_Frame` | Held frame: guard breaks open, exposed | Sekiro reference | Y3 |
| `HitStop_AfterImage_Frame` | Held frame: real attacker visible, afterimage trailing behind | Bleach | Y3 |
| `HitStop_Aerial_Connect_Frame` | Held frame: mid-air strike connecting | DBZ | Y3 |
| `HitStop_Finisher_Hold_Frame` | Extra-long held frame (~0.5s) for cinematic finishers | Anime moment | Y3 |

---

## Catalog total

Approximately **245 animations** across 14 categories.

Distribution:

| Category | Count |
|---|---|
| Locomotion | 20 |
| Reactions | 25 |
| Dodges | 12 |
| Punches | 25 |
| Kicks | 20 |
| Hand signs and casts | 35 |
| Skill cast poses | 25 |
| Speed and mobility | 20 |
| Aerial | 15 |
| Environment | 15 |
| Signatures | 30 |
| Stance idles | 14 |
| Death | 10 |
| Hit-stop frames | 10 |
| **Total** | **~256** |

Below the "thousands" mental ceiling, above the "I can fake it with Mixamo alone" floor. Real, ambitious, but achievable across a 3-year span.

---

## Priority distribution

How many of these are realistically Year 1 vs later:

| Priority | Count | What this gets you |
|---|---|---|
| **Year 1** | ~80-90 | Functional combat: locomotion, basic punches/kicks, core casts, key reactions, basic dodges, primary stances. The game is playable and looks decent. |
| **Year 2** | ~100-110 | Combat with personality: most hero signatures, environment interactions, varied combos, full stance suite, aerial basics, weather casts. The game has identity. |
| **Year 3** | ~50-60 | Polish and elite content: rare signatures, complete sign roster, advanced aerials, advanced effects-stops, dramatic deaths. The game shines. |
| **Polish** | ~10-15 | Optional gloss for endgame: extreme finishers, unique boss-tier effects, late-game cosmetic flourishes. Skip if shipping is at risk. |

**Year 1 ships with ~85 animations.** That's the actual immediate target. The rest is forward roadmap.

---

## How to use this catalog

This is a brainstorming and reference doc, not a build target. Use it to:

1. **Pull reference video for animations you're about to build.** If working on `Attack_Punch_Heavy_01`, watch the Naruto/DBZ-style power punches referenced and absorb the timing.
2. **Spot-check your skill design.** When designing a new combo, browse this catalog — does an animation already exist that fits? If yes, use it. If not, do you really need a new one, or can you pick a close match?
3. **Plan asset-pack purchases.** Look at which categories are biggest unmet gaps for you. If you have no signature animations and no good kicks, those are the asset pack categories to prioritize buying.
4. **Track progress.** Year 1 finishes when you've got ~85 animations from the Year 1 priority list working in-game. Treat this as a checklist over time.

---

## What this catalog deliberately doesn't include

- **VFX and particles.** Those are presentation-layer concerns, separate from animation clips. The hand sign animation is the body motion; the elemental effect particles are a separate VFX system.
- **Audio.** Each animation pairs with a sound effect; that's its own catalog.
- **Per-element variants of the same animation.** A "fire punch" and a "water punch" can use the same animation with different VFX layered on top. Don't animate variants per element — animate once, layer effects.
- **Camera moves.** Cinematic camera framing (slow-mo, dramatic angles) is a separate cinemachine layer, not an animation clip.

---

## Final note

Looking at this catalog might feel overwhelming, but here's the reframe: **you've already designed almost all of these.** The combat doc demanded specific kinds of animations (knockback, knockdown, exchanges, casts, signatures). The environment doc demanded others (terrain interactions, climbing). The vision doc demanded the stylistic feel.

This catalog is just *what those design decisions imply when you write them all out*. You haven't grown the scope; you've *enumerated* it.

That's actually good news. The hard part — deciding what kind of game this should be — is done. What remains is execution. One animation at a time.

You're not building 256 things. You're building 1 animation, then another, then another, for two or three years. Each one is its own small day's work.

Now stop reading and go to bed. Tomorrow's the big day.
