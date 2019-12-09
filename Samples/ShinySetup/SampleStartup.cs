//#define STARTUP_ATTRIBUTES
//#define STARTUP_AUTO

using Acr.UserDialogs.Forms;
using Microsoft.Extensions.DependencyInjection;
using Samples.Settings;
using Samples.ShinyDelegates;
using Shiny;
using Shiny.Logging;
using Shiny.Notifications;

#if STARTUP_ATTRIBUTES
//[assembly: ShinySqliteIntegration(true, true, true, true, true)]
//[assembly: ShinyJob(typeof(SampleJob), "MyIdentifier", BatteryNotLow = true, DeviceCharging = false, RequiredInternetAccess = Shiny.Jobs.InternetAccess.Any)]
[assembly: ShinyAppCenterIntegration(Constants.AppCenterTokens, true, true)]
[assembly: ShinyService(typeof(SampleSqliteConnection))]
[assembly: ShinyService(typeof(GlobalExceptionHandler))]
[assembly: ShinyService(typeof(CoreDelegateServices))]
[assembly: ShinyService(typeof(JobLoggerTask))]
[assembly: ShinyService(typeof(IUserDialogs), typeof(UserDialogs))]
[assembly: ShinyService(typeof(IFullService), typeof(FullService))]
[assembly: ShinyService(typeof(IAppSettings), typeof(AppSettings))]

#if !STARTUP_AUTO
[assembly: ShinyNotifications(typeof(NotificationDelegate), true)]
[assembly: ShinyBeacons(typeof(BeaconDelegate))]
[assembly: ShinyBleCentral(typeof(BleCentralDelegate))]
[assembly: ShinyGps(typeof(LocationDelegates))]
[assembly: ShinyGeofences(typeof(LocationDelegates))]
[assembly: ShinyMotionActivity]
[assembly: ShinySensors]
[assembly: ShinyHttpTransfers(typeof(HttpTransferDelegate))]
[assembly: ShinySpeechRecognition]
#endif
#endif

namespace Samples.ShinySetup
{
    public class SampleStartup : ShinyStartup
    {
        public override void ConfigureServices(IServiceCollection services)
        {
            Log.UseConsole();
            Log.UseDebug();
            services.UseMemoryCache();
            services.UseAppCenterLogging(Constants.AppCenterTokens, true, true);

            services.RegisterJob(new Shiny.Jobs.JobInfo(typeof(GeofenceBackgroundJob))
            {
                RequiredInternetAccess = Shiny.Jobs.InternetAccess.Any
            });

            //services.UseSqliteLogging(true, true);
            //services.UseSqliteCache();
            //services.UseSqliteSettings();
            //services.UseSqliteStorage();

#if STARTUP_ATTRIBUTES
            services.RegisterModule(new AssemblyServiceModule());
#if STARTUP_AUTO
            services.RegisterModule(new AutoRegisterModule());
#endif
#else
            UseAllServices(services);
#endif
        }


        static void UseAllServices(IServiceCollection services)
        {
            // your infrastructure
            services.AddSingleton<SampleSqliteConnection>();
            services.AddSingleton<CoreDelegateServices>();
            services.AddSingleton<IUserDialogs, UserDialogs>();
            services.AddSingleton<IAppSettings, AppSettings>();

            // startup tasks
            services.AddSingleton<GlobalExceptionHandler>();
            services.AddSingleton<IFullService, FullService>();
            services.AddSingleton<JobLoggerTask>();

            // register all of the shiny stuff you want to use
            services.UseHttpTransfers<HttpTransferDelegate>();
            services.UseBeacons<BeaconDelegate>();
            services.UseBleCentral<BleCentralDelegate>();
            services.UseBlePeripherals();

            services.UseGeofencing<LocationDelegates>();
            services.UseGps<LocationDelegates>();
            services.UseMotionActivity();

            Notification.DefaultCategory = "Test";
            services.UseNotifications<NotificationDelegate>(
                true,
                null,
                null,
                new NotificationCategory(
                    "Test",
                    new NotificationAction("Reply", "Reply", NotificationActionType.TextReply),
                    new NotificationAction("Reply-All", "Reply All", NotificationActionType.TextReply),
                    new NotificationAction("Yes", "Yes", NotificationActionType.OpenApp),
                    new NotificationAction("No", "No", NotificationActionType.Destructive)
                )
            );
            services.UseSpeechRecognition();

            services.UseAllSensors();
            //services.UseAccelerometer();
            //services.UseAmbientLightSensor();
            //services.UseBarometer();
            //services.UseCompass();
            //services.UseMagnetometer();
            //services.UsePedometer();
            //services.UseProximitySensor();
            //services.UseHeartRateMonitor();
            //services.UseTemperature();
            //services.UseHumidity();
        }

        //#if DEBUG
        //        public override IServiceProvider CreateServiceProvider(IServiceCollection services)
        //            => services.BuildServiceProvider(true);
        //#endif
    }
}
