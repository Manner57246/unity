"""Build the river survey, event plates, and transparent animation layers for Crossing."""
from pathlib import Path
import math
import random
from PIL import Image, ImageDraw

out = Path(__file__).resolve().parents[1]/'Assets/Rouge/Resources/Crossing'
out.mkdir(parents=True,exist_ok=True)
rng=random.Random(316)
ink='#263640'
slate='#4e666c'
wood='#70564b'
gold='#e9b677'
light='#f2dcaf'

def river(x):
    return 188+54*math.sin(x/112-.4)+.09*(x-260)

def pine(d,x,y,s):
    d.polygon([(x,y),(x+10*s,y+2*s),(x+14*s,y),(x+2*s,y-2*s)],fill='#8c9884')
    d.line((x,y,x,y-9*s),fill=wood,width=2)
    for n in range(3):
        b=y-2*s-n*4*s
        d.polygon([(x,b-10*s),(x-(6-n)*s,b),(x+(7-n)*s,b)],fill=['#506962','#57756a','#6b8070'][n])

def house(d,x,y,s=1):
    def pts(p):return [(x+a*s,y+b*s) for a,b in p]
    d.polygon(pts([(0,0),(21,4),(21,-12),(0,-16)]),fill='#c3af91')
    d.polygon(pts([(21,4),(31,-1),(31,-15),(21,-12)]),fill='#7a8279')
    d.polygon(pts([(-3,-14),(7,-26),(28,-22),(21,-10)]),fill='#935e50')
    d.polygon(pts([(21,-10),(28,-22),(34,-14)]),fill=wood)
    d.polygon(pts([(4,-11),(7,-10),(7,-3),(4,-4)]),fill=ink)
    d.polygon(pts([(13,-9),(17,-8),(17,1),(13,0)]),fill=wood)

def bridge(d,x,y):
    for offset in (12,0):
        d.line((x-17+offset,y+30,x+11+offset,y-42),fill=wood,width=3)
    for i in range(25):
        yy=y+30-i*3;xx=x-17+i*1.17
        d.line((xx,yy,xx+13,yy+1),fill=['#a1896e','#c3ac86'][i%2],width=2)
    for i in (0,8,16,24):
        yy=y+30-i*3;xx=x-17+i*1.17
        d.line((xx,yy,xx,yy-11),fill=slate,width=2)
    d.line((x-17,y+20,x+11,y-53),fill=ink,width=1)

def boat(d,x,y,s=1):
    d.polygon([(x-13*s,y-3*s),(x+9*s,y-4*s),(x+17*s,y-8*s),
               (x+12*s,y),(x-7*s,y+4*s)],fill=wood)
    d.line((x-11*s,y-3*s,x+11*s,y-4*s),fill=light,width=max(1,int(s)))
    d.line((x,y-7*s,x+8*s,y+s),fill='#c29e71',width=max(1,int(s)))

im=Image.new('RGB',(640,400),'#aab69b');d=ImageDraw.Draw(im)
grid={(i,j):(i*16+rng.randint(-5,5),j*16+rng.randint(-5,5)) for i in range(-1,43) for j in range(-1,28)}
for j in range(-1,27):
    for i in range(-1,42):
        a,b,c,e=grid[i,j],grid[i+1,j],grid[i+1,j+1],grid[i,j+1]
        for tri in ((a,b,e),(b,c,e)):
            k=int(7*math.sin(i*.61)+5*math.cos(j*.7))+rng.randint(-4,4)
            d.polygon(tri,fill=(174+k,182+k,152+k))
for x in range(640):
    y=river(x); w=28+5*math.sin(x/62)
    for lo,hi,col in [(-w-5,w+6,'#d3c7ab'),(-w,w,'#709294'),(-w+5,w-4,'#72999b'),(-w+11,w-10,'#638c91')]:
        d.line((x,y+lo,x,y+hi),fill=col)
for _ in range(680):
    x=rng.randrange(640);y=river(x)+rng.uniform(-25,25)
    d.line((x,y,x+rng.randrange(2,10),y),fill=rng.choice(['#83a6a0','#91afa6','#6d9496']))
for x,y in sorted([(rng.randint(0,640),rng.randint(0,406)) for _ in range(635)],key=lambda v:v[1]):
    if abs(y-river(x))<49 or math.sin(x*.032)+math.cos(y*.039)<0:continue
    pine(d,x,y,rng.choice([.65,.85,1.15]))
path=[(116,148),(162,165),(218,179),(265,184),(302,178),(355,158),(396,137),(459,122),(527,112)]
d.line(path,fill='#9c9680',width=6);d.line(path,fill='#d8c9a6',width=3)
for x,y in [(111,140),(134,155),(160,149),(356,150),(384,141),(503,113),(526,108)]:house(d,x,y)
house(d,99,154,1.3)
d.ellipse((124,145,139,163),fill=wood,outline=ink,width=2)
for i in range(6):
    a=math.pi*i/3;d.line((131,154,131+6*math.cos(a),154+7*math.sin(a)),fill='#c19e74')
bridge(d,483,river(483))
for i in range(14):
    x=296+i*1.4;y=191+i*1.8;d.line((x-5,y+2,x+4,y-2),fill=wood,width=3)
d.polygon([(358,256),(383,239),(407,235),(395,248)],fill='#d8cba9')
im.save(out/'River.png')
flooded=im.copy();fw=ImageDraw.Draw(flooded)
for x in range(640):
    y=river(x)
    fw.line((x,y-61,x,y+58),fill='#739096')
