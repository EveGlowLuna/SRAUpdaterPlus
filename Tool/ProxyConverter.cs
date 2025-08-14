using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SRAUpdaterPlus.Tool
{
    public class ProxyConverter
    {
        public static List<string> ConvertToProxy(string url)
        {
            if (url.StartsWith("https://github.com") || url.StartsWith("https://raw.githubusercontent.com"))
            {
                var Content = new List<string>();
                for (int i = 0; i < Parameter.PROXY.Count; i++)
                {
                    string proxyUrl = Parameter.PROXY[i] + url;
                    Content.Add(proxyUrl);
                   
                }
                return Content;
            }
            else
            {
                return null;
            }
        }
    }
}
