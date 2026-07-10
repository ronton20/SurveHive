# Audio Credits

## SFX (`Assets/Audio/Sfx/`) — original, procedurally synthesized

All SFX in this folder are **original content** generated procedurally for
SurveHive (no license restrictions — owned by the project). They were
synthesized by the pure-stdlib Python script kept at
`Tools/Audio/synth.py`, which builds each clip from oscillators + envelopes to
fit the bee/honey pixel-art theme:

- **Combat** (hit / kill / player hurt / player death / boss stinger): amplitude
  -modulated **sawtooth** = bee-buzz character, with pitch glides and wingbeat
  tremolo.
- **Positive events** (pickup / level-up / victory): clean **triangle/square**
  chiptune — rising honey "bloops" and major-arpeggio fanfares.
- **Skills**: each a distinct gesture — barrage "zzt", lance "pew", honey
  "blorp", pollen puff, static zap crackle, ember whoosh+pop.

Regenerate or tweak them by editing `Tools/Audio/synth.py`, running it to write
WAVs into `Assets/Audio/Sfx/`, then re-running `SurveHive/Apply Phase 5A Audio
Service` to rebuild the `AudioLibrary` asset.

> **Swappable for final polish:** every clip maps 1:1 to an `SfxId` via
> `AudioLibrary.asset`. To replace any sound with an AI-generated (e.g.
> ElevenLabs) or sourced clip, drop a file with the same name into
> `Assets/Audio/Sfx/` and re-run the Phase 5A pass — no code changes needed.

| Files | Event |
|---|---|
| `hit_00/01/02.wav` | enemy takes damage (3 variants, throttled) |
| `kill_00/01.wav` | enemy dies (2 variants, throttled) |
| `pickup_00/01.wav` | currency pickup |
| `levelup_00.wav` | level-up |
| `playerhurt_00.wav` | player takes damage |
| `playerdeath_00.wav` | player dies |
| `victory_00.wav` | Queen defeated / run won |
| `uiclick_00.wav` | UI button click |
| `uihover_00.wav` | UI button hover (subtler, throttled) |
| `bossstinger_00/01.wav` | Queen radial stinger burst |
| `skillstingerbarrage_00/01.wav` | Stinger Barrage fire |
| `skillpiercinglance_00/01.wav` | Piercing Lance fire |
| `skillhoneysplash_00/01.wav` | Honey Splash fire |
| `skillpollencloud_00/01.wav` | Pollen Cloud (defined; aura tick is muted in-game) |
| `skillstaticwings_00/01.wav` | Static Wings fire |
| `skillembersting_00/01.wav` | Ember Sting fire |

## Music (`Assets/Audio/Music/`) — CC0, sourced

Both tracks are **CC0** (public domain — no attribution required). Credited
anyway per each author's request.

| File | Source | Author | License |
|---|---|---|---|
| `menu_loop.ogg` | OpenGameArt — "Simple Menu/Background Music Loop" | polosik | CC0 |
| `beehive_loop.mp3` | OpenGameArt — "Happy Adventure (Loop)" | TinyWorlds | CC0 |

https://opengameart.org — license: https://creativecommons.org/publicdomain/zero/1.0/

## Pre-existing

- `Shoot.wav` (`Assets/Audio/`) — the baseline auto-attack blip, added before
  the Phase 5 audio pass (Phase 1/2); origin undocumented at the time.
