using System;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Samples.Models;
using Samples.Settings;
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
                Source = "Geofence",
                Entered = newStatus == GeofenceState.Entered,
                Date = DateTime.Now
            });

            await Task.WhenAll(
                this.services.SendNotification(
                    "Geofence Event",
                    $"{region.Identifier} was {newStatus}",
                    newStatus == GeofenceState.Entered
                        ? (Expression<Func<IAppSettings, bool>>)(x => x.UseNotificationsGeofenceEntry)
                        : (x => x.UseNotificationsGeofenceExit)
                ),
                ReportEvents("OnStatusChanged", default)
            );
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
                if (cancelToken.IsCancellationRequested)
                {
                    Log.Write(eventName, "Cancellation Requested");
                    break;
                }

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
