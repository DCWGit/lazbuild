"""Download pinned official tagStandard41h12 images; make a letter-size vector sheet."""
import base64,pathlib,struct,urllib.request,zlib

REV='f3fd9a7add5bfd82a886fc65240fdb8e3c9ac5a1'
ROOT=pathlib.Path(__file__).resolve().parent
BASE=f'https://raw.githubusercontent.com/AprilRobotics/apriltag-imgs/{REV}/tagStandard41h12'

def pixels(png):
 if png[:8]!=b'\x89PNG\r\n\x1a\n':raise ValueError('PNG signature')
 at=8;data=b'';width=height=None
 while at<len(png):
  n=struct.unpack('>I',png[at:at+4])[0];kind=png[at+4:at+8];body=png[at+8:at+8+n];at+=12+n
  if kind==b'IHDR':
   width,height,depth,color,*_=struct.unpack('>IIBBBBB',body)
   if depth!=8 or color!=6:raise ValueError('Expected RGBA PNG')
  if kind==b'IDAT':data+=body
 bpp=4;stride=width*bpp;raw=zlib.decompress(data);rows=[];previous=bytearray(stride);at=0
 for _ in range(height):
  kind=raw[at];at+=1;row=bytearray(raw[at:at+stride]);at+=stride
  for j in range(stride):
   a=row[j-bpp] if j>=bpp else 0;b=previous[j];c=previous[j-bpp] if j>=bpp else 0
   if kind==1:row[j]=(row[j]+a)&255
   elif kind==2:row[j]=(row[j]+b)&255
   elif kind==3:row[j]=(row[j]+((a+b)//2))&255
   elif kind==4:
    p=a+b-c;ds=[abs(p-a),abs(p-b),abs(p-c)];row[j]=(row[j]+(a,b,c)[ds.index(min(ds))])&255
   elif kind!=0:raise ValueError('Unknown PNG filter')
  rows.append([tuple(row[j:j+4]) for j in range(0,stride,4)]);previous=row
 return rows

def generate():
 marks=[]
 for tag in range(3):
  name=f'tag41_12_{tag:05}.png';path=ROOT/name
  if not path.exists():
   path.write_bytes(urllib.request.urlopen(urllib.request.Request(f'{BASE}/{name}',headers={'User-Agent':'SpatialBuild'})).read())
  marks.append(pixels(path.read_bytes()))
 lines=['<svg xmlns="http://www.w3.org/2000/svg" width="215.9mm" height="279.4mm" viewBox="0 0 215.9 279.4">','<rect width="215.9" height="279.4" fill="white"/>']
 for tag,grid in enumerate(marks):
  x=67.95;y=6+tag*89;unit=80/9
  for row in range(9):
   for col in range(9):
    r,g,b,a=grid[row][col]
    if a>128 and r+g+b<384:lines.append(f'<rect x="{x+col*unit:.5f}" y="{y+row*unit:.5f}" width="{unit:.5f}" height="{unit:.5f}" fill="black"/>')
  lines.append(f'<text x="107.95" y="{y+85:.2f}" font-family="Arial" font-size="4" text-anchor="middle">CONTROL {tag+1} / tagStandard41h12 ID {tag}</text>')
 lines+=['<path d="M8 275 H108 M8 273 V277 M108 273 V277" stroke="black" stroke-width=".3"/>','<text x="112" y="276" font-family="Arial" font-size="3.5">100 mm print scale check</text>','</svg>']
 (ROOT/'SpatialBuild-3-Control-Targets.svg').write_text('\n'.join(lines),encoding='utf8')
 print('Wrote letter-size vector targets. Print at 100%, measure the black border and 100 mm check.')

if __name__=='__main__':generate()
