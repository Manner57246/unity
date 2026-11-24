"""Render the overlapping open-vowel cries for the well scene (no spoken words)."""
import math
import random
import struct
import wave
from pathlib import Path

rate = 32000
left = [0.0] * int(rate * 1.75)
right = left.copy()
rng = random.Random(634)
for onset, duration, pitch, pan in [(0,.65,273,-.40),(.072,.85,173,.37),(.16,.51,360,.19),
                                     (.20,.96,190,-.18),(.34,.83,234,.65),(.49,.74,146,-.57),
                                     (.71,.58,319,-.26),(.81,.56,204,.23)]:
    phase = 0
    # Open-mouth formants, with subtle variation in the voices across the square.
    resonances = []
    shift = rng.uniform(.92,1.12)
    for freq,bw,gain in ((840,110,1),(1340,150,.66),(2770,230,.27)):
        radius = math.exp(-math.pi*bw/rate)
        resonances.append([2*radius*math.cos(2*math.pi*freq*shift/rate),-radius**2,(1-radius)*gain,0,0])
    for n in range(int(rate*duration)):
        t=n/rate
        u=t/duration
        f=pitch*(.80+.43*math.sin(math.pi*u)+.023*math.sin(37*t))+rng.uniform(-2,2)
        phase = (phase+f/rate)%1
        pulse = math.exp(-phase*10)*4-0.4 + rng.uniform(-.20,.20)
        voice=0
        for b in resonances:
            v=b[2]*pulse+b[0]*b[3]+b[1]*b[4]
            b[4],b[3]=b[3],v
            voice+=v
        env=min(1,t/.035)*min(1,(duration-t)/.18)*(1-.32*u)
        idx=n+int(rate*onset)
        left[idx]+=voice*env*(1-pan)
        right[idx]+=voice*env*(1+pan)
peak=max(max(map(abs,left)),max(map(abs,right)))
out=Path(__file__).resolve().parents[1]/"Assets/Rouge/Resources/Village/CrowdCry.wav"
with wave.open(str(out),"wb") as wav:
    wav.setparams((2,2,rate,0,"NONE","not compressed"))
    wav.writeframes(b"".join(struct.pack("<hh",int(l/peak*24200),int(r/peak*24200)) for l,r in zip(left,right)))
