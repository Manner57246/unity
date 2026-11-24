"""Rebuild the Frosthold pixel plates with Pillow. Coordinates are in 2:1 map pixels."""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parents[1] / "Assets/Rouge/Resources/Frosthold"
out.mkdir(parents=True, exist_ok=True)
rng = random.Random(719)
W, H = 640, 400
im = Image.new("RGB", (W, H))
d = ImageDraw.Draw(im)

def tint(a, n):
    return tuple(max(0, min(255, int(v+n))) for v in a)

def terrain(x, z):
    # A low saddle between two massifs leaves a visible, snow-covered approach.
    peaks = [(-15, 78, 100, 125), (92, 38, 85, 80), (221, -12, 78, 84),
             (589, 24, 122, 115), (683, 156, 122, 98), (16, 307, 87, 81),
             (595, 385, 127, 85), (408, 84, 67, 26)]
    h = max(0, *(v*(1-math.hypot(x-a,(z-b)*1.2)/rad) for a,b,rad,v in peaks))
    return h + 5*math.sin(x*.035+z*.023) + 3*math.cos(z*.05-x*.02)

grid = {}
step = 12
for j in range(-3, 42):
    for i in range(-2, 57):
        x, z = i*step+rng.uniform(-4,4), j*step+rng.uniform(-4,4)
        h = terrain(x,z)
        grid[i,j] = (x,z,h)
for j in range(-3,41):
    for i in range(-2,56):
        a,b,c,e = grid[i,j],grid[i+1,j],grid[i+1,j+1],grid[i,j+1]
        for v in ((a,b,e),(b,c,e)):
            (x,z,h),(x2,z2,h2),(x3,z3,h3)=v
            determinant=(x2-x)*(z3-z)-(x3-x)*(z2-z)
            dx=((h2-h)*(z3-z)-(h3-h)*(z2-z))/determinant
            dz=((x2-x)*(h3-h)-(x3-x)*(h2-h))/determinant
            steep=math.hypot(dx,dz)
            base=(204,221,225) if steep<.69 else (146,169,186) if steep<1.04 else (101,126,149)
            d.polygon([(u,w-n*.65) for u,w,n in v],fill=tint(base,dx*19+dz*11+rng.uniform(-5,5)))

# The ravine and its ledges are visible below the trestle.
gorge=[(-10,180),(60,167),(108,179),(159,163),(204,173),(247,149),(302,164),
       (339,147),(371,161),(408,146),(459,166),(513,149),(564,166),(613,154),(651,165)]
for dy,col in [(23,'#e3eff0'),(17,'#607c90'),(9,'#667f92'),(3,'#344f64')]:
    d.polygon(gorge+[(x,y+dy) for x,y in reversed(gorge)],fill=col)
for x,y in gorge[1:-1]:
    d.line((x,y+3,x+7,y+13),fill='#96a8ae')
route=[(85,337),(125,310),(181,308),(209,281),(258,272),(289,239),(326,227),
       (343,202),(335,159),(332,139),(371,121),(408,109),(431,96)]
for w,c in [(7,'#bdcbd0'),(3,'#acbfc8'),(1,'#dbe3df')]:
    d.line(route,fill=c,width=w)
def dist(x,y,a,b):
    vx,vy=b[0]-a[0],b[1]-a[1]
    t=max(0,min(1,((x-a[0])*vx+(y-a[1])*vy)/(vx*vx+vy*vy)))
    return math.hypot(x-a[0]-t*vx,y-a[1]-t*vy)

def pine(x,y,s):
    d.polygon([(x-2*s,y),(x+8*s,y+2*s),(x+11*s,y+1*s),(x,y-2*s)],fill='#a2b6c0')
    d.rectangle((x-s,y-5*s,x+s,y),fill='#414f58')
    for n in (0,1,2):
        a=5-n; b=y-(2+4*n)*s
        d.polygon([(x,b-9*s),(x-a*s,b),(x+a*s,b)],fill='#405966')
        d.polygon([(x,b-9*s),(x-(a-1)*s,b-2*s),(x+s,b-4*s)],fill='#cbdde0')
