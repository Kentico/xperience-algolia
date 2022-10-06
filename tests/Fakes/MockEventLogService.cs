using System.Collections.Generic;

using CMS.Core;
using CMS.EventLog;

namespace Kentico.Xperience.Algolia.Test
{
    internal class MockEventLogService : EventLogService
    {
        public List<EventLogData> LoggedEvents => new List<EventLogData>();


        public override void LogEvent(EventLogData eventLogData)
        {
            LoggedEvents.Add(eventLogData);
        }
    }
}
