# Synergy Highlight Mod

This mod shows internal game compatibility data in real-time via colored overlays so you can see synergy scores while building your scripts.

<img width="2558" height="1438" alt="screenshot" src="https://github.com/user-attachments/assets/3d85533c-8fe5-4b89-9fa5-77443775e235" />

## How to use

1. Install [BepInEx (Mono build)](https://github.com/BepInEx/BepInEx/releases) into your Hollywood Animal folder (commonly at `/Steam/steamapps/common/Hollywood Animal/`)
2. Launch the game once so BepInEx creates `/Hollywood Animal/BepInEx/plugins/`.
3. Copy `SynergyHighlightMod.dll` into `/Hollywood Animal/BepInEx/plugins/`.
4. Launch the game again.

## Color Meanings

Tag Overlays

- Green (4.0+): Peak synergy.
- Yellow (3.5-3.9): Decent fit.
- None (2.6-3.4): Neutral.
- Red (< 2.5): Score penalty.

## Genre Borders

- Green (+0.35): Massive pairing bonus.
- Lime (+0.10 to +0.34): Small boost.
- Red (-0.10): Avoid this combo.

## Slider Warnings

The bar above the genre slider monitors your pair bonus:

- Green: Bonus is active.
- Red: Bonus Lost. Check your genre percentages.

## Advertising Highlights

The score is computed from the same formula the game uses to calculate expected audience. For each `(demographic, score type)` pair a company covers:

```
subgroupScore = AudienceFraction[scoreType]
              × audienceGroupWeight[demographic][scoreType]
              × AdsEfficiency[quality]
              × movieScore[scoreType]
```

- **AudienceFraction** - fraction of all cinema-goers that respond to each score type: BASE 55%, COM 30%, ART 15%.
- **audienceGroupWeight** - demographic's share of that fraction: teens and young adults dominate COM; young adults dominate ART and BASE; adults (AM/AF) have low weights in every pool.
- **AdsEfficiency** - agency quality multiplier: budget=0.15, standard=0.30, premium=0.50.
- **movieScore** - the movie's ArtTotal, CommercialTotal, or Baseline from the current stage result.

Each subgroup score is normalised against the best possible for this movie (highest achievable across all 18 demographic × score-type combinations at max quality), then averaged across the company's covered pairs.

### Advertising colors

- Green (≥ 0.65): This company is among the most efficient choices for this movie.
- Yellow (0.45 - 0.64): Decent - above average.
- None (0.26 - 0.44): Below average for this movie.
- Red (≤ 0.25): Poor fit - this company's audience pool or quality tier makes it inefficient.

## Scoring Logic

The mod tracks three separate variables:

- Tag/Genre Compatibility: A 1-5 score average between tags and your primary genres.
- Pairing Bonuses: Specific hardcoded buffs (e.g. Action + Adventure).
- Logic Penalties: Buffs lost when required tags are missing (e.g. Antagonist Finale tags with no Antagonist).

## Genre Thresholds

The "Genre-Pair Bonus" only kicks in if you meet these exact ratios:

- Top two genres must sum to 70% or more.
- The smaller of the two genres must be at least 35%.
