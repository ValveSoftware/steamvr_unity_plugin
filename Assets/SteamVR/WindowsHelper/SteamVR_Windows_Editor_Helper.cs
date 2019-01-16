using UnityEngine;
using System.Collections;

namespace Valve.VR
{
    public class SteamVR_Windows_Editor_Helper
    {
        public enum BrowserApplication
        {
            Unknown,
            InternetExplorer,
            Firefox,
            Chrome,
            Opera,
            Safari,
            Edge,
        }

        public static BrowserApplication GetDefaultBrowser()
        {
#if UNITY_EDITOR
    #if UNITY_STANDALONE_WIN
            const string userChoice = @"Software\Microsoft\Windows\Shell\Associations\UrlAssociations\http\UserChoice";
            string progId;
            using (Microsoft.Win32.RegistryKey userChoiceKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(userChoice))
            {
                if (userChoiceKey == null)
                {
                    return BrowserApplication.Unknown;
                }
                object progIdValue = userChoiceKey.GetValue("Progid");
                if (progIdValue == null)
                {
                    return BrowserApplication.Unknown;
                }
                progId = progIdValue.ToString();
                switch (progId)
                {
                    case "IE.HTTP":
                        return BrowserApplication.InternetExplorer;
                    case "FirefoxURL":
                        return BrowserApplication.Firefox;
                    case "ChromeHTML":
                        return BrowserApplication.Chrome;
                    case "OperaStable":
                        return BrowserApplication.Opera;
                    case "SafariHTML":
                        return BrowserApplication.Safari;
                    case "AppXq0fevzme2pys62n3e0fbqa7peapykr8v":
                        return BrowserApplication.Edge;
                    default:
                        return BrowserApplication.Unknown;
                }
            }
    #else
            return BrowserApplication.Firefox;
    #endif
#else
            return BrowserApplication.Firefox;
#endif
        }
    }
}