for _ in range(1100):
    x=rng.randrange(640);y=river(x)+rng.randint(-56,52)
    fw.line((x,y,x+rng.randrange(3,12),y),fill=rng.choice(['#94a9a6','#66868c']))
for x,y in ((473,141),(488,137),(309,210)):fw.line((x,y,x,y-8),fill=wood,width=3)
flooded.save(out/'Flood.png')
skiff=Image.new('RGBA',(40,24));boat(ImageDraw.Draw(skiff),18,15)
skiff.save(out/'Boat.png')
walker=Image.new('RGBA',(18,24));w=ImageDraw.Draw(walker)
w.line((13,21,16,2),fill=wood,width=2)
w.rectangle((5,17,8,23),fill=ink);w.rectangle((10,17,12,22),fill=ink)
w.polygon([(5,8),(11,8),(14,18),(3,19)],fill='#3a646a')
w.rectangle((6,4,10,8),fill='#c59874');w.line((2,4,14,4),fill=wood,width=2)
w.rectangle((5,1,11,4),fill='#b98d57');walker.save(out/'Ferryman.png')

def skyplate():
    pic=Image.new('RGB',(240,116),'#c4c7ae');p=ImageDraw.Draw(pic)
    for y in range(55):p.line((0,y,240,y),fill=(187+y//2,193+y//3,179-y//5))
    for k,col in [(0,'#91aaa2'),(1,'#738f86')]:
        pts=[(-5,70)]+[(x,35+k*10+int(7*math.sin(x*.03+k))+rng.randrange(-4,4)) for x in range(-5,252,9)]+[(250,80)]
        p.polygon(pts,fill=col)
    p.rectangle((0,58,240,116),fill='#74979a')
    for _ in range(210):
        x=rng.randint(-5,240);y=rng.randint(59,116)
        p.line((x,y,x+rng.randint(2,15),y),fill=rng.choice(['#87a9a4','#64878b']))
    return pic,p

pic,p=skyplate()
p.polygon([(0,70),(74,66),(108,78),(22,89),(0,87)],fill='#997a60')
for x in (9,50,84):p.rectangle((x,65,x+4,81),fill=wood)
p.polygon([(40,56),(58,52),(68,72),(59,77),(36,72)],fill='#984f43')
p.rectangle((43,43,54,58),fill='#b58e75');p.polygon([(43,43),(46,38),(54,39),(57,44),(51,46)],fill=ink)
p.line((58,58,66,63,75,61),fill='#ceaa83',width=3);p.point((74,60),fill=gold)
p.ellipse((45,61,58,69),fill=slate,outline=ink)
p.polygon([(18,94),(139,85),(221,72),(239,74),(218,91),(136,109),(41,113)],fill=wood)
p.line((22,94,140,86,224,73),fill=light,width=2)
p.line((139,84,139,53),fill=ink,width=2)
pic.save(out/'Landing.png')
for name in ('Lamp','Shade','Hand','Thumb','Rabbit','BentEar'):
    layer=Image.new('RGBA',(240,116));p=ImageDraw.Draw(layer)
    if name=='Lamp':
        p.rectangle((134,60,144,72),fill=ink);p.rectangle((136,63,142,69),fill='#ffc879')
        p.arc((136,55,142,65),180,360,fill=ink,width=2)
    elif name=='Shade':
        p.polygon([(58,72),(80,74),(114,87),(148,88),(160,94),(152,97),(122,93),(102,94),(80,81)],fill='#35434c')
    elif name in ('Hand','Thumb'):
        p.polygon([(170,110),(171,98),(176,93),(176,87),(179,85),(180,93),(182,85),(185,84),(185,93),(189,86),(191,86),(189,98),(182,106)],fill='#1e2e38')
        if name=='Thumb':p.line((174,99,165,91,163,93),fill=ink,width=3)
    else:
        p.ellipse((146,53,168,72),fill=ink)
        p.ellipse((159,49,172,61),fill=ink)
        p.line((162,51,157,36),fill=ink,width=4)
        p.line([(168,51),(170,37),(175,40)] if name=='BentEar' else [(168,51),(171,33)],fill=ink,width=4)
    layer.save(out/(name+'.png'))
pic,p=skyplate();house(p,16,78,2)
p.rectangle((112,17,220,94),fill='#a17456');p.rectangle((119,23,214,88),fill='#eccd9a')
p.line((113,90,220,90),fill=wood,width=5)
p.polygon([(90,96),(97,60),(110,57),(119,91)],fill='#536d74')
p.ellipse((97,48,109,61),fill='#bb9071');p.line((108,69,129,62),fill='#bb9071',width=3)
pic.save(out/'Mill.png')
pic,p=skyplate()
p.line((0,29,237,44),fill=ink,width=7);p.line((0,31,236,46),fill=wood,width=2)
for x in range(0,240,19):
    y=30+x//16;p.ellipse((x-2,y-3,x+6,y+2),outline='#ab9b86')
for x in (56,128,181):
    p.line((x,50,x+4,73,x+2,94),fill='#485e61',width=4)
    for j in range(3):p.line((x+j*2,84,x+j*2-2,78),fill='#354d54',width=2)
pic.save(out/'Chain.png')
pic,p=skyplate()
p.polygon([(8,73),(36,47),(192,37),(232,51),(185,76),(34,91)],fill='#614e46')
for j in range(4):p.line((28,66+j*5,190,44+j*5),fill='#9b795b',width=2)
for x in (82,92,111,120,132):p.line((x,61,x+5,52,x+6,60),fill=light,width=2)
pic.save(out/'Hull.png')

