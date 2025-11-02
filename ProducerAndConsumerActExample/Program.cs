// See https://aka.ms/new-console-template for more information

#region

using ProducerAndConsumerActExample.Kafka;
using ProducerAndConsumerActExample.RabbitMQ;
using ProducerAndConsumerActExample.Redis;

#endregion

Console.WriteLine("Hello, World!");

var redisConnectionString = "localhost:6379";

var rabbitConnectionString = "amqp://guest:guest@localhost:5672";

var kafkaConnectionString = "localhost:9092";


var redisPublisher = new RedisStreamPublisherAndConsumerWithAck(redisConnectionString, "redis-stream");

var rabbitMqPublisher = new RabbitMqPublisherAndConsumerWithAck(rabbitConnectionString, "rabbitmq-queue");

var kafka = new KafkaPublisherAndConsumerWithAck(kafkaConnectionString, "kafka-topic");


await redisPublisher.PublishMessageWithAck("Hello, Redis Stream with Ack!");

await rabbitMqPublisher.PublishMessageWithAck("Hello, RabbitMQ with Ack!");
await kafka.PublishMessageWithAck("Hello, Kafka with Ack!");