#!/usr/bin/env python3
"""Procedural SFX synth for SurveHive (bee/honey pixel-art theme).

Pure stdlib (wave + math + random). Generates 16-bit mono 44.1kHz WAVs.
Design language:
  - Combat (hit/kill/boss/hurt/death): amplitude-modulated SAWTOOTH = bee buzz.
  - Positive events (pickup/levelup/victory): clean TRIANGLE/SQUARE chiptune.
  - Skills: each a distinct short gesture (volley zzt, pew, honey blorp, zap, whoosh).
Everything fades in/out a few ms to avoid clicks, then peak-normalizes.
"""
import wave, math, struct, random, os, sys

SR = 44100
# Default writes straight into the project's SFX folder when run from the repo
# root (`python3 Tools/Audio/synth.py`); pass a path to write elsewhere.
OUT = sys.argv[1] if len(sys.argv) > 1 else "Assets/Audio/Sfx"
os.makedirs(OUT, exist_ok=True)


# ---- primitives -----------------------------------------------------------
def saw(ph):        # phase in cycles [0,1)
    x = ph % 1.0
    return 2.0 * x - 1.0

def square(ph, duty=0.5):
    return 1.0 if (ph % 1.0) < duty else -1.0

def tri(ph):
    x = ph % 1.0
    return 4.0 * abs(x - 0.5) - 1.0

def sine(ph):
    return math.sin(2.0 * math.pi * ph)


def env_ad(i, n, atk, dec, hold=0.0):
    """Attack-decay envelope (seconds). hold = flat portion after attack."""
    t = i / SR
    total = n / SR
    if t < atk:
        return t / atk if atk > 0 else 1.0
    t2 = t - atk
    if t2 < hold:
        return 1.0
    t3 = t2 - hold
    remain = max(1e-4, total - atk - hold)
    return max(0.0, 1.0 - t3 / remain)

def env_exp(i, n, atk, k):
    """Fast attack, exponential decay (k = decay rate, higher=faster)."""
    t = i / SR
    if t < atk:
        return t / atk if atk > 0 else 1.0
    return math.exp(-k * (t - atk))


def buzz(dur, f0, f1, wing=52.0, wing_depth=0.45, vibrato=0.04, atk=0.004, k=None, hold=0.0):
    """Bee-buzz voice: saw glide f0->f1 with wingbeat tremolo + slight vibrato."""
    n = int(SR * dur)
    buf = [0.0] * n
    ph = 0.0
    vph = 0.0
    wph = 0.0
    for i in range(n):
        frac = i / n
        f = f0 + (f1 - f0) * frac
        fv = f * (1.0 + vibrato * sine(vph))
        ph += fv / SR
        vph += 7.0 / SR          # 7 Hz vibrato
        wph += wing / SR
        trem = 1.0 - wing_depth + wing_depth * (0.5 + 0.5 * sine(wph))
        e = env_exp(i, n, atk, k) if k else env_ad(i, n, atk, dur - atk, hold)
        buf[i] = saw(ph) * trem * e
    return buf


def arp(notes, note_dur, wave_fn=square, gap=0.0, vib=0.0, atk=0.006):
    """Chiptune arpeggio: list of freqs played in sequence."""
    parts = []
    for j, f in enumerate(notes):
        n = int(SR * note_dur)
        b = [0.0] * n
        ph = 0.0
        vph = 0.0
        for i in range(n):
            fv = f * (1.0 + vib * sine(vph))
            ph += fv / SR
            vph += 6.0 / SR
            e = env_ad(i, n, atk, note_dur)
            b[i] = wave_fn(ph) * e
        parts.append(b)
        if gap > 0:
            parts.append([0.0] * int(SR * gap))
    return sum(parts, [])


def noise_burst(dur, atk, k, lp=0.5, base=0.0, bandf=0.0, banddepth=0.0):
    """Filtered-ish noise (one-pole lowpass) with optional tonal band via AM."""
    n = int(SR * dur)
    buf = [0.0] * n
    prev = 0.0
    bph = 0.0
    for i in range(n):
        w = random.uniform(-1.0, 1.0)
        prev = prev + lp * (w - prev)   # simple lowpass
        s = prev
        if bandf > 0:
            bph += bandf / SR
            s *= (1.0 - banddepth + banddepth * (0.5 + 0.5 * sine(bph)))
        e = env_exp(i, n, atk, k)
        buf[i] = (s + base) * e
    return buf


