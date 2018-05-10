using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.IO;
using HtmlAgilityPack;

namespace DACommentRssFeed
{
    
    public class CommentEntry
    {
        public bool HasAnswer { get; set; }
        public string Body { get; set; }
        public DateTime Timestamp { get; set; }
        public Uri Link { get; set; }
        public string Author { get; set; }
    }

    public enum CommentAPIState
    {
        Success,
        NetworkError,
        InvalidLink
    }

    class RSSCommentFeedGenerator
    {
        private static DateTime timestampParsing(string title, string innerText)
        {
            int day = 1, month = 3, year = 2018, h = 0, m = 0, s = 0;
            List<string> months = new List<string>(new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" });
            string regexTime = @"\d{1,2}:\d\d:\d\d [A|P]M";
            string regexDate = @"[JFMASOND][a-z]{2} \d?\d\, \d\d\d\d";
            Match match = Regex.Match(title, regexTime);
            if (String.IsNullOrEmpty(match.Value)) match = Regex.Match(innerText, regexTime);
            if (!String.IsNullOrEmpty(match.Value))
            {
                string[] temp1 = match.Value.Split(new char[] { ' ' }, 2, StringSplitOptions.None);
                if (temp1.Length == 2)
                {
                    string[] temp2 = temp1[0].Split(new char[] { ':' }, 3, StringSplitOptions.None);
                    s = Int32.Parse(temp2[2]);
                    m = Int32.Parse(temp2[1]);
                    h = Int32.Parse(temp2[0]);
                    h = h - (((temp1[1] == "AM") && (h == 12)) ? 12 : 0) + (((temp1[1] == "PM") && (h >= 1) && (h < 12)) ? 12 : 0);
                }
            }
            match = Regex.Match(title, regexDate);
            if (String.IsNullOrEmpty(match.Value)) match = Regex.Match(innerText, regexDate);
            if (!String.IsNullOrEmpty(match.Value))
            {
                string[] temp1 = match.Value.Split(new char[] { ' ', ',' }, 3, StringSplitOptions.RemoveEmptyEntries);
                month = months.IndexOf(temp1[0]) + 1;
                year = Int32.Parse(temp1[2]);
                day = Int32.Parse(temp1[1]);
            }
            return (new DateTime(year, month, day, h, m, s));
        }

        public async static Task<Tuple<CommentAPIState, List<CommentEntry>, string>> loadFeed(string dalink)
        {
            HttpClient wc = new HttpClient();
            wc.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (X11; Linux i686; rv:13.0) Gecko/13.0 Firefox/13.0");
            List<CommentEntry> list = new List<CommentEntry>();
            HtmlDocument doc;
            HtmlNodeCollection nodes;
            HtmlNode group;
            int i = 1;
            int offset = 0;
            bool read_page_num = false;
            Uri dalinkuri = new Uri(dalink);
            string link;
            string sourceHtml = "";
            string group_title = null;
            while (i > 0)
            {
                link = dalinkuri.AbsoluteUri + ((offset > 0) ? ("?offset=" + offset) : "");
                try
                {
                    HttpResponseMessage hrm = await wc.GetAsync(link);
                    if ((hrm != null) && hrm.IsSuccessStatusCode)
                    {
                        sourceHtml = await hrm.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
                catch (Exception e) {
                    sourceHtml = "";
                }
                if (String.IsNullOrEmpty(sourceHtml)) return (new Tuple<CommentAPIState, List<CommentEntry>, string>(CommentAPIState.NetworkError, null, group_title));
                doc = new HtmlDocument();
                doc.LoadHtml(sourceHtml);
                group = doc.DocumentNode.SelectSingleNode(@"//h1/span/a[contains(@class, 'group')]");
                nodes = doc.DocumentNode.SelectNodes(@"//div[@id='gmi-CCommentThread']/div");
                if ((group == null)||(nodes == null)) return (new Tuple<CommentAPIState, List<CommentEntry>, string>(CommentAPIState.InvalidLink, null, group_title));
                group_title = group.InnerText;
                if (!read_page_num) {
                    HtmlNode a = doc.DocumentNode.SelectSingleNode(@"//div[@class='pagination']//li[@class='number'][last()]/a");
                    string pages = a.GetAttributeValue("data-offset", "");
                    if (String.IsNullOrEmpty(pages))
                    {
                        return (new Tuple<CommentAPIState, List<CommentEntry>, string>(CommentAPIState.InvalidLink, null, group_title));
                    }
                    i += Int32.Parse(pages) / 10;
                    read_page_num = true;
                }
                foreach (var node in nodes)
                {
                    bool replies = false;
                    var a = node.SelectSingleNode("./following-sibling::a[1]");
                    if (a != null)
                    {
                        string snum = Regex.Match(a.InnerText, @"\d+").Value;
                        if (!String.IsNullOrEmpty(snum))
                        {
                            int temp_num;
                            if (Int32.TryParse(snum, out temp_num)) replies = (temp_num > 0);
                        }
                    }
                    HtmlNode timeNode = node.SelectSingleNode(".//span[contains(@class, 'cc-time')]/a");
                    if (timeNode == null) continue;
                    var x = new Uri(timeNode.GetAttributeValue("href", ""));
                    string s = node.SelectSingleNode(".//div[contains(@class, 'cc-meta')]//following-sibling::div[1]").InnerText;
                    s = timeNode.InnerText;
                    s = node.SelectSingleNode(".//span[contains(@class, 'cc-name')]").InnerText;
                    list.Add(new CommentEntry()
                    {
                        Link = new Uri(timeNode.GetAttributeValue("href", "")),
                        Body = node.SelectSingleNode(".//div[contains(@class, 'cc-meta')]//following-sibling::div[1]").InnerText.Replace("&","&amp;"),
                        Timestamp = timestampParsing(timeNode.GetAttributeValue("title", ""), timeNode.InnerText),
                        Author = node.SelectSingleNode(".//span[contains(@class, 'cc-name')]").InnerText.Replace("&","&amp;"),
                        HasAnswer = replies
                    });
                }
                i--;
                offset += 10;
            }
            return (new Tuple<CommentAPIState, List<CommentEntry>, string>(CommentAPIState.Success, list, group_title));
        }

        public static string rssCommentFeed(List<CommentEntry> list, string title, string url) {
            string r = "";
            r += "<?xml version=\"1.0\"?>\n<rss version=\"2.0\">\n<channel>\n<title>" + title + "</title>\n";
            r += "<description>Comment feed for DeviantArt " + title + " group</description>\n";
            r += "<link>" + url + "</link>\n</channel>\n";
            for (int i = 0; i < list.Count; i++)
            {
                r += "<item>\n<title>" + list[i].Author + " " + list[i].Timestamp.ToShortDateString() + " " + list[i].Timestamp.ToShortTimeString() + "</title>\n";
                r += "<link>" + list[i].Link + "</link>\n";
                r += "<pubDate>" + list[i].Timestamp.ToString("r") + "</pubDate>\n";
                r += "<description>" + list[i].Body + "</description>\n";
                r += "</item>\n";
            }
            r += "</rss>";
            return (r);
        }

        public static int Main(string[] args) {
            int timeout = 60000;
            string file = "", url = "";
            Console.WriteLine("RSS generator for DeviantArt's group comment wall");
            if (args.Length == 0) {
                Console.WriteLine(@"dacrf.exe ""url_to_group_page"" ""path\to\outputilename.rss"" [timeout_seconds]");
                return (0);
            } else if (args.Length > 3 || args.Length < 2) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Invalid argument length!");
                return (1);
            }
            if (args.Length == 3) { 
		        if (!Int32.TryParse(args[2], out timeout)) {
                    Console.WriteLine("ERROR: Invalid timeout value");
                    return (1);
		        } else {
                    timeout *= 1000;
                }
            }
            url = args[0];
            file = args[1];
            Uri dauri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out dauri) || 
                !((dauri.Scheme == Uri.UriSchemeHttp)||(dauri.Scheme == Uri.UriSchemeHttps)) || 
                !dauri.Host.EndsWith(".deviantart.com", StringComparison.InvariantCultureIgnoreCase))
            { 
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Invalid link '"+url);                
            }
            Task<Tuple<CommentAPIState, List<CommentEntry>, string>> result = loadFeed(url);
            if (timeout > 0)
            {
                result.Wait(timeout);
            }
            else
            {
                result.Wait();
            }
            if (!result.IsCompleted) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ERROR: Timeout");
                return (2);
            }
            if (result.Result.Item1 != CommentAPIState.Success) { 
                Console.WriteLine("ERROR: "+((result.Result.Item1 == CommentAPIState.InvalidLink)?"Invalid link":"Network")+"\n");
                return ((result.Result.Item1 == CommentAPIState.InvalidLink)?3:4);
            }
            string rss = rssCommentFeed(result.Result.Item2, result.Result.Item3, url);
            File.WriteAllText(file, rss);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("OK: Finished correctly");
            return (0);
        }
    }
}
