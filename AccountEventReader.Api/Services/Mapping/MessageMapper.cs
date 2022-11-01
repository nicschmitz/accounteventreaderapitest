
namespace AccountEventReader.Api.Services.Mapping
{
    public interface IMessageMapper
    {
        ProactiveCommsMessage Map(CisMessage inputMessage); 
    }

    public class MessageMapperFactory
    {
        private Dictionary<string, Type> _mappers;

        public MessageMapperFactory()
        {
            _mappers = new Dictionary<string, Type>();
            Register("NEWBILL", typeof(NewBillMapper));
            Register("BUDSTOP", typeof(BudStopMapper));
            Register("BUDSTOPCC", typeof(BudStopCCMapper));
        }

        private void Register(string eventType, Type mapperType)
        {
            _mappers.Add(eventType, mapperType);
        }

        public IMessageMapper Create(string eventType)
        {
            var instance = Activator.CreateInstance(_mappers[eventType]);
            if (instance is null)
                throw new Exception($"No mapping handler found for event type: {eventType}");

            return (IMessageMapper)instance;
        }
    }

    public class NewBillMapper : IMessageMapper
    {
        public ProactiveCommsMessage Map(CisMessage inputMessage)
        {
            return new ProactiveCommsMessage() { EventType = inputMessage.EventType, CorrelationId = Guid.NewGuid(), AccountId = inputMessage.AccountId, NotificationEventId = inputMessage.NotificationEventId };
        }
    }

    public class BudStopMapper : IMessageMapper
    {
        public ProactiveCommsMessage Map(CisMessage inputMessage)
        {
            return new ProactiveCommsMessage() { EventType = inputMessage.EventType, CorrelationId = Guid.NewGuid(), AccountId = inputMessage.AccountId, NotificationEventId = inputMessage.NotificationEventId };
        }
    }

    public class BudStopCCMapper : IMessageMapper
    {
        public ProactiveCommsMessage Map(CisMessage inputMessage)
        {
            return new ProactiveCommsMessage() { EventType = inputMessage.EventType, CorrelationId = Guid.NewGuid(), AccountId = inputMessage.AccountId, NotificationEventId = inputMessage.NotificationEventId };
        }
    }
}
