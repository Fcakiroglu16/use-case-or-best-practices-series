#region

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.RateLimiting;

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
            true, // publisherConfirmationsEnabled: Publisher confirmations (onaylar) aktif. 
                  // Mesajların broker tarafından alındığını doğrular. 
                  // True olduğunda, yayınlanan her mesaj için broker'dan onay bekler.
            true, // publisherConfirmationTrackingEnabled: Publisher confirmation takibi aktif.
                  // Gönderilen mesajların hangi sırada onaylandığını izler.
                  // Bu sayede hangi mesajların başarılı/başarısız olduğunu takip edebilirsiniz.
            new ThrottlingRateLimiter(100) // ConsumerDispatchConcurrency: Eş zamanlı mesaj işleme sınırı.
                                           // Saniyede maksimum 100 mesajın işlenmesine izin verir.
                                           // Bu, sistemin aşırı yüklenmesini önler ve performansı dengeler.
        );

        var channelOpts2 = new CreateChannelOptions(true, true, new FixedWindowRateLimiter(
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10, // 1 saniyede 10 mesaj
                Window = TimeSpan.FromSeconds(1), // 1 saniyelik pencere
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 20 // 20 mesaj kuyrukta bekleyebilir
            }));

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
        Console.WriteLine("------------------------------------------------------------");
        try
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


            Console.WriteLine($"✅ Message published to queue '{_queueName}'");
            Console.WriteLine($"   Content: {message}");
            Console.WriteLine("✅ Message confirmed by broker");
            Console.WriteLine("------------------------------------------------------------");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to publish message: {ex.Message}");
            Console.WriteLine("------------------------------------------------------------");
            throw;
        }
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
            Console.WriteLine("------------------------------------------------------------");
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                Console.WriteLine($"📨 Message received from queue '{_queueName}'");
                Console.WriteLine($"   Delivery Tag: {ea.DeliveryTag}");
                Console.WriteLine($"   Content: {message}");

                messageHandler(message);

                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                Console.WriteLine($"✅ Message acknowledged: {message}");
                Console.WriteLine("------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing message: {ex.Message}");

                // Reject and requeue the message on failure
                await _channel.BasicNackAsync(ea.DeliveryTag, false, true);
                Console.WriteLine($"⏸️ Message rejected and requeued: {message}");
                Console.WriteLine("------------------------------------------------------------");
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