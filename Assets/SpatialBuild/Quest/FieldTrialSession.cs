using System;
using System.IO;
using UnityEngine;

namespace SpatialBuild.Quest {
    // User observations, not measurement-solver evidence or installation approval.
    public sealed class FieldTrialSession {
        public int taskIndex,checkIndex;
        public float reportedOffsetMm=-1;
        public bool checkMode;
        public readonly bool[] reviewed=new bool[5];
        readonly string session=Guid.NewGuid().ToString("N");
        public string LastMessage {get;private set;}="No physical checks recorded";
        public static Vector3 CheckPoint(int index) {
            switch(index%4){case 1:return new Vector3(2,0,0);case 2:return new Vector3(0,0,2);case 3:return new Vector3(2,0,2);default:return Vector3.zero;}
        }
        public bool Record(float scale,Transform world) {
            if(!checkMode){LastMessage="Show a reference point first";return false;}
            if(Mathf.Abs(scale-1f)>.001f){LastMessage="Switch to full scale before recording";return false;}
            if(float.IsNaN(reportedOffsetMm)||float.IsInfinity(reportedOffsetMm)||reportedOffsetMm<0){LastMessage="Enter your measured offset first";return false;}
            bool saved=Write("user_reported_offset",world,reportedOffsetMm);
            if(saved)LastMessage="Saved P"+checkIndex+": "+reportedOffsetMm.ToString("F0")+" mm (user report)";
            return saved;
        }
        public void Review(Transform world){if(Write("hanger_reviewed_not_verified",world,0))reviewed[taskIndex]=true;}
        public void Event(string action,Transform world){Write(action,world,0);}
        bool Write(string action,Transform world,float offset) {
            try {
                var entry=new Entry{utc=DateTime.UtcNow.ToString("O"),session=session,action=action,taskId="H"+(184+taskIndex),
                    pointId="P"+checkIndex,projectPointUnity=CheckPoint(checkIndex),previewPosition=world.position,
                    previewRotation=world.rotation,previewScale=world.localScale.x,reportedOffsetMm=offset,
                    status="UNREGISTERED",source="manual_user_observation"};
                File.AppendAllText(Path.Combine(Application.persistentDataPath,"field-trial-"+session+".jsonl"),JsonUtility.ToJson(entry)+"\n");return true;
            }catch(Exception e){LastMessage="Could not save trial log";Debug.LogWarning("Field trial log: "+e.Message);return false;}
        }
        [Serializable] class Entry {
            public string utc,session,action,taskId,pointId,status,source;
            public Vector3 projectPointUnity,previewPosition;
            public Quaternion previewRotation;
            public float previewScale,reportedOffsetMm;
        }
    }
}
