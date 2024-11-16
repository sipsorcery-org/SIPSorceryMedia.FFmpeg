﻿using FFmpeg.AutoGen.Abstractions;
using Microsoft.Extensions.Logging;
using SIPSorcery;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SIPSorceryMedia.FFmpeg
{
    public class FFmpegCameraSource : FFmpegVideoSource
    {
        private static ILogger logger = LogFactory.CreateLogger<FFmpegCameraSource>();

        private readonly Camera _camera;

        /// <summary>
        /// Construct an FFmpeg camera/input device source provided input camera.Path.
        /// </summary>
        /// <remarks>See </remarks>
        /// <param name="camera.Path"></param>
        public FFmpegCameraSource(string path) : this(FFmpegCameraManager.GetCameraByPath(path) ?? new() { Path = path })
        {
        }

        /// <summary>
        /// Construct an FFmpeg camera/input device source provided a <see cref="Camera"/>.
        /// </summary>
        /// <param name="camera"></param>
        /// <exception cref="NotSupportedException">Platform is currently not supported.</exception>
        public unsafe FFmpegCameraSource(Camera camera)
        {
            _camera = camera;

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
            decoderOptions["camera_index"] = camera.Path;
#endif
            CreateVideoDecoder(camera.Path, aVInputFormat, false, true);
            InitialiseDecoder(decoderOptions);
        }

        /// <summary>
        /// Filter for the desired <see cref="Camera.CameraFormat"/>(s) to use
        /// and resets the underlying <see cref="FFmpegVideoDecoder"/>.
        /// </summary>
        /// <remarks>Will use highest framerate then resolution after filtered.
        /// </remarks>
        /// <param name="formatFilter">Filter function.</param>
        /// <returns><see langword="true"/> If decoder resets successfully.
        /// <br/>Increase FFmpeg verbosity / loglevel for more information.</returns>
        public bool RestrictCameraFormats(Func<Camera.CameraFormat, bool> formatFilter)
        {
            var maxAllowedres = _camera.AvailableFormats?.Where(formatFilter.Invoke)
                                    .OrderByDescending(c => c.FPS)
                                    .ThenByDescending(c => c.Width > c.Height ? c.Width : c.Height)
                                    .Select(c => new Dictionary<string, string>()
                                    {
                                        { "pixel_format", ffmpeg.av_get_pix_fmt_name(c.PixelFormat) },
                                        { "video_size", $"{c.Width}x{c.Height}" },
                                        { "framerate", $"{c.FPS}" },
                                    })
                                    .FirstOrDefault();
            
            if(maxAllowedres is null)
                logger.LogWarning($"camera/input device \"{_camera.Name}\" doesn't have any recognizable filtered formats to be used.");

            return SetCameraDeviceOptions(maxAllowedres);
        }

        /// <summary>
        /// Filter for available FFmpeg camera/input device options and resets the underlying
        /// <see cref="FFmpegVideoDecoder"/> with the specified options.
        /// </summary>
        /// <remarks>Will use highest framerate then resolution after filtered.
        /// <br/><br/>
        /// <i>This is an advanced control for camera/input devices options filtering.
        /// <br/>Most usage will use <see cref="RestrictCameraFormats"/> filter.</i>
        /// <br/><br/> See <see href="https://www.ffmpeg.org/ffmpeg-devices.html">FFmpeg documentation on the device options</see>
        /// for your system's <see cref="AVInputFormat"/> (i.e. dshow, avfoundation, v4l2, etc.)
        /// </remarks>
        /// <param name="optFilter">Filter function.</param>
        /// <returns><see langword="true"/> If decoder resets successfully.
        /// <br/>Increase FFmpeg verbosity / loglevel for more information.</returns>
        public bool RestrictCameraOptions(Func<Dictionary<string, string>, bool> optFilter)
        {
            var filtered = _camera.AvailableOptions?.Where(optFilter.Invoke)
                                .OrderByDescending(d => int.Parse(d["max_fps"]))
                                .ThenByDescending(d => int.Parse(d["min_fps"]))
                                .ThenByDescending(d =>
                                {
                                    var max_s = d["max_s"].Split(['x'], StringSplitOptions.RemoveEmptyEntries);
                                    var max_w = int.Parse(max_s[0]);
                                    var max_h = int.Parse(max_s[1]);

                                    return max_h > max_w ? max_h : max_w;
                                })
                                .ThenByDescending(d =>
                                {
                                    var min_s = d["min_s"].Split(['x'], StringSplitOptions.RemoveEmptyEntries);
                                    var min_w = int.Parse(min_s[0]);
                                    var min_h = int.Parse(min_s[1]);

                                    return min_h > min_w ? min_h : min_w;
                                })
                                .FirstOrDefault()?
                                .Where(kp => !kp.Key.Contains("min_"))
                                .ToDictionary(d => d.Key switch
                                    {
                                        "max_s" => "video_size",
                                        "max_fps" => "framerate",
                                        _ => d.Key
                                    },
                                    kp => kp.Value
                                );

            if (filtered is null)
                logger.LogWarning($"No camera/input device options to be used.");

            return SetCameraDeviceOptions(filtered);
        }

        /// <summary>
        /// Resets the underlying <see cref="FFmpegVideoDecoder"/> with the provided options.
        /// </summary>
        /// <remarks>
        /// <br/><i>This is an advanced options for if you know/static preconfigured device options beforehand.
        /// Most usage will use <see cref="RestrictCameraFormats"/> or <see cref="RestrictCameraOptions"/> filter.</i>
        /// <br/><br/>
        /// See <see href="https://www.ffmpeg.org/ffmpeg-devices.html">FFmpeg documentation on the device options</see>
        /// for your system's <see cref="AVInputFormat"/> (i.e. dshow, avfoundation, v4l2, etc.)
        /// </remarks>
        /// <param name="options">A dictionary of device options</param>
        /// <returns><see langword="true"/> If decoder resets successfully.
        /// <br/>Increase FFmpeg verbosity / loglevel for more information.</returns>
        public bool SetCameraDeviceOptions(Dictionary<string, string>? options)
        {
            _videoDecoder?.Dispose();

            return InitialiseDecoder(options);
        }
    }
}
