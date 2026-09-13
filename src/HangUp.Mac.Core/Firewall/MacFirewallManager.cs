using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HangUp.Mac.Core.Models;

namespace HangUp.Mac.Core.Firewall
{
    public class MacFirewallManager
    {
        private const string HOSTS_PATH = "/etc/hosts";
        private const string DEFAULT_MACOS_HOSTS =
@"##
# Host Database
#
# localhost is used to configure the loopback interface
# when the system is booting.  Do not change this entry.
##
127.0.0.1	localhost
255.255.255.255	broadcasthost
::1             localhost
";

        // For testing/mocking when running on non-macOS (e.g. Windows dev)
        private readonly HashSet<string> _mockBlockedApps = new(StringComparer.OrdinalIgnoreCase);

        public MacFirewallManager()
        {
        }

        public async Task BlockAppAsync(AppProfile app)
        {
            if (OperatingSystem.IsWindows())
            {
                _mockBlockedApps.Add(app.Name);
                await Task.Delay(200);
                return;
            }

            string currentHosts = ReadCurrentHosts();
            string updatedHosts = ApplyAppBlock(currentHosts, app, block: true);
            await WriteHostsAndFlushDnsAsync(updatedHosts);
        }

        public async Task UnblockAppAsync(AppProfile app)
        {
            if (OperatingSystem.IsWindows())
            {
                _mockBlockedApps.Remove(app.Name);
                await Task.Delay(200);
                return;
            }

            string currentHosts = ReadCurrentHosts();
            string updatedHosts = ApplyAppBlock(currentHosts, app, block: false);
            await WriteHostsAndFlushDnsAsync(updatedHosts);
        }

        public async Task BlockAllAppsAsync(IEnumerable<AppProfile> apps)
        {
            if (OperatingSystem.IsWindows())
            {
                foreach (var app in apps) _mockBlockedApps.Add(app.Name);
                await Task.Delay(300);
                return;
            }

            string currentHosts = ReadCurrentHosts();
            string updatedHosts = currentHosts;
            foreach (var app in apps)
            {
                updatedHosts = ApplyAppBlock(updatedHosts, app, block: true);
            }
            await WriteHostsAndFlushDnsAsync(updatedHosts);
        }

        public async Task UnblockAllAppsAsync(IEnumerable<AppProfile> apps)
        {
            if (OperatingSystem.IsWindows())
            {
                _mockBlockedApps.Clear();
                await Task.Delay(300);
                return;
            }

            string currentHosts = ReadCurrentHosts();
            string updatedHosts = currentHosts;
            foreach (var app in apps)
            {
                updatedHosts = ApplyAppBlock(updatedHosts, app, block: false);
            }
            await WriteHostsAndFlushDnsAsync(updatedHosts);
        }

        private static string ReadCurrentHosts()
        {
            try
            {
                if (File.Exists(HOSTS_PATH))
                {
                    return File.ReadAllText(HOSTS_PATH, Encoding.UTF8);
                }
            }
            catch
            {
            }
            return DEFAULT_MACOS_HOSTS;
        }

