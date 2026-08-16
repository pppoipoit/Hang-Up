using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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

        public MainWindowViewModel()
        {
            _profileStore = new ProfileStore();
            _firewallManager = new MacFirewallManager();

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

        private async Task HandleToggleAppAsync(AppItemViewModel appVm, bool blocked)
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

                StatusMessage = $"{appVm.Name} {(blocked ? "Blocked" : "Allowed")}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                // Revert toggle state on failure
                appVm.SetBlockedSilent(!blocked);
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

                foreach (var appVm in Apps)
                {
                    await _firewallManager.BlockAppAsync(appVm.Profile);
                    appVm.SetBlockedSilent(true);
                }

                StatusMessage = "All applications blocked";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
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

                foreach (var appVm in Apps)
                {
                    await _firewallManager.UnblockAppAsync(appVm.Profile);
                    appVm.SetBlockedSilent(false);
                }

                StatusMessage = "All applications allowed";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
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
}
