﻿using System.Collections.Generic;
using SIPSorceryMedia.FFmpeg;

namespace FFmpegConsoleApp
{
    class Program
    {
        // private const string LIB_PATH = @"/usr/local/Cellar/ffmpeg/4.4.1_5/lib"; // On MacBookPro
        private const string LIB_PATH = @"C:\ffmpeg-4.4.1-full_build-shared\bin"; // On Windows

        //private const string LIB_PATH = @"..\..\..\..\..\lib\x64";

        static void Main(string[] args)
        {
            // Initialise FFmpeg librairies
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_TRACE, LIB_PATH);

            List<Camera> cameras;
            List<Monitor> monitors;

            FFmpegInit.useSpecificLogCallback();
            cameras = FFmpegCameraManager.GetCameraDevices();

            FFmpegInit.useDefaultLogCallback();
            cameras = FFmpegCameraManager.GetCameraDevices();



            monitors = FFmpegMonitorManager.GetMonitorDevices();
        }
    }
}