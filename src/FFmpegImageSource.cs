using FFmpeg.AutoGen;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SIPSorceryMedia.FFmpeg
{
    public class FFmpegImageSource : IFFmpegVideoSource, IDisposable
    {
        private static ILogger logger = SIPSorcery.LogFactory.CreateLogger<FFmpegImageSource>();

        internal static List<VideoFormat> _supportedVideoFormats = Helper.GetSupportedVideoFormats();

        internal bool _isStarted;
        internal bool _isPaused;
        internal bool _isClosed;

        internal FFmpegVideoDecoder? _videoDecoder;

        internal VideoFrameConverter? _videoFrameYUV420PConverter = null;
        internal VideoFrameConverter? _videoFrameBGR24Converter = null;

        internal FFmpegVideoEncoder _videoEncoder;
        internal bool _forceKeyFrame;

        internal MediaFormatManager<VideoFormat> _videoFormatManager;

        public event EncodedSampleDelegate? OnVideoSourceEncodedSample;
        public event RawExtVideoSampleDelegate? OnVideoSourceRawExtSample;

#pragma warning disable CS0067
        public event RawVideoSampleDelegate? OnVideoSourceRawSample;
        public event SourceErrorDelegate? OnVideoSourceError;
#pragma warning restore CS0067
        public event Action? OnEndOfFile;

        public FFmpegImageSource()
        {
            _videoFormatManager = new MediaFormatManager<VideoFormat>(_supportedVideoFormats);
            _videoEncoder = new FFmpegVideoEncoder();
        }

        public unsafe void CreateVideoDecoder(String path, AVInputFormat* avInputFormat, bool repeat = false, bool isCamera = false)
        {
            _videoDecoder = new FFmpegVideoDecoder(path, avInputFormat, repeat, isCamera);
            _videoDecoder.OnVideoFrame += VideoDecoder_OnVideoFrame;

            _videoDecoder.OnEndOfFile += () =>
            {
                logger.LogDebug($"File source decode complete for {path}.");
                OnEndOfFile?.Invoke();
                _videoDecoder.Dispose();
            };
        }

        public void InitialiseDecoder(Dictionary<string, string>? decoderOptions = null)
        {
            _videoDecoder?.InitialiseSource(decoderOptions);
        }

        public bool IsPaused() => _isPaused;

        public List<VideoFormat> GetVideoSourceFormats()
        {
            return _videoFormatManager.GetSourceFormats();
        }

        public void SetVideoSourceFormat(VideoFormat videoFormat)
        {
            logger.LogDebug($"Setting video source format to {videoFormat.FormatID}:{videoFormat.Codec} {videoFormat.ClockRate}.");
            _videoFormatManager.SetSelectedFormat(videoFormat);
        }
        public void RestrictFormats(Func<VideoFormat, bool> filter)
        {
            _videoFormatManager.RestrictFormats(filter);
        }

        public void ForceKeyFrame() => _forceKeyFrame = true;
        public void ExternalVideoSourceRawSample(uint durationMilliseconds, int width, int height, byte[] sample, VideoPixelFormatsEnum pixelFormat) => throw new NotImplementedException();
        public bool HasEncodedVideoSubscribers() => OnVideoSourceEncodedSample != null;
        public bool IsVideoSourcePaused() => _isPaused;
        public Task StartVideo() => Start();
        public Task PauseVideo() => Pause();
        public Task ResumeVideo() => Resume();
        public Task CloseVideo() => Close();

        private unsafe void VideoDecoder_OnVideoFrame(ref AVFrame frame)
        {
            if ((OnVideoSourceEncodedSample != null) || (OnVideoSourceRawExtSample != null))
            {
                int frameRate = (int)_videoDecoder.VideoAverageFrameRate;
                frameRate = (frameRate <= 0) ? Helper.DEFAULT_VIDEO_FRAME_RATE : frameRate;
                uint timestampDuration = (uint)(Helper.VIDEO_SAMPLING_RATE / frameRate);

                var width = frame.width;
                var height = frame.height;

                var scaleFactor = 1;
                var targetWidth = (int)Math.Ceiling((double)width / scaleFactor);
                var targetHeight = (int)Math.Ceiling((double)height / scaleFactor);
                var targetFps = frameRate;

                // Manage Raw Sample
                //if (OnVideoSourceRawExtSample != null)
                //{
                    if (_videoFrameBGR24Converter == null ||
                        _videoFrameBGR24Converter.SourceWidth != width ||
                        _videoFrameBGR24Converter.SourceHeight != height)
                    {
                        _videoFrameBGR24Converter = new VideoFrameConverter(
                            width, height,
                            (AVPixelFormat)frame.format,
                            targetWidth, targetHeight,
                            AVPixelFormat.AV_PIX_FMT_BGR24);
                        logger.LogDebug($"Frame format: [{frame.format}]");
                    }

                    var frameBGR24 = _videoFrameBGR24Converter.Convert(ref frame);

                    //byte[] sampleBGR24 = _videoFrameBGR24Converter.ConvertFrame(ref frame);
                    //if (frameBGR24 != null)
                    {
                        FFmpegImageRawSample imageRawSample = new FFmpegImageRawSample
                        {
                            Width = targetWidth,
                            Height = targetHeight,
                            Stride = frameBGR24.linesize[0],
                            Sample = (IntPtr)frameBGR24.data[0],
                            PixelFormat = VideoPixelFormatsEnum.Rgb
                        };
                        OnVideoSourceRawExtSample?.Invoke(timestampDuration, imageRawSample);
                    }

                    //byte[] sampleBGR24 = _videoFrameBGR24Converter.ConvertFrame(ref frame);
                    //if (sampleBGR24 != null)
                    //    OnVideoSourceRawExtSample?.Invoke(timestampDuration, targetWidth, targetHeight, sampleBGR24, VideoPixelFormatsEnum.Rgb);
                //}

                // Manage Encoded Sample
                if (OnVideoSourceEncodedSample != null)
                {
                //    if (_videoFrameYUV420PConverter == null ||
                //        _videoFrameYUV420PConverter.SourceWidth != width ||
                //        _videoFrameYUV420PConverter.SourceHeight != height)
                //    {
                //        _videoFrameYUV420PConverter = new VideoFrameConverter(
                //            width, height,
                //            (AVPixelFormat)frame.format,
                //            targetWidth, targetHeight,
                //            AVPixelFormat.AV_PIX_FMT_YUV420P);
                //        logger.LogDebug($"Frame format: [{frame.format}]");
                //    }
                //    byte[] sampleYUV420P = _videoFrameYUV420PConverter.ConvertFrame(ref frame);

                    //if (sampleYUV420P != null)
                    //if(frameBGR24 != null)
                    {
                        AVCodecID aVCodecId = FFmpegConvert.GetAVCodecID(_videoFormatManager.SelectedFormat.Codec);
                        //byte[]? encodedSample = _videoEncoder.Encode(aVCodecId, sampleYUV420P, targetWidth, targetHeight, targetFps, _forceKeyFrame, AVPixelFormat.AV_PIX_FMT_YUV420P);

                        byte[]? encodedSample = _videoEncoder.Encode(aVCodecId, frameBGR24, targetFps, _forceKeyFrame);

                        if (encodedSample != null)
                        {
                            // Note the event handler can be removed while the encoding is in progress.
                            OnVideoSourceEncodedSample?.Invoke(timestampDuration, encodedSample);
                        }
                        _forceKeyFrame = false;
                    }
                }
            }
        }

        public Task Start()
        {
            if (!_isStarted)
            {
                _isStarted = true;
                _videoDecoder.StartDecode();
            }

            return Task.CompletedTask;
        }

        public async Task Close()
        {
            if (!_isClosed)
            {
                _isClosed = true;
                await _videoDecoder.Close();
                Dispose();
            }
        }

        public Task Pause()
        {
            if (!_isPaused)
            {
                _isPaused = true;
                _videoDecoder.Pause();
            }

            return Task.CompletedTask;
        }

        public async Task Resume()
        {
            if (_isPaused && !_isClosed)
            {
                _isPaused = false;
                await _videoDecoder.Resume();
            }
        }

        public void Dispose()
        {
            _videoDecoder?.Dispose();

            _videoEncoder?.Dispose();
        }

    }
}
