#region

using Confluent.Kafka;

#endregion

namespace ProducerAndConsumerActExample.Kafka;

public class KafkaPublisherAndConsumerWithAck(string bootstrapServers, string topicName)
{
    /// <summary>
    ///     Publishes a message to Kafka topic with acknowledgment
    /// </summary>
    public async Task PublishMessageWithAck(string message)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,

            // Acknowledgment ayarları
            Acks = Acks.All, // Tüm in-sync replicas'lardan onay bekle (en güvenli)
            EnableIdempotence = true, // Tekrar eden mesajları önle
            MaxInFlight = 5, // Aynı anda maksimum 5 istek

            // Retry ayarları
            MessageSendMaxRetries = 3, // Başarısız olursa 3 kez tekrar dene
            RetryBackoffMs = 100 // Retry'lar arası 100ms bekle
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        try
        {
            var deliveryResult = await producer.ProduceAsync(topicName, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(), // Mesaj anahtarı
                Value = message,
                Timestamp = Timestamp.Default
            });

            Console.WriteLine($"Message delivered to {deliveryResult.TopicPartitionOffset}");
            Console.WriteLine($"Partition: {deliveryResult.Partition.Value}, Offset: {deliveryResult.Offset.Value}");
            Console.WriteLine($"Status: {deliveryResult.Status}");
        }
        catch (ProduceException<string, string> ex)
        {
            Console.WriteLine($"Delivery failed: {ex.Error.Reason}");
            throw;
        }
    }

    /// <summary>
    ///     Consumes messages from Kafka topic with manual acknowledgment
    /// </summary>
    public void ConsumeMessageWithAck(Action<string> messageHandler, CancellationToken cancellationToken = default)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = "consumer-group-1", // Consumer group ID

            // Acknowledgment ayarları
            EnableAutoCommit = false, // Manuel commit (acknowledgment)
            AutoOffsetReset = AutoOffsetReset.Earliest // İlk mesajdan başla
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();

        consumer.Subscribe(topicName);
        Console.WriteLine($"Subscribed to topic: {topicName}");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
                try
                {
                    // Mesaj çek (1 saniyelik timeout)
                    var consumeResult = consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult == null)
                        continue;

                    var message = consumeResult.Message.Value;

                    try
                    {
                        Console.WriteLine(
                            $"Message received from partition {consumeResult.Partition.Value}, offset {consumeResult.Offset.Value}");
                        Console.WriteLine($"Key: {consumeResult.Message.Key}, Value: {message}");

                        // Mesajı işle
                        messageHandler(message);

                        // Manuel commit (acknowledgment)
                        consumer.Commit(consumeResult);
                        Console.WriteLine($"Message committed: {message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing message: {ex.Message}");

                        // Hata durumunda commit yapma, mesaj tekrar işlenecek
                        Console.WriteLine($"Message not committed, will be reprocessed: {message}");
                    }
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Consume error: {ex.Error.Reason}");
                }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Consumer cancelled");
        }
        finally
        {
            consumer.Close();
            Console.WriteLine("Consumer closed");
        }
    }
}