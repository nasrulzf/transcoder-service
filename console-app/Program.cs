using System.Text;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using TranscoderService.Console.Transcoder;

var factory = new ConnectionFactory()
{
    Uri = new Uri("amqp://user:password@rabbitmq:5672/")
};

IConnection conn = await factory.CreateConnectionAsync();
using var channel = await conn.CreateChannelAsync();

await channel.ExchangeDeclareAsync(exchange: "logs",
    type: ExchangeType.Fanout);

// declare a server-named queue
QueueDeclareOk queueDeclareResult = await channel.QueueDeclareAsync();
string queueName = queueDeclareResult.QueueName;
await channel.QueueBindAsync(queue: queueName, exchange: "logs", routingKey: string.Empty);

Console.WriteLine(" [*] Waiting for logs.");

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += (model, ea) =>
{
    byte[] body = ea.Body.ToArray();
    var message = Encoding.UTF8.GetString(body);
    Console.WriteLine($" [x] {message}");

    new VideoTranscoder().Transcode(args[0], args[1], FFMpegCore.Enums.VideoSize.Ld);

    return Task.CompletedTask;
};

await channel.BasicConsumeAsync(queueName, autoAck: true, consumer: consumer);

Console.WriteLine(" Press [enter] to exit.");
Console.ReadLine();
