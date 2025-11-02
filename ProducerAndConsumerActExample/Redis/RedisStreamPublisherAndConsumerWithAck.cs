#region

using StackExchange.Redis;
using System.Text.Json;

#endregion

namespace ProducerAndConsumerActExample.Redis;

public class RedisStreamPublisherAndConsumerWithAck
{
    private readonly IDatabase _database;
    private readonly string _streamName;
    private readonly IConnectionMultiplexer _redis;

    public RedisStreamPublisherAndConsumerWithAck(string connectionString, string streamName)
    {
        _streamName = streamName;
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _database = _redis.GetDatabase();
    }

    /// <summary>
    ///     Publishes a message to Redis Stream with acknowledgment
    /// </summary>
    public async Task PublishMessageWithAck(string message)
    {
        try
        {
            // Redis Stream'e mesaj ekle
            var messageId = await _database.StreamAddAsync(
                _streamName,
                new NameValueEntry[]
                {
                    new("message", message),
                    new("timestamp", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()),
                    new("id", Guid.NewGuid().ToString())
                }
            );

            Console.WriteLine($"? Message published to stream '{_streamName}'");
            Console.WriteLine($"   Message ID: {messageId}");
            Console.WriteLine($"   Content: {message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Failed to publish message: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     Consumes messages from Redis Stream with manual acknowledgment
    /// </summary>
    public async Task ConsumeMessageWithAck(
        string consumerGroup,
        string consumerName,
        Action<string> messageHandler,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Consumer group olu?tur (zaten varsa hata vermez)
            try
            {
                await _database.StreamCreateConsumerGroupAsync(
                    _streamName,
                    consumerGroup,
                    StreamPosition.NewMessages
                );
                Console.WriteLine($"? Consumer group '{consumerGroup}' created");
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                Console.WriteLine($"?? Consumer group '{consumerGroup}' already exists");
            }

            Console.WriteLine($"?? Consumer '{consumerName}' started listening on stream '{_streamName}'");
            Console.WriteLine($"   Group: {consumerGroup}");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Pending mesajlar? kontrol et (daha önce al?nm?? ama ACK edilmemi?)
                    var pendingMessages = await _database.StreamReadGroupAsync(
                        _streamName,
                        consumerGroup,
                        consumerName,
                        "0", // Pending mesajlar? oku
                        count: 10
                    );

                    if (pendingMessages.Length > 0)
                    {
                        Console.WriteLine($"?? Found {pendingMessages.Length} pending (unacknowledged) messages");
                        await ProcessMessages(pendingMessages, consumerGroup, messageHandler);
                    }

                    // Yeni mesajlar? oku
                    var newMessages = await _database.StreamReadGroupAsync(
                        _streamName,
                        consumerGroup,
                        consumerName,
                        ">", // Sadece yeni mesajlar
                        count: 10
                    );

                    if (newMessages.Length > 0)
                    {
                        await ProcessMessages(newMessages, consumerGroup, messageHandler);
                    }
                    else
                    {
                        // Yeni mesaj yoksa k?sa bir süre bekle
                        await Task.Delay(100, cancellationToken);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"? Error reading from stream: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Consumer cancelled");
        }
        finally
        {
            Console.WriteLine("Consumer stopped");
        }
    }

    /// <summary>
    ///     Mesajlar? i?le ve acknowledge et
    /// </summary>
    private async Task ProcessMessages(
        StreamEntry[] messages,
        string consumerGroup,
        Action<string> messageHandler)
    {
        foreach (var entry in messages)
        {
            var messageId = entry.Id;
            var values = entry.Values;
            var messageContent = values.FirstOrDefault(v => v.Name == "message").Value.ToString();

            try
            {
                Console.WriteLine($"?? Message received:");
                Console.WriteLine($"   ID: {messageId}");
                Console.WriteLine($"   Content: {messageContent}");

                // Mesaj? i?le
                messageHandler(messageContent);

                // Acknowledge (ACK) - Mesaj ba?ar?yla i?lendi
                var ackedCount = await _database.StreamAcknowledgeAsync(
                    _streamName,
                    consumerGroup,
                    messageId
                );

                if (ackedCount > 0)
                {
                    Console.WriteLine($"? Message acknowledged: {messageContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error processing message: {ex.Message}");
                Console.WriteLine($"?? Message NOT acknowledged (ID: {messageId}), will be reprocessed");

                // ACK yap?lmad??? için mesaj pending listesinde kal?r
                // Bir sonraki okumada tekrar i?lenecek
            }
        }
    }

    /// <summary>
    ///     Pending (ACK edilmemi?) mesajlar? kontrol eder
    /// </summary>
    public async Task<StreamPendingInfo> GetPendingInfo(string consumerGroup)
    {
        var pendingInfo = await _database.StreamPendingAsync(_streamName, consumerGroup);
        
        Console.WriteLine($"?? Pending Messages Info:");
        Console.WriteLine($"   Total Pending: {pendingInfo.PendingMessageCount}");
        Console.WriteLine($"   Consumer Count: {pendingInfo.Consumers.Length}");

        foreach (var consumer in pendingInfo.Consumers)
        {
            Console.WriteLine($"   - {consumer.Name}: {consumer.PendingMessageCount} pending");
        }

        return pendingInfo;
    }

    /// <summary>
    ///     Belirli bir mesaj? yeniden talep et (claim) - ba?ka bir consumer'a ata
    /// </summary>
    public async Task ClaimPendingMessage(string consumerGroup, string consumerName, RedisValue messageId)
    {
        try
        {
            var claimedMessages = await _database.StreamClaimAsync(
                _streamName,
                consumerGroup,
                consumerName,
                minIdleTimeInMs: 5000, // 5 saniyeden uzun süredir i?lenmemi? mesajlar
                messageIds: new[] { messageId }
            );

            Console.WriteLine($"?? Claimed {claimedMessages.Length} messages for consumer '{consumerName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"? Error claiming message: {ex.Message}");
        }
    }

    /// <summary>
    ///     Stream bilgilerini göster
    /// </summary>
    public async Task<StreamInfo> GetStreamInfo()
    {
        var streamInfo = await _database.StreamInfoAsync(_streamName);
        
        Console.WriteLine($"?? Stream Info:");
        Console.WriteLine($"   Name: {_streamName}");
        Console.WriteLine($"   Length: {streamInfo.Length}");
        Console.WriteLine($"   First Entry ID: {streamInfo.FirstEntry.Id}");
        Console.WriteLine($"   Last Entry ID: {streamInfo.LastEntry.Id}");
        Console.WriteLine($"   Consumer Groups: {streamInfo.ConsumerGroupCount}");

        return streamInfo;
    }

    public void Dispose()
    {
        _redis?.Dispose();
    }
}