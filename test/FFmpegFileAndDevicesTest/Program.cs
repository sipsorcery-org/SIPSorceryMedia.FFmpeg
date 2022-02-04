﻿
//-----------------------------------------------------------------------------
// Filename: Program.cs
//
// Description: An example WebRTC server application that streams the contents 
// of a media file, such as an mp4, to a WebRTC enabled browser.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
// Christophe Irles (christophe.irles@al-enterprise.com)
// 
// History:
// 17 Sep 2020	Aaron Clauson	Created, Dublin, Ireland.
// 27 Nov 2021 Christophe Irles Split Audio/Video, Add Camera support
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Extensions.Logging;
using SIPSorcery.Media;
using SIPSorcery.Net;
using SIPSorceryMedia.Abstractions;
using WebSocketSharp.Server;

namespace FFmpegFileAndDevicesTest
{
    class Program
    {
        //  /!\ TO DEFINE WHERE ffmpeg librairies are stored
        private const string LIB_PATH = @"..\..\..\..\..\lib\x64"; // @"C:\ffmpeg-4.4.1-full_build-shared\bin";

        private const int WEBSOCKET_PORT = 8081;
        private const string STUN_URL = "stun:stun.sipsorcery.com";

        private static Microsoft.Extensions.Logging.ILogger logger = NullLogger.Instance;

        enum VIDEO_SOURCE
        {
            NONE,
            FILE_OR_STREAM,
            CAMERA,
            SCREEN
        }

        enum AUDIO_SOURCE
        {
            NONE,
            FILE_OR_STREAM,
            MICROPHONE,
        }

        //  /!\   Define some path/urls to some media files - to be set according your environment
        static private string LOCAL_AUDIO_AND_VIDEO_FILE_MP4_BUNNY = @"C:\media\big_buck_bunny.mp4";
        static private string LOCAL_AUDIO_AND_VIDEO_FILE_MP4_MAX = @"C:\media\max_intro.mp4";

        static private string LOCAL_AUDIO_FILE_MP3 = @"C:\media\simplest_ffmpeg_audio_decoder_skycity1.mp3";
        static private string LOCAL_AUDIO_FILE_WAV = @"C:\media\file_example_WAV_5MG.wav";

        static private string DISTANT_AUDIO_AND_VIDEO_FILE_WEBM = @"https://upload.wikimedia.org/wikipedia/commons/3/36/Cosmos_Laundromat_-_First_Cycle_-_Official_Blender_Foundation_release.webm";

        // Define variables according what you want to test
        static private VIDEO_SOURCE VideoSourceType = VIDEO_SOURCE.FILE_OR_STREAM; // VIDEO_SOURCE.FILE_OR_STREAM;
        static private AUDIO_SOURCE AudioSourceType = AUDIO_SOURCE.FILE_OR_STREAM;

        static private VideoCodecsEnum VideoCodec = VideoCodecsEnum.H264; // or VideoCodecsEnum.VP8
        static private AudioCodecsEnum AudioCodec = AudioCodecsEnum.PCMU;

        static private String VideoSourceFile = LOCAL_AUDIO_AND_VIDEO_FILE_MP4_BUNNY; // Used if VideoSource = VIDEO_SOURCE.FILE_OR_STREAM
        static private String AudioSourceFile = LOCAL_AUDIO_AND_VIDEO_FILE_MP4_BUNNY; // used if AudioSource = AUDIO_SOURCE.FILE_OR_STREAM;

        static private string MicrophoneDevicePath = "audio=Microphone (HD Pro Webcam C920)"; // Specific info according end-user devices
        static private string CameraDevicePath = "video=HD Pro Webcam C920"; // Specific info according end-user devices

        static private bool RepeatVideoFile = true; // Used if VideoSource == VIDEO_SOURCE.FILE_OR_STREAM
        static private bool RepeatAudioFile = true; // Used if AudioSource == AUDIO_SOURCE.FILE_OR_STREAM

        static private RTCPeerConnection PeerConnection = null;

        static private IAudioSink audioSink = null;

        static private IVideoSource videoSource = null;
        static private IAudioSource audioSource = null;

