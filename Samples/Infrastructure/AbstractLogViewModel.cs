using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Acr.UserDialogs.Forms;
using Prism.Navigation;
using ReactiveUI;
using Shiny;


namespace Samples.Infrastructure
{
    public abstract class AbstractLogViewModel<TItem> : ViewModel
    {
        protected AbstractLogViewModel(IUserDialogs dialogs)
        {
            this.Dialogs = dialogs;

            this.Logs = new ObservableList<TItem>();
            this.hasLogs = this.Logs
                .WhenCollectionChanged()
                .Select(_ => this.Logs.Count > 0)
                .ToProperty(this, x => x.HasLogs);

            this.Load = ReactiveCommand.CreateFromTask(async () =>
            {
                var logs = await this.LoadLogs();
                this.Logs.ReplaceAll(logs);
            });
            this.Clear = ReactiveCommand.CreateFromTask(this.DoClear);
            this.BindBusyCommand(this.Load);
        }


        protected IUserDialogs Dialogs { get; }
        public ObservableList<TItem> Logs { get; }
        public ReactiveCommand<Unit, Unit> Load { get; }
        public ReactiveCommand<Unit, Unit> Clear { get; }

        readonly ObservableAsPropertyHelper<bool> hasLogs;
        public bool HasLogs => this.hasLogs.Value;


        public override async void Initialize(INavigationParameters parameters)
        {
            base.Initialize(parameters);
            await this.Load.Execute();
        }


        protected abstract Task<IEnumerable<TItem>> LoadLogs();
        protected abstract Task ClearLogs();


        protected virtual void InsertItem(TItem item)
            => this.Logs.Insert(0, item);

        protected virtual async Task DoClear()
        {
            var confirm = await this.Dialogs.Confirm("Clear Logs?");
            if (confirm)
            {
                await this.ClearLogs();
                await this.Load.Execute();
            }
        }
    }
}
