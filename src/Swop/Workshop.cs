using System.IO;
using System.Windows;
using System.Windows.Navigation;

namespace Swop;

public static class Workshop {
    public static readonly string ProgramFiles = Environment.GetEnvironmentVariable("programfiles(x86)");
    public static readonly string WorkshopPath = ProgramFiles + "\\steam\\steamapps\\workshop\\content\\";
    
    public static Dictionary<string, string> GetWorkshopAppIDs(){
        string[] ids = Directory.GetDirectories(WorkshopPath, "*", SearchOption.TopDirectoryOnly);
        Dictionary<string, string> apps = new();
        foreach (string path in ids){
            string id = path.Replace(WorkshopPath, "");
            string? name = Steam.GetGameName(id);
            if (name != null) apps[id] = name;
        }
        return apps;
    }
    
    public static Dictionary<string, string> GetMods(string? id) {
        Utils.ValidatePaths();
        string[] modPaths = Directory.GetDirectories(WorkshopPath + id, "*", SearchOption.TopDirectoryOnly);
        LoadingContentPage? loadingPage = null;
        MainWindow mainWindow = null;
        Application.Current.Dispatcher.Invoke(() => {
            loadingPage = new LoadingContentPage(modPaths.Length);
            mainWindow = (MainWindow)Application.Current.MainWindow;
            mainWindow.OverlayFrame.Navigate(loadingPage);
        });
        Dictionary<string, string> mods = new();
        int i = 0;
        foreach (string path in modPaths) {
            i++;
            if (!File.Exists(path + "\\metadata.mod")) continue;
            string modID = path.Replace(WorkshopPath + id, "").Replace("\\", "");
            string? modName = Steam.GetModName(modID);
            Application.Current.Dispatcher.Invoke(() => loadingPage.UpdateProgress(i, $"Getting Mod Info: {modName} ({modID})"));
            if (modName != null) mods[modID] = modName;
        }
        Utils.CacheMods(mods);
        Application.Current.Dispatcher.Invoke(() => mainWindow.OverlayFrame.Content = null);
        return mods;
    }
}