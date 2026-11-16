# Auto Combat For Naruto

**Drafted Auto-Combat** mod for RimWorld, enhanced with deep integration for **World of Naruto** (`LifeisGame.WorldOfNaruto`), **Taran Magic Framework** (`Taranchuk.TaranMagicFramework`), and **Naruto Expanded (Karma Legacy)** (`Ghost.WorldOfNaruto.KarmaLegacy`).

---

## Features

- **Full Naruto Technique Integration:**
  - Automatically identifies all learned jutsus and techniques from *World of Naruto* and *Naruto Expanded* via *Taran Magic Framework* (159+ abilities).
- **Auto-Cast Transformations & Stances (Pre-Combat Buffs):**
  - Sage Mode, Eight Inner Gates (Bát Môn Độn Giáp), Jinchuriki Cloaks (Vĩ Thú Y), Curse Marks, Susanoo, Sharingan, Byakugan, Rinnegan Path Shift, Mantles, and more.
  - Automatically activates when drafted pawn encounters enemies, with built-in Hediff detection to prevent spamming or deactivating while active.
- **Auto-Cast Summons & Clones:**
  - Automatically summons battle companions (Gamabunta, Manda, Katsuyu, Stone Golem, Clay constructs, Gedo) and spawns Shadow Clones (Ảnh Phân Thân) upon entering combat.
  - Respects in-game validation checks (won't re-summon existing companions or exceed clone limits).
- **Multi-tier Gizmo Resolution:**
  - Right-click any ability gizmo on the drafted toolbar to toggle **Auto-Cast** (displays a golden Auto badge).
  - One-click **"Auto Use All"** button to automate all learned abilities at once.
- **Offensive Jutsu Auto-Targeting:**
  - Handles ranged, melee, and ground/cell AoE jutsus with automatic Line of Sight and range checks.
- **Decoupled Architecture:**
  - Built cleanly using reflection and Harmony, ensuring 100% standalone reliability across any mod list without missing assembly crashes.
- **Supported RimWorld Versions:**
  - 1.5, 1.6, and 1.7.

---

## Recommended Load Order

1. `Harmony`
2. `Core` (and Official DLCs)
3. `Taran Magic Framework`
4. `World of Naruto`
5. `Naruto Expanded`
6. `Drafted Auto-Combat` (this mod)
