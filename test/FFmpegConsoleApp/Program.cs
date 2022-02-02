using System;
using System.Collections.Generic;
using SIPSorceryMedia.FFmpeg;

namespace FFmpegConsoleApp
{
    class Program
    {
        // private const string LIB_PATH = @"/usr/local/Cellar/ffmpeg/4.4.1_5/lib"; // On MacBookPro
        // private const string LIB_PATH = @"C:\ffmpeg-4.4.1-full_build-shared\bin"; // On Windows

        private const string LIB_PATH = @"..\..\..\..\..\lib\x64";

        static void Main(string[] args)
        {
            // Initialise FFmpeg librairies
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_DEBUG, LIB_PATH);

            List<Camera> cameras = FFmpegCameraManager.GetCameraDevices();

            List<Monitor> monitors = FFmpegMonitorManager.GetMonitorDevices();
        }
    }
}
