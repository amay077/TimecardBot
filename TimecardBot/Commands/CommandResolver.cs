using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace TimecardBot.Commands
{
    public class CommandResolver
    {
        private static readonly Regex _regex = new Regex(@"[^0-9a-zA-Zあ-んア-ン]");

        public CommandType Resolve(string text)
        {
            var commands = Enum.GetValues(typeof(CommandType));
            foreach (var cmd in commands)
            {
                var cmdEnum = ((CommandType)cmd);
                var words = cmdEnum.ToWords();
                var matched = Match(text, words);
                if (matched)
                {
                    return cmdEnum;
                }
            }

            return CommandType.None;
        }

        private static bool Match(string text, params string[] words)
        {
            if (string.IsNullOrEmpty(text) || words == null)
            {
                return false;
            }

            try
            {
                var plane = text;
                var cleaned = Clean(text);

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
                Trace.WriteLine($"{text} の判定に失敗 - {e.Message}");
                return false;
            }
        }

        private static string Clean(string text)
        {
            return _regex.Replace(text.Replace("\n", ""), "");
        }

    }
}