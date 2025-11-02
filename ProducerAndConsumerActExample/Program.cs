// See https://aka.ms/new-console-template for more information

#region

using ProducerAndConsumerActExample.RabbitMQ;

#endregion

Console.WriteLine("Hello, World!");

var connectionString = "amqps://ffqycmjd:cYOzydQzAx6gSvtbPdF4zU5T-2r4c-UR@gorilla.lmq.cloudamqp.com/ffqycmjd";

var queueName = "producer_consumer_ack_queue";

var publisherAndConsumer = new RabbitMqPublisherAndConsumerWithAck(connectionString, queueName);

// Publish messages
for (var i = 0; i < 10; i++)
{
    var message = $"Message {i + 1}";
    await publisherAndConsumer.PublishMessageWithAck(message);
    Console.WriteLine($"Published: {message}");
}

// Start consuming messages
await publisherAndConsumer.ConsumeMessageWithAck(message => { Console.WriteLine($"Consumed: {message}"); });