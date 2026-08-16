using System;
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
            var scriptBuilder = new StringBuilder();
            
            if (app.Domains != null && app.Domains.Count > 0)
            {
                // First ensure it's not already blocked to avoid duplicates
                string startMarker = $"# HangUp_Block_Start_{app.Name}";
                string endMarker = $"# HangUp_Block_End_{app.Name}";
                scriptBuilder.AppendLine($"sed -i '' '/{startMarker}/,/{endMarker}/d' /etc/hosts");

                scriptBuilder.AppendLine($"echo '{startMarker}' >> /etc/hosts");
                foreach (var domain in app.Domains)
                {
                    scriptBuilder.AppendLine($"echo '127.0.0.1 {domain}' >> /etc/hosts");
                    scriptBuilder.AppendLine($"echo '::1 {domain}' >> /etc/hosts");
                }
                scriptBuilder.AppendLine($"echo '{endMarker}' >> /etc/hosts");
                
                scriptBuilder.AppendLine("dscacheutil -flushcache");
                scriptBuilder.AppendLine("killall -HUP mDNSResponder");
            }

            if (scriptBuilder.Length > 0)
            {
                await RunAsAdminAsync(scriptBuilder.ToString());
            }
        }

        public async Task UnblockAppAsync(AppProfile app)
        {
            var scriptBuilder = new StringBuilder();
            
            if (app.Domains != null && app.Domains.Count > 0)
            {
                string startMarker = $"# HangUp_Block_Start_{app.Name}";
                string endMarker = $"# HangUp_Block_End_{app.Name}";
                scriptBuilder.AppendLine($"sed -i '' '/{startMarker}/,/{endMarker}/d' /etc/hosts");
                
                scriptBuilder.AppendLine("dscacheutil -flushcache");
                scriptBuilder.AppendLine("killall -HUP mDNSResponder");
            }

            if (scriptBuilder.Length > 0)
            {
                await RunAsAdminAsync(scriptBuilder.ToString());
            }
        }

        private async Task RunAsAdminAsync(string bashScript)
        {
            if (OperatingSystem.IsWindows())
            {
                // Mock behavior for testing UI on Windows
                Console.WriteLine("Mocking Mac Sudo Execution on Windows:\n" + bashScript);
                await Task.Delay(500);
                return;
            }

            // Escape quotes for AppleScript (replace " with \")
            string escapedScript = bashScript.Replace("\"", "\\\"");
            
            // Execute via osascript to get native GUI password prompt
            string appleScript = $"do shell script \"{escapedScript}\" with administrator privileges";
            
            var tcs = new TaskCompletionSource();
            
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "osascript",
                    Arguments = $"-e '{appleScript}'",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            process.Exited += (s, e) => 
            {
                if (process.ExitCode == 0) tcs.SetResult();
                else tcs.SetException(new Exception($"Failed to elevate. Exit code: {process.ExitCode}"));
            };

            process.Start();
            await tcs.Task;
        }
        
        public bool IsAppBlocked(AppProfile app)
        {
            if (OperatingSystem.IsWindows())
                return false; // Mock

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
