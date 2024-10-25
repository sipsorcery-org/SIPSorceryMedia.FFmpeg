# SIPSorceryMedia.FFmpeg

This project is an example of developing a C# library that can use features from [FFmpeg](https://ffmpeg.org/) native libraries and that inegrates with the [SIPSorcery](https://github.com/sipsorcery-org/sipsorcery) real-time communications library.

This project has been tested successfully on Windows, MacOs and Linux.

The classes in this project provide functions to:

 - **Video codecs**: VP8, H264
 - **Audio codecs**: PCMU (G711), PCMA (G711), G722, G729 and Opus
 - **Video input**:
    - using local file or remote using URI (like [this](https://upload.wikimedia.org/wikipedia/commons/3/36/Cosmos_Laundromat_-_First_Cycle_-_Official_Blender_Foundation_release.webm))
    - using camera 
    - using screen
 - **Audio input**:
    - using local file or remote using URI (like [this](https://upload.wikimedia.org/wikipedia/commons/3/36/Cosmos_Laundromat_-_First_Cycle_-_Official_Blender_Foundation_release.webm) or [this](https://upload.wikimedia.org/wikipedia/commons/0/0f/Pop_RockBrit_%28exploration%29-en_wave.wav))
    - using microphone

You can set any **Video input** (or none) with any **Audio input** (or none)

There is no **Audio ouput** in this library. For this you can use [SIPSorcery.SDL2](https://github.com/sipsorcery-org/SIPSorcery.SDL2)

# Installing FFmpeg

## For Windows

Install the [FFmpeg](https://www.ffmpeg.org/) binaries using the packages at https://www.gyan.dev/ffmpeg/builds/#release-builds and include the FFMPEG executable in your PATH to find them automatically.

As of 14 Jan 2024 the command below works on Windows 11 and installs the required FFmpeg binaries and lbraries wheer teh can be found by SIPSorceryMedia.FFmpeg:

`winget install "FFmpeg (Shared)"`

## For Linux

Install the [FFmpeg](https://www.ffmpeg.org/) binaries using the package manager for the distribution.

`sudo apt install ffmpeg`

## For Mac

Install [homebrew](https://brew.sh/)

`brew install ffmpeg`
`brew install mono-libgdiplus`
## For Android
Create a new folder in your project directory and call it libs
Add FFmpeg lib at the project root to the folder you created
then add the following to your project file:

```
	<ItemGroup>
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libavcodec.so" />
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libavdevice.so" />
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libavfilter.so" />
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libavformat.so" />
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libavutil.so" />
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libpostproc.so" />
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libswresample.so" />
		<AndroidNativeLibrary Include="libs\android\arm64-v8a\libswscale.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libavcodec.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libavdevice.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libavfilter.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libavformat.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libavutil.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libpostproc.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libswresample.so" />
		<AndroidNativeLibrary Include="libs\android\armeabi-v7a\libswscale.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libavcodec.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libavdevice.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libavfilter.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libavformat.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libavutil.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libpostproc.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libswresample.so" />
		<AndroidNativeLibrary Include="libs\android\x86\libswscale.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libavcodec.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libavdevice.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libavfilter.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libavformat.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libavutil.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libpostproc.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libswresample.so" />
		<AndroidNativeLibrary Include="libs\android\x86_64\libswscale.so" />
	</ItemGroup>
```
Now you have to get the required permissions for android.
For the camera you have to add the following line to the AndroidManifest.xml
```
<uses-permission android:name="android.permission.CAMERA" />
```
And now you can request the Camera permission, for maui use the following:
```
if((await Permissions.RequestAsync<Permissions.Camera>()) is PermissionStatus.Granted)
{
    //Create your CameraSource here
}
```
This method for requesting permissions works on all platforms but we only need it for android
# Testing

Test 
- with [FFmpegFileAndDevicesTest](./test/FFmpegFileAndDevicesTest) project
- or with the [WebRTC Test Pattern demo](https://github.com/sipsorcery/sipsorcery/tree/master/examples/WebRTCExamples/WebRTCTestPatternServer)


