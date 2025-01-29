using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Swop;
static class Paths {
    public static readonly string DataFolder =
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\titushm\\Swop\\";
    public static readonly string LogFile = $"{DataFolder}\\log.tmp";
    public static readonly string ProfilesDir = $"{DataFolder}\\profiles\\";
}
public static class Utils{
    
    
    public static void ClearLogFile(){
        ValidatePaths();
        File.WriteAllText(Paths.LogFile, "");
        Log("Log file cleared");
    }

    private static void EnsurePath(string path){
        bool isFile = path.Contains(".");
        if (isFile && !File.Exists(path)) {
            File.Create(path);
        } else if (!Directory.Exists(path)) { 
            Directory.CreateDirectory(path);
        }
    }
    public static void ValidatePaths(){
        EnsurePath(Paths.DataFolder);
        EnsurePath(Paths.LogFile);
        EnsurePath(Paths.ProfilesDir);
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

    public static JObject? GetProfileJson(string profileName){
        ValidatePaths();
        string profilePath = Paths.ProfilesDir + profileName;
        if (!File.Exists(profilePath)) return null;
        string jsonString = File.ReadAllText(profilePath);
        JObject? jsonObject = JsonConvert.DeserializeObject<JObject>(jsonString);
        return jsonObject;
    }
    
    public static void WriteProfileJson(string profileName, JObject jsonObject){
        ValidatePaths();
        string profilePath = Paths.ProfilesDir + profileName;
        if (!File.Exists(profilePath)) File.Create(profilePath);
        string jsonString = JsonConvert.SerializeObject(jsonObject);
        File.WriteAllText(profilePath, jsonString);
    }
    
    public static string[] GetProfiles(){
        ValidatePaths();
        string[] profilePaths = Directory.GetDirectories(Paths.ProfilesDir, "*", SearchOption.TopDirectoryOnly);
        List<string> profileNames = new();
        foreach (string path in profilePaths){
            string fileName = path.Split(Paths.ProfilesDir)[1];
            profileNames.Add(fileName);
        }
        return profileNames.ToArray();
    }
}