for x,y in sorted([(rng.randint(0,640),rng.randint(4,410)) for _ in range(620)],key=lambda p:p[1]):
    if terrain(x,y)>41 or min(dist(x,y,a,b) for a,b in zip(route,route[1:]))<15: continue
    if 360<x<513 and 30<y<123 or 145<y<183: continue
    if math.sin(x*.041+math.sin(y*.032)*2)+math.cos(y*.05)<.4: continue
    pine(x,y,rng.choice([.65,.85,1,1.2]))
# A planked bridge with two parapets.
for i in range(42):
    y=151+i; x=333+(y-151)*.20
    d.line((x-6,y,x+6,y-1),fill=['#84786d','#a39d90','#a7b5b7'][i%3])
for sign in (-1,1):
    for y in range(151,194,7):
        x=333+(y-151)*.20+sign*7
        d.line((x,y,x,y-6),fill='#455762',width=2)
    d.line((333+sign*7,145,342+sign*7,187),fill='#72858b',width=2)

def block(x,y,w,h,depth):
    d.polygon([(x,y-h),(x+w,y-h),(x+w,y),(x,y)],fill='#5c7180')
    d.polygon([(x+w,y-h),(x+w+depth,y-h-depth*.6),(x+w+depth,y-depth*.6),(x+w,y)],fill='#364e60')
    d.polygon([(x,y-h),(x+depth,y-h-depth*.6),(x+w+depth,y-h-depth*.6),(x+w,y-h)],fill='#d4e4e6')
    for yy in range(int(y-h+4),int(y),5):
        for xx in range(int(x+2+(yy%2)*3),int(x+w-3),9):
            if rng.random()<.35: d.line((xx,yy,xx+3,yy),fill='#728695')

