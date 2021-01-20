using System.Configuration;

namespace BlackMagicSwitcherWebApi
{
    public static class GlobalAssets
    {
        public static string GetConfigValue(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }


        public static void Init()
        {
            var switcherIp = GetConfigValue("SwitcherIpAddress");
            var swicther = new Switcher(switcherIp);
        }
    }
}