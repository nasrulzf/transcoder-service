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

    switch (transcoderRequest.Definition)
    {
        case "LD":
            videoTranscoder.Transcode(transcoderRequest!.FilePath, transcoderRequest!.TranscodedDirectory, FFMpegCore.Enums.VideoSize.Ld);
            break;
        case "HD":
            videoTranscoder.Transcode(transcoderRequest!.FilePath, transcoderRequest!.TranscodedDirectory, FFMpegCore.Enums.VideoSize.Hd);
            break;
        case "FHD":
            videoTranscoder.Transcode(transcoderRequest!.FilePath, transcoderRequest!.TranscodedDirectory, FFMpegCore.Enums.VideoSize.FullHd);
            break;
    }

    await channel.BasicAckAsync(ea.DeliveryTag, false);
};

await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

// Keep the connection alive
while (conn.IsOpen)
{
    await Task.Delay(1000);
}

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
