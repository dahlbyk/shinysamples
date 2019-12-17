using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Acr.UserDialogs.Forms;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Prism.Navigation;
using Shiny.Jobs;
using Shiny;


namespace Samples.Jobs
{
    public class ListViewModel : ViewModel
    {
        readonly IJobManager jobManager;
        readonly IUserDialogs dialogs;


        public ListViewModel(IJobManager jobManager,
                             INavigationService navigator,
                             IUserDialogs dialogs)
        {
            this.jobManager = jobManager;
            this.dialogs = dialogs;

            this.hasJobs = this.WhenAnyValue(x => x.Jobs)
                .Select(x => (x?.Count ?? 0) > 0)
                .ToProperty(this, x => x.HasJobs);

            this.Create = navigator.NavigateCommand("CreateJob");

            this.LoadJobs = ReactiveCommand.CreateFromTask(async () =>
            {
                var jobs = await jobManager.GetJobs();

                this.Jobs = jobs
                    .Select(x => new CommandItem
                    {
                        Text = x.Identifier,
                        Detail = x.LastRunUtc?.ToLocalTime().ToString("G") ?? "Never Run",
                        PrimaryCommand = ReactiveCommand.CreateFromTask(() => jobManager.Run(x.Identifier)),
                        SecondaryCommand = ReactiveCommand.CreateFromTask(() => jobManager.Cancel(x.Identifier))
                    })
                    .ToList();
            });
            this.BindBusyCommand(this.LoadJobs);

            this.RunAllJobs = ReactiveCommand.CreateFromTask(async () =>
            {
                if (!await this.AssertJobs())
                    return;

                if (this.jobManager.IsRunning)
                    await dialogs.Alert("Job Manager is already running");
                else
                {
                    dialogs.Toast("Job Batch Started");
                    await this.jobManager.RunAll();
                }
            });

            this.CancelAllJobs = ReactiveCommand.CreateFromTask(async _ =>
            {
                if (!await this.AssertJobs())
                    return;

                var confirm = await dialogs.Confirm("Are you sure you wish to cancel all jobs?");
                if (confirm)
                {
                    await this.jobManager.CancelAll();
                    this.LoadJobs.Execute();
                }
            });
        }


        public ReactiveCommand<Unit, Unit> LoadJobs { get; }
        public ReactiveCommand<Unit, Unit> CancelAllJobs { get; }
        public ReactiveCommand<Unit, Unit> RunAllJobs { get; }
        public ICommand Create { get; }

        [Reactive] public List<CommandItem> Jobs { get; private set; }

        readonly ObservableAsPropertyHelper<bool> hasJobs;
        public bool HasJobs => this.hasJobs.Value;


        public override void OnAppearing()
        {
            base.OnAppearing();
            this.LoadJobs.Execute();
            this.jobManager.JobStarted.Subscribe(_ => this.LoadJobs.Execute()).DisposedBy(this.DeactivateWith);
            this.jobManager.JobFinished.Subscribe(_ => this.LoadJobs.Execute()).DisposedBy(this.DeactivateWith);
        }


        async Task<bool> AssertJobs()
        {
            var jobs = await this.jobManager.GetJobs();
            if (!jobs.Any())
            {
                await this.dialogs.Alert("There are no jobs");
                return false;
            }

            return true;
        }
    }
}
