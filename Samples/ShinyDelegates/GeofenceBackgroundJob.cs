using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;
using Shiny.Logging;

namespace Samples.ShinyDelegates
{
    public class GeofenceBackgroundJob : IJob
    {
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

            using var client = new HttpClient();

            await Task.WhenAll(
                events.GroupBy(e => e.Identifier)
                    .Select(async group =>
                    {
                        var latest = group.OrderByDescending(e => e.Date).First();
                        string eventType = latest.Entered ? "entered" : "exited";

                        var response = await client.PostAsync(Constants.SlackWebhook,
                            new StringContent($"{{\"text\": \"Geofence {group.Key} {eventType} at {latest.Date:h:mm tt}\"}}"),
                            cancelToken);

                        var now = DateTime.Now;
                        foreach (var ge in group)
                            ge.Reported = now;
                    })
                );

            await services.Connection.UpdateAllAsync(events);

            return true;
        }
    }
}
