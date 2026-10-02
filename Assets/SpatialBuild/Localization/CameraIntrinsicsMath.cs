using System;
using SpatialBuild.Coordinates;
namespace SpatialBuild.Localization {
 public struct DetectorIntrinsics {public double fx,fy,cx,cy;public bool Valid=>fx>0&&fy>0&&DVec.Number(fx)&&DVec.Number(fy)&&DVec.Number(cx)&&DVec.Number(cy);}
 public static class CameraIntrinsicsMath {
  // MRUK sensor crop followed by resolution scaling, then bottom-up -> native
  // top-down coordinates. Pixel-center convention must be checked on device.
  public static DetectorIntrinsics Native(int sensorW,int sensorH,int imageW,int imageH,double sensorFx,double sensorFy,double sensorCx,double sensorCy){
   if(sensorW<=0||sensorH<=0||imageW<=0||imageH<=0)return default;
   double sx=(double)imageW/sensorW,sy=(double)imageH/sensorH;double factor=Math.Max(sx,sy);sx/=factor;sy/=factor;
   double cropX=sensorW*(1-sx)*.5,cropY=sensorH*(1-sy)*.5,cropW=sensorW*sx,cropH=sensorH*sy;
   return new DetectorIntrinsics {fx=sensorFx*imageW/cropW,fy=sensorFy*imageH/cropH,cx=(sensorCx-cropX)*imageW/cropW,cy=imageH-(sensorCy-cropY)*imageH/cropH};
  }
 }
}
