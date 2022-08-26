using CMS.Core;
using CMS.EventLog;

using System.Collections.Generic;

namespace Kentico.Xperience.AlgoliaSearch.Test
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
