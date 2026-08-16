using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using HangUp.Core.Config;
using HangUp.Core.Firewall;
using HangUp.Core.Hosts;
using HangUp.Core.Models;

namespace HangUp.App
{
    public class Form1 : Form
    {
        // For dragging the borderless window
        public const int WM_NCLBUTTONDOWN = 0xA1;
        public const int HT_CAPTION = 0x2;

        [DllImport("user32.dll")]
        public static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();

        private readonly ProfileStore _profileStore;
        private readonly FirewallManager _firewall;
        private readonly HostsFileEditor _hosts;
        private readonly FirewallStatusService _statusService;

        private FlowLayoutPanel _appsPanel;
        private Label _activeCountLabel;
        private Label _inactiveCountLabel;
        private Label _rulesCountLabel;
        
        private ProgressRing _blockedRing;
        private ProgressRing _rulesRing;

        private List<AppProfile> _profiles;
        private Dictionary<string, BlockStatus> _statuses;

        // Theme colors
        private readonly Color _bgPrimary = ColorTranslator.FromHtml("#0a0e27");
        private readonly Color _bgSecondary = ColorTranslator.FromHtml("#101535");
        private readonly Color _textPrimary = ColorTranslator.FromHtml("#f1f5f9");
        private readonly Color _textSecondary = ColorTranslator.FromHtml("#94a3b8");
        private readonly Color _successColor = ColorTranslator.FromHtml("#22c55e");
        
        public Form1()
        {
            // Set up form
            Text = "HangUp";
            Size = new Size(480, 840);
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = _bgPrimary;
            DoubleBuffered = true;
            this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            // Make form draggable
            this.MouseDown += Form_MouseDown;

            // Initialize services
            _profileStore = new ProfileStore();
            _firewall = new FirewallManager();
            _hosts = new HostsFileEditor();
            _statusService = new FirewallStatusService(_firewall);

            _profiles = _profileStore.GetProfiles();
            _statuses = _statusService.GetAllStatuses(_profiles);

            InitializeUI();
            UpdateDashboardStats();
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                ReleaseCapture();
                SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
            }
        }

