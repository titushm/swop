using System.IO;
using System.Windows.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Swop;

public static class Utils{
    public static class Paths {
        public static readonly string DataFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\titushm\\Swop\\";
        public static readonly string LogFile = $"{DataFolder}\\log.tmp";
        public static readonly string ProfilesDir = $"{DataFolder}\\profiles\\";
        public static readonly string ConfigFile = $"{DataFolder}\\config.json";
        public static readonly string ModCacheFile = $"{DataFolder}\\mod_name_cache.json";
    }
    private static readonly object FileLock = new();

    public static bool EnsureProfileOverwrite(string profileName, ItemCollection items){
        bool canceled = false;
        foreach (ListBoxItem item in items){
            if (item.Tag.ToString()?.ToLower() == profileName.ToLower()){
                AdonisUI.Controls.MessageBoxResult result = AdonisUI.Controls.MessageBox.Show("Are you sure you want to overwrite your profile?", "Overwrite Profile", AdonisUI.Controls.MessageBoxButton.OKCancel);
                if (result != AdonisUI.Controls.MessageBoxResult.OK) canceled = true;
                break;
            }
        }
        return canceled;
    }
    public static void ClearLogFile(){
        ValidatePaths();
        File.WriteAllText(Paths.LogFile, "");
        Log("Log file cleared");
    }

    private static void EnsurePath(string path){
        bool isFile = path.Contains(".");
        if (isFile && !File.Exists(path)) {
            File.Create(path);
        } else if (!isFile && !Directory.Exists(path)) { 
            Directory.CreateDirectory(path);
        }
    }
    
    public static void ValidatePaths(){
        EnsurePath(Paths.DataFolder);
        EnsurePath(Paths.LogFile);
        EnsurePath(Paths.ProfilesDir);
        EnsurePath(Paths.ModCacheFile);
    }
    
    public static void Log(string text){
        ValidatePaths();
        try {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            StreamWriter logWriter = File.AppendText(Paths.LogFile);
            logWriter.Write($"[{timeStamp}] {text}\n");
            logWriter.Close();
        }
        catch{
            // ignored
        }
    }
    public static Dictionary<string, string> GetModCache(){
        ValidatePaths();
        Dictionary<string, string>? cache;
        lock (FileLock){
            string jsonString = File.ReadAllText(Paths.ModCacheFile);
            cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
        }
        return cache ?? new Dictionary<string, string>();
    }
    
    public static void CacheMods(Dictionary<string, string> modNames){
        ValidatePaths();
        lock (FileLock){
            Dictionary<string, string> cache = GetModCache();
            foreach (KeyValuePair<string,string> mod in modNames){
                cache[mod.Key] = mod.Value;
            }
            string jsonString = JsonConvert.SerializeObject(cache);
            File.WriteAllText(Paths.ModCacheFile, jsonString);
        }
    }
    
    public static T? GetConfigProperty<T>(string propertyName) {
        ValidatePaths();
        string jsonString = File.ReadAllText(Paths.ConfigFile);
        JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
        if (jsonObject != null && jsonObject.ContainsKey(propertyName)) {
            return jsonObject[propertyName]!.ToObject<T>();
        }

        return default;
    }

    public static void SetConfigProperty(string propertyName, JToken value) {
        ValidatePaths(); 
        string jsonString = File.ReadAllText(Paths.ConfigFile);
        JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
        if (jsonObject != null && jsonObject.ContainsKey(propertyName)) {
            jsonObject.Property(propertyName)?.Remove();
        }

        jsonObject?.Add(propertyName, value);
        jsonString = JsonConvert.SerializeObject(jsonObject);
        File.WriteAllText(Paths.ConfigFile, jsonString);
    }
    
    public static void DeleteProfile(string profileName){
        ValidatePaths();
        string profilePath = Paths.ProfilesDir + profileName + ".json";
        if (!File.Exists(profilePath)) return;
        lock (FileLock) {
            File.Delete(profilePath);
        }
        File.Delete(profilePath);
    }
    
    public static JObject? GetProfileJson(string profileName){
        JObject? jsonObject;
        lock (FileLock) {
            ValidatePaths();
            string profilePath = Paths.ProfilesDir + profileName + ".json";
            if (!File.Exists(profilePath)) return null;
            string jsonString = File.ReadAllText(profilePath);
            jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
        }
        return jsonObject;
    }
    
    public static void WriteProfileJson(string profileName, object obj){
        ValidatePaths();
        lock (FileLock){
            string profilePath = Paths.ProfilesDir + profileName + ".json";
            string jsonString = JsonConvert.SerializeObject(obj);
            File.WriteAllText(profilePath, jsonString);
        }
    }
    
    public static Dictionary<string, JObject> GetProfiles(){
        ValidatePaths();
        Dictionary<string, JObject> profiles = new();
        lock (FileLock){
            string[] profilePaths = Directory.GetFiles(Paths.ProfilesDir, "*.json", SearchOption.TopDirectoryOnly);
            foreach (string path in profilePaths){
                string profileName = path.Split(Paths.ProfilesDir)[1].Replace(".json", "");
                JObject? profileJson = GetProfileJson(profileName);
                if (profileJson == null) continue;
                profiles[profileName] = profileJson;
            }
        }
        return profiles;
    }

    public static bool EnsureProfile(string path){
        if (!File.Exists(path)) return false;
        string jsonString = File.ReadAllText(path);
        JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
        bool isValid = true;
        if (jsonObject != null){
            foreach (KeyValuePair<string, JToken?> item in jsonObject){
                try{  item.Value?.ToObject<bool>(); } catch{ isValid = false;}

                if (!isValid || item.Key.GetType() != typeof(String) || item.Value.ToObject<bool>().GetType() != typeof(Boolean)){
                    isValid = false;
                    break;
                }
            }
        }
        if (isValid) return true;
        return false;
    }
}