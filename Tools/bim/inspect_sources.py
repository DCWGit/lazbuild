import pathlib,ifcopenshell,ifcopenshell.util.unit,collections,json
root=pathlib.Path('SourceData/dental-clinic')
for f in ['arc.ifc','str.ifc','mep.ifc']:
 m=ifcopenshell.open(str(root/f));print(f,m.schema,ifcopenshell.util.unit.calculate_unit_scale(m),len(m.by_type('IfcElement')))
 print(collections.Counter(e.is_a() for e in m.by_type('IfcElement')).most_common(15))
 print('sites',[(s.Name,s.RefLatitude,s.RefLongitude,s.RefElevation) for s in m.by_type('IfcSite')])