        private void InitializeUI()
        {
            // Title Bar
            var titleBar = new Panel { Height = 40, Dock = DockStyle.Top, BackColor = Color.Transparent };
            titleBar.MouseDown += Form_MouseDown;
            
            var closeBtn = new Button
            {
                Text = "✕",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = _textSecondary,
                FlatStyle = FlatStyle.Flat,
                Size = new Size(40, 40),
                Location = new Point(Width - 40, 0),
                Cursor = Cursors.Hand,
                BackColor = Color.Transparent
            };
            closeBtn.FlatAppearance.BorderSize = 0;
            closeBtn.FlatAppearance.MouseOverBackColor = Color.FromArgb(40, 255, 255, 255);
            closeBtn.FlatAppearance.MouseDownBackColor = Color.FromArgb(60, 255, 255, 255);
            closeBtn.Click += (s, e) => Close();
            titleBar.Controls.Add(closeBtn);

            // Header Section
            var headerPanel = new Panel { Location = new Point(0, 40), Size = new Size(Width, 120), BackColor = Color.Transparent };
            headerPanel.MouseDown += Form_MouseDown;

            // Load embedded icon image instead of drawing emoji
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            Image? mainIconImg = null;
            using (var stream = assembly.GetManifestResourceStream("HangUp.App.assets.handup.png"))
            {
                if (stream != null)
                {
                    mainIconImg = Image.FromStream(stream);
                }
            }

            Panel iconBox = new Panel { Size = new Size(80, 80), Location = new Point((Width - 80) / 2, -5) };
            if (mainIconImg != null)
            {
                iconBox.Paint += (s, e) => {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var rect = new Rectangle(0, 0, 80, 80);
                    e.Graphics.DrawImage(mainIconImg, rect);
                };
            }
            else
            {
                // Fallback if image not found
                iconBox.Paint += (s, e) => {
                    var rect = new Rectangle(0, 0, 64, 64);
                    using (var brush = new LinearGradientBrush(rect, ColorTranslator.FromHtml("#3b82f6"), ColorTranslator.FromHtml("#f97316"), 45f))
                    {
                        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                        e.Graphics.FillRoundedRectangle(brush, rect, 16);
                    }
                    TextRenderer.DrawText(e.Graphics, "⚡", new Font("Segoe UI Emoji", 24), rect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                };
            }
            headerPanel.Controls.Add(iconBox);

            var titleLabel = new Panel
            {
                Size = new Size(250, 40),
                Location = new Point((Width - 250) / 2, 64),
                BackColor = Color.Transparent
            };
            titleLabel.MouseDown += Form_MouseDown;
            titleLabel.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = titleLabel.ClientRectangle;
                using (var brush = new LinearGradientBrush(rect, ColorTranslator.FromHtml("#3b82f6"), ColorTranslator.FromHtml("#f97316"), 0f))
                {
                    var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    var font = new Font("Segoe UI", 20, FontStyle.Bold);
                    using (var path = new GraphicsPath())
                    {
                        path.AddString("Hang Up !!", font.FontFamily, (int)FontStyle.Bold, 24, rect, format);
                        e.Graphics.FillPath(brush, path);
                    }
                }
            };

            var subtitleLabel = new Label
            {
                Text = "Manage your application firewall",
                Font = new Font("Segoe UI", 10),
                ForeColor = _textSecondary,
                AutoSize = true,
                BackColor = Color.Transparent
            };
            subtitleLabel.Location = new Point((Width - subtitleLabel.PreferredWidth) / 2, 100);
            subtitleLabel.MouseDown += Form_MouseDown;

            headerPanel.Controls.Add(titleLabel);
            headerPanel.Controls.Add(subtitleLabel);

            // Status Bar
            var statusBar = new FlowLayoutPanel
            {
                Location = new Point(20, 168),
                Size = new Size(Width - 40, 40),
                WrapContents = false,
                BackColor = Color.Transparent
            };
            statusBar.MouseDown += Form_MouseDown;

            _activeCountLabel = CreateStatusChip("Blocked", "0", true);
            _inactiveCountLabel = CreateStatusChip("Allowed", "0", false);
            _rulesCountLabel = CreateStatusChip("Total Rules", "0", true);

            statusBar.Controls.Add(_activeCountLabel.Parent);
            statusBar.Controls.Add(_inactiveCountLabel.Parent);
            statusBar.Controls.Add(_rulesCountLabel.Parent);

            // Apps Panel
            _appsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 215),
                Size = new Size(Width - 40, 390),
                AutoScroll = false,
                BackColor = Color.Transparent,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            PopulateAppCards();

            // Progress Section
            var progressPanel = new Panel
            {
                Location = new Point(20, 620),
                Size = new Size(Width - 40, 120),
                BackColor = Color.Transparent
            };
            progressPanel.MouseDown += Form_MouseDown;
            
            _blockedRing = new ProgressRing { Location = new Point(60, 0), Label = "Blocked App", Value = 0, GradientStart = "#3b82f6", GradientEnd = "#06b6d4" };
            _rulesRing = new ProgressRing { Location = new Point(280, 0), Label = "Total Rules", Value = 0, MaxValue = 100, GradientStart = "#f97316", GradientEnd = "#f43f5e" };

            progressPanel.Controls.Add(_blockedRing);
            progressPanel.Controls.Add(_rulesRing);

            // Action Buttons
            var actionsPanel = new FlowLayoutPanel
            {
                Location = new Point(20, 760),
                Size = new Size(Width - 40, 50),
                WrapContents = false,
                BackColor = Color.Transparent
            };

