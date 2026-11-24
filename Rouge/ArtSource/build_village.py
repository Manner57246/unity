"""Pixel plates and season palettes for the village chapter. Rebuild with Pillow."""
import json
import math
import random
import re
import uuid
from pathlib import Path
from PIL import Image, ImageDraw, ImageOps
from story_plates import ice, mark_ice, home_plate, village_green, mill_plate, south_night

res = Path(__file__).resolve().parents[1] / "Assets/Rouge/Resources"
out = res / "Village"
out.mkdir(exist_ok=True)
rng = random.Random(733)
ink, wood, light, skin = "#26343b", "#694d40", "#f8dfab", "#c59471"

def save(img, name, template="Crossing/River"):
    img.save(out / (name+".png"))
    meta = out / (name+".png.meta")
    if not meta.exists():
        source = (res / (template+".png.meta")).read_text()
        meta.write_text(re.sub(r"(?m)^guid: .*","guid: "+uuid.uuid4().hex,source))

save(ice(),"Ice")
save(mill_plate(),"MillHouse")
save(mill_plate(True),"Cellar")
save(south_night(False),"Refuge")
save(south_night(True),"Bells")
river = Image.open(res/"Crossing/River.png").convert("RGB")
# The ferry house and sounding poles are visible long before the river freezes.
d=ImageDraw.Draw(river)
d.polygon([(201,287),(219,290),(219,279),(201,276)],fill="#b1a38c")
d.polygon([(219,290),(226,286),(226,276),(219,279)],fill="#7a8276")
d.polygon([(198,276),(206,265),(225,268),(219,280)],fill="#886254")
d.polygon([(219,280),(225,268),(229,276)],fill="#655b52")
d.rectangle((205,279,208,284),fill="#564f47")
for x,y in ((198,205),(226,270)):
    d.line((x,y,x,y-7),fill="#654b45",width=2)
    d.line((x,y-7,x,y-5),fill="#b76e52",width=3)
save(river,"Spring")
for name, palette in [("Summer",("#354a48","#92a36e","#e8d39d")),
                      ("Autumn",("#3e4146","#bc975f","#ead8ac")),
                      ("Winter",("#394655","#a1b0b5","#f0ecdc"))]:
    gray = ImageOps.grayscale(river)
    graded = ImageOps.colorize(gray,black=palette[0],white=palette[2],mid=palette[1],blackpoint=29,midpoint=135,whitepoint=208)
    pic=Image.blend(river,graded,.85 if name=="Winter" else .64)
    if name == "Winter": mark_ice(pic)
    save(pic,name)

