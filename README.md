# Synergy Highlight Mod

This mod shows internal game compatibility data in real-time via colored overlays so you can see synergy scores while building your scripts.

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

## Scoring Logic

The mod tracks three separate variables:

- Tag/Genre Compatibility: A 1-5 score average between tags and your primary genres.
- Pairing Bonuses: Specific hardcoded buffs (e.g. Action + Adventure).
- Logic Penalties: Buffs lost when required tags are missing (e.g. Antagonist Finale tags with no Antagonist).

## Genre Thresholds

The "Genre-Pair Bonus" only kicks in if you meet these exact ratios:

- Top two genres must sum to 70% or more.
- The smaller of the two genres must be at least 35%.
