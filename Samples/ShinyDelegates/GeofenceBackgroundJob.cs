using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Shiny.Jobs;

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
            Analytics.TrackEvent("BackgroundJobRun");

            var events = await services.Connection.GeofenceEvents.Where(x => x.Reported != null).ToListAsync();

            using var client = new HttpClient();

            await Task.WhenAll(
                events.GroupBy(e => e.Identifier)
                    .Select(async group =>
                    {
                        var latest = group.OrderByDescending(e => e.Date).First();
                        string eventType = latest.Entered ? "entered" : "exited";

                        var response = await client.PostAsync(Constants.SlackWebhook,
                            new StringContent($"{{\"text\": \"Geofence {group.Key} {eventType}\"}}"),
                            cancelToken);

                        foreach (var ge in group)
                            ge.Reported = true;

                        await services.Connection.UpdateAllAsync(group);
                    })
                );

            return true;
        }
    }
}