def turret(x,y,w,h):
    block(x,y,w,h,8)
    d.polygon([(x-3,y-h),(x+w//2+2,y-h-w*.95),(x+w+5,y-h-4),(x+w,y-h+1)],fill='#31485b')
    d.polygon([(x-3,y-h),(x+w//2+2,y-h-w*.95),(x+w//2-2,y-h+1)],fill='#b8ced6')
    d.rectangle((x+w*.45,y-h+5,x+w*.45+2,y-h+11),fill='#f1c375')
def flag(x,y):
    d.line((x,y,x,y-19),fill='#374850')
    d.polygon([(x+1,y-19),(x+15,y-20),(x+10,y-16),(x+13,y-14),(x+1,y-13)],fill='#994936')
    d.line((x+3,y-18,x+4,y-14),fill='#efc080')
turret(420,73,18,30);block(416,78,42,23,10);turret(444,79,18,37);flag(455,28)
block(396,91,70,16,12)
for x in range(396,466,7):block(x,78,4,5,3)
turret(391,100,17,22);turret(456,97,16,22)
block(416,102,23,20,8)
d.polygon([(421,102),(421,91),(427,85),(433,91),(433,102)],fill='#233645')
for x in (422,425,428,431):d.line((x,89+abs(427-x)//2,x,100),fill='#87929a')
flag(396,64);flag(463,63)
block(247,267,20,11,7);turret(242,273,11,16);flag(254,246)
for x,y in ((81,336),(289,239),(408,110)):
    d.line((x,y,x,y-10),fill='#665443',width=2)
    d.polygon([(x-3,y-9),(x,y-16),(x+2,y-12),(x+3,y-9)],fill='#f7b459')
im.save(out/'Pass.png')
hero=Image.new('RGBA',(25,33));d=ImageDraw.Draw(hero)
def shape(p,col): d.polygon(p,fill=col)
shape([(8,26),(16,26),(16,32),(13,32),(12,29),(10,32),(7,32)],'#192433')
shape([(10,9),(16,9),(18,17),(21,23),(24,28),(17,27),(14,30),(11,28),(7,30),(1,28),(5,21),(6,15)],'#141c27')
shape([(7,14),(11,12),(9,22),(4,27),(7,20)],'#3d4b55')
shape([(14,14),(16,17),(18,26),(14,24)],'#29343e')
shape([(8,13),(6,10),(8,5),(12,1),(16,4),(18,10),(15,14)],'#101823')
shape([(7,9),(8,5),(12,1),(15,3),(12,4)],'#62717b')
shape([(10,8),(14,6),(16,9),(14,12),(11,11)],'#090f17')
d.point((14,9),fill='#e9bc75');d.line((9,14,14,13),fill='#8a8270')
hero.save(out/'Cloak.png')

from story_plates import exterior, hall
for name in ("Ridge","Watch","Gate"):
    exterior(name).save(out/(name+".png"))
hall().save(out/"Courtyard.png")

# The battle controller addresses the same 160px cells and tags as the expedition fighter.
import json
timing=json.loads((out.parent/'WingedScout/Timing.json').read_text(encoding='utf-8-sig'))
sheet=Image.new('RGBA',(timing['meta']['size']['w'],timing['meta']['size']['h']))
for tag in timing['meta']['frameTags']:
    for index in range(tag['from'],tag['to']+1):
        pose=index-tag['from']
        attacking=tag['name'] in ('Slash','Backhand','Overhead')
        lean=[-5,-10,-1,18,11,3][pose] if attacking else (pose%2)*2
        body=hero.resize((52,70-(pose%2)*2),Image.Resampling.NEAREST)
        body=body.rotate(-lean,Image.Resampling.NEAREST,expand=True)
        cell=Image.new('RGBA',(160,160))
        cell.alpha_composite(body,(72-body.width//2,119-body.height))
        blade=ImageDraw.Draw(cell)
        # Scabbard hangs behind the hip. The silhouette stays closed through the windup.
        blade.line((48,106,77,89),fill='#101922',width=6)
        blade.line((48,106,77,89),fill='#52606a',width=2)
        blade.line((75,86,80,92),fill='#ae9368',width=2)
        angle=([214,231,273,337,358,325] if tag['name']=='Backhand' else
               [157,214,278,29,48,329] if tag['name']=='Overhead' else
               [205,230,275,329,351,319])[pose] if attacking else 317
        theta=math.radians(angle)
        grip=(82+max(0,lean)//2,89)
        tip=(grip[0]+42*math.cos(theta),grip[1]+42*math.sin(theta))
        if attacking and 2 <= pose <= 4:
            blade.line((grip,tip),fill='#475e6d',width=5)
            blade.line((grip,tip),fill='#d8ecf1',width=2)
            blade.line((grip[0]-3,grip[1]-4,grip[0]+3,grip[1]+4),fill='#c59b64',width=3)
        else:
            blade.line((78,89,87,84),fill='#3a3436',width=4)
            blade.line((79,88,81,88),fill='#b9a479',width=2)
        rect=timing['frames'][index]['frame']
        sheet.alpha_composite(cell,(rect['x'],rect['y']))
sheet.save(out/'CloakCombat.png')

# Local backdrops use the same framing as the existing battle floor.
for gate in (False,True):
    img=Image.new('RGB',(480,288),'#9baebc')
    d=ImageDraw.Draw(img)
    for j in range(2):
        pts=[(-20,110)]
        for x in range(-20,530,14):
            pts.append((x,47+j*22+int(18*math.sin(x*.023+j*3))+rng.randrange(-9,10)))
        d.polygon(pts+[(510,142),(-20,142)],fill=['#8298a9','#546d81'][j])
    d.rectangle((0,106,480,288),fill='#a9bdc8')
    for y in range(112,288,9):
        for x in range(-8,480,14):
            k=rng.randrange(-9,10)
            d.polygon([(x,y),(x+9,y-1),(x+14,y+1),(x+3,y+4)],fill=tint((164,185,198),k))
    if gate:
        d.rectangle((0,16,480,105),fill='#465a6a')
        for y in range(17,105,7):
            for x in range(-8+(y%2)*8,480,17):
                d.rectangle((x,y,x+15,y+5),fill=tint((77,97,111),rng.randrange(-6,7)))
        for x in range(0,480,18):
            d.rectangle((x,5,x+10,16),fill='#4c6374')
            d.rectangle((x,4,x+10,6),fill='#d9e6e8')
        d.polygon([(216,105),(216,52),(240,28),(264,52),(264,105)],fill='#20313e')
        for x in range(219,264,6):d.line((x,33+abs(x-240),x,98),fill='#788895',width=2)
        for x in (153,306):
            d.polygon([(x,20),(x+19,20),(x+19,64),(x+9,56),(x,66)],fill='#843e38')
    else:
        for x in range(-10,500,19):
            d.line((x,86,x,112),fill='#655e58',width=4)
            d.line((x,89,x+19,90),fill='#cddade',width=3)
        for x in (48,401):
            d.rectangle((x,66,x+25,110),fill='#586b77')
            d.polygon([(x-5,67),(x+12,46),(x+31,68)],fill='#dbe7e8')
            d.rectangle((x+11,77,x+15,86),fill='#f5be71')
    for x in (164,305):
        d.rectangle((x-2,98,x+2,112),fill='#4f4743')
        d.polygon([(x-4,99),(x-2,90),(x,93),(x+3,87),(x+4,97),(x,101)],fill='#e8a15e')
    img.resize((1280,768),Image.Resampling.NEAREST).save(out/('NorthGate.png' if gate else 'Windpost.png'))

# Cold light behind an empty chair keeps the outline readable against the interior.
hall=Image.new('RGB',(480,288),'#47505f')
d=ImageDraw.Draw(hall)
for y in range(0,116,9):
    for x in range(-12+(y%2)*11,480,23):
        d.rectangle((x,y,x+21,y+7),fill=tint((59,67,80),rng.randrange(-7,7)))
for x in (72,136,209,256,331,395):
    d.polygon([(x,86),(x,24),(x+8,12),(x+16,24),(x+16,86)],fill='#a8bbca')
    d.line((x+8,15,x+8,85),fill='#5e7084',width=2)
    d.line((x,43,x+16,43),fill='#5e7084',width=2)
d.rectangle((0,116,480,288),fill='#84909e')
for y in range(116,288,10):
    for x in range(-14+(y%3)*8,480,26):
        d.line((x,y,x+23,y),fill='#778694')
d.polygon([(220,116),(260,116),(312,288),(158,288)], fill='#725b62')
d.rectangle((230,86,251,111),fill='#4c3e40')
d.rectangle((225,107,256,114),fill='#594a4a')
for x in (228,251):d.rectangle((x,113,x+3,120),fill='#493d40')
hall.resize((1280,768),Image.Resampling.NEAREST).save(out/'ShadowHall.png')

# Nine poses follow the same strip layout as the ordinary enemies.
shade=Image.new('RGBA',(9*80,80))
for frame,(lean,bob) in enumerate([(0,0),(1,-1),(4,2),(-8,0),(5,1),(-5,2),(2,4),(6,4),(9,12)]):
    silhouette=Image.new('RGBA',(80,80))
    d=ImageDraw.Draw(silhouette)
    d.polygon([(28+lean,22+bob),(37+lean,10+bob),(43+lean,7+bob),(51+lean,22+bob),
               (47+lean,29+bob),(52+lean,35+bob),(58,52),(66,65),(57,62),
               (60,69),(48,64),(42,70),(34,66),(25,70),(27,63),(12,68),
               (19,57),(24+lean,40),(30+lean,29+bob)],fill='#0b101a')
    d.line((28+lean,22+bob,37+lean,10+bob,43+lean,7+bob),fill='#35404e',width=1)
    shade.paste(silhouette,(frame*80,0))
shade.save(out/'Shadow.png')

