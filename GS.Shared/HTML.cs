using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace GS.Shared
{
    public class HTML
    {
        public bool IsValidUri(string uri)
        {
            if (!Uri.IsWellFormedUriString(uri, UriKind.Absolute))
                return false;
            if (!Uri.TryCreate(uri, UriKind.Absolute, out var tmp))
                return false;
            return tmp.Scheme == Uri.UriSchemeHttp || tmp.Scheme == Uri.UriSchemeHttps;
        }

        public void OpenUri(string uri)
        {
            if (!IsValidUri(uri)) return;
            System.Diagnostics.Process.Start(uri);
        }
        
        public List<LinkItem> FindLinks(string file)
        {
            var list = new List<LinkItem>();

            // 1.
            // Find all matches in file.
            var m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                var value = m.Groups[1].Value;
                var i = new LinkItem();

                // 3.
                // Get href attribute.
                var m2 = Regex.Match(value, @"href=\""(.*?)\""",
                    RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }

                // 4.
                // Remove inner tags from text.
                var t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                    RegexOptions.Singleline);
                i.Text = t;

                list.Add(i);
            }

            return list;
        }
    }

    public struct LinkItem
    {
        public string Href;
        public string Text;

        public override string ToString()
        {
            return Href + "\n\t" + Text;
        }
    }
}
