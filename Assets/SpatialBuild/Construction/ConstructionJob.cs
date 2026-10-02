using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
namespace SpatialBuild.Construction {
 [Serializable] public class WorkStep {public string targetId,title,material;public float quantity;public string unit;}
 [Serializable] public class JobDefinition {public string id,title;public WorkStep[] steps;}
 public sealed class ConstructionJob {
  public readonly JobDefinition definition;
  public readonly HashSet<string> done=new HashSet<string>();
  readonly List<string> history=new List<string>();
  public int index;
  public WorkStep Current=>index<definition.steps.Length?definition.steps[index]:null;
  public ConstructionJob(JobDefinition job){
   if(job==null||job.steps==null||job.steps.Length==0||string.IsNullOrEmpty(job.id))throw new ArgumentException("Job has no steps or ID");
   var ids=new HashSet<string>();foreach(var step in job.steps)if(step==null||string.IsNullOrEmpty(step.targetId)||!ids.Add(step.targetId)||step.quantity<=0)throw new ArgumentException("Invalid or duplicate job step");definition=job;
  }
  public bool MarkDone(){if(Current==null)return false;string id=Current.targetId;if(done.Add(id))history.Add(id);Next();return true;}
  public void Next(){for(int n=1;n<=definition.steps.Length;n++){int next=(index+n)%definition.steps.Length;if(!done.Contains(definition.steps[next].targetId)){index=next;return;}}index=definition.steps.Length;}
  public bool Undo(){if(history.Count==0)return false;string id=history[history.Count-1];history.RemoveAt(history.Count-1);done.Remove(id);index=Array.FindIndex(definition.steps,s=>s.targetId==id);return true;}
  public bool Contains(string id){return Array.Exists(definition.steps,s=>s.targetId==id);}
  public string Materials(){
   var amounts=new Dictionary<string,float>();foreach(var s in definition.steps)if(!done.Contains(s.targetId)){string key=s.material+" ("+s.unit+")";if(!amounts.ContainsKey(key))amounts[key]=0;amounts[key]+=s.quantity;}
   var lines=new List<string>();foreach(var pair in amounts)lines.Add(pair.Value.ToString("0.##")+" "+pair.Key);return lines.Count==0?"Nothing remaining (self-reported)":string.Join("\n",lines);
  }
  [Serializable] class Progress {public string jobId;public string source="user_reported_not_verified";public string[] completed;}
  string PathName=>Path.Combine(Application.persistentDataPath,"job-"+definition.id+".json");
  public void Save(){File.WriteAllText(PathName,JsonUtility.ToJson(new Progress{jobId=definition.id,completed=history.ToArray()},true));}
  public void Load(){if(!File.Exists(PathName))return;var data=JsonUtility.FromJson<Progress>(File.ReadAllText(PathName));if(data==null||data.jobId!=definition.id||data.completed==null)return;foreach(string id in data.completed)if(Contains(id)&&done.Add(id))history.Add(id);index=definition.steps.Length-1;Next();}
 }
}
