#region

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

#endregion

namespace ProducerAndConsumerActExample.RabbitMQ;

public class RabbitMqPublisherAndConsumerWithAck
{
    private readonly IChannel _channel;
    private readonly string _queueName;

    public RabbitMqPublisherAndConsumerWithAck(string hostName, string queueName)
    {
        _queueName = queueName;

        var factory = new ConnectionFactory { Uri = new Uri(hostName) };
        var connection = factory.CreateConnectionAsync().Result;
        var channelOpts = new CreateChannelOptions(
            true,
            true,
            new ThrottlingRateLimiter(100)
        );

        _channel = connection.CreateChannelAsync(channelOpts).Result;

        // Declare the queue
        _channel.QueueDeclareAsync(
            _queueName,
            true,
            false,
            false).Wait();
    }

    /// <summary>
    ///     Publishes a message to the queue with publisher confirms (acknowledgment)
    /// </summary>
    public async Task PublishMessageWithAck(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        var properties = new BasicProperties
        {
            Persistent = true // Make message persistent
        };

        await _channel.BasicPublishAsync(
            string.Empty,
            _queueName,
            false,
            properties,
            body
        );
    }

    /// <summary>
    ///     Consumes messages from the queue with manual acknowledgment
    /// </summary>
    public async Task ConsumeMessageWithAck(Action<string> messageHandler)
    {
        // Disable auto-acknowledgment
        await _channel.BasicQosAsync(0, 1, false);

        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (_, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                Console.WriteLine($"Message received: {message}");


                messageHandler(message);


                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                Console.WriteLine($"Message acknowledged: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");

                // Reject and requeue the message on failure
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                Console.WriteLine($"Message rejected and requeued: {message}");
            }
        };

        await _channel.BasicConsumeAsync(
            _queueName,
            false, // Manual acknowledgment
            consumer);

        Console.WriteLine("Waiting for messages. Press [enter] to exit.");
        Console.ReadLine();
    }
}