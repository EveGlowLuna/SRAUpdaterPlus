using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRAUpdaterPlus.Tool
{
    public class Parameter
    {
        public const string VERSION = "v1.0";
        public const string HASH_URL = "https://gitee.com/yukikage/sraresource/raw/main/SRA/hash.json";
        public const string AUTHOR = "EveGlowLuna";
        public const string TARGET_URL = "https://github.com/Shasnow/StarRailAssistant/releases/download/v{0}/StarRailAssistant_v{0}.zip"; // Use string.Format to insert the version number, for example: string.Format(TARGET_URL, "v3.8");
        public const string VERSION_REMOTE_URL = "https://raw.githubusercontent.com/Shasnow/StarRailAssistant/main/version.json";
        public static List<string> PROXY = new List<string>
        {
            "https://gh-proxy.com/",
            "https://tvv.tw/",
            "https://ghproxy.1888866.xyz/",
            "https://github.chenc.dev/"
        };
        public static void ChangeProxy(string url)
        {
            PROXY.Clear();
            // 格式化URL为标准格式
            string formattedUrl = url.Trim().ToLower();
            
            // 确保URL以https://开头
            if (!formattedUrl.StartsWith("https://"))
            {
                if (formattedUrl.StartsWith("http://"))
                {
                    formattedUrl = "https://" + formattedUrl.Substring(7);
                }
                else
                {
                    formattedUrl = "https://" + formattedUrl;
                }
            }

            // 确保URL以/结尾
            if (!formattedUrl.EndsWith("/"))
            {
                formattedUrl += "/";
            }

            PROXY.Add(formattedUrl);
        }

        public static readonly Dictionary<string, string> DOWNLOAD_HEADERS = new Dictionary<string, string>
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.3" },
                { "Referer", "https://github.com/" },
                { "Accept-Language", "zh-CN,zh;q=0.8,en;q=0.6" }
            };
        public static string LOCATED_DIR = AppDomain.CurrentDomain.BaseDirectory;
        public static void ChangeDirLocate(string dir)
        {
            LOCATED_DIR = dir;
            SAVE_PATH = Path.Combine(LOCATED_DIR, "Temp");
        }
        public static string SAVE_PATH = Path.Combine(LOCATED_DIR, "Temp"); // The path where to save the downloaded file
        // You're right, but mirrorc is not supported in the current version. If you want to use it, please wait for the next update.


    }
}
