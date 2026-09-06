using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using HangUp.Mac.Core.Config;
using HangUp.Mac.Core.Firewall;

namespace HangUp.Mac.App.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private readonly ProfileStore _profileStore;
        private readonly MacFirewallManager _firewallManager;
        private bool _isBusy;
        private string _statusMessage = "Ready";

        public ObservableCollection<AppItemViewModel> Apps { get; } = new();

        public int BlockedCount => Apps.Count(a => a.IsBlocked);
        public int AllowedCount => Apps.Count(a => !a.IsBlocked);
        public int TotalRulesCount => Apps.Where(a => a.IsBlocked).Sum(a => a.RulesCount);

        public string BlockedPercentage
        {
            get
            {
                if (Apps.Count == 0) return "0%";
                int pct = (int)Math.Round((double)BlockedCount / Apps.Count * 100);
                return $"{pct}%";
            }
        }

        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged();
                    (BlockAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (UnblockAllCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (_statusMessage != value)
                {
                    _statusMessage = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand BlockAllCommand { get; }
        public ICommand UnblockAllCommand { get; }

        public MainWindowViewModel()
        {
            _profileStore = new ProfileStore();
            _firewallManager = new MacFirewallManager();

            BlockAllCommand = new RelayCommand(async () => await BlockAllAsync(), () => !IsBusy);
            UnblockAllCommand = new RelayCommand(async () => await UnblockAllAsync(), () => !IsBusy);

            LoadApps();
        }

        public void LoadApps()
        {
            Apps.Clear();
            var profiles = _profileStore.GetProfiles();

            foreach (var profile in profiles)
            {
                bool isBlocked = _firewallManager.IsAppBlocked(profile);
                var itemVm = new AppItemViewModel(profile, isBlocked);
                itemVm.OnToggleRequested += async (vm, blocked) => await HandleToggleAppAsync(vm, blocked);
                Apps.Add(itemVm);
            }

            NotifyStatsChanged();
        }

        public async Task HandleToggleAppAsync(AppItemViewModel appVm, bool blocked)
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = blocked ? $"Blocking {appVm.Name}..." : $"Unblocking {appVm.Name}...";

                if (blocked)
                {
                    await _firewallManager.BlockAppAsync(appVm.Profile);
                }
                else
                {
                    await _firewallManager.UnblockAppAsync(appVm.Profile);
                }

                // Verify actual state from /etc/hosts
                bool actuallyBlocked = _firewallManager.IsAppBlocked(appVm.Profile);
                appVm.SetBlockedSilent(actuallyBlocked);

                StatusMessage = $"{appVm.Name} {(actuallyBlocked ? "Blocked" : "Allowed")}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                // Revert to true status from /etc/hosts
                bool actuallyBlocked = _firewallManager.IsAppBlocked(appVm.Profile);
                appVm.SetBlockedSilent(actuallyBlocked);
            }
            finally
            {
                IsBusy = false;
                NotifyStatsChanged();
            }
        }

        public async Task BlockAllAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Blocking all applications...";

                var profiles = Apps.Select(a => a.Profile).ToList();
                await _firewallManager.BlockAllAppsAsync(profiles);

                foreach (var appVm in Apps)
                {
                    bool actuallyBlocked = _firewallManager.IsAppBlocked(appVm.Profile);
                    appVm.SetBlockedSilent(actuallyBlocked);
                }

                StatusMessage = "All applications blocked successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                foreach (var appVm in Apps)
                {
                    bool actuallyBlocked = _firewallManager.IsAppBlocked(appVm.Profile);
                    appVm.SetBlockedSilent(actuallyBlocked);
                }
            }
            finally
            {
                IsBusy = false;
                NotifyStatsChanged();
            }
        }

        public async Task UnblockAllAsync()
        {
            if (IsBusy) return;

            try
            {
                IsBusy = true;
                StatusMessage = "Unblocking all applications...";

                var profiles = Apps.Select(a => a.Profile).ToList();
                await _firewallManager.UnblockAllAppsAsync(profiles);

                foreach (var appVm in Apps)
                {
                    bool actuallyBlocked = _firewallManager.IsAppBlocked(appVm.Profile);
                    appVm.SetBlockedSilent(actuallyBlocked);
                }

                StatusMessage = "All applications allowed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                foreach (var appVm in Apps)
                {
                    bool actuallyBlocked = _firewallManager.IsAppBlocked(appVm.Profile);
                    appVm.SetBlockedSilent(actuallyBlocked);
                }
            }
            finally
            {
                IsBusy = false;
                NotifyStatsChanged();
            }
        }

        public void NotifyStatsChanged()
        {
            OnPropertyChanged(nameof(BlockedCount));
            OnPropertyChanged(nameof(AllowedCount));
            OnPropertyChanged(nameof(TotalRulesCount));
            OnPropertyChanged(nameof(BlockedPercentage));
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Func<Task>? _asyncExecute;
        private readonly Action? _syncExecute;
        private readonly Func<bool>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged;

        public RelayCommand(Func<Task> asyncExecute, Func<bool>? canExecute = null)
        {
            _asyncExecute = asyncExecute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action syncExecute, Func<bool>? canExecute = null)
        {
            _syncExecute = syncExecute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke() ?? true);

        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter)) return;
            try
            {
                _isExecuting = true;
                RaiseCanExecuteChanged();
                if (_asyncExecute != null) await _asyncExecute();
                else _syncExecute?.Invoke();
            }
            finally
            {
                _isExecuting = false;
                RaiseCanExecuteChanged();
            }
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
