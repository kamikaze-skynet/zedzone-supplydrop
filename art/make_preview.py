# Renders the in-code helicopter sprite (same pixel rules as src/Art.cs) into workshop/preview.png.
import math
from PIL import Image, ImageDraw, ImageFont
W,H=60,26
def body():
    px=[[(0,0,0,0)]*W for _ in range(H)]
    O=(30,34,22,255);OL=(84,94,58,255);OD=(62,70,42,255);OLL=(112,124,80,255)
    G=(120,170,190,255);GS=(200,230,240,255);SK=(48,50,50,255);ST=(220,180,40,255);L=(230,40,30,255)
    cy=12.5
    def s(x,y,c):
        if 0<=x<W and 0<=y<H: px[y][x]=c
    for x in range(28,56): s(x,2,SK); s(x,23,SK)
    for k in range(2):
        x=33+k*14
        for y in range(2,24): s(x,y,SK)
    s(56,3,SK); s(56,22,SK)
    for x in range(2,33):
        half=1.5+(x-2)*0.05
        for y in range(H):
            d=abs(y-cy)
            if d<=half+1: s(x,y,O if d>half else (OLL if y>cy else OD))
    for x in range(7,12):
        for y in range(5,21): s(x,y,O if (y in(5,20) or x in(7,11)) else OL)
    for x in range(1,7): s(x,12,O); s(x,13,O)
    ex,rx,ry=41,16.5,9.5
    for x in range(W):
        for y in range(H):
            nx=(x+.5-ex)/rx; ny=(y+.5-cy-.5)/ry; r=nx*nx+ny*ny
            if r>1: continue
            s(x,y,O if r>.82 else (OLL if ny>.35 else (OD if ny<-.45 else OL)))
    for x in range(33,42):
        for y in range(9,17): s(x,y,O if (x==33 or y in(9,16)) else OD)
    s(32,9,SK); s(32,16,SK)
    for y in range(H):
        for x in (44,45):
            if px[y][x][3]>0 and px[y][x]!=O: s(x,y,ST)
    gx,grx,gry=52,5.5,7
    for x in range(46,W):
        for y in range(H):
            nx=(x+.5-gx)/grx; ny=(y+.5-cy-.5)/gry; r=nx*nx+ny*ny
            if r>1 or px[y][x][3]==0 or px[y][x]==O: continue
            s(x,y,GS if (ny>.2 and nx<.3) else G)
    s(2,12,L)
    return px
def rotor(n,blades):
    px=[[(0,0,0,0)]*n for _ in range(n)]
    c=(n-1)/2; R=n/2-.5
    nb=2 if blades<=1 else blades*2; div=2 if blades<=1 else blades
    for y in range(n):
        for x in range(n):
            dx,dy=x-c,y-c; d=math.hypot(dx,dy); col=(0,0,0,0)
            if d<=R:
                col=(20,20,20,34)
                for b in range(nb):
                    a=math.pi*b/div; ax,ay=math.cos(a),math.sin(a)
                    along=dx*ax+dy*ay; across=abs(-dx*ay+dy*ax)
                    if along>0 and across<=(1.3 if n>20 else .8): col=(26,26,26,210)
            if d<=(3 if n>20 else 1.2): col=(60,60,60,255)
            px[y][x]=col
    return px
def img(px):
    h=len(px); w=len(px[0]); im=Image.new('RGBA',(w,h))
    for y in range(h):
        for x in range(w): im.putpixel((x,h-1-y),px[y][x])  # texture rows go bottom-up
    return im
S=6
b=img(body()).resize((W*S,H*S),Image.NEAREST)
r=img(rotor(72,3)).resize((72*S,72*S),Image.NEAREST)
t=img(rotor(14,1)).resize((14*S,14*S),Image.NEAREST)
sh=Image.new('RGBA',b.size,(0,0,0,0)); a=b.split()[3].point(lambda v:72 if v else 0); sh.putalpha(a)
bg=Image.new('RGBA',(640,360),(74,96,52,255)); d=ImageDraw.Draw(bg)
import random; random.seed(3)
for _ in range(900):
    x,y=random.randrange(640),random.randrange(360); c=random.choice([(66,88,46),(82,104,58),(90,84,58)]); d.rectangle([x,y,x+3,y+3],fill=c)
d.rectangle([290,250,318,272],fill=(150,110,60),outline=(60,40,20),width=3)  # crate
d.line([292,261,316,261],fill=(60,40,20),width=2)
d.ellipse([296,226,312,242],outline=(230,40,30),width=3)
bg.alpha_composite(sh,(150+40,150))
cx,cy=150,70
bg.alpha_composite(b,(cx,cy))
pivx=cx+int(37*S); pivy=cy+H*S//2
bg.alpha_composite(r,(pivx-r.width//2,pivy-r.height//2))
bg.alpha_composite(t,(cx+3*S-t.width//2,pivy-t.height//2-12))
try: f=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',40); f2=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',20)
except: f=f2=ImageFont.load_default()
d.text((22,282),'SUPPLY DROP',font=f,fill=(255,210,60),stroke_width=3,stroke_fill=(20,20,10))
d.text((24,330),'Grab the crate before the horde does',font=f2,fill=(240,240,230),stroke_width=2,stroke_fill=(20,20,10))
bg.convert('RGB').save('/home/user/zedzone-supplydrop/workshop/preview.png')
print('ok')
