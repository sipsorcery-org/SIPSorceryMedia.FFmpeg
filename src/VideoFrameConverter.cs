﻿using System;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace SIPSorceryMedia.FFmpeg
{
    public sealed unsafe class VideoFrameConverter : IDisposable
    {
        private static ILogger logger = SIPSorcery.LogFactory.CreateLogger<VideoFrameConverter>();

        private readonly IntPtr _convertedFrameBufferPtr;
        private readonly int _srcWidth;
        private readonly int _srcHeight;
        private readonly int _dstWidth;
        private readonly int _dstHeight;
        private readonly byte_ptrArray4 _dstData;
        private readonly int_array4 _dstLinesize;
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
            _dstData = new byte_ptrArray4();
            _dstLinesize = new int_array4();

            ffmpeg.av_image_fill_arrays(ref _dstData, ref _dstLinesize, (byte*)_convertedFrameBufferPtr, destinationPixelFormat, dstWidth, dstHeight, 1)
                .ThrowExceptionIfError();

            logger.LogDebug($"Successfully initialised ffmpeg based image converted for {srcWidth}:{srcHeight}:{sourcePixelFormat}->{dstWidth}:{dstHeight}:{_dstPixelFormat}.");


            _dstFrame = ffmpeg.av_frame_alloc();
            _dstFrame->width = _dstWidth;
            _dstFrame->height = _dstHeight;
            _dstFrame->data.UpdateFrom(_dstData);
            _dstFrame->linesize.UpdateFrom(_dstLinesize);
            _dstFrame->format = (int)_dstPixelFormat;
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(_convertedFrameBufferPtr);
            ffmpeg.sws_freeContext(_pConvertContext);
        }

        public AVFrame Convert(IntPtr srcData)
        {
            byte_ptrArray4 src = new byte_ptrArray4();
            int_array4 srcStride = new int_array4();

            byte* pSrcData = (byte*)srcData;
            ffmpeg.av_image_fill_arrays(ref src, ref srcStride, pSrcData, _srcPixelFormat, _srcWidth, _srcHeight, 1).ThrowExceptionIfError();

            ffmpeg.sws_scale(_pConvertContext, src, srcStride, 0, _srcHeight, _dstData, _dstLinesize).ThrowExceptionIfError();

            var data = new byte_ptrArray8();
            data.UpdateFrom(_dstData);
            var linesize = new int_array8();
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

        public AVFrame Convert(byte[] srcData)
        {
            byte_ptrArray4 src = new byte_ptrArray4();
            int_array4 srcStride = new int_array4();

            fixed (byte* pSrcData = srcData)
            {
                ffmpeg.av_image_fill_arrays(ref src, ref srcStride, pSrcData, _srcPixelFormat, _srcWidth, _srcHeight, 1).ThrowExceptionIfError();
            }

            ffmpeg.sws_scale(_pConvertContext, src, srcStride, 0, _srcHeight, _dstData, _dstLinesize).ThrowExceptionIfError();

            var data = new byte_ptrArray8();
            data.UpdateFrom(_dstData);
            var linesize = new int_array8();
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
            //int linesz0 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 0);
            //int linesz1 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 1);
            //int linesz2 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 2);

            byte_ptrArray4 src = new byte_ptrArray4();
            int_array4 srcStride = new int_array4();

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

        public AVFrame Convert(ref AVFrame frame)
        {
            ffmpeg.sws_scale(_pConvertContext,
                frame.data, frame.linesize, 0, frame.height,
                _dstData, _dstLinesize);

            var data = new byte_ptrArray8();
            data.UpdateFrom(_dstData);
            var linesize = new int_array8();
            linesize.UpdateFrom(_dstLinesize);

            return new AVFrame
            {
                data = data,
                linesize = linesize,
                width = _dstWidth,
                height = _dstHeight,
                format = (int)_dstPixelFormat
            };

            //ffmpeg.sws_scale(_pConvertContext,
            //    frame.data, frame.linesize, 0, frame.height,
            //    _dstFrame->data, _dstFrame->linesize);
            //return _dstFrame;
        }

        public byte[] ConvertFrame(ref AVFrame frame)
        {
            //int linesz0 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 0);
            //int linesz1 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 1);
            //int linesz2 = ffmpeg.av_image_get_linesize(_srcPixelFormat, _dstSize.Width, 2);

            //byte_ptrArray4 src = new byte_ptrArray4();
            //int_array4 srcStride = new int_array4();

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
