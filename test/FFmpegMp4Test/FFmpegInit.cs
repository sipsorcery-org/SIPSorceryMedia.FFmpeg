﻿using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using FFmpeg.AutoGen.Abstractions;
using FFmpeg.AutoGen.Bindings.DynamicallyLoaded;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;


namespace FFmpegMp4Test
{
    public enum FfmpegLogLevelEnum
    {
        AV_LOG_PANIC = 0,
        AV_LOG_FATAL = 8,
        AV_LOG_ERROR = 16,
        AV_LOG_WARNING = 24,
        AV_LOG_INFO = 32,
        AV_LOG_VERBOSE = 40,
        AV_LOG_DEBUG = 48,
        AV_LOG_TRACE = 56,
    }

    public static unsafe class FFmpegInit
    {
        private static ILogger logger = NullLogger.Instance;
        private static bool registered = false;

        private static av_log_set_callback_callback? logCallback;
        private static String storedLogs = "";

        public static String GetStoredLogs(Boolean clear = true)
        {
            if (clear)
            {
                String log = storedLogs;
                storedLogs = "";
                return log;
            }
            return storedLogs;
        }

        public static void ClearStoredLogs()
        {
            storedLogs = "";
        }

        public static void UseSpecificLogCallback(Boolean storeLogs = true)
        {
            // We clear previous stored logs
            if (storeLogs)
                ClearStoredLogs();

            logCallback = (p0, level, format, vl) =>
            {
                if ((!storeLogs) && (level > ffmpeg.av_log_get_level())) return;

                var lineSize = 1024;
                var lineBuffer = stackalloc byte[lineSize];
                var printPrefix = 1;
                ffmpeg.av_log_format_line(p0, level, format, vl, lineBuffer, lineSize, &printPrefix);
                var line = Marshal.PtrToStringAnsi((IntPtr)lineBuffer);
                //Console.Write(line);
                if (storeLogs)
                    storedLogs += line;
            };
            ffmpeg.av_log_set_callback(logCallback);
        }

        public static void UseDefaultLogCallback()
        {
            logCallback = (p0, level, format, vl) => ffmpeg.av_log_default_callback(p0, level, format, vl);

            ffmpeg.av_log_set_callback(logCallback);
        }

        public static void Initialise(FfmpegLogLevelEnum? logLevel = null, String? libPath = null, ILogger? appLogger = null)
        {
            if (appLogger != null)
            {
                logger = appLogger;
            }

            RegisterFFmpegBinaries(libPath);

            logger.LogInformation($"FFmpeg version info: {ffmpeg.av_version_info()}");

            if (logLevel.HasValue)
            {
                ffmpeg.av_log_set_level((int)logLevel.Value);
            }
        }

        internal static void SetFFmpegBinariesPath(string path)
        {
            DynamicallyLoadedBindings.LibrariesPath = path;
            DynamicallyLoadedBindings.Initialize();

            registered = true;

            ffmpeg.avdevice_register_all();
        }

        internal static void RegisterFFmpegBinaries(String? libPath = null)
        {
            if (registered)
                return;
#if NET5_0_OR_GREATER
            if (OperatingSystem.IsAndroid())
            {
                SetFFmpegBinariesPath("");
                return;
            }
#endif
            if (libPath == null)
            {
                // search the system path, handle with and without .exe extension
                string ffmpegExecutable = "ffmpeg";
                string? path = Environment.GetEnvironmentVariable("PATH")?
                    .Split(';')
                    .Where(s => File.Exists(Path.Combine(s, ffmpegExecutable)) || File.Exists(Path.Combine(s, ffmpegExecutable  + ".exe")))
                    .FirstOrDefault();

                if (path != null)
                {
                    logger.LogInformation($"FFmpeg binaries found in system path at: {path}");
                    SetFFmpegBinariesPath(path);
                    return;
                }

                // search from the current folder up
                var current = Environment.CurrentDirectory;
                var probe = Path.Combine("FFmpeg", "bin", Environment.Is64BitProcess ? "x64" : "x86");
                while (current != null)
                {
                    var ffmpegBinaryPath = Path.Combine(current, probe);
                    if (Directory.Exists(ffmpegBinaryPath))
                    {
                        logger.LogInformation($"FFmpeg binaries found in: {ffmpegBinaryPath}");
                        SetFFmpegBinariesPath(ffmpegBinaryPath);
                        return;
                    }

                    current = Directory.GetParent(current)?.FullName;
                }
            }
            else
            {
                if (Directory.Exists(libPath))
                {
                    logger.LogInformation($"FFmpeg binaries path set to: {libPath}");
                    SetFFmpegBinariesPath(libPath);
                    return;
                }
            }
            throw new ApplicationException("Unable to find FFMPEG binaries");
        }

        public static unsafe string? av_strerror(int error)
        {
            var bufferSize = 1024;
            var buffer = stackalloc byte[bufferSize];
            ffmpeg.av_strerror(error, buffer, (ulong)bufferSize);
            var message = Marshal.PtrToStringAnsi((IntPtr)buffer);
            return message;
        }

        public static int ThrowExceptionIfError(this int error)
        {
            if (error < 0)
            {
                throw new ApplicationException(av_strerror(error));
            }
            return error;
        }
    }
}
