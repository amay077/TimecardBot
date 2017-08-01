using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using TimecardLogic.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AdaptiveCards;
using Newtonsoft.Json;
using TimecardLogic;
using Microsoft.Azure.WebJobs.Host;
using TimecardLogic.DataModels;

namespace TimecardFunctions
{
    class MessageSender
    {
        private readonly TraceWriter _log;

        public MessageSender(TraceWriter log)
        {
            _log = log;
        }

        public async void Send(bool disableFilter)
        {
            //var serviceUrl = "https://smba.trafficmanager.net/apis/";
            //var serviceUrl = "https://timecardbot20170730023129.azurewebsites.net/api";
            var appId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            var appPassword = ConfigurationManager.AppSettings["MicrosoftAppPassword"];

            var nowUtc = DateTime.Now.ToUniversalTime();

            var usersRepo = new UsersRepository();
            var users = await usersRepo.GetAllUsers();

            var conversationStateRepo = new ConversationStateRepository();

            _log.Info($"処理ユーザー数: {users.Count()}");
            foreach (var user in users)
            {
                _log.Info($"ユーザー: {user.NickName}({user.UserId})");

                int startHour, startMinute;
                int endHour, endMinute;

                var tzUser = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzUser); // ユーザーのタイムゾーンでの現在時刻
                var nowUserTzText = $"{nowUserTz.Year:0000}/{nowUserTz.Month:00}/{nowUserTz.Day:00}";  // ユーザーTZ現在時刻を文字列化
                var nowHour = nowUserTz.Hour;
                var nowStepedMinute = nowUserTz.Minute / 30 * 30;

                Util.ParseHHMM(user.AskEndOfWorkStartTime, out startHour, out startMinute);
                Util.ParseHHMM(user.AskEndOfWorkEndTime, out endHour, out endMinute);

                var durationMinutes = (endHour * 60 + endMinute) - (startHour * 60 + startMinute);
                if (durationMinutes <= 0)
                {
                    _log.Warning($"{user.UserId} は、開始時刻({user.AskEndOfWorkStartTime})と終了時刻({user.AskEndOfWorkEndTime})が逆転しているので何もしない。");
                    continue;
                }

                var rangeStart = new DateTime(nowUserTz.Year, nowUserTz.Month, nowUserTz.Day, startHour, startMinute, 0);
                var rangeEnd = rangeStart.AddMinutes(durationMinutes);

                var stateEntity = await conversationStateRepo.GetStatusByUserId(user.UserId);

                var currentTargetDate = stateEntity?.TargetDate ?? "2000/01/01";

                // ターゲット日付と現在時刻が同じで、
                // 打刻済/今日はもう聞かないで/休日だったら何もしない
                var currentState = stateEntity?.State ?? AskingState.None;
                if (!disableFilter)
                {
                    if (string.Equals(nowUserTzText, currentTargetDate) &&
                        (currentState == AskingState.DoNotAskToday || currentState == AskingState.Punched || currentState == AskingState.TodayIsOff))
                    {
                        _log.Info($"ターゲット日付({currentTargetDate})とユーザーTZ現在日付({nowUserTzText})が同じで、State が {currentState} なので何もしない");
                        continue;
                    }

                    // 今日の曜日はユーザー設定で有効か？
                    var enableDayOfWeek = (user.DayOfWeekEnables?.Length ?? 0) - 1 > (int)nowUserTz.DayOfWeek ?
                        (user.DayOfWeekEnables[(int)nowUserTz.DayOfWeek] == '1') : true;
                    if (!enableDayOfWeek)
                    {
                        _log.Info($"ユーザーTZ現在日付({nowUserTzText})の曜日は仕事が休みなので何もしない");
                        continue;
                    }

                    // FIXME 毎年ある祝日か、単発の休日かの管理が面倒なので、とりまオミットしておく
                    //// 祝日か？(面倒だからJson文字列のまま検索しちゃう)
                    //var isHoliday = user.HolidaysJson?.Contains($"\"{nowUserTz:M/d}\"") ?? false; // "6/1" みたいにダブルコートして検索すればいいっしょ
                    //if (isHoliday)
                    //{
                    //    _log.Info($"ユーザーTZ現在日付({nowUserTzText})の休日に設定されている何もしない");
                    //    continue;
                    //}

                    var containsTimeRange = rangeStart <= nowUserTz && nowUserTz <= rangeEnd;

                    // 聞き取り終了時刻を過ぎていたらStateをNoneにする
                    // AskingEoW のまま y を打たれると打刻できてしまうので。
                    if (rangeStart > rangeEnd)
                    {
                        await conversationStateRepo.UpsertState(
                            user.PartitionKey, user.UserId, AskingState.None, $"{endHour:00}{endMinute:00}",
                            nowUserTzText);
                    }

                    if (!containsTimeRange)
                    {
                        _log.Info($"現在時刻({nowUserTz}) が {rangeStart} から {rangeEnd} の範囲外なので何もしない");
                        continue;
                    }
                }

                var conversationRef = JsonConvert.DeserializeObject<ConversationReference>(user.ConversationRef);

                MicrosoftAppCredentials.TrustServiceUrl(conversationRef.ServiceUrl);
                var connector = new ConnectorClient(new Uri(conversationRef.ServiceUrl), appId, appPassword);

                var message = conversationRef.GetPostToUserMessage();
                message = message.CreateReply();

                message.Text = $"{user.NickName} さん、お疲れさまです。{nowHour}時{nowStepedMinute:00}分 です、今日のお仕事は終わりましたか？\n\n" +
                    $"--\n\ny:終わった\n\nn:終わってない\n\nd:今日は徹夜";
                message.Locale = "ja-Jp";

                //message.Attachments.Add(new Attachment()
                //{
                //    ContentType = AdaptiveCard.ContentType,
                //    Content = MakeAdaptiveCard()
                //});

                await connector.Conversations.ReplyToActivityAsync(message);

                await conversationStateRepo.UpsertState(
                    user.PartitionKey, conversationRef.User.Id, AskingState.AskingEoW, $"{nowHour:00}{nowStepedMinute:00}",
                    nowUserTzText);
            }
        }

