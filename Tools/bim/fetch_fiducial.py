import urllib.request,json,pathlib,concurrent.futures
root=pathlib.Path(__file__).resolve().parents[2]
req=lambda url:json.load(urllib.request.urlopen(urllib.request.Request(url,headers={'User-Agent':'SpatialBuild'})))
rev=req('https://api.github.com/repos/keijiro/jp.keijiro.apriltag/commits/main')['sha']
tree=req(f'https://api.github.com/repos/keijiro/jp.keijiro.apriltag/git/trees/{rev}?recursive=1')['tree']
prefix='Packages/jp.keijiro.apriltag/'
def fetch(e):
 p=root/e['path'];p.parent.mkdir(parents=True,exist_ok=True);urllib.request.urlretrieve(f"https://raw.githubusercontent.com/keijiro/jp.keijiro.apriltag/{rev}/{e['path']}",p)
with concurrent.futures.ThreadPoolExecutor(max_workers=10) as pool:list(pool.map(fetch,[e for e in tree if e['type']=='blob' and e['path'].startswith(prefix)]))
(root/'docs/FIDUCIAL_VENDOR.md').write_text(f'Vendored jp.keijiro.apriltag from https://github.com/keijiro/jp.keijiro.apriltag commit {rev}. BSD license retained. Embedded package includes Android arm64 and desktop native libraries. Local change: measured fx/fy/cx/cy overload replaces centered-FOV assumption for SpatialBuild. Native detector uses tagStandard41h12.\n')
print(rev)
