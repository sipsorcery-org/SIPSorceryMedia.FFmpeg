using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FFmpeg.AutoGen;
using SIPSorceryMedia.Abstractions;
using SIPSorceryMedia.FFmpeg;

namespace FFmpegConsoleApp
{
    class Program
    {
        //private const string LIB_PATH = @"/usr/local/Cellar/ffmpeg/4.4.1_5/lib"; // On MacBookPro
        private const string LIB_PATH = @"C:\ffmpeg-4.4.1-full_build-shared\bin"; // On Windows

        //private const string LIB_PATH = @"..\..\..\..\..\lib\x64";


        static private VideoFrameConverter converter = null;
        static readonly StringBuilder builder = new StringBuilder();
        static readonly char[] asciiPixels = " `'.,-~:;<>\"^=+*!?|\\/(){}[]#&$@".ToCharArray();

        static void Main(string[] args)
        {
            VideoCodecsEnum VideoCodec = VideoCodecsEnum.H264;
            IVideoSource videoSource;

            // Initialise FFmpeg librairies
            FFmpegInit.Initialise(FfmpegLogLevelEnum.AV_LOG_FATAL, LIB_PATH);

            // Get cameras and monitors
            List<Camera> cameras = FFmpegCameraManager.GetCameraDevices();
            List<Monitor> monitors = FFmpegMonitorManager.GetMonitorDevices();

            // If we have both Camera and Monitors, ask user to choose 
            char keyChar = ' ';
            if ( (cameras?.Count > 0) && (monitors?.Count > 0) )
            {
                while (true)
                {
                    Console.Clear();
                    Console.WriteLine("\nDo we want to use Camera or a Monitor ?");
                    Console.Write("\n [c] - Camera ");
                    Console.Write("\n [m] - Monitor ");
                    Console.WriteLine("\n");
                    Console.Out.Flush();

                    var keyConsole = Console.ReadKey();
                    if ((keyConsole.KeyChar == 'c') || (keyConsole.KeyChar == 'm'))
                    {
                        keyChar = keyConsole.KeyChar;
                        break;
                    }
                }
            }
            else
            {
                if (cameras?.Count > 0)
                    keyChar = 'c';
                else if (monitors?.Count > 0)
                    keyChar = 'm';
            }

            // Selection is correct ?
            if (!((keyChar == 'c') || (keyChar == 'm')))
            {
                Console.WriteLine("\nYou have no monitor neither a camera ... It means that somehting bad happened in enumeration");
                return;
            }

            // Do we manage a camera ?
            if (keyChar == 'c')
            {
                int cameraIndex = 0;
                if (cameras?.Count > 1)
                {
                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine("\nWhich camera do you want to use:");
                        int index = 0;
                        foreach (Camera camera in cameras)
                        {
                            Console.Write($"\n [{index}] - {camera.Name} ");
                            index++;
                        }
                        Console.WriteLine("\n");
                        Console.Out.Flush();

                        var keyConsole = Console.ReadKey();
                        if (int.TryParse("" + keyConsole.KeyChar, out int keyValue) && keyValue < index && keyValue >= 0)
                        {
                            cameraIndex = keyValue;
                            break;
                        }
                    }
                }

                var selectedCamera = cameras[cameraIndex];
                SIPSorceryMedia.FFmpeg.FFmpegCameraSource cameraSource = new SIPSorceryMedia.FFmpeg.FFmpegCameraSource(selectedCamera.Path);
                videoSource = cameraSource as IVideoSource;

            }
            // Do we manage a Monitor ?
            else 
            {
                int monitorIndex = 0;
                if (monitors?.Count > 1)
                {
                    while (true)
                    {
                        Console.Clear();
                        Console.WriteLine("\nWhich Monitor do you want to use:");
                        int index = 0;
                        foreach (Monitor monitor in monitors)
                        {
                            Console.Write($"\n [{index}] - {monitor.Name} {(monitor.Primary ? " PRIMARY" : "")}");
                            index++;
                        }
                        Console.WriteLine("\n");
                        Console.Out.Flush();

                        var keyConsole = Console.ReadKey();
                        if (int.TryParse("" + keyConsole.KeyChar, out int keyValue) && keyValue < index && keyValue >= 0)
                        {
                            monitorIndex = keyValue;
                            break;
                        }
                    }
                }

                var selectedMonitor = monitors[monitorIndex];
                SIPSorceryMedia.FFmpeg.FFmpegScreenSource screenSource = new SIPSorceryMedia.FFmpeg.FFmpegScreenSource(selectedMonitor.Path, selectedMonitor.Rect, 20);
                videoSource = screenSource as IVideoSource;
            }


            videoSource.RestrictFormats(x => x.Codec == VideoCodec);
            videoSource.SetVideoSourceFormat(videoSource.GetVideoSourceFormats().Find(x => x.Codec == VideoCodec));
            videoSource.OnVideoSourceRawSample+= FileSource_OnVideoSourceRawSample;
            videoSource.StartVideo();

            for (var loop = true; loop;)
            {
                var cki = Console.ReadKey(true);
                switch (cki.Key)
                {
                    case ConsoleKey.Q:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Escape:
                        Console.CursorVisible = true;
                        loop = false;
                        break;
                }
            }
        }

        private static void FileSource_OnVideoSourceRawSample(uint durationMilliseconds, RawImage rawImage)
        {
            if (converter == null 
                || Console.WindowWidth != converter.SourceWidth 
                || Console.WindowHeight != converter.SourceWidth)
            {
                // We can't just override converter
                // We have to dispose the previous one and instanciate a new one with the new window size.
                converter?.Dispose();
                converter = new VideoFrameConverter(rawImage.Width, rawImage.Height, AVPixelFormat.AV_PIX_FMT_RGB24, Console.WindowWidth, Console.WindowHeight, FFmpeg.AutoGen.AVPixelFormat.AV_PIX_FMT_GRAY8);
            }

            // Resize the frame to the size of the terminal window, then draw it in ASCII.
            var frame = converter.Convert(rawImage.Sample);
            DrawAsciiFrame(frame);
        }

        private static unsafe void DrawAsciiFrame(AVFrame frame)
        {
            // We don't call Console.Clear() here because it actually adds stutter.
            // Go ahead and try this example in Alacritty to see how smooth it is!
            builder.Clear();
            Console.SetCursorPosition(0, 0);
            int length = frame.width * frame.height;

            var RawData = new ReadOnlySpan<byte>(frame.data[0], frame.linesize[0] * frame.height);

            // Since we know that the frame has the exact size of the terminal window,
            // we have no need to add any newline characters. Thus we can just go through
            // the entire byte array to build the ASCII converted string.
            for (int i = 0; i < length; i++)
            {
                builder.Append(asciiPixels[RangeMap(RawData[i], 0, 255, 0, asciiPixels.Length - 1)]);
            }

            Console.Write(builder.ToString());
            Console.Out.Flush();
        }

        public static int RangeMap(int x, int in_min, int in_max, int out_min, int out_max)
        => (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
    }
}