        private AdaptiveCard MakeAdaptiveCard()
        {
            AdaptiveCard card = new AdaptiveCard()
            {
                Body = new List<CardElement>()
                {
                    new Container()
                    {
                        Speak = "<s>Hello!</s><s>Are you looking for a flight or a hotel?</s>",
                        Items = new List<CardElement>()
                        {
                            new ColumnSet()
                            {
                                Columns = new List<Column>()
                                {
                                    new Column()
                                    {
                                        Size = ColumnSize.Auto,
                                        Items = new List<CardElement>()
                                        {
                                            new Image()
                                            {
                                                Url = "https://placeholdit.imgix.net/~text?txtsize=65&txt=Adaptive+Cards&w=300&h=300",
                                                Size = ImageSize.Medium,
                                                Style = ImageStyle.Person
                                            }
                                        }
                                    },
                                    new Column()
                                    {
                                        Size = ColumnSize.Stretch,
                                        Items = new List<CardElement>()
                                        {
                                            new TextBlock()
                                            {
                                                Text =  "Hello!",
                                                Weight = TextWeight.Bolder,
                                                IsSubtle = true
                                            },
                                            new TextBlock()
                                            {
                                                Text = "Are you looking for a flight or a hotel?",
                                                Wrap = true
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                },
                // Buttons
                Actions = new List<ActionBase>() {
                    new ShowCardAction()
                    {
                        Title = "Hotels",
                        Speak = "<s>Hotels</s>",
                        Card = new AdaptiveCard()
                        {
                            Body = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text = "Flights is not implemented =(",
                                    Speak = "<s>Flights is not implemented</s>",
                                    Weight = TextWeight.Bolder
                                }
                            }
                        }
                    },
                    new ShowCardAction()
                    {
                        Title = "Flights",
                        Speak = "<s>Flights</s>",
                        Card = new AdaptiveCard()
                        {
                            Body = new List<CardElement>()
                            {
                                new TextBlock()
                                {
                                    Text = "Flights is not implemented =(",
                                    Speak = "<s>Flights is not implemented</s>",
                                    Weight = TextWeight.Bolder
                                }
                            }
                        }
                    }
                }
            };

            Attachment attachment = new Attachment()
            {
                ContentType = AdaptiveCard.ContentType,
                Content = card
            };

            //var reply = context.MakeMessage();
            //reply.Attachments.Add(attachment);

            //await context.PostAsync(reply, CancellationToken.None);

            //context.Wait(MessageReceivedAsync);

            return card;
        }

    }
}
