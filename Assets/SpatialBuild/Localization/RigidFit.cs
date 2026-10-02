using System;
using System.Collections.Generic;
using SpatialBuild.Coordinates;

namespace SpatialBuild.Localization {
 // Weighted Horn point registration. Marker orientation is not required:
 // the surveyed datum is the physical center of the printed tag.
 public static class RigidFit {
  public static bool TrySolve(IReadOnlyList<DVec> project,IReadOnlyList<DVec> quest,IReadOnlyList<double> weights,DVec projectOrigin,out RigidD questFromProjectLocal){
   questFromProjectLocal=RigidD.Identity;
   if(project.Count<3||quest.Count!=project.Count||weights.Count!=project.Count)return false;
   double sum=0;DVec a=new DVec(),b=new DVec();
   for(int i=0;i<project.Count;i++){
    if(!project[i].Finite||!quest[i].Finite||!DVec.Number(weights[i])||weights[i]<=0)return false;
    var p=project[i]-projectOrigin;sum+=weights[i];a+=p*weights[i];b+=quest[i]*weights[i];
   }
   if(!DVec.Number(sum)||sum<=0)return false;a=a*(1/sum);b=b*(1/sum);
   var s=new double[3,3];
   for(int i=0;i<project.Count;i++){
    var p=project[i]-projectOrigin-a;var q=quest[i]-b;double w=weights[i]/sum;
    s[0,0]+=w*p.x*q.x;s[0,1]+=w*p.x*q.y;s[0,2]+=w*p.x*q.z;
    s[1,0]+=w*p.y*q.x;s[1,1]+=w*p.y*q.y;s[1,2]+=w*p.y*q.z;
    s[2,0]+=w*p.z*q.x;s[2,1]+=w*p.z*q.y;s[2,2]+=w*p.z*q.z;
   }
   double xx=s[0,0],xy=s[0,1],xz=s[0,2],yx=s[1,0],yy=s[1,1],yz=s[1,2],zx=s[2,0],zy=s[2,1],zz=s[2,2];
   var n=new double[4,4];
   n[0,0]=xx+yy+zz;n[0,1]=yz-zy;n[0,2]=zx-xz;n[0,3]=xy-yx;
   n[1,0]=n[0,1];n[1,1]=xx-yy-zz;n[1,2]=xy+yx;n[1,3]=zx+xz;
   n[2,0]=n[0,2];n[2,1]=n[1,2];n[2,2]=-xx+yy-zz;n[2,3]=yz+zy;
   n[3,0]=n[0,3];n[3,1]=n[1,3];n[3,2]=n[2,3];n[3,3]=-xx-yy+zz;
   var v=new double[4,4];for(int i=0;i<4;i++)v[i,i]=1;
   for(int sweep=0;sweep<80;sweep++){
    int p=0,q=1;double largest=0;
    for(int i=0;i<4;i++)for(int j=i+1;j<4;j++)if(Math.Abs(n[i,j])>largest){largest=Math.Abs(n[i,j]);p=i;q=j;}
    if(largest<1e-14)break;
    double angle=.5*Math.Atan2(2*n[p,q],n[q,q]-n[p,p]);double c=Math.Cos(angle),t=Math.Sin(angle);
    // Jacobi rotation J with p,p=c; p,q=t; q,p=-t; q,q=c.
    for(int k=0;k<4;k++)if(k!=p&&k!=q){double np=n[k,p],nq=n[k,q];n[k,p]=n[p,k]=c*np-t*nq;n[k,q]=n[q,k]=t*np+c*nq;}
    double pp=n[p,p],pq=n[p,q],qq=n[q,q];n[p,p]=c*c*pp-2*c*t*pq+t*t*qq;n[q,q]=t*t*pp+2*c*t*pq+c*c*qq;n[p,q]=n[q,p]=0;
    for(int k=0;k<4;k++){double vp=v[k,p],vq=v[k,q];v[k,p]=c*vp-t*vq;v[k,q]=t*vp+c*vq;}
   }
   int best=0;for(int i=1;i<4;i++)if(n[i,i]>n[best,best])best=i;
   var rot=new DQuat(v[1,best],v[2,best],v[3,best],v[0,best]);
   questFromProjectLocal=new RigidD(b-rot.Rotate(a),rot);return true;
  }
 }
}
