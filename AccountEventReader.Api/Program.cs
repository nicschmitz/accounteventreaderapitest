using AccountEventReader.Api;
using AccountEventReader.Api.Services.Mapping;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Bogus;
using System.Text.Json;

internal class Program
{
    public static MessageMapperFactory _messageMapperFactory;
    public static IMessageProducer _secondaryProducer;
    public static ISession session;
    public static JsonSerializerOptions defaultOptions = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    private static void Main(string[] args)
    {
        _messageMapperFactory = new MessageMapperFactory();

        var correlationId = Guid.NewGuid().ToString();

        var cisMessageFaker = new Faker<CisMessage>()
            .RuleFor(x => x.EventType, x => x.PickRandom(new List<string>() { "NEWBILL", "BUDSTOP", "BUDSTOPCC" }))
            .RuleFor(x => x.AccountId, x => x.Random.Long())
            .RuleFor(x => x.NotificationEventId, x => x.Random.Long().ToString());

        var options = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

        var brokerUri = new Uri("activemq:tcp://192.168.1.208:61616");
        var factory = new ConnectionFactory(brokerUri);

        var connection = factory.CreateConnection();
        connection.Start();

        session = connection.CreateSession(Apache.NMS.AcknowledgementMode.AutoAcknowledge);

        var destination = session.GetQueue("testqueue");

        var secondaryDestination = session.GetQueue("testoutput");

        var producer = session.CreateProducer(destination);
        producer.DeliveryMode = Apache.NMS.MsgDeliveryMode.Persistent;

        var consumer = session.CreateConsumer(destination);
        consumer.Listener += Consumer_Listener;

        _secondaryProducer = session.CreateProducer(secondaryDestination);
        _secondaryProducer.DeliveryMode = MsgDeliveryMode.Persistent;

        while (true)
        {
            var cisMessage = cisMessageFaker.Generate();

            var serializedThing = JsonSerializer.Serialize(cisMessage, options);
            producer.Send(session.CreateTextMessage(serializedThing));
        }

        session.Close();
        connection.Close();
    }

    private static void Consumer_Listener(Apache.NMS.IMessage message)
    {
        var txtMessage = message as ITextMessage;
        var thing = JsonSerializer.Deserialize<CisMessage>(txtMessage.Text);

        Console.WriteLine($"{thing.EventType}");
        var mapper = _messageMapperFactory.Create(thing.EventType);

        var mappedThing = mapper.Map(thing);
        Console.WriteLine($"{mappedThing.EventType}");

        var serializedMappedThing = JsonSerializer.Serialize(mappedThing, defaultOptions);

        _secondaryProducer.Send(session.CreateTextMessage(serializedMappedThing));
    }
}