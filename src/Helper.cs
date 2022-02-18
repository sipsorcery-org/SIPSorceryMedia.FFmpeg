using FFmpeg.AutoGen;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIPSorceryMedia.FFmpeg
{
    public class Helper
    {
        public const int MIN_SLEEP_MILLISECONDS = 15;
        public const int DEFAULT_VIDEO_FRAME_RATE = 30;

        public static List<VideoFormat> GetSupportedVideoFormats() => new List<VideoFormat>
        {
            new VideoFormat(VideoCodecsEnum.VP8,    96,     VideoFormat.DEFAULT_CLOCK_RATE),
            new VideoFormat(VideoCodecsEnum.H264,   100,    VideoFormat.DEFAULT_CLOCK_RATE)
        };

        public static List<AudioFormat> GetSupportedAudioFormats() => new List<AudioFormat>
        {
            new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMU),
            new AudioFormat(SDPWellKnownMediaFormatsEnum.PCMA),
            new AudioFormat(SDPWellKnownMediaFormatsEnum.G722)
        };

    }
}
