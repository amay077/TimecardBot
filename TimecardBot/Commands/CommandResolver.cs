using CSharp.Japanese.Kanaxs;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using TimecardLogic;
using TimecardLogic.DataModels;

namespace TimecardBot.Commands
{
    public class CommandResolver
    {
        private static readonly Regex _regex = new Regex(@"[^0-9a-zA-Zあ-んア-ン]");

        public Command Resolve(string text)
        {
            try
            {
                var commands = Enum.GetValues(typeof(CommandType));
                foreach (var cmd in commands)
                {
                    var cmdEnum = ((CommandType)cmd);
                    var words = cmdEnum.ToWords();
                    var matched = Match(text, words);
                    if (matched)
                    {
                        return new Command(cmdEnum, text);
                    }
                }

                // 時刻（hhmm）が入力されたら AnswerToEoW とする
                var hhmm = Hhmm.Parse(text);
                if (!hhmm.IsEmpty)
                {
                    return new Command(CommandType.AnswerToEoWWithTime, text);
                }
                return new Command(CommandType.None, text);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"CommandResolver.Resolve failed. text: {text} - {ex.Message} - {ex.StackTrace}");
                return new Command(CommandType.None, text);
            }
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
            var hankaku = Kana.ToHankaku(text);
            hankaku = hankaku
                .Replace("\0", "")
                .Replace("\b", "")
                .Replace("\n", "")
                .Replace("\r", "")
                .Replace("\t", "")
                .Replace("(", "")
                .Replace(")", "")
                .Replace(":", "")
                ;
            return _regex.Replace(hankaku, "");
        }

    }
}