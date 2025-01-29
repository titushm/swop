using System.IO;

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
    
}