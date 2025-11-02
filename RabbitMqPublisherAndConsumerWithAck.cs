using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace YourNamespace;

public class RabbitMqPublisherAndConsumerWithAck
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly string _queueName;

    public RabbitMqPublisherAndConsumerWithAck(string hostName, string queueName)
    {
        _queueName = queueName;
        
        var factory = new ConnectionFactory { HostName = hostName };
        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();
        
        // Declare the queue
        _channel.QueueDeclare(
            queue: _queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    /// <summary>
    /// Publishes a message to the queue with publisher confirms (acknowledgment)
    /// </summary>
    public void PublishMessageWithAck(string message)
    {
        // Enable publisher confirms
        _channel.ConfirmSelect();

        var body = Encoding.UTF8.GetBytes(message);

        var properties = _channel.CreateBasicProperties();
        properties.Persistent = true; // Make message persistent

        _channel.BasicPublish(
            exchange: string.Empty,
            routingKey: _queueName,
            basicProperties: properties,
            body: body);

        // Wait for confirmation from the broker
        bool isConfirmed = _channel.WaitForConfirms(TimeSpan.FromSeconds(5));
        
        if (isConfirmed)
        {
            Console.WriteLine($"Message published and confirmed: {message}");
        }
        else
        {
            Console.WriteLine($"Message failed to confirm: {message}");
            throw new Exception("Message was not confirmed by the broker");
        }
    }

    /// <summary>
    /// Consumes messages from the queue with manual acknowledgment
    /// </summary>
    public void ConsumeMessageWithAck(Action<string> messageHandler)
    {
        // Disable auto-acknowledgment
        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        
        consumer.Received += (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                Console.WriteLine($"Message received: {message}");
                
                // Process the message
                messageHandler(message);
                
                // Acknowledge the message after successful processing
                _channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
                Console.WriteLine($"Message acknowledged: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing message: {ex.Message}");
                
                // Reject and requeue the message on failure
                _channel.BasicNack(deliveryTag: ea.DeliveryTag, multiple: false, requeue: true);
                Console.WriteLine($"Message rejected and requeued: {message}");
            }
        };

        _channel.BasicConsume(
            queue: _queueName,
            autoAck: false, // Manual acknowledgment
            consumer: consumer);

        Console.WriteLine("Waiting for messages. Press [enter] to exit.");
        Console.ReadLine();
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
    }
}