        private static string ApplyAppBlock(string hostsContent, AppProfile app, bool block)
        {
            // Normalize line endings to LF
            string content = hostsContent.Replace("\r\n", "\n").TrimEnd();

            // Markers to remove (supports new formatted markers and legacy markers)
            string newStartMarker = $"# --- HangUp_Block_Start_{app.Name} ---";
            string newEndMarker = $"# --- HangUp_Block_End_{app.Name} ---";
            string legacyStartMarker = $"# HangUp_Block_Start_{app.Name}";
            string legacyEndMarker = $"# HangUp_Block_End_{app.Name}";

            // Remove existing block section for this app
            var lines = content.Split('\n').ToList();
            var filteredLines = new List<string>();
            bool inBlock = false;

            foreach (var line in lines)
            {
                string trimmed = line.Trim();
                if (trimmed == newStartMarker || trimmed == legacyStartMarker)
                {
                    inBlock = true;
                    continue;
                }
                if (inBlock)
                {
                    if (trimmed == newEndMarker || trimmed == legacyEndMarker)
                    {
                        inBlock = false;
                    }
                    continue;
                }
                filteredLines.Add(line);
            }

            // If blocking, append new block rules
            if (block && app.Domains != null && app.Domains.Count > 0)
            {
                filteredLines.Add(string.Empty);
                filteredLines.Add(newStartMarker);
                foreach (var domain in app.Domains)
                {
                    if (!string.IsNullOrWhiteSpace(domain))
                    {
                        string d = domain.Trim();
                        filteredLines.Add($"127.0.0.1 {d}");
                        filteredLines.Add($"::1 {d}");
                    }
                }
                filteredLines.Add(newEndMarker);
            }

            // Clean up duplicate blank lines and ensure trailing newline
            var result = new StringBuilder();
            bool prevBlank = false;
            foreach (var l in filteredLines)
            {
                bool isBlank = string.IsNullOrWhiteSpace(l);
                if (isBlank && prevBlank) continue;
                result.Append(l).Append('\n');
                prevBlank = isBlank;
            }

            return result.ToString();
        }

        private async Task WriteHostsAndFlushDnsAsync(string newHostsContent)
        {
            // Use /tmp (world-accessible, standard Unix temp directory on macOS)
            string tempHostsPath = $"/tmp/hangup_hosts_{Guid.NewGuid():N}";
            string tempScriptPath = $"/tmp/hangup_apply_{Guid.NewGuid():N}.sh";

            // Write target hosts content to /tmp with UTF8 without BOM
            await File.WriteAllTextAsync(tempHostsPath, newHostsContent, new UTF8Encoding(false));

            // Create apply shell script in /tmp
            var scriptBuilder = new StringBuilder();
            scriptBuilder.AppendLine("#!/bin/sh");
            scriptBuilder.AppendLine("set -e");
            scriptBuilder.AppendLine($"cp '{tempHostsPath}' '{HOSTS_PATH}'");
            scriptBuilder.AppendLine($"chmod 644 '{HOSTS_PATH}'");
            scriptBuilder.AppendLine("dscacheutil -flushcache || true");
            scriptBuilder.AppendLine("killall -HUP mDNSResponder || true");

            await File.WriteAllTextAsync(tempScriptPath, scriptBuilder.ToString(), new UTF8Encoding(false));

            try
            {
                // Execute via osascript with administrator privileges using ArgumentList
                // No quotes inside quotes needed because we execute 'sh /tmp/hangup_apply_xxx.sh'
                string appleScript = $"do shell script \"/bin/sh {tempScriptPath}\" with administrator privileges";

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

                var stdoutTask = process.StandardOutput.ReadToEndAsync();
                var stderrTask = process.StandardError.ReadToEndAsync();

                await Task.WhenAll(stdoutTask, stderrTask, process.WaitForExitAsync());

                string stdErr = await stderrTask;

                if (process.ExitCode != 0)
                {
                    throw new Exception(string.IsNullOrWhiteSpace(stdErr) ? $"Exit code {process.ExitCode}" : stdErr.Trim());
                }
            }
            finally
            {
                // Clean up temporary files in /tmp
                try { if (File.Exists(tempHostsPath)) File.Delete(tempHostsPath); } catch { }
                try { if (File.Exists(tempScriptPath)) File.Delete(tempScriptPath); } catch { }
            }
        }

        public bool IsAppBlocked(AppProfile app)
        {
            if (OperatingSystem.IsWindows())
            {
                return _mockBlockedApps.Contains(app.Name);
            }

            try
            {
                if (File.Exists(HOSTS_PATH))
                {
                    string content = File.ReadAllText(HOSTS_PATH);
                    return content.Contains($"# --- HangUp_Block_Start_{app.Name} ---") ||
                           content.Contains($"# HangUp_Block_Start_{app.Name}");
                }
            }
            catch
            {
            }
            return false;
        }
    }
}
