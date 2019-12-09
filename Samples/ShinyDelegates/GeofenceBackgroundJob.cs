using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Samples.Models;
using Shiny;
using Shiny.Jobs;
using Shiny.Locations;
using Shiny.Notifications;

namespace Samples.ShinyDelegates
{
    public class GeofenceBackgroundJob : IJob
    {

        CoreDelegateServices services;

        public GeofenceBackgroundJob(CoreDelegateServices services)
        {
            this.services = services;
        }

        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            Analytics.TrackEvent("BackgroundJobRun");

            List<GeofenceEvent> geoEvents = await services.Connection.GeofenceEvents.Where(x => !x.Reported).OrderBy(x => x.Date).ToListAsync();

            IList<string> geoEventStrings = new List<string>();
            Dictionary<string, bool> geofenceStatuses = new Dictionary<string, bool>();

            foreach (GeofenceEvent ge in geoEvents) {
                geofenceStatuses[ge.Identifier] = ge.Entered;
            }

            foreach (string key in geofenceStatuses.Keys)
            {
                string eventType = "exited";
                if (geofenceStatuses[key])
                {
                    eventType = "entered";
                }
                using (HttpClient client = new HttpClient())
                {
                    var response = await client.PostAsync(Constants.SlackWebhook, new StringContent($"{{\"text\": \"Geofence {key} {eventType}\"}}"));
                }
            }

            foreach (GeofenceEvent ge in geoEvents)
            {
                ge.Reported = true;
            }

            await services.Connection.UpdateAllAsync(geoEvents);

            return true;
        }
    }
}
