﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FFmpeg.AutoGen;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;

namespace SIPSorceryMedia.FFmpeg
{
    public class FFmpegVideoEndPoint : IFFmpegVideoSink, IDisposable
    {
        public ILogger logger = SIPSorcery.LogFactory.CreateLogger<FFmpegVideoEndPoint>();

        public static readonly List<VideoFormat> SupportedFormats = Helper.GetSupportedVideoFormats();

        private FFmpegVideoEncoder _ffmpegEncoder;

        private MediaFormatManager<VideoFormat> _videoFormatManager;
        private bool _isStarted;
        private bool _isPaused;
        private bool _isClosed;
        private bool _forceKeyFrame;

        public event VideoExtSinkSampleDecodedDelegate ? OnVideoExtSinkDecodedSample;

#pragma warning disable CS0067
        public event VideoSinkSampleDecodedDelegate ? OnVideoSinkDecodedSample;
        //public event EncodedSampleDelegate? OnVideoSourceEncodedSample;
        //public event RawExtVideoSampleDelegate? OnVideoSourceRawExtSample;
        //public event RawVideoSampleDelegate? OnVideoSourceRawSample;
        //public event SourceErrorDelegate? OnVideoSourceError;
#pragma warning restore CS0067

        public FFmpegVideoEndPoint()
        {
            _videoFormatManager = new MediaFormatManager<VideoFormat>(SupportedFormats);
            _ffmpegEncoder = new FFmpegVideoEncoder();
        }

        public MediaEndPoints ToMediaEndPoints()
        {
            return new MediaEndPoints
            {
                //VideoSource = this,
                VideoSink = this
            };
        }

        public List<VideoFormat> GetVideoSinkFormats() => _videoFormatManager.GetSourceFormats();
        public void SetVideoSinkFormat(VideoFormat videoFormat) => _videoFormatManager.SetSelectedFormat(videoFormat);
        public void RestrictFormats(Func<VideoFormat, bool> filter) => _videoFormatManager.RestrictFormats(filter);
        public List<VideoFormat> GetVideoSourceFormats() => _videoFormatManager.GetSourceFormats();
        public void SetVideoSourceFormat(VideoFormat videoFormat) => _videoFormatManager.SetSelectedFormat(videoFormat);
        public void ForceKeyFrame() => _forceKeyFrame = true;
        public bool HasEncodedVideoSubscribers() => OnVideoSinkDecodedSample != null;
        public bool IsVideoSourcePaused() => _isPaused;
        public void GotVideoRtp(IPEndPoint remoteEndPoint, uint ssrc, uint seqnum, uint timestamp, int payloadID, bool marker, byte[] payload) =>
            throw new ApplicationException("The FFmpeg Video End Point requires full video frames rather than individual RTP packets.");

        public void GotVideoFrame(IPEndPoint remoteEndPoint, uint timestamp, byte[] payload, VideoFormat format)
        {
            if (!_isClosed)
            {
                AVCodecID codecID = FFmpegConvert.GetAVCodecID(_videoFormatManager.SelectedFormat.Codec);

                var imageRawSamples = _ffmpegEncoder.Decode(codecID, payload, out var width, out var height);

                if (imageRawSamples == null || width == 0 || height == 0)
                {
                    logger.LogWarning($"Decode of video sample failed, width {width}, height {height}.");
                }
                else
                {
                    foreach (var imageRawSample in imageRawSamples)
                    {
                        OnVideoExtSinkDecodedSample?.Invoke(imageRawSample);
                        //OnVideoSinkDecodedSample?.Invoke(rgbFrame, (uint)width, (uint)height, (int)(width * 3), VideoPixelFormatsEnum.Rgb);
                    }
                }
            }
        }

        public Task PauseVideo()
        {
            _isPaused = true;
            return Task.CompletedTask;
        }

        public Task ResumeVideo()
        {
            _isPaused = false;
            return Task.CompletedTask;
        }

        public Task StartVideo()
        {
            if (!_isStarted)
            {
                _isStarted = true;
            }

            return Task.CompletedTask;
        }

        public Task CloseVideo()
        {
            if (!_isClosed)
            {
                _isClosed = true;
                _ffmpegEncoder?.Dispose();
            }

            return Task.CompletedTask;
        }

        public void ExternalVideoSourceRawSample(uint durationMilliseconds, int width, int height, byte[] sample, VideoPixelFormatsEnum pixelFormat)
        {
            //if (!_isClosed)
            //{
            //    if (OnVideoSourceEncodedSample != null)
            //    {
            //        uint fps = (durationMilliseconds > 0) ? 1000 / durationMilliseconds : Helper.DEFAULT_VIDEO_FRAME_RATE;
            //        if(fps == 0)
            //        {
            //            fps = 1;
            //        }

            //        int stride = (pixelFormat == VideoPixelFormatsEnum.Bgra) ? 4 * width : 3 * width;
            //        var i420Buffer = PixelConverter.ToI420(width, height, stride, sample, pixelFormat);
            //        byte[]? encodedBuffer = _ffmpegEncoder. Encode(FFmpegConvert.GetAVCodecID(_videoFormatManager.SelectedFormat.Codec), i420Buffer, width, height, (int)fps, _forceKeyFrame);

            //        if (encodedBuffer != null)
            //        {
            //            //Console.WriteLine($"encoded buffer: {encodedBuffer.HexStr()}");
            //            uint durationRtpTS = Helper.VIDEO_SAMPLING_RATE / fps;

            //            // Note the event handler can be removed while the encoding is in progress.
            //            OnVideoSourceEncodedSample?.Invoke(durationRtpTS, encodedBuffer);
            //        }

            //        if (_forceKeyFrame)
            //        {
            //            _forceKeyFrame = false;
            //        }
            //    }
            //}
        }

        public void Dispose()
        {
            _ffmpegEncoder?.Dispose();
        }

        public Task PauseVideoSink()
        {
            return Task.CompletedTask;
        }

        public Task ResumeVideoSink()
        {
            return Task.CompletedTask;
        }

        public Task StartVideoSink()
        {
            return Task.CompletedTask;
        }

        public Task CloseVideoSink()
        {
            return Task.CompletedTask;
        }
    }
}
