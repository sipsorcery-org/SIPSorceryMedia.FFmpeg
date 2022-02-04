using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIPSorceryMedia.FFmpeg
{
    public delegate void RawExtVideoSampleDelegate(uint durationMilliseconds, FFmpegImageRawSample imageRawSample);

    public delegate void VideoExtSinkSampleDecodedDelegate(FFmpegImageRawSample imageRawSample);

    public class FFmpegImageRawSample
    {
        public int Width { get; set; }
        public int Height { get; set; } 
        public int Stride { get; set; }

        public IntPtr Sample { get; set; }
        public VideoPixelFormatsEnum PixelFormat { get; set; }
    }

    public interface IFFmpegVideoSource: IVideoSource
    {
        event RawExtVideoSampleDelegate OnVideoSourceRawExtSample;
    }

    public interface IFFmpegVideoSink : IVideoSink
    {
        event VideoExtSinkSampleDecodedDelegate OnVideoExtSinkDecodedSample;
        
    }
}
