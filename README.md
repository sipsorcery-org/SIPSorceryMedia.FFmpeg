# SIPSorceryMedia.FFmpeg

This project is an example of developing a C# library that can use features from [FFmpeg](https://ffmpeg.org/) native libraries and that integrates with the [SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) real-time communications library.

This project has been well tested in **Windows**, **MacOS** and **Linux** using **FFmpeg v4.4.1**

The classes in this project provide functions to:

 - Video codecs: **VP8**, **H264**
 - Audio codecs: **PCMU**, **PCMA**
 - **Video input**:
    - using local file or remote using URI (like [this](https://upload.wikimedia.org/wikipedia/commons/3/36/Cosmos_Laundromat_-_First_Cycle_-_Official_Blender_Foundation_release.webm))
    - using camera
    - using screen (a monitor ar a part of it)
 - **Audio input**:
    - using local file or remote using URI (like [this](https://upload.wikimedia.org/wikipedia/commons/3/36/Cosmos_Laundromat_-_First_Cycle_-_Official_Blender_Foundation_release.webm))
    - using microphone 

You can set any **Video input** (or none) with any **Audio input** (or none).

In this example [FFmpegWebRtcReceiver](./test/FFmpegWebRtcReceiver) you can see how to manage **Video output** (the Video of the remote is displayed locally in a Form)

**Audio output** cannot be managed with this project. For this you can use [SIPSorceryMedia.SDL2](https://github.com/sipsorcery-org/SIPSorcery.SDL2) which supports also **Windows**, **MacOS** and **Linux**.

# Installing FFmpeg

## For Windows

No additional steps are required for an x64 build. The nuget package includes the [FFmpeg](https://www.ffmpeg.org/) x64 binaries (vv4.4.1).

## For Linux

Install the [FFmpeg](https://www.ffmpeg.org/) binaries using the package manager for the distribution.

`sudo apt install ffmpeg`

## For Mac

Install [homebrew](https://brew.sh/)

`brew install ffmpeg`

`brew install mono-libgdiplus`

**NOTE**: On MacOS, for security reason, it's necessary to allow access to Camera and Screen capture. If it's not the case, tests application will not work.  

# Testing

Several projects permits to understand how the library can be used (all in C# except one in Cpp):

- [FFmpegConsoleApp](./test/FFmpegConsoleApp) - **Multiplatform application**:
    - Enumerates Camera
    - Enumerates Monitors
    - Let user select a Camera, a Monitor or a File 
    - Display in ASCII the selection in a Terminal Window
    
- [FFmpegFileAndDevicesTest](./test/FFmpegFileAndDevicesTest) - **Multiplatform application**:
    - Use "webrtc.html" in your favorite browser
    - In file "Program.cs" select what you want to use to create a WebRTC Communication:
        - For the video part
            - a file (local or remote)
            - a camera
            - a screen a monitor ar a part of it
        - For the audio part
            - a file (local or remote)
            - a microphone
    - Start the program, the result is streamed using WebRTC in your browser
    
- [FFmpegWebRtcReceiver](./test/FFmpegWebRtcReceiver) - **Windows only application**:
    - Use "capture.html" in your favorite browser
    - Start the program, the Camera used by the browser is streamed using WebRTC to your program

- [FFmpegCppEncodingTest](./test/FFmpegCppEncodingTest) - **Windows only - Cpp application**:
    - Used for internal tests: encoding, H264
    
- [FFmpegEncodingTest](./test/FFmpegEncodingTest) - **Multiplatform**:
    - Used for internal tests: frame convertion: RGB24 <-> YUV420P, H264

- [FFmpegMp4Test](./test/FFmpegMp4Test) - **Multiplatform**:
    - Used for internal tests: to undestand how to read audio/video file wiht FFmpeg
