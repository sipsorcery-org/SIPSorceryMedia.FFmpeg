#if ANDROID
using global::Android.Hardware.Camera2;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorceryMedia.FFmpeg.Interop.Android
{
    internal class AndroidCamera
    {
        private static CameraManager _cameraManager;
        public static List<Camera> GetCameras()
        {
            CameraManager cameraManager = GetCameraManager();
            string[] cameraIdList = cameraManager.GetCameraIdList();
            List<Camera> cameras = new();
            foreach (string cameraId in cameraIdList)
            {
                Camera camera = new()
                {
                    Name = cameraId == "0" ? "Back Camera" : (cameraId == "1") ? "Front Camera" : "Camera" + cameraId,
                    Path = cameraId
                };
                cameras.Add(camera);
            }
            return cameras;
        }
        private static CameraManager GetCameraManager()
        {
            _cameraManager = (CameraManager)global::Android.App.Application.Context.GetSystemService(global::Android.Content.Context.CameraService)!;
            return _cameraManager;
        }
    }
}
#endif