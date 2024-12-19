using FFMpegCore;
using FFMpegCore.Arguments;
using FFMpegCore.Enums;

namespace TranscoderService.Console.Transcoder
{
    public class VideoTranscoder
    {
        /*
        ffmpeg -i original.mp4 -c:a copy \
            -vf "scale=-2:360" \
            -c:v libx264 -profile:v baseline -level:v 3.0 \
            -x264-params scenecut=0:open_gop=0:min-keyint=72:keyint=72 \
            -minrate 600k -maxrate 600k -bufsize 600k -b:v 600k \
            -y h264_baseline_360p_600.mp4
        */
        public string Transcode(string videoPath, string outputPath, VideoSize videoSize)
        {
            string fileName = Path.GetFileNameWithoutExtension(videoPath);
            string ext = Path.GetExtension(videoPath);
            string fileOutputResult = Path.Combine(outputPath, fileName + "_h264_" + videoSize.ToString() + ext);
            System.Console.WriteLine($"Transcoding file started. file : {videoPath}");
            FFMpegArguments
                .FromFileInput(videoPath)
                .OutputToFile(fileOutputResult, false, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithVideoFilters(filterOptions => filterOptions.Scale(videoSize))
                    .WithConstantRateFactor(23) // Consider using CRF instead of bitrate for better quality
                    .WithVariableBitrate(3) // 600kbps                    
                    .WithVideoBitrate(600000)
                    .WithCustomArgument("-x264-params")
                    .WithCustomArgument("scenecut=0:open_gop=0:min-keyint=72:keyint=72")
                    .WithFastStart())
                .ProcessSynchronously();

            System.Console.WriteLine($"Transcoding file completed: {fileOutputResult}");
            System.Console.WriteLine("");

            return fileOutputResult;
        }
    }
}