using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Swop;
using System.Net.Http;



public static class Steam {
        private static readonly HttpClient HttpClient = new HttpClient();
        public static readonly string StoreUrl = "https://store.steampowered.com/app/";
        public static readonly string FileDetailsUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=";

        public static string? GetGameName(string id){
                HtmlWeb web = new HtmlWeb();
                HtmlDocument document = web.Load(StoreUrl + id);
                HtmlNode metaTag = document.DocumentNode.SelectSingleNode("//meta[@property='og:url']");
                string url = metaTag?.GetAttributeValue("content", string.Empty) ?? "";
                return url?.Split(["store.steampowered.com/app/" + id], StringSplitOptions.None)
                        .ElementAtOrDefault(1)?
                        .Replace("/", "") ?? null;
        }
        
        public static string? GetModName(string id){
                Dictionary<string, string> modCache = Utils.GetModCache();
                if (modCache.TryGetValue(id, out var name)) return name;
                HtmlWeb web = new HtmlWeb();
                HtmlDocument document = web.Load(FileDetailsUrl + id);
                HtmlNode metaTag = document.DocumentNode.SelectSingleNode("//meta[@property='og:title']");
                string title = metaTag?.GetAttributeValue("content", string.Empty) ?? "";
                return title.Replace("Steam Workshop::", "").Replace("&quot;", "");
        }
}