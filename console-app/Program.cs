using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TranscoderService.Console;
using TranscoderService.Console.Transcoder;

var factory = new ConnectionFactory()
{
    Uri = new Uri("amqp://user:password@rabbitmq:5672")
};

IConnection conn = await factory.CreateConnectionAsync();
using var channel = await conn.CreateChannelAsync();

string transcoderExchange = "transcoder";
var videoTranscoder = new VideoTranscoder();
var packager = new ShakaPackagerWrapper();

await channel.ExchangeDeclareAsync(exchange: transcoderExchange,
    type: ExchangeType.Fanout);

// declare a server-named queue
QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
string queueName = queueDeclareResult.QueueName;
await channel.QueueBindAsync(queue: queueName, exchange: transcoderExchange, routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for request.");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    var transcoderRequest = JsonSerializer.Deserialize<TranscodeRequest>(message);

    Console.WriteLine($"Transcoding Request Received : {transcoderRequest!.FilePath}");

    try
    {
        List<(string, FFMpegCore.Enums.VideoSize)> result = new();

        string fileName = Path.GetFileNameWithoutExtension(transcoderRequest!.FilePath);

        result.Add((videoTranscoder.Transcode(transcoderRequest!.FilePath, transcoderRequest!.TranscodedDirectory, FFMpegCore.Enums.VideoSize.Ld), FFMpegCore.Enums.VideoSize.Ld));
        result.Add((videoTranscoder.Transcode(transcoderRequest!.FilePath, transcoderRequest!.TranscodedDirectory, FFMpegCore.Enums.VideoSize.Hd), FFMpegCore.Enums.VideoSize.Hd));
        result.Add((videoTranscoder.Transcode(transcoderRequest!.FilePath, transcoderRequest!.TranscodedDirectory, FFMpegCore.Enums.VideoSize.FullHd), FFMpegCore.Enums.VideoSize.FullHd));

        Console.WriteLine($"Transcoding Request Completed");

        Console.WriteLine($"DRM Packaging Started");

        if (!Directory.Exists(Path.Combine(transcoderRequest!.TranscodedDirectory, fileName)))
            Directory.CreateDirectory(Path.Combine(transcoderRequest!.TranscodedDirectory, fileName));

        string arguments = @$"in={result[0].Item1},stream=audio,output={Path.Combine(transcoderRequest!.TranscodedDirectory, fileName)}/audio.mp4,drm_label=AUDIO ";
        foreach (var res in result)
        {
            switch (res.Item2)
            {
                case FFMpegCore.Enums.VideoSize.Ld:
                    arguments += @$"in={res.Item1},stream=video,output={Path.Combine(transcoderRequest!.TranscodedDirectory, fileName)}/h264_360p.mp4,drm_label=SD  ";
                    break;
                case FFMpegCore.Enums.VideoSize.Hd:
                    arguments += @$"in={res.Item1},stream=video,output={Path.Combine(transcoderRequest!.TranscodedDirectory, fileName)}/h264_720p.mp4,drm_label=HD ";
                    break;
                case FFMpegCore.Enums.VideoSize.FullHd:
                    arguments += @$"in={res.Item1},stream=video,output={Path.Combine(transcoderRequest!.TranscodedDirectory, fileName)}/h264_1080p.mp4,drm_label=HD ";
                    break;
            }
        }

        arguments += @$"--enable_raw_key_encryption ";
        arguments += @$"--keys label=AUDIO:key_id=43feb387fb6e44edb9dc67fb6dbfaabb:key=0a9ca9cf0d7199dc844c0a786390aed5,label=SD:key_id=43feb387fb6e44edb9dc67fb6dbfaabb:key=0a9ca9cf0d7199dc844c0a786390aed5,label=HD:key_id=43feb387fb6e44edb9dc67fb6dbfaabb:key=0a9ca9cf0d7199dc844c0a786390aed5 ";
        arguments += @$"--protection_systems Widevine ";
        arguments += @$"--mpd_output {Path.Combine(transcoderRequest!.TranscodedDirectory, fileName)}/h264.mpd";

        string shakaPackagerResult = packager.Execute(arguments);

        Console.WriteLine($"Processing DRM Completed");

    }
    catch (Exception ex)
    {
        Console.WriteLine($"Transcoding Request Error : {ex.Message}");
    }

    await Task.CompletedTask;
};

await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

// Keep the connection alive
while (conn.IsOpen)
{
    await Task.Delay(1000);
}

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
