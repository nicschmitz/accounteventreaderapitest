using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AccountEventReader.Api
{
    public class CisMessage
    {
        public long AccountId { get; set; }
        public string NotificationEventId { get; set; } = String.Empty;
        public string EventType { get; set; } = string.Empty;
    }
}
