using FFmpeg.AutoGen;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DirectShowLib;

namespace SIPSorceryMedia.FFmpeg
{
    public unsafe class FFmpegCameraManager
    {
        static public List<Camera>? GetCameraDevices()
        {
            List<Camera>? result = null;

            string inputFormat = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "dshow"
                                    : RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "v4l2"
                                    : RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "avfoundation"
                                    : throw new NotSupportedException($"Cannot find adequate input format - OSArchitecture:[{RuntimeInformation.OSArchitecture}] - OSDescription:[{RuntimeInformation.OSDescription}]");

            
            // FFmpeg doesn't implement avdevice_list_input_sources() for the DShow input format yet.
            if (inputFormat == "dshow")
            {
                result = new List<Camera>();
                var dsDevices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                for (int i = 0; i < dsDevices.Length; i++)
                {
                    var dsDevice = dsDevices[i];
                    if ((dsDevice.Name != null) && (dsDevice.Name.Length > 0))
                    {
                        Camera camera = new Camera
                        {
                            Name = dsDevice.Name,
                            Path = "video=" + dsDevice.Name
                        };
                        result.Add(camera);
                    }
                }
            }
            else
            {
                AVInputFormat* avInputFormat = ffmpeg.av_find_input_format(inputFormat);
                AVDeviceInfoList* avDeviceInfoList = null;

                int nb = ffmpeg.avdevice_list_input_sources(avInputFormat, null, null, &avDeviceInfoList);
                if (nb >= 0)
                {
                    int nDevices = avDeviceInfoList->nb_devices;
                    var avDevices = avDeviceInfoList->devices;

                    result = new List<Camera>();
                    for (int i = 0; i < nDevices; i++)
                    {
                        var avDevice = avDevices[i];
                        var name = Marshal.PtrToStringAnsi((IntPtr)avDevice->device_description);
                        var path = Marshal.PtrToStringAnsi((IntPtr)avDevice->device_name);

                        if ((name != null) && (name.Length > 0))
                        {
                            Camera camera = new Camera
                            {
                                Name = name,
                                Path = path
                            };
                            result.Add(camera);
                        }
                    }

                    ffmpeg.avdevice_free_list_devices(&avDeviceInfoList);
                }
                else
                {
                    AVFormatContext* pFormatCtx = ffmpeg.avformat_alloc_context();
                    AVDictionary* options = null;
                    ffmpeg.av_dict_set(&options, "list_devices", "true", 0);

                    ffmpeg.av_log(null, (int)FfmpegLogLevelEnum.AV_LOG_TRACE, $"START - list_devices ENUM\r\n");

                    nb = ffmpeg.avformat_open_input(&pFormatCtx, null, avInputFormat, &options); // Here nb is < 0 ... But we have anyway an output from av_log which can be parsed ...
                    ffmpeg.avformat_close_input(&pFormatCtx);

                    ffmpeg.av_log(null, (int)FfmpegLogLevelEnum.AV_LOG_TRACE, $"END - list_devices ENUM\r\n");
                }
            }
            return result;
        }
    }

    public class Camera
    {
        public String Name { get; set; }

        public String Path { get; set; }
    }
}
