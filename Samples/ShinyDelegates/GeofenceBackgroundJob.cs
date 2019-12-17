using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Shiny.Jobs;
using Shiny.Logging;

namespace Samples.ShinyDelegates
{
    public class GeofenceBackgroundJob : IJob
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly CoreDelegateServices services;

        public GeofenceBackgroundJob(CoreDelegateServices services)
        {
            this.services = services;
        }

        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var events = await services.Connection.GeofenceEvents.Where(x => x.Reported == null).ToListAsync();
            Log.Write(jobInfo.Identifier, $"Unreported: {events.Count}");

            if (events.Count == 0)
                return true;

            foreach (var group in events.GroupBy(e => e.Identifier))
            {
                var latest = group.OrderByDescending(e => e.Date).First();
                string eventType = latest.Entered ? "entered" : "exited";
                var text = $"Group {group.Key} {eventType} at {latest.Date:h:mm tt}";
                Log.Write(jobInfo.Identifier, text);

                var response = await httpClient.PostAsync(Constants.SlackWebhook,
                    new StringContent(JsonConvert.SerializeObject(new { text }), null, "application/json"),
                    cancelToken);

                var now = DateTime.Now;
                foreach (var ge in group)
                    ge.Reported = now;

                await services.Connection.UpdateAllAsync(group);
            }

            return true;
        }
    }
}
