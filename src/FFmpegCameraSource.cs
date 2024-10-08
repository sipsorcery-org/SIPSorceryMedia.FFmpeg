using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorceryMedia.FFmpeg
{
    public class FFmpegCameraSource : FFmpegVideoSource
    {
        private static ILogger logger = SIPSorcery.LogFactory.CreateLogger<FFmpegCameraSource>();

        public unsafe FFmpegCameraSource(string path)
        {
            string inputFormat = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dshow"
                                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "v4l2"
                                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "avfoundation"
#if NET5_0_OR_GREATER
                                    : OperatingSystem.IsAndroid() ? "android_camera"
                                    : OperatingSystem.IsIOS() ? "avfoundation"
#endif
                                    : throw new NotSupportedException($"Cannot find adequate input format - OSArchitecture:[{RuntimeInformation.OSArchitecture}] - OSDescription:[{RuntimeInformation.OSDescription}]");

            AVInputFormat* aVInputFormat = ffmpeg.av_find_input_format(inputFormat);
            var decoderOptions = new Dictionary<string, string>();
#if ANDROID
            decoderOptions["camera_index"] = path;
#endif
            CreateVideoDecoder(path, aVInputFormat, false, true);
            InitialiseDecoder(decoderOptions);
        }
    }
}
