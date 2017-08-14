using Microsoft.Bot.Builder.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using TimecardLogic.DataModels;
using TimecardBot.DataModels;
using TimecardBot.Commands;
using System.Diagnostics;

namespace TimecardBot.Dialogs
{
    [Serializable]
    public class UserPreferenceDialog : IDialog<UserPreferenceDialog.Result>
    {

        [Serializable]
        public sealed class Result
        {
            public UserPreferenceType PrefType { get; }
            public string Text { get; }

            internal Result(UserPreferenceType prefType, string text)
            {
                this.PrefType = prefType;
                this.Text = text;
            }
        }

        private UserPreferenceType _prefType;
        private readonly User _currentUser;

        public UserPreferenceDialog(User user)
        {
            _currentUser = user;
        }

        public async Task StartAsync(IDialogContext context)
        {
            var options = new[] {
                UserPreferenceType.NickName,
                UserPreferenceType.EndOfWorkTime,
                UserPreferenceType.EndOfConfirmTime,
                UserPreferenceType.DayOfWeekEnables,
                UserPreferenceType.TimeZoneId,
                UserPreferenceType.Cancel
            };

            var descriptions = new[] {
                UserPreferenceType.NickName.ToAlias() + $"({_currentUser.NickName})",
                UserPreferenceType.EndOfWorkTime.ToAlias() + $"({_currentUser.FormattedAskEndOfWorkStartTime})",
                UserPreferenceType.EndOfConfirmTime.ToAlias() + $"({_currentUser.FormattedAskEndOfWorkEndTime})",
                UserPreferenceType.DayOfWeekEnables.ToAlias() + $"({_currentUser.OffWeekDayLabels})",
                UserPreferenceType.TimeZoneId.ToAlias() + $"({_currentUser.TimeZoneId})",
                UserPreferenceType.Cancel.ToAlias(),
            };

            PromptDialog.Choice(context, ReceivedPreferenceAsync, options, 
                "変更したい設定項目を選んで下さい。", 
                descriptions: descriptions);
        }

        private async Task ReceivedPreferenceAsync(IDialogContext context, IAwaitable<UserPreferenceType> result)
        {
            var prefType = await result;
            _prefType = prefType;
            await CommandPreferenceMenu(context, prefType);
        }

        private async Task CommandPreferenceMenu(IDialogContext context, UserPreferenceType prefType)
        {
            try
            {
                var option = string.Empty;
                switch (prefType)
                {
                    case UserPreferenceType.NickName:
                        break;
                    case UserPreferenceType.EndOfWorkTime:
                        option = " hhmm の形式で";
                        break;
                    case UserPreferenceType.EndOfConfirmTime:
                        option = " hhmm の形式で";
                        break;
                    case UserPreferenceType.DayOfWeekEnables:
                        option = " 土日 のような形式で";
                        break;
                    case UserPreferenceType.TimeZoneId:
                        break;
                    case UserPreferenceType.Cancel:
                    default:
                        context.Fail(new OperationCanceledException($"Unsuported prefType - {prefType}"));
                        return;
                }

                var prompt = $"{prefType.ToAlias()} を{option}入力して下さい。" +
                    "中止する場合は「中止」、「やめる」または「cancel」とタイプしてください。";
                PromptDialog.Text(context, ReceivedPreferenceTextAsync, prompt);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ReceivedPreferenceAsync failed - {ex.Message} - {ex.StackTrace}");
                context.Fail(new OperationCanceledException($"ReceivedPreferenceAsync failed - {prefType}", ex));
            }
        }

        private async Task ReceivedPreferenceTextAsync(IDialogContext context, IAwaitable<string> result)
        {
            var text = await result;
            var error = string.Empty;

            var cancelTerms = CommandType.Cancel.ToWords();
            if (cancelTerms.Any(x=>string.Equals(x, text)))
            {
                context.Fail(new OperationCanceledException($"Cancel by user - {text}"));
                return;
            }

            switch (_prefType)
            {
                case UserPreferenceType.NickName:
                    break;
                case UserPreferenceType.EndOfWorkTime:
                case UserPreferenceType.EndOfConfirmTime:
                    {
                        // hhmm のバリデーション
                        var hhmm = Hhmm.Parse(text);
                        if (hhmm.IsEmpty)
                        {
                            error = "時刻は hhmm の形式で入力して下さい。";
                        }
                    }
                    break;
                case UserPreferenceType.DayOfWeekEnables:
                    {
                        // 休みの曜日のバリデーション
                        if (!text.All(x => User.WEEKDAYS.Contains(x.ToString())))
                        {
                            error = "休みの曜日は「月火水木金土日」から「土日」、「水金日」などの形式で入力して下さい。";
                        }
                    }
                    break;
                case UserPreferenceType.TimeZoneId:
                    {
                        // タイムゾーンのバリデーション
                        try
                        {
                            TimeZoneInfo.FindSystemTimeZoneById(text);
                        }
                        catch (Exception)
                        {
                            error = "有効なタイムゾーンではありません。";
                        }
                    }
                    break;
                default:
                    break;
            }

            if (string.IsNullOrEmpty(error))
            {
                // 値を返す
                context.Done(new Result(_prefType, text));
            }
            else
            {
                // 再度入力させる
                await context.PostAsync(error);
                await CommandPreferenceMenu(context, _prefType);
            }
        }
    }
}