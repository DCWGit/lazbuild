using System;
namespace SpatialBuild.Localization {
 [Serializable] public sealed class ControlProfile {
  public string schemaVersion,projectId,projectRevision,calibrationVersion,surveyBasis,notes;
  public ControlReference[] controls;
 }
}
