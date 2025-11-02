#region

using StackExchange.Redis;

#endregion

namespace ProducerAndConsumerActExample.Redis;

public class RedisStreamPublisherAndConsumerWithAck
{
    private readonly IDatabase _database;
    private readonly IConnectionMultiplexer _redis;
    private readonly string _streamName;

    public RedisStreamPublisherAndConsumerWithAck(string connectionString, string streamName)
    {
        _streamName = streamName;
        _redis = ConnectionMultiplexer.Connect(connectionString);
        _database = _redis.GetDatabase();
    }

    /// <summary>
    ///     Publishes a message to Redis Stream with acknowledgment verification
    /// </summary>
    public async Task<bool> PublishMessageWithAck(string message)
    {
        Console.WriteLine("------------------------------------------------------------");
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

            // messageId kontrolü - RedisValue.Null değilse başarılı
            if (messageId.IsNull)
            {
                Console.WriteLine("❌ Message was not added to stream");
                Console.WriteLine("------------------------------------------------------------");
                return false;
            }

            Console.WriteLine($"✅ Message published to stream '{_streamName}'");
            Console.WriteLine($"   Message ID: {messageId}");
            Console.WriteLine($"   Content: {message}");

            // EKSTRA: Mesajın gerçekten yazıldığını doğrula
            var verificationResult = await VerifyMessagePublished(messageId);

            if (verificationResult)
                Console.WriteLine($"✅ Message verified in stream: {messageId}");
            else
                Console.WriteLine($"⚠️ Message verification failed: {messageId}");

            Console.WriteLine("------------------------------------------------------------");
            return verificationResult;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to publish message: {ex.Message}");
            Console.WriteLine("------------------------------------------------------------");
            throw;
        }
    }

    /// <summary>
    ///     Mesajın stream'de olduğunu doğrula
    /// </summary>
    private async Task<bool> VerifyMessagePublished(RedisValue messageId)
    {
        try
        {
            // Mesajın stream'de olup olmadığını kontrol et
            var entries = await _database.StreamRangeAsync(
                _streamName,
                messageId,
                messageId,
                1
            );

            return entries.Length > 0;
        }
        catch
        {
            return false;
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
            // Consumer group oluştur (zaten varsa hata vermez)
            try
            {
                await _database.StreamCreateConsumerGroupAsync(
                    _streamName,
                    consumerGroup,
                    StreamPosition.NewMessages
                );
                Console.WriteLine($"✅ Consumer group '{consumerGroup}' created");
            }
            catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP"))
            {
                Console.WriteLine($"ℹ️ Consumer group '{consumerGroup}' already exists");
            }

            Console.WriteLine($"📥 Consumer '{consumerName}' started listening on stream '{_streamName}'");
            Console.WriteLine($"   Group: {consumerGroup}");

            while (!cancellationToken.IsCancellationRequested)
                try
                {
                    // Pending mesajları kontrol et (daha önce alınmış ama ACK edilmemiş)
                    var pendingMessages = await _database.StreamReadGroupAsync(
                        _streamName,
                        consumerGroup,
                        consumerName,
                        "0", // Pending mesajları oku
                        10
                    );

                    if (pendingMessages.Length > 0)
                    {
                        Console.WriteLine($"⚠️ Found {pendingMessages.Length} pending (unacknowledged) messages");
                        await ProcessMessages(pendingMessages, consumerGroup, messageHandler);
                    }

                    // Yeni mesajları oku
                    var newMessages = await _database.StreamReadGroupAsync(
                        _streamName,
                        consumerGroup,
                        consumerName,
                        ">", // Sadece yeni mesajlar
                        10
                    );

                    if (newMessages.Length > 0)
                        await ProcessMessages(newMessages, consumerGroup, messageHandler);
                    else
                        // Yeni mesaj yoksa kısa bir süre bekle
                        await Task.Delay(100, cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Error reading from stream: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
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
    ///     Mesajları işle ve acknowledge et
    /// </summary>
    private async Task ProcessMessages(
        StreamEntry[] messages,
        string consumerGroup,
        Action<string> messageHandler)
    {
        foreach (var entry in messages)
        {
            Console.WriteLine("------------------------------------------------------------");
            var messageId = entry.Id;
            var values = entry.Values;
            var messageContent = values.FirstOrDefault(v => v.Name == "message").Value.ToString();

            try
            {
                Console.WriteLine($"📨 Message received from stream '{_streamName}'");
                Console.WriteLine($"   Message ID: {messageId}");
                Console.WriteLine($"   Content: {messageContent}");

                // Mesajı işle
                messageHandler(messageContent);

                // Acknowledge (ACK) - Mesaj başarıyla işlendi
                var ackedCount = await _database.StreamAcknowledgeAsync(
                    _streamName,
                    consumerGroup,
                    messageId
                );

                if (ackedCount > 0)
                    Console.WriteLine($"✅ Message acknowledged: {messageContent}");

                Console.WriteLine("------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error processing message: {ex.Message}");
                Console.WriteLine($"⏸️ Message NOT acknowledged (ID: {messageId}), will be reprocessed");
                Console.WriteLine("------------------------------------------------------------");

                // ACK yapılmadığı için mesaj pending listesinde kalır
                // Bir sonraki okumada tekrar işlenecek
            }
        }
    }

    /// <summary>
    ///     Pending (ACK edilmemiş) mesajları kontrol eder
    /// </summary>
    public async Task<StreamPendingInfo> GetPendingInfo(string consumerGroup)
    {
        var pendingInfo = await _database.StreamPendingAsync(_streamName, consumerGroup);

        Console.WriteLine("📊 Pending Messages Info:");
        Console.WriteLine($"   Total Pending: {pendingInfo.PendingMessageCount}");
        Console.WriteLine($"   Consumer Count: {pendingInfo.Consumers.Length}");

        foreach (var consumer in pendingInfo.Consumers)
            Console.WriteLine($"   - {consumer.Name}: {consumer.PendingMessageCount} pending");

        return pendingInfo;
    }

    /// <summary>
    ///     Belirli bir mesajı yeniden talep et (claim) - başka bir consumer'a ata
    /// </summary>
    public async Task ClaimPendingMessage(string consumerGroup, string consumerName, RedisValue messageId)
    {
        try
        {
            var claimedMessages = await _database.StreamClaimAsync(
                _streamName,
                consumerGroup,
                consumerName,
                5000, // 5 saniyeden uzun süredir işlenmemiş mesajlar
                new[] { messageId }
            );

            Console.WriteLine($"🔄 Claimed {claimedMessages.Length} messages for consumer '{consumerName}'");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error claiming message: {ex.Message}");
        }
    }

    /// <summary>
    ///     Stream bilgilerini göster
    /// </summary>
    public async Task<StreamInfo> GetStreamInfo()
    {
        var streamInfo = await _database.StreamInfoAsync(_streamName);

        Console.WriteLine("📊 Stream Info:");
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