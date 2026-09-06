using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using HangUp.Mac.Core.Models;

namespace HangUp.Mac.Core.Firewall
{
    public class MacFirewallManager
    {
        public MacFirewallManager()
        {
        }

        public async Task BlockAppAsync(AppProfile app)
        {
            var sb = new StringBuilder();
            sb.Append(GenerateBlockScript(app));
            sb.AppendLine("dscacheutil -flushcache || true");
            sb.AppendLine("killall -HUP mDNSResponder || true");

            await RunAsAdminAsync(sb.ToString());
        }

        public async Task UnblockAppAsync(AppProfile app)
        {
            var sb = new StringBuilder();
            sb.Append(GenerateUnblockScript(app));
            sb.AppendLine("dscacheutil -flushcache || true");
            sb.AppendLine("killall -HUP mDNSResponder || true");

            await RunAsAdminAsync(sb.ToString());
        }

        public async Task BlockAllAppsAsync(IEnumerable<AppProfile> apps)
        {
            var sb = new StringBuilder();
            foreach (var app in apps)
            {
                sb.Append(GenerateBlockScript(app));
            }
            sb.AppendLine("dscacheutil -flushcache || true");
            sb.AppendLine("killall -HUP mDNSResponder || true");

            await RunAsAdminAsync(sb.ToString());
        }

        public async Task UnblockAllAppsAsync(IEnumerable<AppProfile> apps)
        {
            var sb = new StringBuilder();
            foreach (var app in apps)
            {
                sb.Append(GenerateUnblockScript(app));
            }
            sb.AppendLine("dscacheutil -flushcache || true");
            sb.AppendLine("killall -HUP mDNSResponder || true");

            await RunAsAdminAsync(sb.ToString());
        }

        private static string GenerateBlockScript(AppProfile app)
        {
            var sb = new StringBuilder();
            if (app.Domains == null || app.Domains.Count == 0) return string.Empty;

            string startMarker = $"# HangUp_Block_Start_{app.Name}";
            string endMarker = $"# HangUp_Block_End_{app.Name}";

            // 1. Clean existing block marker and lines if present
            sb.AppendLine($"sed -i '' '/{startMarker}/,/{endMarker}/d' /etc/hosts");

            // 2. Append new block rules
            sb.AppendLine($"echo '{startMarker}' >> /etc/hosts");
            foreach (var domain in app.Domains)
            {
                if (!string.IsNullOrWhiteSpace(domain))
                {
                    string trimmed = domain.Trim();
                    sb.AppendLine($"echo '127.0.0.1 {trimmed}' >> /etc/hosts");
                    sb.AppendLine($"echo '::1 {trimmed}' >> /etc/hosts");
                }
            }
            sb.AppendLine($"echo '{endMarker}' >> /etc/hosts");

            return sb.ToString();
        }

        private static string GenerateUnblockScript(AppProfile app)
        {
            if (app.Domains == null || app.Domains.Count == 0) return string.Empty;

            string startMarker = $"# HangUp_Block_Start_{app.Name}";
            string endMarker = $"# HangUp_Block_End_{app.Name}";

            return $"sed -i '' '/{startMarker}/,/{endMarker}/d' /etc/hosts\n";
        }

        private async Task RunAsAdminAsync(string bashScript)
        {
            if (string.IsNullOrWhiteSpace(bashScript)) return;

            if (OperatingSystem.IsWindows())
            {
                // Mock behavior for testing UI on Windows
                Console.WriteLine("Mocking Mac Sudo Execution on Windows:\n" + bashScript);
                await Task.Delay(300);
                return;
            }

            // Write temporary shell script with standard LF line endings in /tmp
            string tempScript = Path.Combine(Path.GetTempPath(), $"hangup_{Guid.NewGuid():N}.sh");
            string fullScript = "#!/bin/sh\n" + bashScript.Replace("\r\n", "\n") + "\n";
            await File.WriteAllTextAsync(tempScript, fullScript, new UTF8Encoding(false));

            try
            {
                // Grant executable permissions
                var chmodInfo = new ProcessStartInfo
                {
                    FileName = "chmod",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                chmodInfo.ArgumentList.Add("+x");
                chmodInfo.ArgumentList.Add(tempScript);

                using var chmodProcess = Process.Start(chmodInfo);
                if (chmodProcess != null)
                {
                    await chmodProcess.WaitForExitAsync();
                }

                // Execute via osascript with native macOS administrator password prompt
                // Using ArgumentList ensures zero command-line escaping / quote breakage
                string appleScript = $"do shell script \"\\\"{tempScript}\\\"\" with administrator privileges";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };
                startInfo.ArgumentList.Add("-e");
                startInfo.ArgumentList.Add(appleScript);

                using var process = new Process { StartInfo = startInfo };
                process.Start();

                string stdErr = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Failed to elevate ({process.ExitCode}): {stdErr}");
                }
            }
            finally
            {
                // Always clean up temp script
                if (File.Exists(tempScript))
                {
                    try { File.Delete(tempScript); } catch { }
                }
            }
        }

        public bool IsAppBlocked(AppProfile app)
        {
            if (OperatingSystem.IsWindows())
                return false;

            try
            {
                if (File.Exists("/etc/hosts"))
                {
                    string content = File.ReadAllText("/etc/hosts");
                    return content.Contains($"# HangUp_Block_Start_{app.Name}");
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
