using System.Collections.Generic;

namespace HangUp.Mac.Core.Models
{
    public class AppProfile
    {
        public string Name { get; set; } = "";
        public List<string> Paths { get; set; } = new();
        public List<string> Domains { get; set; } = new();
        public List<string> Services { get; set; } = new();
        public string Icon { get; set; } = "📦";
        public string GradientStart { get; set; } = "#3b82f6";
        public string GradientEnd { get; set; } = "#06b6d4";
    }
}
