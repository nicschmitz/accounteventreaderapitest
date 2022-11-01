using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AccountEventReader.Api
{
    [DebuggerDisplay("NotificationEventId={NotificationEventId}")]
    public class ProactiveCommsMessage
    {
        public string NotificationEventId { get; set; } = string.Empty;
        public long AccountId { get; set; } 
        public Guid CorrelationId { get; set; }

        public string EventType { get; set; } = string.Empty;

        public override string ToString()
        {
            return $"{NotificationEventId} {EventType}";
        }
    }
}
