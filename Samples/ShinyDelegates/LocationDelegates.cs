using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Samples.Models;
using Shiny.Locations;
using Shiny.Logging;

namespace Samples.ShinyDelegates
{
    public class LocationDelegates : IGeofenceDelegate, IGpsDelegate
    {
        readonly HttpClient httpClient = new HttpClient();
        readonly CoreDelegateServices services;
        public LocationDelegates(CoreDelegateServices services) => this.services = services;


        public Task OnReading(IGpsReading reading)
            => this.services.Connection.InsertAsync(new GpsEvent
            {
                Latitude = reading.Position.Latitude,
                Longitude = reading.Position.Longitude,
                Altitude = reading.Altitude,
                PositionAccuracy = reading.PositionAccuracy,
                Heading = reading.Heading,
                HeadingAccuracy = reading.HeadingAccuracy,
                Speed = reading.Speed,
                Date = reading.Timestamp.ToLocalTime()
            });


        public async Task OnStatusChanged(GeofenceState newStatus, GeofenceRegion region)
        {
            await this.services.Connection.InsertAsync(new GeofenceEvent
            {
                Identifier = region.Identifier,
                Entered = newStatus == GeofenceState.Entered,
                Date = DateTime.Now
            });
            var notify = newStatus == GeofenceState.Entered
                ? this.services.AppSettings.UseNotificationsGeofenceEntry
                : this.services.AppSettings.UseNotificationsGeofenceExit;

            await this.services.SendNotification(
                "Geofence Event",
                $"{region.Identifier} was {newStatus}",
                x => notify
            );

            await ReportEvents("OnStatusChanged", default);
        }

        public async Task ReportEvents(string eventName, CancellationToken cancelToken)
        {
            var events = await services.Connection.GeofenceEvents.ToListAsync();
            var unreported = events.Count(e => e.Reported == null);
            Log.Write(eventName, $"Unreported: {unreported}");

            if (unreported == 0)
                return;

            foreach (var group in events.GroupBy(e => e.Identifier))
            {
                var latest = group.OrderByDescending(e => e.Date).First();
                if (latest.Reported != null)
                    continue;

                try
                {
                    string eventType = latest.Entered ? "entered" : "exited";
                    var text = $"Group {group.Key} {eventType} at {latest.Date:h:mm tt}";
                    Log.Write(eventName, text);

                    var response = await httpClient.PostAsync(Constants.SlackWebhook,
                        new StringContent(JsonConvert.SerializeObject(new { text }), null, "application/json"),
                        cancelToken);

                    latest.Reported = DateTime.Now;
                    await services.Connection.UpdateAsync(latest);
                }
                catch (Exception ex)
                {
                    Log.Write(ex,
                        ("GeofenceId", group.Key),
                        ("LatestEvent", latest.Date.ToString("u")),
                        ("EventCount", group.Count().ToString()));
                }
            }
        }
    }
}
