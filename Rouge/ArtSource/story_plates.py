"""Illustrated story panels at their native pixel resolution."""
import math
import random
from PIL import Image, ImageDraw

W,H=360,238
INK="#1e2d37"
GOLD="#edc483"
PAPER="#e4e1c8"
RUST="#a75648"

class Plate:
    def __init__(self,seed):
        self.im=Image.new("RGB",(W,H),"#8a9b9e")
        self.d=ImageDraw.Draw(self.im)
        self.r=random.Random(seed)

    def poly(self,pts,color):
        self.d.polygon(pts,fill=color)

    def sky(self,sun=(246,48)):
        d=self.d
        for y,c in ((45,"#a2afa8"),(69,"#b8bca9"),(94,"#c7c3aa")):
            d.rectangle((0,y,W,H),fill=c)
        x,y=sun
        d.ellipse((x-19,y-19,x+19,y+19),fill="#f0dfaa")
        for i in range(36):
            x=self.r.randrange(-20,W);y=self.r.randrange(5,82)
            d.line((x,y,x+self.r.randrange(8,44),y),fill="#c4c7b4")
        for k,(depth,c,facet) in enumerate(((93,"#919fa0","#b5beb4"),(117,"#738991","#a6b2ae"),(141,"#526d79","#869b9e"))):
            verts=[]
            for x in range(-45,W+65,38):
                h=self.r.randrange(15,65)
                verts.extend([(x,depth),(x+20,depth-h)])
                self.poly([(x-1,depth),(x+20,depth-h),(x+45,depth+5)],c)
                self.poly([(x+20,depth-h),(x+22,depth-h+13),(x+16,depth-h+14),(x+12,depth-h+23),(x+8,depth-h+21)],facet)
            d.rectangle((0,depth+1,W,H),fill=c)

    def pine(self,x,y,h,c):
        self.d.line((x,y,x,y-h//2),fill=c,width=max(1,h//14))
        for n in range(3):
            w=int(h*(.23-.044*n));b=y-h*.12-n*h*.21
            self.poly([(x,b-h*.43),(x-w,b),(x-w//3,b-1),(x+1,b+1),(x+w,b)],c)

    def ground(self,y):
        self.poly([(0,y),(68,y-4),(153,y+2),(221,y-3),(360,y-10),(360,H),(0,H)],"#c2cfcb")
        for i in range(390):
            x=self.r.randrange(W);yy=self.r.randrange(y+2,H)
            self.d.line((x,yy,x+self.r.randrange(1,9),yy-1),fill=self.r.choice(("#a6bbbf","#dbe0d5","#bcc9c5")))
    def wall(self,x,y,w,h,body="#425761"):
        d=self.d
        d.rectangle((x,y-h,x+w,y),fill=body)
        for j in range(0,h-4,7):
            for i in range(1+(j%2)*7,w-6,15):
                if self.r.random()<.52:
                    a=x+i;b=y-j
                    d.line((a,b,a+self.r.randrange(3,11),b),fill="#657780")
                    d.point((a-1,b-1),fill="#354b56")
        for i in range(x,x+w-3,12):
            d.rectangle((i,y-h-5,i+6,y-h),fill=body)
            d.line((i,y-h-6,i+6,y-h-6),fill="#d5dcd0")
    def tower(self,x,y,w,h):
        d=self.d;t=y-h
        self.wall(x,y,w,h)
        self.poly([(x+w*.64,t),(x+w,t),(x+w,y),(x+w*.64,y)],"#334956")
        self.poly([(x-5,t+2),(x+w*.48,t-w*.91),(x+w+6,t+1)],"#253947")
        self.poly([(x-5,t),(x+w*.48,t-w*.91),(x+w*.28,t-4),(x+4,t-1)],"#c1cecc")
        d.line((x-2,t+3,x+w+4,t+2),fill="#71838a",width=2)
        for i in (0,1):
            a=int(x+w*.32);b=int(t+9+i*18)
            if b<y-10: d.rectangle((a,b,a+2,b+6),fill=GOLD)
    def flag(self,x,y,h=40):
        d=self.d; d.line((x,y,x,y-h),fill=INK,width=2)
        d.line((x-3,y-h+2,x+18,y-h+2),fill=INK,width=2)
        self.poly([(x+2,y-h+3),(x+16,y-h+3),(x+16,y-8),(x+9,y-12),(x+2,y-8)],RUST)
        d.line((x+8,y-h+9,x+8,y-h+17),fill=GOLD,width=2)
        d.line((x+5,y-h+12,x+12,y-h+12),fill=GOLD,width=2)
    def figure(self,x,y,s,guard=False,shadow=False):
        def p(pts,c):self.poly([(x+int(i*s),y+int(j*s)) for i,j in pts],c)
        self.d.ellipse((int(x-8*s),int(y-s),int(x+12*s),int(y+2*s)),fill="#748991" if not shadow else "#384a56")
        p([(-4,-7),(0,-7),(-1,0),(-5,0)],INK)
        p([(1,-6),(4,-6),(6,0),(2,0)],INK)
        p([(-4,-15),(-1,-18),(4,-15),(5,-10),(9,-3),(4,-4),(2,-2),(-2,-4),(-9,-2),(-6,-9)],"#121f2b" if not guard else "#8b4d43")
        p([(-5,-12),(-1,-15),(-2,-10),(-6,-5)],"#384954" if not guard else "#ba7155")
        p([(-5,-16),(-5,-20),(-1,-25),(3,-24),(6,-18),(4,-15)],"#172530" if not guard else "#677e87")
        p([(-4,-21),(-1,-25),(3,-24),(-1,-23)],"#91a3a3")
        if guard:
            p([(-5,-19),(5,-20),(6,-18),(-4,-17)],INK)
            p([(-4,-17),(0,-17),(0,-16),(-4,-16)],GOLD)
            p([(7,-2),(8,-2),(11,-25),(10,-25)],"#604c43")
            p([(9,-25),(11,-31),(12,-24)],"#d5d9c5")
        elif not shadow:
            p([(-10,-3),(2,-11),(4,-10),(-8,-2)],"#5c5652")
            p([(0,-10),(2,-13),(3,-12),(2,-9)],GOLD)

def exterior(name):
    a=Plate({"Ridge":41,"Watch":75,"Gate":18}[name]);d=a.d
    a.sky()
    for i in range(95):
        x=a.r.randrange(-8,368);y=a.r.randrange(135,153);a.pine(x,y,a.r.randrange(9,28),"#425d67")
    if name=="Ridge":
        a.poly([(105,170),(134,136),(157,137),(185,102),(236,91),(285,103),(315,150),(360,145),(360,195)],"#465b68")
        a.wall(158,115,118,21);a.tower(177,101,17,24);a.tower(207,103,24,44);a.tower(248,108,17,31)
        a.flag(228,67,31);a.ground(179)
        a.poly([(0,190),(60,173),(103,164),(143,174),(186,154),(157,181),(99,191),(48,203),(0,218)],"#82989e")
        for x,y,h in [(4,209,64),(30,193,44),(347,175,55),(328,172,27)]:a.pine(x,y,h,"#324c57")
        a.figure(100,208,1.55)
        for i in range(8): d.line((72-i*6,213+i*2,75-i*6,213+i*2),fill="#7c9399",width=2)
    elif name=="Watch":
        a.ground(158);a.tower(247,159,43,60)
        a.poly([(0,204),(179,155),(203,162),(0,225)],"#354c56")
        a.poly([(0,196),(177,152),(211,158),(0,214)],"#887c68")
        for i in range(13):
            x=i*16; y=198-i*3.65
            d.line((x,y,x+15,y+5),fill="#c3b59b")
        for x,y in ((17,199),(73,183),(132,168),(186,155)):
            d.line((x,y,x,y-16),fill="#4d504b",width=3)
        d.line((16,185,188,139),fill="#525b59",width=2)
        a.flag(220,159,65);a.figure(91,202,1.7);a.figure(213,177,1.16,True)
        a.pine(342,221,78,"#304650")
    else:
        a.wall(-5,180,370,58);a.tower(26,178,52,82);a.tower(272,177,56,96)
        a.wall(96,181,158,87);a.ground(181)
        a.poly([(139,181),(139,142),(149,124),(174,110),(199,124),(209,142),(209,181)],"#8b9896")
        a.poly([(145,181),(145,143),(155,128),(174,118),(195,130),(202,144),(202,181)],INK)
        for x in range(148,201,6):
            y=124+int(abs(x-174)*.65);d.line((x,y,x,y+23),fill="#79868a",width=2)
        for x in (114,225):
            a.flag(x,156,51)
            d.ellipse((x-2,162,x+3,166),fill=GOLD)
        a.poly([(148,182),(202,182),(242,237),(102,237)],"#98a7a5")
        a.figure(119,212,1.45);a.figure(218,186,1.06,True)
    return a.im

def hall():
    a=Plate(70);d=a.d;d.rectangle((0,0,W,H),fill="#263944")
    for x in (0,70,140,210,280):
        a.poly([(x,142),(x,57),(x+12,31),(x+34,12),(x+56,31),(x+69,57),(x+69,142)],"#405661")
        a.poly([(x+7,140),(x+7,57),(x+17,36),(x+34,20),(x+53,38),(x+61,60),(x+61,140)],"#253944")
        a.poly([(x+21,110),(x+21,59),(x+34,37),(x+47,59),(x+47,110)],"#99a9a4")
        d.line((x+34,40,x+34,111),fill="#4c646c",width=3);d.line((x+21,76,x+47,76),fill="#50636b",width=2)
        d.rectangle((x-3,31,x+3,150),fill=INK)
    a.poly([(0,144),(360,144),(360,238),(0,238)],"#71868d")
    for x in range(-360,801,47): d.line((180+(x-180)*.15,144,x,238),fill="#566c76")
    for y in (150,158,170,187,213):d.line((0,y,W,y),fill="#536a75")
    a.poly([(188,147),(224,147),(294,237),(164,237)],"#784e4b")
    for x in (43,299):a.flag(x,109,84)
    for x in (122,263):
        d.line((x,127,x,151),fill=INK,width=3);d.ellipse((x-3,119,x+3,128),fill=GOLD)
    d.rectangle((195,113,219,145),fill="#584a41");d.rectangle((198,117,216,137),fill="#856953")
    d.line((193,143,223,143),fill=GOLD,width=2)
    for x in (197,217):d.rectangle((x,145,x+2,152),fill=INK)
    a.figure(102,208,2);a.figure(210,170,1.35,shadow=True)
    return a.im

def ice():
    a=Plate(110);d=a.d;a.sky((72,52));a.ground(137)
    for x in range(-5,370,8):a.pine(x,137,a.r.randrange(13,38),"#526971")
    a.poly([(0,168),(90,151),(246,143),(360,148),(360,239),(0,239)],"#688e9a")
    a.poly([(42,238),(113,190),(228,149),(255,146),(181,195),(155,238)],"#acd6d5")
    for i in range(310):
        x=a.r.randrange(W);y=a.r.randrange(162,H);d.line((x,y,x+a.r.randrange(2,12),y),fill="#a6c9c9")
    for x,y,h in [(62,224,33),(242,150,13)]:
        d.line((x,y,x,y-h),fill="#704e45",width=3);d.line((x,y-h,x,y-h+5),fill=RUST,width=4)
    a.poly([(241,141),(241,124),(265,122),(273,129),(273,143)],"#5d635e")
    a.poly([(236,126),(250,111),(270,110),(278,126)],"#c1ceca")
    d.rectangle((251,128,255,134),fill=GOLD)
    return a.im

def mark_ice(pic):
    d=ImageDraw.Draw(pic);r=random.Random(23)
    def river(x):return 188+54*math.sin(x/112-.4)+.09*(x-260)
    for y in range(194,278):
        bend=round(2*math.sin(y*.14))
        left=198+bend+(y//5)%2
        right=226+bend-(y//7)%2
        for x in range(left,right+1):
            if abs(y-river(x))>30+5*math.sin(x/62): continue
            color="#a4d2d3" if x<left+3 or x>right-3 else "#95c6cd"
            if x==left or x==right: color="#d4e9df"
            d.point((x,y),fill=color)
    for i in range(34):
        x=r.randrange(201,222);y=int(river(x))+r.randrange(-24,25)
        d.line((x,y,min(224,x+r.randrange(2,6)),y-1),fill=r.choice(("#bce6de","#86b9c4")))
    for x,y in ((201,220),(221,243),(203,255)):
        d.line((x,y,x+3,y-3,x+3,y-6,x+7,y-8),fill="#74a2b3")
    for i in range(13):
        y=209+i*4
        x=212+int(3*math.sin(i*.4))+(i%2)*2
        d.line((x,y,x+1,y),fill="#d7e6de")
    for x,y in ((198,205),(226,270)):
        d.line((x,y,x,y-7),fill="#654b45",width=2);d.line((x,y-7,x,y-5),fill="#c16f52",width=3)
def timber(a,x,y,w,h,snow):
    d=a.d; ridge=y-h-w*.20
    a.poly([(x,y),(x,y-h),(x+w*.76,y-h),(x+w*.76,y+2)],"#b7a789")
    a.poly([(x+w*.76,y+2),(x+w*.76,y-h),(x+w,y-h-3),(x+w,y-3)],"#7d8073")
    for xx in (0,.31,.74):
        d.line((x+w*xx,y-h,x+w*xx,y),fill="#59564d",width=max(2,int(w*.027)))
    d.line((x,y-h*.45,x+w*.75,y-h*.45),fill="#695e50",width=max(2,int(w*.022)))
    d.line((x+1,y-h*.96,x+w*.3,y-h*.48),fill="#716a58",width=max(2,int(w*.023)))
    a.poly([(x-5,y-h+2),(x+w*.21,ridge),(x+w*.98,ridge-4),(x+w*.77,y-h+2)],"#655958")
    for i in range(24):
        tx=x+w*(.23+(i%8)*.082)-(i//8)*w*.055;ty=ridge+(i//8)*w*.052
        d.line((tx,ty,tx+w*.065,ty),fill="#8e7769",width=2)
    if snow:
        a.poly([(x-5,y-h),(x+w*.21,ridge-2),(x+w*.99,ridge-5),(x+w*.91,ridge+w*.045),(x+w*.66,ridge+w*.063),(x+w*.54,ridge+w*.1),(x+w*.29,ridge+w*.09),(x+w*.21,ridge+w*.13)],"#d3d9d0")
    a.poly([(x+w*.77,y-h),(x+w*.98,ridge-4),(x+w*1.08,y-h-4)],"#4c5653")
    d.rectangle((x+w*.08,y-h*.43,x+w*.235,y-h*.12),fill=INK)
    d.rectangle((x+w*.10,y-h*.40,x+w*.21,y-h*.15),fill=GOLD)
    d.line((x+w*.16,y-h*.42,x+w*.16,y-h*.13),fill="#645b4d",width=2)
    d.rectangle((x+w*.45,y-h*.76,x+w*.67,y),fill="#645247")
    d.rectangle((x+w*.47,y-h*.72,x+w*.65,y),fill="#443e37")
    d.line((x+w*.56,y-h*.67,x+w*.56,y-2),fill="#685346",width=2)
    d.point((x+w*.62,y-h*.34),fill=GOLD)

def home_plate(winter,person):
    a=Plate(108);d=a.d;a.sky((278,57))
    for x in range(-5,370,9): a.pine(x,151,a.r.randrange(20,45),"#516963")
    a.ground(166)
    if not winter: a.poly([(0,169),(92,163),(218,166),(360,157),(360,238),(0,238)],"#a4a386")
    a.poly([(59,238),(157,196),(215,179),(240,183),(184,212),(162,238)],"#a9a391")
    a.poly([(150,96),(150,69),(161,69),(162,101)],"#665f58")
    timber(a,114,185,195,70,winter)
    # Bark and forked branches frame the door from the same viewpoint in both seasons.
    a.poly([(16,228),(25,203),(31,156),(23,133),(14,91),(20,93),(33,123),(36,131),(46,114),(55,84),(61,82),(55,111),(43,142),(42,178),(37,207),(44,225)],"#4e514a")
    for x,y,xx,yy in [(20,98,-4,65),(20,99,24,75),(55,98,89,75),(51,116,95,108),(60,87,74,48),(36,148,9,134)]:
        d.line((x,y,xx,yy),fill="#635f53",width=3)
        d.line((xx,yy,xx+4,yy-10),fill="#635f53")
        if not winter:
            for i in range(13):
                tx=xx+a.r.randrange(-18,18);ty=yy+a.r.randrange(-11,10)
                a.poly([(tx-4,ty),(tx,ty-3),(tx+6,ty-2),(tx+8,ty),(tx+2,ty+2)],a.r.choice(("#819076","#939c7e","#d6d6b8")))
    for x in (0,16,34,52,70,89):
        d.line((x,228,x+1,196),fill="#6e6658",width=4)
    d.line((0,210,93,203),fill="#8a7965",width=4)
    if not winter:
        person(d,222,189,1.7,"#985f51",False)
        a.poly([(217,158),(220,151),(226,151),(230,160),(225,158)],"#ded6b9")
        d.line((219,169,212,173,205,170),fill="#d0a886",width=3)
    return a.im

def village_green(stage,mount,person):
    winter=stage!="Well"
    a=Plate(831);d=a.d;a.sky((73,43))
    for x in range(-3,365,7): a.pine(x,130,a.r.randrange(17,39),"#566c66")
    for x,y,w in ((-14,145,72),(61,134,61),(173,133,69),(260,145,103)):
        timber(a,x,y,w,int(w*.41),winter)
    a.ground(151)
    # The well rope, scoop and gatepost remain in place through all four cuts.
    d.rectangle((78,152,124,167),fill="#69797b");d.ellipse((76,148,126,156),fill="#bbc4b9")
    d.ellipse((81,149,120,154),fill=INK)
    for x in (79,121): d.rectangle((x,117,x+3,152),fill="#6b6053")
    d.line((76,117,127,117),fill="#504c44",width=4);d.line((103,119,103,151),fill="#b29770")
    if not winter:
        person(d,131,179,1.45,"#887357");return a.im
    mount(d,254,151,.84); mount(d,298,162,1.0)
    a.flag(290,133,83)
    if stage in ("Riders","Blade","Glint","Cut"):
        person(d,138,184,1.75,"#8d7354")
        d.line((142,163,152,158,155,151),fill="#c99b76",width=3)
        d.ellipse((152,145,158,151),fill="#886844")
    elif stage in ("Stagger","Kneel"):
        # Keep his feet on the same stones through the two breaking poses.
        falling=Image.new("RGBA",(92,76))
        person(ImageDraw.Draw(falling),36,59,1.75,"#8d7354")
        angle= -23 if stage=="Stagger" else -64
        falling=falling.rotate(angle,Image.Resampling.NEAREST,center=(36,59))
        a.im.paste(falling,(99 if stage=="Stagger" else 93,125),falling)
        sx,sy=(157,152) if stage=="Stagger" else (162,165)
        d.line((sx,sy,sx+4,sy-5),fill="#856544",width=2)
        d.ellipse((sx+2,sy-8,sx+8,sy-4),fill="#93734e")
    else:
        a.poly([(117,183),(124,177),(133,176),(146,179),(149,185),(123,186)],"#776855")
        d.ellipse((144,178,153,185),fill="#c59d7a")
        d.line((159,180,167,177),fill="#856544");d.ellipse((167,175,172,178),fill="#93734e")
    mount(d,214,184,1.52,stage in ("Blade","Glint"))
    if stage in ("Glint","Cut"):
        # The bright edge advances from the raised blade, then leaves a thin afterimage.
        if stage=="Glint":
            a.poly([(173,103),(177,107),(183,109),(177,109),(173,114),(174,109),(168,107),(173,107)],"#fff4c8")
        else:
            a.poly([(192,117),(181,110),(164,110),(148,115),(131,129),(117,151),(113,164),
                    (124,148),(136,135),(154,121),(171,116)],"#adcbd0")
            a.poly([(185,111),(165,112),(149,118),(129,137),(115,160),(123,142),(141,122),(155,115)],"#fff4cb")
            d.line((190,137,178,141,150,155),fill="#dae5d6",width=2)
    offsets={"Flinch":2,"Rush":15,"Scatter":43}
    for i,x in enumerate((2,33,62,92,273,305,335,362)):
        direction=-1 if i<4 else 1
        shift=offsets.get(stage,0)
        x+=direction*int(shift*(1.0+(i%3)*.16))
        y=216+(i%3)*9
        person(d,x,y,2.7,("#54615a","#78645b","#5e6970")[i%3])
        if shift:
            d.line((x,y-31,x+direction*9,y-37,x+direction*11,y-43),fill="#bc9277",width=3)
            if shift>=15:d.line((x,y-9,x-direction*8,y-5,x-direction*14,y-7),fill=INK,width=4)
    return a.im
def mill_plate(night=False):
    a=Plate(46);d=a.d;a.sky((74,44));a.ground(151)
    a.poly([(0,171),(84,151),(179,151),(270,169),(360,171),(360,238),(0,238)],"#749298")
    for i in range(210):
        x=a.r.randrange(W);y=a.r.randrange(172,H);d.line((x,y,x+a.r.randrange(2,9),y),fill="#9bb1ae")
    timber(a,78,158,168,72,night)
    d.ellipse((66,115,125,178),fill="#4c5350",outline="#aa987b",width=4)
    for i in range(12):
        t=i*math.pi/6
        d.line((95,147,95+28*math.cos(t),147+28*math.sin(t)),fill="#9a866c",width=2)
    d.ellipse((91,143,100,152),fill="#554b41")
    a.poly([(125,196),(336,161),(360,166),(360,180),(115,225)],"#73685a")
    for x in range(124,360,14):
        y=196-(x-125)*.16;d.line((x,y,x-3,y+14),fill="#a69a7e")
    if night:
        a.im=Image.blend(a.im,Image.new("RGB",a.im.size,"#172833"),.65);d=ImageDraw.Draw(a.im)
        d.line((184,156,205,153),fill=GOLD,width=2)
    return a.im

def south_night(ring):
    pic=ice()
    pic=Image.blend(pic,Image.new("RGB",pic.size,"#162b39"),.65)
    d=ImageDraw.Draw(pic);d.rectangle((251,128,255,133),fill=GOLD)
    if ring:
        for i in range(17):
            t=i/17;x=92+int(t*145);y=225-int(t*73)+(i%2)*3
            d.line((x,y,x,y-4),fill=INK,width=2)
            d.rectangle((x+1,y-3,x+2,y-2),fill=GOLD)
            d.line((x,y+2,x+2,y+2),fill="#b7aa80")
    return pic