        static void Main()
        {
            Console.WriteLine("WebRTC MP4 Source Demo");

            logger = AddConsoleLogger();

            // Initialise FFmpeg librairies
            SIPSorceryMedia.FFmpeg.FFmpegInit.Initialise(SIPSorceryMedia.FFmpeg.FfmpegLogLevelEnum.AV_LOG_DEBUG, LIB_PATH);

            // Start web socket.
            Console.WriteLine("Starting web socket server...");
            var webSocketServer = new WebSocketServer(IPAddress.Any, WEBSOCKET_PORT);
            webSocketServer.AddWebSocketService<WebRTCWebSocketPeer>("/", (peer) => peer.CreatePeerConnection = CreatePeerConnection);
            webSocketServer.Start();

            Console.WriteLine($"Waiting for web socket connections on {webSocketServer.Address}:{webSocketServer.Port}...");
            Console.WriteLine("Press ctrl-c to exit.");

            // Ctrl-c will gracefully exit the call at any point.
            ManualResetEvent exitMe = new ManualResetEvent(false);
            Console.CancelKeyPress += delegate (object sender, ConsoleCancelEventArgs e)
            {
                e.Cancel = true;
                exitMe.Set();
            };

            // Wait for a signal saying the call failed, was cancelled with ctrl-c or completed.
            exitMe.WaitOne();
        }

