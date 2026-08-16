using System.Collections.Generic;
using System.Linq;
using HangUp.Core.Models;

namespace HangUp.Core.Firewall
{
    public class BlockStatus
    {
        public bool IsBlocked { get; set; }
        public int RuleCount { get; set; }
    }

    public class FirewallStatusService
    {
        private readonly FirewallManager _firewallManager;

        public FirewallStatusService(FirewallManager firewallManager)
        {
            _firewallManager = firewallManager;
        }

        public BlockStatus GetBlockStatus(AppProfile profile)
        {
            if (profile == null) return new BlockStatus { IsBlocked = false, RuleCount = 0 };

            int ruleCount = _firewallManager.CountOurRules(profile);
            
            return new BlockStatus
            {
                IsBlocked = ruleCount > 0,
                RuleCount = ruleCount
            };
        }

        public Dictionary<string, BlockStatus> GetAllStatuses(IEnumerable<AppProfile> profiles)
        {
            var statuses = new Dictionary<string, BlockStatus>();
            foreach (var profile in profiles)
            {
                statuses[profile.Name] = GetBlockStatus(profile);
            }
            return statuses;
        }
    }
}
