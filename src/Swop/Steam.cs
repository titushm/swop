using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

namespace Swop;
using System.Net.Http;



public static class Steam {
        private static readonly HttpClient HttpClient = new HttpClient();
        public static readonly string SteamUrl = "https://store.steampowered.com/app/";
        
        public static string? GetGameName(string id){
                HtmlWeb web = new HtmlWeb();
                HtmlDocument document = web.Load(SteamUrl + id);
                HtmlNode metaTag = document.DocumentNode.SelectSingleNode("//meta[@property='og:url']");
                string url = metaTag?.GetAttributeValue("content", string.Empty) ?? "";
                return url?.Split(["store.steampowered.com/app/" + id], StringSplitOptions.None)
                        .ElementAtOrDefault(1)?
                        .Replace("/", "") ?? null;
        }
}