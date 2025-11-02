// See https://aka.ms/new-console-template for more information

#region

using ProducerAndConsumerActExample.Kafka;
using ProducerAndConsumerActExample.RabbitMQ;
using ProducerAndConsumerActExample.Redis;

#endregion


var redisConnectionString = "localhost:6379";

var rabbitConnectionString = "amqp://guest:guest@localhost:5672";

var kafkaConnectionString = "localhost:9092";


var redisPublisher = new RedisStreamPublisherAndConsumerWithAck(redisConnectionString, "redis-stream");

var rabbitMqPublisher = new RabbitMqPublisherAndConsumerWithAck(rabbitConnectionString, "rabbitmq-queue");

var kafka = new KafkaPublisherAndConsumerWithAck(kafkaConnectionString, "kafka-topic");


await redisPublisher.PublishMessageWithAck("Hello, Redis Stream with Ack!");
await rabbitMqPublisher.PublishMessageWithAck("Hello, RabbitMQ with Ack!");
await kafka.PublishMessageWithAck("Hello, Kafka with Ack!");


await redisPublisher.ConsumeMessageWithAck("redis-consumer-group", "redis-consumer",
    message => { Console.WriteLine($"Redis Stream Consumed Message: {message}"); });


await rabbitMqPublisher.ConsumeMessageWithAck(message =>
{
    Console.WriteLine($"RabbitMQ Consumed Message: {message}");
});

kafka.ConsumeMessageWithAck(message => { Console.WriteLine($"Kafka Consumed Message: {message}"); });