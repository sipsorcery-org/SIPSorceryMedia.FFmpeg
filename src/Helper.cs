using FFmpeg.AutoGen;
using SIPSorceryMedia.Abstractions;
using System;
using System.Collections.Generic;
using System.Text;

namespace SIPSorceryMedia.FFmpeg
{
    public class Helper
    {
        public const int MIN_SLEEP_MILLISECONDS = 15;
        public const int DEFAULT_VIDEO_FRAME_RATE = 30;

        public const int VP8_FORMATID = 96;
        public const int H264_FORMATID = 100;

        internal static List<VideoFormat> GetSupportedVideoFormats() => new List<VideoFormat>
        {
            new VideoFormat(VideoCodecsEnum.VP8, VP8_FORMATID, VideoFormat.DEFAULT_CLOCK_RATE),
            new VideoFormat(VideoCodecsEnum.H264, H264_FORMATID, VideoFormat.DEFAULT_CLOCK_RATE)
        };

        public static string GetH264ProfileName(string profileHex)
        {
            if (string.IsNullOrEmpty(profileHex))
                return "";

            // Normalize case
            profileHex = profileHex.ToUpperInvariant();

            // Map hex profile to FFmpeg profile names
            return profileHex switch
            {
                "42" => "baseline",
                "4D" => "main",
                "58" => "extended",
                "64" => "high",
                _ => "" // Unknown profile
            };
        }

        public static string GetH264LevelValue(string levelHex)
        {
            if (string.IsNullOrEmpty(levelHex))
                return "";

            // Normalize case
            levelHex = levelHex.ToUpperInvariant();

            // Map hex level to FFmpeg level string
            return levelHex switch
            {
                "0A" => "1.0",
                "0B" => "1.1",
                "0C" => "1.2",
                "0D" => "1.3",
                "14" => "2.0",
                "15" => "2.1",
                "16" => "2.2",
                "1E" => "3.0",
                "1F" => "3.1",
                "20" => "3.2",
                "28" => "4.0",
                "29" => "4.1",
                _ => "" // Unknown level
            };
        }
        public static Dictionary<string, string> ParseWebRtcParameters(string input)
        {
            var parameters = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(input))
                return parameters;

            foreach (var pair in input.Split(';'))
            {
                var keyValue = pair.Split('=');
                if (keyValue.Length == 2)
                {
                    parameters[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            return parameters;
        }
    }
}