def blip(dur, f0, f1, wave_fn=tri, atk=0.004, k=None, vib=0.0):
    """Clean tonal blip with pitch glide (honey bloop / ui click)."""
    n = int(SR * dur)
    buf = [0.0] * n
    ph = 0.0
    vph = 0.0
    for i in range(n):
        frac = i / n
        f = f0 + (f1 - f0) * frac
        fv = f * (1.0 + vib * sine(vph))
        ph += fv / SR
        vph += 5.0 / SR
        e = env_exp(i, n, atk, k) if k else env_ad(i, n, atk, dur - atk)
        buf[i] = wave_fn(ph) * e
    return buf


# ---- mix / write ----------------------------------------------------------
def mix(*layers):
    n = max(len(l) for l in layers)
    out = [0.0] * n
    for l in layers:
        for i in range(len(l)):
            out[i] += l[i]
    return out

def write(name, buf, gain=0.9):
    peak = max(1e-6, max(abs(x) for x in buf))
    scale = gain / peak
    # short fade in/out to kill clicks
    fade = min(220, len(buf) // 8)
    data = bytearray()
    for i, x in enumerate(buf):
        s = x * scale
        if i < fade:
            s *= i / fade
        if i > len(buf) - fade:
            s *= (len(buf) - i) / fade
        s = max(-1.0, min(1.0, s))
        data += struct.pack('<h', int(s * 32767))
    path = os.path.join(OUT, name)
    with wave.open(path, 'w') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(bytes(data))
    print(f"  {name:32s} {len(buf)/SR*1000:5.0f} ms")


random.seed(1337)

# ---- combat (bee buzz) ----------------------------------------------------
# Enemy hit: short punchy buzz-thud, 3 variants for pitch variety.
for i, f in enumerate((215, 235, 255)):
    write(f"hit_{i:02d}.wav", buzz(0.085, f, f * 0.68, wing=58, k=34, atk=0.002), gain=0.85)

# Enemy kill: wetter downward buzz-squish + low thump, 2 variants.
for i, (f0, f1) in enumerate(((275, 95), (300, 110))):
    b = buzz(0.19, f0, f1, wing=48, wing_depth=0.5, k=16, atk=0.002)
    thump = blip(0.19, 150, 60, wave_fn=sine, k=18)
    write(f"kill_{i:02d}.wav", mix(b, [x * 0.5 for x in thump]), gain=0.9)

# Player hurt: harsher, lower, dissonant buzz + noise transient.
b = buzz(0.16, 190, 120, wing=40, wing_depth=0.55, k=15, atk=0.001)
nz = noise_burst(0.06, 0.0, 60, lp=0.7)
write("playerhurt_00.wav", mix(b, [x * 0.4 for x in nz]), gain=0.95)

# Player death: dark descending power-down buzz.
write("playerdeath_00.wav", buzz(0.95, 300, 45, wing=44, wing_depth=0.55, k=None, atk=0.01), gain=0.95)

# Boss stinger burst: menacing layered low buzz swarm.
low = buzz(0.38, 130, 105, wing=38, wing_depth=0.5, k=None, atk=0.006)
mid = buzz(0.38, 185, 150, wing=61, wing_depth=0.45, k=None, atk=0.006)
write("bossstinger_00.wav", mix(low, [x * 0.6 for x in mid]), gain=0.95)
write("bossstinger_01.wav", mix(buzz(0.34, 150, 120, wing=41, k=None, atk=0.006),
                                [x * 0.6 for x in buzz(0.34, 205, 165, wing=57, k=None, atk=0.006)]), gain=0.95)

# ---- positive events (chiptune) ------------------------------------------
# Pickup: quick rising honey bloop, 2 variants.
write("pickup_00.wav", blip(0.10, 620, 950, wave_fn=tri, k=22), gain=0.7)
write("pickup_01.wav", blip(0.10, 700, 1050, wave_fn=tri, k=22), gain=0.7)

# Level-up: bright ascending major arpeggio C5-E5-G5-C6.
write("levelup_00.wav", arp([523, 659, 784, 1047], 0.085, wave_fn=square, vib=0.01), gain=0.72)

# Victory: longer triumphant fanfare (two rising runs, ends on held C6).
fan = arp([523, 659, 784, 1047, 988, 1047], 0.12, wave_fn=square, vib=0.015)
hold = blip(0.45, 1047, 1047, wave_fn=tri, k=None, vib=0.02)
write("victory_00.wav", fan + [x * 0.8 for x in hold], gain=0.72)

# UI click: crisp short blip (clean, not buzzy).
write("uiclick_00.wav", blip(0.045, 820, 720, wave_fn=square, k=60), gain=0.55)

# UI hover: soft, tiny, higher tick — subtler than the click so sweeping the
# cursor over buttons reads as a light touch, not a second click. Triangle (not
# square) keeps it gentle; blip uses no RNG so this insert leaves the later
# noise-based clips byte-identical.
write("uihover_00.wav", blip(0.028, 900, 1080, wave_fn=tri, k=80), gain=0.32)

# ---- skills (distinct gestures) ------------------------------------------
# Stinger Barrage: quick sharp buzzy volley "zzt", 2 variants.
write("skillstingerbarrage_00.wav", buzz(0.11, 360, 240, wing=72, wing_depth=0.5, k=30, atk=0.001), gain=0.8)
write("skillstingerbarrage_01.wav", buzz(0.11, 400, 270, wing=78, wing_depth=0.5, k=30, atk=0.001), gain=0.8)

# Piercing Lance: thin sharp descending "pew".
write("skillpiercinglance_00.wav", blip(0.14, 560, 250, wave_fn=saw, k=20), gain=0.78)
write("skillpiercinglance_01.wav", blip(0.14, 620, 280, wave_fn=saw, k=20), gain=0.78)

# Honey Splash: wet descending "blorp" + low splat.
blorp = blip(0.20, 420, 170, wave_fn=sine, k=None, vib=0.06)
splat = noise_burst(0.10, 0.0, 40, lp=0.35)
write("skillhoneysplash_00.wav", mix(blorp, [x * 0.35 for x in splat]), gain=0.82)
write("skillhoneysplash_01.wav", mix(blip(0.20, 460, 190, wave_fn=sine, k=None, vib=0.06),
                                     [x * 0.35 for x in noise_burst(0.10, 0.0, 40, lp=0.35)]), gain=0.82)

# Pollen Cloud: soft airy puff (defined for completeness; aura tick is muted in-game).
write("skillpollencloud_00.wav", noise_burst(0.26, 0.02, 9, lp=0.15, bandf=6, banddepth=0.3), gain=0.55)
write("skillpollencloud_01.wav", noise_burst(0.24, 0.02, 10, lp=0.15, bandf=7, banddepth=0.3), gain=0.55)

# Static Wings: electric zap crackle (bright square + noise, fast jitter).
def zap(dur, seed):
    random.seed(seed)
    n = int(SR * dur)
    buf = [0.0] * n
    ph = 0.0
    f = 900.0
    for i in range(n):
        if i % 300 == 0:
            f = random.uniform(600, 1400)
        ph += f / SR
        e = env_exp(i, n, 0.001, 26)
        s = square(ph, 0.5) * 0.7 + random.uniform(-1, 1) * 0.3
        buf[i] = s * e
    return buf
write("skillstaticwings_00.wav", zap(0.16, 5), gain=0.72)
write("skillstaticwings_01.wav", zap(0.16, 9), gain=0.72)

# Ember Sting: fiery whoosh rising into a low pop/boom.
whoosh = noise_burst(0.16, 0.03, 10, lp=0.25)
boom = blip(0.16, 180, 70, wave_fn=sine, k=22)
write("skillembersting_00.wav", mix(whoosh, [x * 0.7 for x in boom]), gain=0.85)
write("skillembersting_01.wav", mix(noise_burst(0.16, 0.03, 11, lp=0.25),
                                    [x * 0.7 for x in blip(0.16, 200, 78, wave_fn=sine, k=22)]), gain=0.85)

print("done")
