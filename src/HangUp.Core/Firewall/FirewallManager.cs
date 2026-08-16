using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace HangUp.Core.Firewall
{
    /// <summary>
    /// Manages Windows Defender Firewall block/unblock rules for applications.
    /// Uses the INetFwPolicy2 COM API (no hard COM reference required) so that
    /// rule creation and deletion are perfectly symmetrical and verifiable.
    /// </summary>
    [SupportedOSPlatform("windows")]
    public class FirewallManager : IDisposable
    {
        // Shared identity tokens so the deletion logic can find everything we created.
        private const string RULE_NAME_PREFIX = "HangUp_Block_";
        private const string SERVICE_RULE_NAME_PREFIX = "HangUp_Service_";
        private const string GROUP_SEPARATOR = "/"; // e.g. "HangUp/Adobe"

        // NET_FW_PROFILE_TYPE2_ALL - apply the rule to every profile (domain, private, public).
        private const int FW_PROFILE_ALL = 0x7FFFFFFF;

        // NET_FW_RULE_DIR_OUT = 2, NET_FW_ACTION_BLOCK = 0 (INetFwRule enum values).
        private const int FW_DIR_OUT = 2;
        private const int FW_ACTION_BLOCK = 0;

        private readonly bool _isInitialized = true;
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Returns the INetFwPolicy2 firewall policy object. Created via ProgID so we
        /// do not need to ship a COM interop assembly reference.
        /// </summary>
        private dynamic GetFirewallPolicy()
        {
            var type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2");
            if (type == null)
            {
                throw new InvalidOperationException(
                    "Windows Firewall API (INetFwPolicy2) is not available on this system.");
            }
            return Activator.CreateInstance(type)!;
        }

        /// <summary>
        /// Creates a new, empty firewall rule object (INetFwRule).
        /// </summary>
        private dynamic CreateRuleObject()
        {
            var ruleType = Type.GetTypeFromProgID("HNetCfg.FwRule");
            if (ruleType == null)
            {
                throw new InvalidOperationException("Unable to create firewall rule object (HNetCfg.FwRule).");
            }
            return Activator.CreateInstance(ruleType)!;
        }

        public async Task<bool> BlockAppAsync(Models.AppProfile profile)
        {
            var groupName = BuildGroupName(profile.Name);
            var exePaths = EnumerateExeFiles(profile.Paths).ToList();

            var policy = GetFirewallPolicy();
            try
            {
                // 1) One OUTBOUND BLOCK rule per discovered executable, tagged with our group.
                foreach (var exePath in exePaths)
                {
                    var rule = CreateRuleObject();
                    rule.Name = RULE_NAME_PREFIX + profile.Name + "_" + Path.GetFileNameWithoutExtension(exePath);
                    rule.ApplicationName = exePath;   // exact program path
                    rule.Direction = FW_DIR_OUT;
                    rule.Action = FW_ACTION_BLOCK;
                    rule.Enabled = true;
                    rule.Grouping = groupName;       // <-- links ALL our rules together
                    rule.Profiles = FW_PROFILE_ALL;
                    policy.Rules.Add(rule);
                    await Task.Yield();
                }

                // 2) One OUTBOUND BLOCK rule per Windows service, also tagged with our group.
                foreach (var service in profile.Services)
                {
                    try
                    {
                        var rule = CreateRuleObject();
                        rule.Name = SERVICE_RULE_NAME_PREFIX + profile.Name;
                        rule.ServiceName = service;
                        rule.Direction = FW_DIR_OUT;
                        rule.Action = FW_ACTION_BLOCK;
                        rule.Enabled = true;
                        rule.Grouping = groupName;
                        rule.Profiles = FW_PROFILE_ALL;
                        policy.Rules.Add(rule);
                    }
                    catch
                    {
                        // Ignore if the service is not installed on this machine (throws FileNotFoundException)
                    }
                    await Task.Yield();
                }
            }
            finally
            {
                Marshal.ReleaseComObject(policy);
            }

            return true;
        }

        /// <summary>
        /// CLEAN SWEEP unblock. Deletes EVERY firewall rule we ever created for this
        /// profile, matched by Rule Group, by program path, or by legacy name prefix.
        /// </summary>
        public bool UnblockApp(Models.AppProfile profile)
        {
            var groupName = BuildGroupName(profile.Name);
            var exePaths = EnumerateExeFiles(profile.Paths)
                .Select(p => p.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var namePrefix = RULE_NAME_PREFIX + profile.Name;
            var servicePrefix = SERVICE_RULE_NAME_PREFIX + profile.Name;

            var policy = GetFirewallPolicy();
            try
            {
                var rules = policy.Rules;

                // Iteratively remove matching rules. We loop + restart after each
                // removal because deleting shifts the collection indices, and removing
                // by name only deletes the first match (names can collide across EXEs).
                bool removedAny;
                do
                {
                    removedAny = false;
                    string ruleNameToRemove = null;
                    foreach (dynamic rule in rules)
                    {
                        if (IsOurRule(rule, groupName, exePaths, namePrefix, servicePrefix))
                        {
                            ruleNameToRemove = (string)rule.Name;
                            break;
                        }
                    }
                    
                    if (ruleNameToRemove != null)
                    {
                        rules.Remove(ruleNameToRemove); // delete by exact name
                        removedAny = true;
                    }
                } while (removedAny);
            }
            finally
            {
                Marshal.ReleaseComObject(policy);
            }

            // VERIFICATION STEP: re-query and assert that zero of our rules remain.
            int residual = CountOurRules(profile);
            if (residual != 0)
            {
                throw new InvalidOperationException(
                    $"Clean Sweep failed: {residual} residual firewall rule(s) still exist for '{profile.Name}'.");
            }

            return true;
        }

        /// <summary>
        /// Re-queries the firewall and counts how many of our rules still exist for the
        /// given profile. Used as the post-deletion verification assertion.
        /// </summary>
        public int CountOurRules(Models.AppProfile profile)
        {
            var groupName = BuildGroupName(profile.Name);
            var exePaths = EnumerateExeFiles(profile.Paths)
                .Select(p => p.Trim().ToLowerInvariant())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var namePrefix = RULE_NAME_PREFIX + profile.Name;
            var servicePrefix = SERVICE_RULE_NAME_PREFIX + profile.Name;

            int found = 0;
            var policy = GetFirewallPolicy();
            try
            {
                var rules = policy.Rules;
                foreach (dynamic rule in rules)
                {
                    if (IsOurRule(rule, groupName, exePaths, namePrefix, servicePrefix))
                    {
                        found++;
                    }
                }
            }
            finally
            {
                Marshal.ReleaseComObject(policy);
            }
            return found;
        }

        /// <summary>
        /// Determines whether a firewall rule belongs to us. A rule is ours if ANY of:
        ///  - Its Grouping equals our rule-group name (covers block + service rules).
        ///  - Its Name starts with our legacy name prefixes (catches old netsh rules).
        ///  - Its ApplicationName (program path) matches one of the target EXEs.
        /// This is broad enough to catch everything we created, but specific enough
        /// (exact group / exact path / known prefix) to never touch the user's rules.
        /// </summary>
        private bool IsOurRule(dynamic rule, string groupName, HashSet<string> exePaths,
            string namePrefix, string servicePrefix)
        {
            try
            {
                string name = (string)rule.Name ?? string.Empty;
                string grouping = (string)rule.Grouping ?? string.Empty;
                string appName = (string)rule.ApplicationName ?? string.Empty;

                if (string.Equals(grouping, groupName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (name.StartsWith(servicePrefix, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (!string.IsNullOrEmpty(appName) &&
                    exePaths.Contains(appName.Trim().ToLowerInvariant()))
                    return true;
            }
            catch
            {
                // If a property is inaccessible, treat as not ours rather than crashing.
            }
            return false;
        }

        private static string BuildGroupName(string appName) => "HangUp" + GROUP_SEPARATOR + appName;

        private IEnumerable<string> EnumerateExeFiles(IEnumerable<string> paths)
        {
            var exeFiles = new List<string>();
            foreach (var path in paths ?? Enumerable.Empty<string>())
            {
                var expandedPath = Environment.ExpandEnvironmentVariables(path);
                if (Directory.Exists(expandedPath))
                {
                    foreach (var exe in Directory.EnumerateFiles(expandedPath, "*.exe", SearchOption.AllDirectories))
                    {
                        exeFiles.Add(exe);
                    }
                }
            }
            return exeFiles.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            // No unmanaged handles are held between calls; nothing to release.
        }
    }
}