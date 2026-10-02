import concurrent.futures,hashlib,json,pathlib,urllib.request
root=pathlib.Path(__file__).resolve().parents[2]
revision='7c102ef652a0820f673b52cb56188efd9da7ddba'
def fetch(name):
 url=f'https://huggingface.co/datasets/sylvainHellin/ifc-bench/resolve/{revision}/projects/dental_clinic/{name}'
 path=root/'SourceData/dental-clinic'/name
 if not path.exists(): urllib.request.urlretrieve(url,path)
 return dict(file=name,url=url,bytes=path.stat().st_size,sha256=hashlib.sha256(path.read_bytes()).hexdigest())
if __name__=='__main__':
 with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool: sources=list(pool.map(fetch,['arc.ifc','str.ifc','mep.ifc','license.txt','model_card.md']))
 (root/'Tools/bim/sources.json').write_text(json.dumps(dict(datasetRevision=revision,sources=sources),indent=2))
 print(json.dumps(sources,indent=2))
