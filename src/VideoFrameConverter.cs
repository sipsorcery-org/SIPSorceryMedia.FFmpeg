﻿using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SIPSorceryMedia.FFmpeg
{
    public sealed unsafe class VideoFrameConverter : IDisposable
    {
        private static ILogger logger = SIPSorcery.LogFactory.CreateLogger<VideoFrameConverter>();

        private IntPtr _convertedFrameBufferPtr;
        private readonly int _srcWidth;
        private readonly int _srcHeight;
        private readonly int _dstWidth;
        private readonly int _dstHeight;
        private readonly byte_ptr4 _dstData;
        private readonly int4 _dstLinesize;
        private readonly SwsContext* _pConvertContext;
        private readonly AVPixelFormat _srcPixelFormat;
        private readonly AVPixelFormat _dstPixelFormat;

        private readonly AVFrame *_dstFrame;

        public int SourceWidth => _srcWidth;
        public int SourceHeight => _srcHeight;
        public int DestinationWidth => _dstWidth;
        public int DestinationHeight => _dstHeight;

        public VideoFrameConverter(int srcWidth, int srcHeight, AVPixelFormat sourcePixelFormat,
           int dstWidth, int dstHeight, AVPixelFormat destinationPixelFormat)
        {
            _srcWidth = srcWidth;
            _srcHeight = srcHeight;
            _dstWidth = dstWidth;
            _dstHeight = dstHeight;
            _srcPixelFormat = sourcePixelFormat;
            _dstPixelFormat = destinationPixelFormat;

            _pConvertContext = ffmpeg.sws_getContext(srcWidth, srcHeight, sourcePixelFormat,
                dstWidth, dstHeight, destinationPixelFormat,
                ffmpeg.SWS_FAST_BILINEAR, null, null, null);
            if (_pConvertContext == null)
            {
                throw new ApplicationException("Could not initialize the conversion context.");
            }

            var convertedFrameBufferSize = ffmpeg.av_image_get_buffer_size(destinationPixelFormat, dstWidth, dstHeight, 1).ThrowExceptionIfError();

            _convertedFrameBufferPtr = Marshal.AllocHGlobal(convertedFrameBufferSize);
            _dstData = new byte_ptr4();
            _dstLinesize = new int4();

            ffmpeg.av_image_fill_arrays(ref _dstData, ref _dstLinesize, (byte*)_convertedFrameBufferPtr, destinationPixelFormat, dstWidth, dstHeight, 1)
                .ThrowExceptionIfError();

            _dstFrame = ffmpeg.av_frame_alloc();
            _dstFrame->width = _dstWidth;
            _dstFrame->height = _dstHeight;
            _dstFrame->data.UpdateFrom(_dstData);
            _dstFrame->linesize.UpdateFrom(_dstLinesize);
            _dstFrame->format = (int)_dstPixelFormat;

            logger.LogDebug($"Successfully initialised ffmpeg based image converted for {srcWidth}:{srcHeight}:{sourcePixelFormat}->{dstWidth}:{dstHeight}:{_dstPixelFormat}.");
        }

        #region Dispose
        private bool IsDisposed => _convertedFrameBufferPtr == IntPtr.Zero;
        private void EnsureNotDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException(nameof(VideoFrameConverter));
        }
        public void Dispose()
        {
            if (IsDisposed) return;

            Marshal.FreeHGlobal(_convertedFrameBufferPtr);
            _convertedFrameBufferPtr = IntPtr.Zero;
            ffmpeg.sws_freeContext(_pConvertContext);
        }
        #endregion

        public AVFrame Convert(IntPtr srcData)
        {
            return Convert((byte*)srcData);
        }

        public AVFrame Convert(byte[] srcData)
        {
            AVFrame result;
            fixed (byte* pSrcData = srcData)
            {
                result = Convert(pSrcData);
            }
            return result;
        }

        public AVFrame Convert(byte * pSrcData)
        {
            EnsureNotDisposed();

            byte_ptr4 src = new byte_ptr4();
            int4 srcStride = new int4();

            ffmpeg.av_image_fill_arrays(ref src, ref srcStride, pSrcData, _srcPixelFormat, _srcWidth, _srcHeight, 1).ThrowExceptionIfError();

            ffmpeg.sws_scale(_pConvertContext, src, srcStride, 0, _srcHeight, _dstData, _dstLinesize).ThrowExceptionIfError();

            var data = new byte_ptr8();
            data.UpdateFrom(_dstData);
            var linesize = new int8();
            linesize.UpdateFrom(_dstLinesize);

            return new AVFrame
            {
                data = data,
                linesize = linesize,
                width = _dstWidth,
                height = _dstHeight,
                format = (int)_dstPixelFormat
            };
        }

        // to convert Bitmap to Frame
        public byte[] ConvertToBuffer(byte[] srcData)
        {
            EnsureNotDisposed();

            //int linesz0 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 0);
            //int linesz1 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 1);
            //int linesz2 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 2);

            byte_ptr4 src = new byte_ptr4();
            int4 srcStride = new int4();

            fixed (byte* pSrcData = srcData)
            {
                ffmpeg.av_image_fill_arrays(ref src, ref srcStride, pSrcData, _srcPixelFormat, _srcWidth, _srcHeight, 1).ThrowExceptionIfError();
            }

            ffmpeg.sws_scale(_pConvertContext, src, srcStride, 0, _srcHeight, _dstData, _dstLinesize).ThrowExceptionIfError();

            int outputBufferSize = ffmpeg.av_image_get_buffer_size(_dstPixelFormat, _dstWidth, _dstHeight, 1);
            byte[] outputBuffer = new byte[outputBufferSize];

            fixed (byte* pOutData = outputBuffer)
            {
                ffmpeg.av_image_copy_to_buffer(pOutData, outputBufferSize, _dstData, _dstLinesize, _dstPixelFormat, _dstWidth, _dstHeight, 1)
                    .ThrowExceptionIfError();
            }

            return outputBuffer;
        }

        public AVFrame Convert(AVFrame frame)
        {
            EnsureNotDisposed();

            try
            {
                int result = ffmpeg.av_frame_copy_props(&frame, _dstFrame);


                if (result >= 0)
                    result = ffmpeg.sws_scale(_pConvertContext,
                                frame.data, frame.linesize, 0, frame.height,
                                _dstData, _dstLinesize);



                if (result < 0)
                {
                    return new AVFrame
                    {
                        width = 0,
                        height = 0
                    };
                }

                var data = new byte_ptr8();
                data.UpdateFrom(_dstData);
                var linesize = new int8();
                linesize.UpdateFrom(_dstLinesize);

                return new AVFrame
                {
                    data = data,
                    linesize = linesize,
                    width = _dstWidth,
                    height = _dstHeight,
                    format = (int)_dstPixelFormat
                };

            }
            catch
            {
                return new AVFrame
                {
                    width = 0,
                    height = 0
                };
            }

            //ffmpeg.sws_scale(_pConvertContext,
            //    frame.data, frame.linesize, 0, frame.height,
            //    _dstFrame->data, _dstFrame->linesize);
            //return _dstFrame;
        }

        public byte[] ConvertFrame(ref AVFrame frame)
        {
            EnsureNotDisposed();

            //int linesz0 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 0);
            //int linesz1 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 1);
            //int linesz2 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 2);

            //byte_ptr4 src = new byte_ptr4();
            //int4 srcStride = new int4();

            //fixed (byte* pSrcData = srcData)
            //{
            //    ffmpeg.av_image_fill_arrays(ref src, ref srcStride, pSrcData, _srcPixelFormat, _srcWidth, _srcHeight, 1).ThrowExceptionIfError();
            //}

            ffmpeg.sws_scale(_pConvertContext, frame.data, frame.linesize, 0, frame.height, _dstData, _dstLinesize).ThrowExceptionIfError();

            int outputBufferSize = ffmpeg.av_image_get_buffer_size(_dstPixelFormat, _dstWidth, _dstHeight, 1);
            byte[] outputBuffer = new byte[outputBufferSize];

            fixed (byte* pOutData = outputBuffer)
            {
                ffmpeg.av_image_copy_to_buffer(pOutData, outputBufferSize, _dstData, _dstLinesize, _dstPixelFormat, _dstWidth, _dstHeight, 1)
                    .ThrowExceptionIfError();
            }

            return outputBuffer;
        }
    }
}
