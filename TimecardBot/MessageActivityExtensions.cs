using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;

namespace TimecardBot
{
    public static class MessageActivityExtensions
    {
        static readonly Regex _regex = new Regex(@"[^0-9a-zA-Zあ-んア-ン]");

        public static bool EqualsIntent(this IMessageActivity self, params string[] words)
        {
            if (self == null || words == null)
            {
                return false;
            }

            try
            {
                var plane = self.Text;
                var cleaned = Clean(self.Text);

                var match = words.Any(w => string.Equals(plane, w, StringComparison.CurrentCultureIgnoreCase));
                if (match)
                {
                    return true;
                }
                else
                {
                    return words.Any(w => string.Equals(cleaned, w, StringComparison.CurrentCultureIgnoreCase));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{self?.Text ?? ""} の判定に失敗 - {e.Message}");
                return false;
            }
        }

        private static string Clean(string text)
        {
            return _regex.Replace(text.Replace("\n", ""), "");
        }
    }
}