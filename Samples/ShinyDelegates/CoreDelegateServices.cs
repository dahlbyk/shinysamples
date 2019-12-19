using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Samples.Settings;
using Shiny;
using Shiny.Logging;
using Shiny.Notifications;

namespace Samples.ShinyDelegates
{
    public class CoreDelegateServices
    {
        public CoreDelegateServices(SampleSqliteConnection conn,
                                    INotificationManager notifications,
                                    IAppSettings appSettings)
        {
            this.Connection = conn;
            this.Notifications = notifications;
            this.AppSettings = appSettings;
        }


        public SampleSqliteConnection Connection { get; }
        public INotificationManager Notifications { get; }
        public IAppSettings AppSettings { get; }


        public async Task SendNotification(string title, string message, Expression<Func<IAppSettings, bool>> expression = null)
        {
            Log.Write(title, message);

            var notify = expression == null
                ? true
                : this.AppSettings.ReflectGet(expression);

            if (notify)
                await this.Notifications.Send(title, message);
        }
    }
}
