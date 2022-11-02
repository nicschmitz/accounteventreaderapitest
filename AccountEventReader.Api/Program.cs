using AccountEventReader.Api;
using AccountEventReader.Api.Services.Mapping;
using Apache.NMS;
using Apache.NMS.ActiveMQ;
using Bogus;
using System.Text.Json;

internal class Program
{
    private static MessageMapperFactory _messageMapperFactory;
    private static IMessageProducer _secondaryProducer;
    private static ISession _session;
    private static IConnection _connection;
    private static JsonSerializerOptions _defaultSerializerOptions = new JsonSerializerOptions { WriteIndented = false, DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };

    static async Task Main(string[] args)
    {
        _messageMapperFactory = new MessageMapperFactory();

        var correlationId = Guid.NewGuid().ToString();

        var cisMessageFaker = new Faker<CisMessage>()
            .RuleFor(x => x.EventType, x => x.PickRandom(new List<string>() { "NEWBILL", "BUDSTOP", "BUDSTOPCC" }))
            .RuleFor(x => x.AccountId, x => x.Random.Long())
            .RuleFor(x => x.NotificationEventId, x => x.Random.Long().ToString());        

        var brokerUri = new Uri("activemq:tcp://192.168.1.208:61616");
        var factory = new ConnectionFactory(brokerUri);

        _connection = await factory.CreateConnectionAsync();
        await _connection.StartAsync();
        
        _session = await _connection.CreateSessionAsync(Apache.NMS.AcknowledgementMode.AutoAcknowledge);
        
        var destination = await _session.GetQueueAsync("testqueue");
        var secondaryDestination = await _session.GetQueueAsync("testoutput");

        var producer = await _session.CreateProducerAsync(destination);        
        producer.DeliveryMode = Apache.NMS.MsgDeliveryMode.Persistent;

        var consumer = await _session.CreateConsumerAsync(destination);
        consumer.Listener += Consumer_Listener;

        _secondaryProducer = await _session.CreateProducerAsync(secondaryDestination);
        _secondaryProducer.DeliveryMode = MsgDeliveryMode.Persistent;
      
        Console.WriteLine("Press ESC to stop");
        do
        {
            while (!Console.KeyAvailable)
            {
                var cisMessage = cisMessageFaker.Generate();
                var serializedCisMessage = JsonSerializer.Serialize(cisMessage, _defaultSerializerOptions);
                var message = await _session.CreateTextMessageAsync(serializedCisMessage);
                await producer.SendAsync(message);                
            }
        } while (Console.ReadKey(true).Key != ConsoleKey.Escape);
                
        await _session.CloseAsync();
        await _connection.CloseAsync();
    }
  
    private static async void Consumer_Listener(Apache.NMS.IMessage message)
    {
        var txtMessage = message as ITextMessage;
        var thing = JsonSerializer.Deserialize<CisMessage>(txtMessage.Text);

        Console.WriteLine($"Received event type: {thing.EventType}");
        var mapper = _messageMapperFactory.Create(thing.EventType);

        var mappedThing = mapper.Map(thing);
        Console.WriteLine($"Mapped to thing: {mappedThing.EventType}");

        var serializedMappedThing = JsonSerializer.Serialize(mappedThing, _defaultSerializerOptions);
        var outboundMessage = await _session.CreateTextMessageAsync(serializedMappedThing);
        await _secondaryProducer.SendAsync(outboundMessage);
    }
}