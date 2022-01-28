using CMS.Core;
using CMS.EventLog;

namespace Kentico.Xperience.AlgoliaSearch.Test
{
    internal class MockEventLogService : EventLogService
    {
        public EventLogData LoggedEvent {
            get;
            set;
        }


        public override void LogEvent(EventLogData eventLogData)
        {
            LoggedEvent = eventLogData;
        }
    }
}
