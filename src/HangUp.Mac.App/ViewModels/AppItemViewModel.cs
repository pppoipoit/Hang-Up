using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HangUp.Mac.Core.Models;

namespace HangUp.Mac.App.ViewModels
{
    public class AppItemViewModel : INotifyPropertyChanged
    {
        private bool _isBlocked;
        private bool _isUpdating;

        public AppProfile Profile { get; }
        public string Name => Profile.Name;
        public string GradientStart => Profile.GradientStart;
        public string GradientEnd => Profile.GradientEnd;
        public int RulesCount => Profile.Domains?.Count ?? 0;
        public string RulesText => $"{RulesCount} domains";

        public Bitmap? IconImage { get; private set; }

        public bool IsBlocked
        {
            get => _isBlocked;
            set
            {
                if (_isBlocked != value)
                {
                    _isBlocked = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(StatusText));
                    OnPropertyChanged(nameof(StatusColor));
                    OnPropertyChanged(nameof(StatusDotColor));

                    if (!_isUpdating)
                    {
                        OnToggleRequested?.Invoke(this, value);
                    }
                }
            }
        }

        public string StatusText => IsBlocked ? "Blocked" : "Allowed";
        public string StatusColor => IsBlocked ? "#f43f5e" : "#94a3b8";
        public string StatusDotColor => IsBlocked ? "#f43f5e" : "#22c55e";

        public event Action<AppItemViewModel, bool>? OnToggleRequested;

        public AppItemViewModel(AppProfile profile, bool isBlocked)
        {
            Profile = profile;
            _isBlocked = isBlocked;
            LoadIcon();
        }

        public void SetBlockedSilent(bool blocked)
        {
            _isUpdating = true;
            IsBlocked = blocked;
            _isUpdating = false;
        }

        private void LoadIcon()
        {
            try
            {
                string iconName = Profile.Icon;
                if (!iconName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
                {
                    iconName = Path.GetFileNameWithoutExtension(iconName) + ".png";
                }

                var uri = new Uri($"avares://HangUp.Mac.App/Assets/{iconName}");
                if (AssetLoader.Exists(uri))
                {
                    using var stream = AssetLoader.Open(uri);
                    IconImage = new Bitmap(stream);
                }
            }
            catch
            {
                // Fallback icon if loading fails
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
