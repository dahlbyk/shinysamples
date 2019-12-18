using System.Threading;
using System.Threading.Tasks;
using Shiny.Jobs;

namespace Samples.ShinyDelegates
{
    public class GeofenceBackgroundJob : LocationDelegates, IJob
    {
        public GeofenceBackgroundJob(CoreDelegateServices services) : base(services)
        {
        }

        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            await ReportEvents(jobInfo.Identifier, cancelToken);
            return true;
        }

    }
}
