﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;

namespace SIPSorceryMedia.FFmpeg
{
    public class FFmpegMicrophoneSource : FFmpegAudioSource
    {
        private static ILogger logger = SIPSorcery.LogFactory.CreateLogger<FFmpegMicrophoneSource>();

        public unsafe FFmpegMicrophoneSource(string path, IAudioEncoder audioEncoder, uint frameSize = 960) : base(audioEncoder, frameSize)
        {
            string inputFormat = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dshow"
                : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "alsa"
                : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "avfoundation"
#if NET5_0_OR_GREATER
                : OperatingSystem.IsAndroid() ? "android_camera"
                : OperatingSystem.IsIOS() ? "avfoundation"
#endif
                : throw new NotSupportedException($"Cannot find adequate input format - OSArchitecture:[{RuntimeInformation.OSArchitecture}] - OSDescription:[{RuntimeInformation.OSDescription}]");
            
            AVInputFormat* aVInputFormat = ffmpeg.av_find_input_format(inputFormat);

            CreateAudioDecoder(path, aVInputFormat, false, true);
            
            
        }
    }
}