        static private Task<RTCPeerConnection> CreatePeerConnection()
        {
            
            RTCConfiguration config = new RTCConfiguration
            {
                iceServers = new List<RTCIceServer> { new RTCIceServer { urls = STUN_URL } }
            };

            PeerConnection = new RTCPeerConnection(config);

            switch(VideoSourceType)
            {
                case VIDEO_SOURCE.FILE_OR_STREAM:
                    // Do we use same file for Audio ?
                    if ((AudioSourceType == AUDIO_SOURCE.FILE_OR_STREAM)  && (AudioSourceFile == VideoSourceFile))
                    {
                        SIPSorceryMedia.FFmpeg.FFmpegFileSource fileSource = new SIPSorceryMedia.FFmpeg.FFmpegFileSource(VideoSourceFile, RepeatVideoFile, new AudioEncoder(), true);
                        fileSource.OnEndOfFile += () => PeerConnection.Close("source eof");

                        videoSource = fileSource as IVideoSource;
                        audioSource = fileSource as IAudioSource;
                    }
                    else
                    {
                        SIPSorceryMedia.FFmpeg.FFmpegFileSource fileSource = new SIPSorceryMedia.FFmpeg.FFmpegFileSource(VideoSourceFile, RepeatVideoFile, new AudioEncoder(), true);
                        fileSource.OnEndOfFile += () => PeerConnection.Close("source eof");

                        videoSource = fileSource as IVideoSource;
                    }
                    break;

                case VIDEO_SOURCE.CAMERA:
                    List<SIPSorceryMedia.FFmpeg.Camera>? cameras = SIPSorceryMedia.FFmpeg.FFmpegCameraManager.GetCameraDevices();
                    
                    SIPSorceryMedia.FFmpeg.Camera? camera = null;
                    if (cameras?.Count > 0 )
                    {
                        // Get last one
                        camera = cameras.Last();
                    }
                    if (camera != null)
                        videoSource = new SIPSorceryMedia.FFmpeg.FFmpegCameraSource(camera.Path);
                    else
                        throw new NotSupportedException($"Cannot find adequate camera ...");
                    
                    break;

                case VIDEO_SOURCE.SCREEN:
                    List<SIPSorceryMedia.FFmpeg.Monitor>? monitors = SIPSorceryMedia.FFmpeg.FFmpegMonitorManager.GetMonitorDevices();
                    SIPSorceryMedia.FFmpeg.Monitor? primaryMonitor = null;
                    if (monitors?.Count > 0)
                    {
                        foreach(SIPSorceryMedia.FFmpeg.Monitor monitor in monitors)
                        {
                            if (monitor.Primary)
                            {
                                primaryMonitor = monitor;
                                break;
                            }
                        }
                        if (primaryMonitor == null)
                            primaryMonitor = monitors[0];
                    }

                    if(primaryMonitor != null)
                        videoSource = new SIPSorceryMedia.FFmpeg.FFmpegScreenSource(primaryMonitor.Path, primaryMonitor.Rect, 10);
                    else
                        throw new NotSupportedException($"Cannot find adequate monitor ...");
                    break;
            }

            if(audioSource == null)
            {
                switch(AudioSourceType)
                {
                    case AUDIO_SOURCE.FILE_OR_STREAM:
                        SIPSorceryMedia.FFmpeg.FFmpegFileSource fileSource = new SIPSorceryMedia.FFmpeg.FFmpegFileSource(AudioSourceFile, RepeatAudioFile, new AudioEncoder(), false);
                        fileSource.OnEndOfFile += () => PeerConnection.Close("source eof");

                        audioSource = fileSource as IAudioSource;
                        break;

                    case AUDIO_SOURCE.MICROPHONE:
                        audioSource = new SIPSorceryMedia.FFmpeg.FFmpegMicrophoneSource(MicrophoneDevicePath, new AudioEncoder());
                        break;
                }
            }

            if(videoSource != null)
            {
                videoSource.RestrictFormats(x => x.Codec == VideoCodec);

                MediaStreamTrack videoTrack = new MediaStreamTrack(videoSource.GetVideoSourceFormats(), MediaStreamStatusEnum.SendRecv);
                PeerConnection.addTrack(videoTrack);


                videoSource.OnVideoSourceEncodedSample += PeerConnection.SendVideo;
                PeerConnection.OnVideoFormatsNegotiated += (videoFormats) => videoSource.SetVideoSourceFormat(videoFormats.First());
            }

            if(audioSource != null)
            {
                audioSource.RestrictFormats(x => x.Codec == AudioCodec);

                MediaStreamTrack audioTrack = new MediaStreamTrack(audioSource.GetAudioSourceFormats(), MediaStreamStatusEnum.SendRecv);
                PeerConnection.addTrack(audioTrack);

                audioSource.OnAudioSourceEncodedSample += AudioSource_OnAudioSourceEncodedSample; 
                PeerConnection.OnAudioFormatsNegotiated += (audioFormats) => audioSource.SetAudioSourceFormat(audioFormats.First());
            }

            PeerConnection.onconnectionstatechange += async (state) =>
            {
                logger.LogDebug($"Peer connection state change to {state}.");

                if (state == RTCPeerConnectionState.failed)
                {
                    PeerConnection.Close("ice disconnection");
                }
                else if (state == RTCPeerConnectionState.closed)
                {
                    if(videoSource != null)
                        await videoSource.CloseVideo();

                    if (audioSink != null)
                        await audioSink.CloseAudioSink();

                    if (audioSource != null)
                        await audioSource.CloseAudio();
                }
                else if (state == RTCPeerConnectionState.connected)
                {
                    if (videoSource != null)
                        await videoSource.StartVideo();

                    if (audioSink != null)
                    {
                        await audioSink.StartAudioSink();
                    }

                    if (audioSource != null)
                        await audioSource.StartAudio();
                }
            };

            // Diagnostics.
            //pc.OnReceiveReport += (re, media, rr) => logger.LogDebug($"RTCP Receive for {media} from {re}\n{rr.GetDebugSummary()}");
            //pc.OnSendReport += (media, sr) => logger.LogDebug($"RTCP Send for {media}\n{sr.GetDebugSummary()}");
            //pc.GetRtpChannel().OnStunMessageReceived += (msg, ep, isRelay) => logger.LogDebug($"STUN {msg.Header.MessageType} received from {ep}.");
            PeerConnection.oniceconnectionstatechange += (state) => logger.LogDebug($"ICE connection state change to {state}.");

            return Task.FromResult(PeerConnection);
        }

        private static void AudioSource_OnAudioSourceEncodedSample(uint durationRtpUnits, byte[] sample)
        {
            PeerConnection.SendAudio(durationRtpUnits, sample);

            if (audioSink != null)
                audioSink.GotAudioRtp(null, 0, 0, 0, 0, false, sample);
        }

        /// <summary>
        ///  Adds a console logger. Can be omitted if internal SIPSorcery debug and warning messages are not required.
        /// </summary>
        static private Microsoft.Extensions.Logging.ILogger AddConsoleLogger()
        {
            var seriLogger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.Is(Serilog.Events.LogEventLevel.Debug)
                .WriteTo.Console()
                .CreateLogger();
            var factory = new SerilogLoggerFactory(seriLogger);
            SIPSorcery.LogFactory.Set(factory);
            return factory.CreateLogger<Program>();
        }
    }
}