            var blockAllBtn = new PremiumButton("Block All", "#3b82f6", "#f97316", true);
            blockAllBtn.Click += async (s, e) => await BulkAction(true);
            
            var unblockAllBtn = new PremiumButton("Unblock All", "#ef4444", "#ef4444", false);
            unblockAllBtn.Click += async (s, e) => await BulkAction(false);

            actionsPanel.Controls.Add(blockAllBtn);
            actionsPanel.Controls.Add(unblockAllBtn);

            // Add all to form
            Controls.Add(titleBar);
            Controls.Add(headerPanel);
            Controls.Add(statusBar);
            Controls.Add(_appsPanel);
            Controls.Add(progressPanel);
            Controls.Add(actionsPanel);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            using (var brush = new LinearGradientBrush(ClientRectangle, _bgPrimary, _bgSecondary, 90f))
            {
                e.Graphics.FillRectangle(brush, ClientRectangle);
            }
        }

        private Label CreateStatusChip(string label, string value, bool isActive)
        {
            var panel = new Panel
            {
                Size = new Size(130, 36),
                Margin = new Padding(0, 0, 10, 0)
            };
            panel.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (var brush = new SolidBrush(Color.FromArgb(20, 255, 255, 255)))
                {
                    e.Graphics.FillRoundedRectangle(brush, panel.ClientRectangle, 8);
                }
                using (var pen = new Pen(Color.FromArgb(40, 255, 255, 255), 1))
                {
                    e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), 8);
                }
                
                // Draw dot
                var dotBrush = new SolidBrush(isActive ? _successColor : _textSecondary);
                e.Graphics.FillEllipse(dotBrush, 8, 14, 8, 8);
                
                TextRenderer.DrawText(e.Graphics, label, new Font("Segoe UI", 9), new Point(20, 10), _textSecondary);
            };

            var valLabel = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = _textPrimary,
                AutoSize = true,
                Location = new Point(100, 9),
                BackColor = Color.Transparent
            };
            panel.Controls.Add(valLabel);

            return valLabel;
        }

        private void PopulateAppCards()
        {
            _appsPanel.Controls.Clear();

            if (_profiles.Count == 0)
            {
                var lbl = new Label
                {
                    Text = "No apps loaded.\n" + _profileStore.LastDebugInfo,
                    ForeColor = Color.Orange,
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10)
                };
                _appsPanel.Controls.Add(lbl);
                return;
            }

            foreach (var profile in _profiles)
            {
                var status = _statuses.ContainsKey(profile.Name) ? _statuses[profile.Name] : new BlockStatus();
                
                var card = new Panel
                {
                    Size = new Size(_appsPanel.Width - 10, 80),
                    Margin = new Padding(0, 0, 0, 12),
                    Tag = profile
                };

                card.Paint += (s, e) =>
                {
                    e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                    var rect = card.ClientRectangle;
                    
                    // Glass background
                    using (var brush = new SolidBrush(Color.FromArgb(15, 255, 255, 255)))
                    {
                        e.Graphics.FillRoundedRectangle(brush, rect, 12);
                    }
                    // Glass border
                    using (var pen = new Pen(Color.FromArgb(30, 255, 255, 255), 1))
                    {
                        e.Graphics.DrawRoundedRectangle(pen, new Rectangle(0, 0, rect.Width - 1, rect.Height - 1), 12);
                    }

                    // Accent line
                    if (status.IsBlocked)
                    {
                        using (var brush = new LinearGradientBrush(new Rectangle(0, 0, 4, rect.Height), ColorTranslator.FromHtml(profile.GradientStart), ColorTranslator.FromHtml(profile.GradientEnd), 90f))
                        {
                            e.Graphics.FillPath(brush, GraphicsExtensions.GetRoundedRectPath(new Rectangle(0, 0, 4, rect.Height), 12, true, false, false, true));
                        }
                    }

                    // Icon background
                    var iconRect = new Rectangle(16, 16, 48, 48);
                    
                    var asm = System.Reflection.Assembly.GetExecutingAssembly();
                    Image? cardIconImg = null;
                    using (var stream = asm.GetManifestResourceStream($"HangUp.App.assets.{profile.Icon}"))
                    {
                        if (stream != null)
                        {
                            cardIconImg = Image.FromStream(stream);
                        }
                    }

                    if (cardIconImg != null)
                    {
                        e.Graphics.DrawImage(cardIconImg, iconRect);
                    }
                    else
                    {
                        using (var brush = new SolidBrush(Color.FromArgb(30, ColorTranslator.FromHtml(profile.GradientStart))))
                        {
                            e.Graphics.FillRoundedRectangle(brush, iconRect, 10);
                        }
                        TextRenderer.DrawText(e.Graphics, profile.Icon, new Font("Segoe UI Emoji", 20), iconRect, Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                    }
                };

                var nameLabel = new Label
                {
                    Text = profile.Name,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = _textPrimary,
                    Location = new Point(76, 20),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };

                var statusLabel = new Label
                {
                    Text = status.IsBlocked ? "● Blocked" : "● Allowed",
                    Font = new Font("Segoe UI", 9, FontStyle.Bold),
                    ForeColor = status.IsBlocked ? _successColor : _textSecondary,
                    Location = new Point(76, 45),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };

                var ruleCountLabel = new Label
                {
                    Text = $"{status.RuleCount} rules",
                    Font = new Font("Segoe UI", 8),
                    ForeColor = _textSecondary,
                    Location = new Point(150, 46),
                    AutoSize = true,
                    BackColor = Color.Transparent
                };

                var toggle = new PremiumToggle
                {
                    Location = new Point(card.Width - 70, 26),
                    Checked = status.IsBlocked,
                    GradientStart = profile.GradientStart,
                    GradientEnd = profile.GradientEnd
                };

                toggle.CheckedChanged += async (s, e) =>
                {
                    status.IsBlocked = toggle.Checked;
                    statusLabel.Text = toggle.Checked ? "● Blocking..." : "● Allowing...";
                    statusLabel.ForeColor = Color.Yellow;
                    
                    try
                    {
                        if (toggle.Checked)
                        {
                            await _firewall.BlockAppAsync(profile);
                            _hosts.BlockDomains(profile);
                        }
                        else
                        {
                            _firewall.UnblockApp(profile);
                            _hosts.UnblockDomains(profile);
                        }
                        
                        // Update status
                        _statuses[profile.Name] = _statusService.GetBlockStatus(profile);
                        status = _statuses[profile.Name];
                        
                        statusLabel.Text = status.IsBlocked ? "● Blocked" : "● Allowed";
                        statusLabel.ForeColor = status.IsBlocked ? _successColor : _textSecondary;
                        ruleCountLabel.Text = $"{status.RuleCount} rules";
                        
                        UpdateDashboardStats();
                        card.Invalidate(); // Redraw accent line
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error processing {profile.Name}: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        toggle.Checked = !toggle.Checked;
                    }
                };

                card.Controls.Add(nameLabel);
                card.Controls.Add(statusLabel);
                card.Controls.Add(ruleCountLabel);
                card.Controls.Add(toggle);

                _appsPanel.Controls.Add(card);
            }
        }

        private void UpdateDashboardStats()
        {
            if (_profiles.Count == 0) return;

            int active = _statuses.Values.Count(s => s.IsBlocked);
            int total = _profiles.Count;
            int rules = _statuses.Values.Sum(s => s.RuleCount);

            _activeCountLabel.Text = active.ToString();
            _inactiveCountLabel.Text = (total - active).ToString();
            _rulesCountLabel.Text = rules.ToString();

            _blockedRing.Value = total > 0 ? (int)((float)active / total * 100) : 0;
            _rulesRing.Value = rules;
        }

        private async Task BulkAction(bool block)
        {
            foreach (Control c in _appsPanel.Controls)
            {
                if (c is Panel card)
                {
                    var toggle = card.Controls.OfType<PremiumToggle>().FirstOrDefault();
                    if (toggle != null && toggle.Checked != block)
                    {
                        toggle.Checked = block; 
                        await Task.Delay(150); // Slight delay for UI responsiveness
                    }
                }
            }
        }
    }

    public static class GraphicsExtensions
    {
        public static void FillRoundedRectangle(this Graphics g, Brush brush, Rectangle bounds, int radius)
        {
            using (var path = GetRoundedRectPath(bounds, radius))
            {
                g.FillPath(brush, path);
            }
        }

        public static void DrawRoundedRectangle(this Graphics g, Pen pen, Rectangle bounds, int radius)
        {
            using (var path = GetRoundedRectPath(bounds, radius))
            {
                g.DrawPath(pen, path);
            }
        }

        public static GraphicsPath GetRoundedRectPath(Rectangle bounds, int radius, bool tl = true, bool tr = true, bool br = true, bool bl = true)
        {
            int d = radius * 2;
            var path = new GraphicsPath();
            
            if (tl) path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            else path.AddLine(bounds.X, bounds.Y, bounds.X, bounds.Y);

            if (tr) path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            else path.AddLine(bounds.Right, bounds.Y, bounds.Right, bounds.Y);

            if (br) path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            else path.AddLine(bounds.Right, bounds.Bottom, bounds.Right, bounds.Bottom);

            if (bl) path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            else path.AddLine(bounds.X, bounds.Bottom, bounds.X, bounds.Bottom);

            path.CloseFigure();
            return path;
        }
    }

    // Custom Button without WinForms artifacts
    public class PremiumButton : Control
    {
        private bool _isHover;
        private bool _isDown;
        private string _text;
        private string _c1, _c2;
        private bool _isPrimary;

        public PremiumButton(string text, string color1, string color2, bool isPrimary)
        {
            _text = text;
            _c1 = color1;
            _c2 = color2;
            _isPrimary = isPrimary;
            Size = new Size(200, 45);
            Margin = new Padding(0, 0, 15, 0);
            Font = new Font("Segoe UI", 10, FontStyle.Bold);
            ForeColor = isPrimary ? Color.White : ColorTranslator.FromHtml(color1);
            Cursor = Cursors.Hand;
            DoubleBuffered = true;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnMouseEnter(EventArgs e) { _isHover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHover = false; _isDown = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _isDown = true; Invalidate(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e) { _isDown = false; Invalidate(); base.OnMouseUp(e); }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            
            if (_isPrimary)
            {
                using (var brush = new LinearGradientBrush(rect, ColorTranslator.FromHtml(_c1), ColorTranslator.FromHtml(_c2), 45f))
                {
                    if (_isHover && !_isDown) e.Graphics.FillRoundedRectangle(new SolidBrush(Color.FromArgb(40, 255, 255, 255)), rect, 8); // hover brighten
                    e.Graphics.FillRoundedRectangle(brush, rect, 8);
                    if (_isDown) e.Graphics.FillRoundedRectangle(new SolidBrush(Color.FromArgb(40, 0, 0, 0)), rect, 8); // click darken
                }
            }
            else
            {
                using (var brush = new SolidBrush(Color.FromArgb(_isHover ? 40 : 20, ColorTranslator.FromHtml(_c1))))
                {
                    e.Graphics.FillRoundedRectangle(brush, rect, 8);
                    if (_isDown) e.Graphics.FillRoundedRectangle(new SolidBrush(Color.FromArgb(40, 0, 0, 0)), rect, 8);
                }
                using (var pen = new Pen(ColorTranslator.FromHtml(_c1), 1))
                {
                    e.Graphics.DrawRoundedRectangle(pen, rect, 8);
                }
            }
            
            TextRenderer.DrawText(e.Graphics, _text, Font, rect, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
        }
    }

    public class PremiumToggle : Control
    {
        private bool _checked;
        public string GradientStart { get; set; } = "#3b82f6";
        public string GradientEnd { get; set; } = "#06b6d4";

        public bool Checked
        {
            get => _checked;
            set
            {
                if (_checked != value)
                {
                    _checked = value;
                    Invalidate();
                    CheckedChanged?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        public event EventHandler CheckedChanged;

        public PremiumToggle()
        {
            Size = new Size(52, 28);
            DoubleBuffered = true;
            Cursor = Cursors.Hand;
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            if (_checked)
            {
                using (var brush = new LinearGradientBrush(rect, ColorTranslator.FromHtml(GradientStart), ColorTranslator.FromHtml(GradientEnd), 45f))
                {
                    e.Graphics.FillRoundedRectangle(brush, rect, Height / 2);
                }
            }
            else
            {
                using (var brush = new SolidBrush(Color.FromArgb(40, 255, 255, 255)))
                {
                    e.Graphics.FillRoundedRectangle(brush, rect, Height / 2);
                }
                using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255), 1))
                {
                    e.Graphics.DrawRoundedRectangle(pen, rect, Height / 2);
                }
            }

            // Thumb
            int thumbSize = 20;
            int thumbX = _checked ? Width - thumbSize - 4 : 4;
            var thumbRect = new Rectangle(thumbX, (Height - thumbSize) / 2, thumbSize, thumbSize);

            using (var shadowBrush = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
            {
                e.Graphics.FillEllipse(shadowBrush, thumbRect.X, thumbRect.Y + 2, thumbSize, thumbSize);
            }

            using (var thumbBrush = new SolidBrush(_checked ? Color.White : Color.FromArgb(200, 255, 255, 255)))
            {
                e.Graphics.FillEllipse(thumbBrush, thumbRect);
            }
        }

        protected override void OnClick(EventArgs e)
        {
            Checked = !Checked;
        }
    }

    public class ProgressRing : Control
    {
        private int _value;
        public int MaxValue { get; set; } = 100;
        public string Label { get; set; } = "";
        public string GradientStart { get; set; } = "#3b82f6";
        public string GradientEnd { get; set; } = "#06b6d4";

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                Invalidate();
            }
        }

        public ProgressRing()
        {
            DoubleBuffered = true;
            Size = new Size(100, 120);
            SetStyle(ControlStyles.SupportsTransparentBackColor, true);
            BackColor = Color.Transparent;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            
            var ringRect = new Rectangle(14, 10, 72, 72);
            
            // Background ring
            using (var pen = new Pen(Color.FromArgb(20, 255, 255, 255), 6))
            {
                e.Graphics.DrawArc(pen, ringRect, 0, 360);
            }

            // Fill ring
            float angle = MaxValue > 0 ? ((float)_value / MaxValue) * 360f : 0;
            if (angle > 0)
            {
                using (var brush = new LinearGradientBrush(ringRect, ColorTranslator.FromHtml(GradientStart), ColorTranslator.FromHtml(GradientEnd), 45f))
                using (var pen = new Pen(brush, 6) { StartCap = LineCap.Round, EndCap = LineCap.Round })
                {
                    e.Graphics.DrawArc(pen, ringRect, -90, angle);
                }
            }

            // Value text
            var valFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
            TextRenderer.DrawText(e.Graphics, _value.ToString() + (Label == "Blocked App" ? "%" : ""), new Font("Segoe UI", 16, FontStyle.Bold), new Rectangle(14, 10, 72, 72), Color.White, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            
            // Label text
            TextRenderer.DrawText(e.Graphics, Label, new Font("Segoe UI", 9), new Rectangle(0, 90, 100, 20), ColorTranslator.FromHtml("#94a3b8"), TextFormatFlags.HorizontalCenter | TextFormatFlags.Top);
        }
    }
}