def background(winter=False):
    pic=Image.new("RGB",(240,116))
    d=ImageDraw.Draw(pic)
    for y in range(116):
        k=min(y,60)
        d.line((0,y,240,y),fill=(154+k,169+k//2,175-k//6) if winter else (146+k,167+k//2,160-k//5))
    for n,col in enumerate(("#a5b0a7","#788f86","#546d67")):
        pts=[(x,30+n*8+int(6*math.sin(x*.035+n)+3*math.cos(x*.09))) for x in range(-8,249,4)]
        d.polygon(pts+[(248,84),(-8,84)],fill=col)
    d.rectangle((0,73,240,116),fill="#cbd0c8" if winter else "#a49e7c")
    for i in range(300):
        x=rng.randrange(240); y=rng.randrange(74,116)
        d.line((x,y,x+rng.randrange(1,6),y),fill=rng.choice(("#aaaead","#e6e2d0") if winter else ("#989970","#bbad88")))
    return pic

def cottage(d,x,y,s,snow=False):
    def poly(points,c): d.polygon([(x+a*s,y+b*s) for a,b in points],fill=c)
    poly([(0,0),(36,0),(36,-23),(0,-23)],"#a89a84")
    poly([(36,0),(53,-6),(53,-26),(36,-23)],"#7b7e74")
    poly([(6,-22),(6,-39),(10,-39),(10,-22)],wood)
    poly([(-4,-20),(7,-34),(43,-35),(36,-21)],"#c1c6bf" if snow else "#855951")
    poly([(36,-21),(43,-35),(57,-25)],"#605657")
    for i in (1,15,34):poly([(i,-20),(i+2,-20),(i+2,0),(i,0)],wood)
    poly([(19,0),(29,0),(29,-14),(19,-14)],wood)
    poly([(5,-10),(11,-10),(11,-17),(5,-17)],"#dfb77e")

def person(d,x,y,s,coat,hat=True):
    def poly(pts,c):d.polygon([(x+a*s,y+b*s) for a,b in pts],fill=c)
    for a in (-3,2):poly([(a,-7),(a+2,-7),(a+2,0),(a-1,0)],ink)
    poly([(-4,-14),(-1,-16),(3,-15),(5,-7),(3,-5),(-5,-6)],coat)
    poly([(-2,-20),(2,-20),(3,-17),(2,-14),(-2,-15)],skin)
    if hat:poly([(-4,-19),(-2,-22),(2,-22),(4,-19),(1,-18)],"#52463f")

save(home_plate(False,person),"Hearth")
save(home_plate(True,person),"Door")

def mount(d,x,y,s,blade=False):
    def poly(pts,c):d.polygon([(x+a*s,y+b*s) for a,b in pts],fill=c)
    for xx,k in ((-12,2),(9,-2),(-8,-4),(13,4)):
        poly([(xx,-12),(xx+3,-11),(xx+2,-5),(xx+k,0),(xx+k-3,0),(xx-1,-6)],ink)
    poly([(-18,-18),(-10,-24),(9,-22),(17,-24),(22,-18),(18,-9),(10,-8),(-11,-11)],"#464547")
    poly([(-17,-14),(-21,-20),(-23,-20),(-24,-16),(-30,-16),(-31,-20),(-26,-27),(-27,-32),(-23,-29),(-17,-31),(-12,-27),(-8,-20)],"#565358")
    poly([(15,-23),(23,-21),(25,-9),(28,-7),(23,-6),(19,-15)],ink)
    poly([(-12,-24),(4,-24),(7,-12),(0,-14),(-7,-13)],"#783f41")
    poly([(-6,-37),(1,-39),(7,-34),(10,-23),(5,-20),(-6,-23)],"#444e5b")
    poly([(-5,-36),(0,-38),(3,-36),(5,-26),(-3,-27)],"#84959f")
    poly([(0,-38),(6,-36),(15,-24),(6,-25)],"#713e45")
    poly([(-4,-40),(-5,-45),(-2,-48),(2,-48),(5,-44),(4,-40),(1,-37)],"#a6b0ae")
    poly([(-5,-43),(2,-43),(4,-42),(-4,-41)],ink)
    poly([(-4,-25),(2,-25),(4,-21),(0,-12),(-5,-11),(-3,-17)],ink)
    poly([(-25,-25),(-20,-23),(-12,-21),(-6,-29),(-5,-28),(-12,-19),(-24,-23)],"#b59773")
    d.line((x-5*s,y-31*s,x-11*s,y-30*s),fill="#a4aeb2",width=2)
    if blade:
        poly([(-9,-31),(-12,-32),(-34,-51),(-29,-43)],"#e9e6ca")
    else:
        poly([(-10,-30),(-11,-29),(-17,-13),(-14,-17)],"#c3ced0")

for stage in ("Well","Riders","Blade","Glint","Cut","Stagger","Kneel","Fall","Flinch","Rush","Scatter"):
    save(village_green(stage,mount,person),stage)

p=background(); d=ImageDraw.Draw(p)
for k in range(60):
    x=rng.randrange(240);y=rng.randrange(55,82);h=rng.randrange(23,52)
    d.rectangle((x-1,y-h,x+1,y),fill=wood)
    for j in range(3):
        w=(3-j)*3; a=y-8-j*8
        d.polygon([(x-w,a),(x+1,a-h//2),(x+w,a)],fill=("#596956","#657458","#758061")[j])
for x in (0,17,218,233):
    d.polygon([(x-4,108),(x+1,55),(x-1,0),(x+6,0),(x+6,52),(x+4,103)],fill="#514d43")
for i in range(180):
    x=rng.randrange(240);y=rng.randrange(82,116)
    d.line((x,y,x+3,y-1),fill=rng.choice(("#ab8954","#d1b379","#8c8165")))
save(p,"Forest")
clearing=Image.new("RGB",(480,288),"#afa080")
clearing.paste(p.resize((480,124),Image.Resampling.NEAREST),(0,0))
ground=ImageDraw.Draw(clearing)
for k in range(1500):
    x=rng.randrange(480); y=rng.randrange(121,288)
    ground.line((x,y,x+rng.randrange(2,8),y-1),fill=rng.choice(("#a09976","#bdae88","#c0ac82","#9c9479")))
for x,y in ((108,135),(339,133),(103,187),(362,167),(191,136),(259,179)):
    ground.line((x-5,y,x,y+1,x+7,y-3),fill="#80775f",width=2)
save(clearing.resize((1280,768),Image.Resampling.NEAREST),"Clearing","Frosthold/NorthGate")

sheet=Image.new("RGBA",(720,80))
for i in range(9):
    tile=Image.new("RGBA",(80,80));d=ImageDraw.Draw(tile)
    z=[0,-1,2,1,-1,0,2,1,5][i]
    def poly(pts,c):d.polygon([(x,y+z) for x,y in pts],fill=c)
    for x in (25,33,51,59):
        shift=(i+x)%4-1
        poly([(x,41),(x+5,41),(x+2,54),(x+shift,58),(x-3+shift,58)],"#403c3a")
    poly([(7,42),(15,35),(18,28),(23,29),(22,23),(28,26),(31,19),(34,22),(38,18),(41,21),(48,20),(59,24),(66,31),(65,43),(60,49),(30,48),(23,46),(13,48),(7,46)],"#5a4a42")
    poly([(16,36),(26,31),(23,22),(30,27),(31,33),(40,32),(48,37),(40,44),(27,44),(12,47)],"#846751")
    d.point((20,37+z),fill=light);poly([(10,45),(15,44),(13,39)],"#d6c59f")
    d.line((62,29+z,70,34+z,68,40+z),fill=wood,width=2)
    sheet.alpha_composite(tile.resize((51,51),Image.Resampling.NEAREST),(i*80+14,20))
save(sheet,"Boar","Frosthold/Shadow")
timing=json.loads((res/"WingedScout/Timing.json").read_text(encoding="utf-8-sig"))
sheet=Image.new("RGBA",(1280,640))
for i,data in enumerate(timing["frames"]):
    tile=Image.new("RGBA",(160,160));d=ImageDraw.Draw(tile)
    lean=round(math.sin(i*1.5)*4) if i>=14 else i%2
    person(d,72+lean,118,2.8,"#66786b")
    d.polygon([(62+lean,66),(64+lean,54),(79+lean,54),(84+lean,63),(91+lean,66)],fill="#95744f")
    d.line((70+lean,81,83+lean,88,88+lean,84),fill=skin,width=5)
    lift = (i%6)*3 if i>=14 else 0
    d.line((88+lean,85,100+lean+lift,77+lift),fill="#b9b6a8",width=3)
    rect=data["frame"];sheet.alpha_composite(tile,(rect["x"],rect["y"]))
save(sheet,"Hunter","Frosthold/CloakCombat")
