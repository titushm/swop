using System.IO;
using System.Windows;
using Newtonsoft.Json.Linq;

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

    public static async Task ApplyProfile(string gameID, JObject profile){
        string disabledModsPath = Utils.Paths.DisabledModsDir + gameID + "\\";
        await Utils.ValidatePaths();
        if (!Directory.Exists(disabledModsPath)) Directory.CreateDirectory(disabledModsPath);
        string[] disabledMods = Directory.GetDirectories(disabledModsPath, "*", SearchOption.TopDirectoryOnly);
        List<string> enabledMods = new();
        foreach (KeyValuePair<string, JToken> mod in profile){
            if (mod.Value.ToObject<bool>()) enabledMods.Add(mod.Key);
        }

        foreach (var path in disabledMods){
            string folderName = Path.GetFileName(path);
            string modPath = WorkshopPath + gameID + "\\" + folderName;
            if (Directory.Exists(modPath)) Directory.Delete(modPath);
            Directory.Move(path, modPath);
        }
        
        string[] workshopMods = Directory.GetDirectories(WorkshopPath + gameID, "*", SearchOption.TopDirectoryOnly);
        foreach (string path in workshopMods){
            string fileName = Path.GetFileName(path);
            if (!enabledMods.Contains(fileName)){
                if (Directory.Exists(disabledModsPath + fileName)) Directory.Delete(disabledModsPath + fileName);
                Directory.Move(path, disabledModsPath + fileName);
            }
        }
        
    }
    
    public static async Task<Dictionary<string, string>> GetMods(string? id) {
        await Utils.ValidatePaths();
        if (!File.Exists(Utils.Paths.ModCacheFile)) await File.WriteAllTextAsync(Utils.Paths.ModCacheFile, "{}");
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
            string? modName = await Steam.GetModName(modID);
            Application.Current.Dispatcher.Invoke(() => loadingPage.UpdateProgress(i, $"Getting Mod Info: {modName} ({modID})"));
            if (modName != null) mods[modID] = modName;
        }
        Application.Current.Dispatcher.Invoke(() => mainWindow.OverlayFrame.Content = null);
        return mods;
    }
}