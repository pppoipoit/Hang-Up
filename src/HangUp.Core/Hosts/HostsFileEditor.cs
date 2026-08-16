namespace HangUp.Core.Hosts
{
    public class HostsFileEditor
    {
        private readonly string _hostsPath = System.IO.Path.Combine(
            System.Environment.GetFolderPath(System.Environment.SpecialFolder.System), 
            "drivers", "etc", "hosts");

        public bool BlockDomains(Models.AppProfile profile)
        {
            if (!File.Exists(_hostsPath)) return false;
            
            var lines = File.ReadAllLines(_hostsPath).ToList();
            foreach (var domain in profile.Domains)
            {
                var entry = "0.0.0.0 " + domain;
                if (!lines.Any(l => l.Contains(domain)))
                {
                    lines.Add(entry);
                }
            }
            File.WriteAllLines(_hostsPath, lines);
            FlushDns();
            return true;
        }

        public bool UnblockDomains(Models.AppProfile profile)
        {
            if (!File.Exists(_hostsPath)) return false;
            
            var lines = File.ReadAllLines(_hostsPath);
            var filtered = lines.Where(l => !profile.Domains.Any(d => l.Contains("0.0.0.0 " + d)));
            File.WriteAllLines(_hostsPath, filtered);
            FlushDns();
            return true;
        }

        private void FlushDns()
        {
            var p = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "ipconfig",
                    Arguments = "/flushdns",
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            p.Start();
            p.WaitForExit(5000);
        }
    }
}