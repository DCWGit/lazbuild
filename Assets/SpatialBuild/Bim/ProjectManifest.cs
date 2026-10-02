using System;

namespace SpatialBuild.Bim {
 [Serializable] public sealed class ProjectManifest {
  public string schemaVersion,projectId,projectName,revision,units,coordinateSystem,calibrationVersion;
  public double[] projectOrigin,bimToProject;
  public SourceReport[] sources;
  public SpatialElement[] elements;
 }
 [Serializable] public sealed class SourceReport {
  public string file,sha256,schema;
  public int elements,renderableElements,representedElements,allElements;
  public double sourceUnitToMetre,maxFloatMeshErrorM;
  public string[] missingGeometry;
  public double[] boundsMin,boundsMax;
 }
 [Serializable] public sealed class SpatialElement {
  public string id,sourceGuid,name,discipline,disciplineEvidence,category,type,sourceRevision;
  public bool geometryAvailable;
  public string geometryReference,metadata;
  public int byteOffset,vertexCount,indexCount;
  public double[] anchor,localTransform,projectTransform,boundsMin,boundsMax;
 }
}
