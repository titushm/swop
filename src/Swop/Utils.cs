using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Windows.Controls;
using AdonisUI.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Swop;

public static class Utils{
    public static Version VERSION = new(0, 5, 0);
    public static string REPO_URL = "https://github.com/titushm/swop";
    public static HttpClient HttpClient = new();
    public static class Paths {
        public static readonly string DataFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\titushm\\Swop\\";
        public static readonly string LogFile = $"{DataFolder}\\log.tmp";
        public static readonly string ProfilesDir = $"{DataFolder}\\profiles\\";
        public static readonly string ConfigFile = $"{DataFolder}\\config.json";
        public static readonly string ModCacheFile = $"{DataFolder}\\mod_name_cache.json";
        public static readonly string DisabledModsDir = $"{DataFolder}\\disabled\\";
    }
    
        
    public static async void CheckUpdate() {
        await Task.Run(() => {
            try {
                Task<HttpResponseMessage> response = HttpClient.GetAsync(REPO_URL + "/releases/latest");
                string redirectUrl = response.Result.RequestMessage.RequestUri.ToString();
                Version latestVersion = Version.Parse(redirectUrl.Split('/').Last());
                if (latestVersion > VERSION) {
                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate {
                        MessageBoxResult updateMessageResult = MessageBox.Show(
                            $"Update available: {latestVersion}\nWould you like to go to the github repo to update",
                            "Swop", MessageBoxButton.YesNo);
                        if (updateMessageResult == MessageBoxResult.Yes) {
                            Process.Start(new ProcessStartInfo {
                                FileName = REPO_URL + "/releases/latest",
                                UseShellExecute = true
                            });
                        }
                    });
                }
            }
            catch{
                // ignored
            }
        });
    }
    
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
    public static async void ClearLogFile(){
        try{
            File.Delete(Paths.LogFile);
        } catch {}
        await ValidatePaths();
        await Log("Log file cleared");
    }

    public static async Task EnsurePath(string path){
        bool isFile = path.Contains(".");
        if (isFile && !File.Exists(path)) {
            File.Create(path);
        } else if (!isFile && !Directory.Exists(path)) { 
            Directory.CreateDirectory(path);
        }
    }
    
    public static async Task ValidatePaths(){
        await EnsurePath(Paths.DataFolder);
        await EnsurePath(Paths.LogFile);
        await EnsurePath(Paths.ProfilesDir);
    }
    
    public static async Task Log(string text){
        await ValidatePaths();
        try {
            string timeStamp = DateTime.Now.ToString("HH:mm:ss");
            StreamWriter logWriter = File.AppendText(Paths.LogFile);
            await logWriter.WriteAsync($"[{timeStamp}] {text}\n");
            logWriter.Close();
        }
        catch{
            // ignored
        }
    }
    public static async Task<Dictionary<string, string>> GetModCache(){
        await ValidatePaths();
        await EnsurePath(Paths.ModCacheFile);
        Dictionary<string, string>? cache;
        string jsonString = File.ReadAllText(Paths.ModCacheFile);
        cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonString);
        return cache ?? new Dictionary<string, string>();
    }
    
    public static async Task CacheMod(Dictionary<string, string> mods){
        await ValidatePaths();
        Dictionary<string, string> cache = await GetModCache();
        foreach (KeyValuePair<string,string> mod in mods){
            cache[mod.Key] = mod.Value;
        }
        string jsonString = JsonConvert.SerializeObject(cache);
        await File.WriteAllTextAsync(Paths.ModCacheFile, jsonString);
    }
    
    public static async Task<T?> GetConfigProperty<T>(string propertyName) {
        await ValidatePaths();
        if (!File.Exists(Paths.ConfigFile)) await File.WriteAllTextAsync(Paths.ConfigFile, "{}");
        string jsonString = await File.ReadAllTextAsync(Paths.ConfigFile);
        JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
        if (jsonObject != null && jsonObject.ContainsKey(propertyName)) {
            return jsonObject[propertyName]!.ToObject<T>();
        }
        return default;
    }
    
    public static async Task SetConfigProperty(string propertyName, JToken value) {
        await ValidatePaths();
        if (!File.Exists(Paths.ConfigFile)) await File.WriteAllTextAsync(Paths.ConfigFile, "{}");
        string jsonString = await File.ReadAllTextAsync(Paths.ConfigFile);
        JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
        if (jsonObject != null && jsonObject.ContainsKey(propertyName)) {
            jsonObject.Property(propertyName)?.Remove();
        }
    
        jsonObject?.Add(propertyName, value);
        jsonString = JsonConvert.SerializeObject(jsonObject);
        File.WriteAllText(Paths.ConfigFile, jsonString);
    }
    
    public static async Task DeleteProfile(string gameID, string profileName) {
        await ValidatePaths();
        string profilePath = $"{Paths.ProfilesDir}\\{gameID}\\{profileName}.json";
        await EnsurePath(profilePath);
        if (!File.Exists(profilePath)) return;
            try {
                File.Delete(profilePath);
            } catch (Exception ex) {
                await Log(ex.ToString());
            }
    }

    public static async Task<JObject?> GetProfileJson(string gameID, string profileName) {
        await ValidatePaths();
        string profilePath = $"{Paths.ProfilesDir}\\{gameID}\\{profileName}.json";
        await EnsurePath(profilePath);
        if (!File.Exists(profilePath)) return null;

        try {
            string jsonString = await File.ReadAllTextAsync(profilePath);
            return JsonConvert.DeserializeObject<JObject>(jsonString);
        } catch (Exception ex){
            await Log(ex.ToString());
            return null;
        }
    }

    public static async Task WriteProfileJson(string gameID, string profileName, object obj)
    {
        await ValidatePaths();
        string profilePath = $"{Paths.ProfilesDir}\\{gameID}\\{profileName}.json";
        try {
            string jsonString = JsonConvert.SerializeObject(obj);
            await File.WriteAllTextAsync(profilePath, jsonString);
        } catch (Exception ex) {
            await Log(ex.ToString());
        }
    }

    public static async Task<Dictionary<string, JObject>> GetProfiles(string gameID) {
        await ValidatePaths();
        Dictionary<string, JObject> profiles = new();
        string profilesPath = $"{Paths.ProfilesDir}\\{gameID}";
        await EnsurePath(profilesPath);
        string[] profilePaths = Directory.GetFiles(profilesPath, "*.json", SearchOption.TopDirectoryOnly);

        foreach (string path in profilePaths) {
            string profileName = Path.GetFileNameWithoutExtension(path);
            JObject? profileJson = await GetProfileJson(gameID, profileName);
            if (profileJson != null) {
                profiles[profileName] = profileJson;
            }
        }
        return profiles;
    }

    public static bool IsValidProfile(string path) {
        if (!File.Exists(path)) return false;

        try {
                string jsonString = File.ReadAllText(path);
                JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
                if (jsonObject != null) {
                    foreach (KeyValuePair<string, JToken?> item in jsonObject) {
                        if (item.Key.GetType() != typeof(string) || item.Value?.Type != JTokenType.Boolean) {
                            return false;
                        }
                    }
                }
        } catch (Exception) {
            return false;
        }
        return true;
    }
}