using System.Diagnostics;
using HtmlAgilityPack;

namespace Swop;



public static class Steam {
        public static readonly string StoreUrl = "https://store.steampowered.com/app/";
        public static readonly string FileDetailsUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=";

        public static void OpenSteamLink(string link){
                Process.Start(new ProcessStartInfo(link) { UseShellExecute = true });
        }
        public static string? GetGameName(string id){
                HtmlWeb web = new HtmlWeb();
                HtmlDocument document = web.Load(StoreUrl + id);
                HtmlNode metaTag = document.DocumentNode.SelectSingleNode("//meta[@property='og:url']");
                string url = metaTag?.GetAttributeValue("content", string.Empty) ?? "";
                return url?.Split(["store.steampowered.com/app/" + id], StringSplitOptions.None)
                        .ElementAtOrDefault(1)?
                        .Replace("/", "") ?? null;
        }
        
        public static async Task<string?> GetModName(string id, bool instantCache = false){
                Dictionary<string, string> modCache = await Utils.GetModCache();
                if (modCache.TryGetValue(id, out var name)) return name;
                HtmlWeb web = new HtmlWeb();
                HtmlDocument document = web.Load(FileDetailsUrl + id);
                HtmlNode metaTag = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                string title = metaTag?.GetAttributeValue("content", string.Empty) ?? "";
                title = title.Replace("Steam Workshop::", "").Replace("&quot;", "");
                if (title == "Steam Community :: Error") return null;
                if (instantCache){
                        Dictionary<string, string> mod = new();
                        mod[id] = title; 
                        await Utils.CacheMod(mod);
                }
                return title;
        }
}