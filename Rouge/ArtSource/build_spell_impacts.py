"""Build the pixel-art impact layers for the spell renderer."""
import math
from pathlib import Path
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parents[1] / 'Assets/Rouge/Resources/Spells'

def crystal(draw, x, y, dx, dy, w):
    tip = (x+dx, y+dy)
    draw.polygon([(x-w,y),(x-w,y-8),tip,(x+w,y-4),(x+w,y+2)],
                 fill='#4483b9', outline='#c7fcff')
    draw.polygon([(x,y),tip,(x+w,y-4)],fill='#85dbe9')
    draw.line([(x-w,y-8),tip], fill='#f3ffff',width=2)

def bolt(draw, pts):
    for width, color in [(8,'#5754ac'),(4,'#bcadff'),(2,'#f4f9ff')]:
        draw.line(pts,fill=color,width=width)

def impact(kind, frame):
    art=Image.new('RGBA',(128,128))
    draw=ImageDraw.Draw(art)
    t=(frame+1)/12
    extent=math.sin(t*math.pi*.8)
    r=int(12+46*t)
    draw.arc((64-r,91-r//4,64+r,91+r//4),8,174,fill='#97bcdc',width=2)
    if kind=='Freeze':
        for dx,dy,w in [(-24,-34,5),(16,-42,6),(-8,-72,9),(28,-56,7),(0,-48,8)]:
            crystal(draw,64+dx*.75,90+(abs(dx)%7),dx*.42,dy*min(1,t*3),w)
    elif kind=='Conduct':
        for k in range(3):
            x=37+k*26
            pts=[(x,8+k*7),(x-10+frame%4*2,32),(x+3,33),(x-12,58),(x+6,56),(64,88)]
            bolt(draw,pts)
            draw.line([pts[3],(x-22,60),(x-28,75)],fill='#b9baff',width=2)
    else:
        for k in range(8):
            a=math.tau*k/8+.16
            def pt(rad,ang):
                return (64+math.cos(ang)*rad,66+math.sin(ang)*rad*.72)
            draw.polygon([pt(12+t*27,a-.10),pt(16+t*25,a+.11),
                          pt(22+extent*37,a)],fill='#ffffff')
    for k in range(14):
        a=k*2.399
        x=64+math.cos(a)*(14+38*t)
        y=72+math.sin(a)*(16+35*t)-11*t
        draw.line((x,y,x+math.cos(a)*4,y+math.sin(a)*5),fill='#dfedff',width=2)
    return art

for kind in ('Freeze','Conduct','Burst'):
    strip=Image.new('RGBA',(128*12,128))
    for frame in range(12):
        strip.paste(impact(kind,frame),(frame*128,0))
    strip.save(out/(kind+'Impact.png'))

