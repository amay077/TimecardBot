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
using System.Diagnostics;

namespace TimecardFunctions
{
    class MessageSender
    {
        private readonly TraceWriter _log;

        public MessageSender(TraceWriter log)
        {
            _log = log;
        }

        public void Send(bool disableFilter)
        {
            var appId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            var appPassword = ConfigurationManager.AppSettings["MicrosoftAppPassword"];

            var nowUtc = DateTime.Now.ToUniversalTime();
            Log($"UTC現在時刻: {nowUtc}");
            Trace.WriteLine($"UTC現在時刻- {nowUtc}");

            var usersRepo = new UsersRepository();
            var users = await usersRepo.GetAllUsers();

            var conversationStateRepo = new ConversationStateRepository();

            Log($"処理ユーザー数: {users.Count()}");
            foreach (var user in users)
            {
                Log($"ユーザー: {user.NickName}({user.UserId}) ---");

                int startHour, startMinute;
                int endHour, endMinute;
                Util.ParseHHMM(user.AskEndOfWorkStartTime, out startHour, out startMinute);
                Util.ParseHHMM(user.AskEndOfWorkEndTime, out endHour, out endMinute);

                // 24時超過分をオフセットして比較する
                // 19:00～26:00 の設定だった時に、翌日の深夜1時(25時)も送信対象となるように。
                var offsetHour = endHour - 23;
                if (offsetHour < 0)
                {
                    offsetHour = 0;
                }
                Log($"オフセット時間: {offsetHour}h");

                var tzUser = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzUser); // ユーザーのタイムゾーンでの現在時刻
                Log($"ユーザータイムゾーンの現在時刻(オフセット前): {nowUserTz}");
                nowUserTz = nowUserTz.AddHours(-offsetHour);
                var nowHour = nowUserTz.Hour + offsetHour;
                var nowStepedMinute = nowUserTz.Minute / 30 * 30;
                Log($"ユーザータイムゾーンの現在時刻(オフセット前、丸め後): {nowHour}時{nowStepedMinute:00}分");

                var startTotalMinute = (startHour - offsetHour) * 60 + startMinute;
                var endTotalMinute = (endHour - offsetHour) * 60 + endMinute;
                var nowTotalMinute = (nowHour - offsetHour) * 60 + nowStepedMinute;
                Log($"ユーザータイムゾーンの現在時刻(オフセット後、丸め後): {(nowHour - offsetHour)}時{nowStepedMinute:00}分");
                Log($"判定時刻範囲（オフセット前）: {startHour}時{startMinute:00}分～{endHour}時{endMinute:00}分");
                Log($"判定時刻範囲（オフセット後）: {(startHour - offsetHour)}時{startMinute:00}分～{(endHour - offsetHour)}時{endMinute:00}分");

                if (startTotalMinute >= endTotalMinute)
                {
                    Log($"{user.UserId} は、開始時刻({user.AskEndOfWorkStartTime})と終了時刻({user.AskEndOfWorkEndTime})が逆転しているので何もしない。");
                    continue;
                }

                var nowUserTzDateText = $"{nowUserTz.Year:0000}/{nowUserTz.Month:00}/{nowUserTz.Day:00}";  // ユーザーTZ現在時刻を文字列化

                var stateEntity = await conversationStateRepo.GetStatusByUserId(user.UserId);

                var currentTargetDate = stateEntity?.TargetDate ?? "2000/01/01";

                // ターゲット日付と現在時刻が同じで、
                // 打刻済/今日はもう聞かないで/休日だったら何もしない
                var currentState = stateEntity?.State ?? AskingState.None;
                if (!disableFilter)
                {
                    if (string.Equals(nowUserTzDateText, currentTargetDate) &&
                        (currentState == AskingState.DoNotAskToday || currentState == AskingState.Punched || currentState == AskingState.TodayIsOff))
                    {
                        Log($"ターゲット日付({currentTargetDate})とユーザーTZ現在日付({nowUserTzDateText})が同じで、State が {currentState} なので何もしない");
                        continue;
                    }

                    // 今日の曜日はユーザー設定で有効か？
                    var enableDayOfWeek = (user.DayOfWeekEnables?.Length ?? 0) - 1 > (int)nowUserTz.DayOfWeek ?
                        (user.DayOfWeekEnables[(int)nowUserTz.DayOfWeek] == '1') : true;
                    if (!enableDayOfWeek)
                    {
                        Log($"ユーザーTZ現在日付({nowUserTzDateText})の曜日は仕事が休みなので何もしない");
                        continue;
                    }

                    // FIXME 毎年ある祝日か、単発の休日かの管理が面倒なので、とりまオミットしておく
                    //// 祝日か？(面倒だからJson文字列のまま検索しちゃう)
                    //var isHoliday = user.HolidaysJson?.Contains($"\"{nowUserTz:M/d}\"") ?? false; // "6/1" みたいにダブルコートして検索すればいいっしょ
                    //if (isHoliday)
                    //{
                    //    Log($"ユーザーTZ現在日付({nowUserTzText})の休日に設定されている何もしない");
                    //    continue;
                    //}

                    var containsTimeRange = startTotalMinute <= nowTotalMinute && nowTotalMinute <= endTotalMinute;

                    // 聞き取り終了時刻を過ぎていたらStateをNoneにする
                    // AskingEoW のまま y を打たれると打刻できてしまうので。
                    if (startTotalMinute > endTotalMinute)
                    {
                        await conversationStateRepo.UpsertState(
                            user.PartitionKey, user.UserId, AskingState.None, $"{endHour:00}{endMinute:00}",
                            nowUserTzDateText);
                    }

                    if (!containsTimeRange)
                    {
                        Log($"現在時刻({nowUserTz}) が {user.AskEndOfWorkStartTime} から {user.AskEndOfWorkEndTime} の範囲外なので何もしない");
                        continue;
                    }
                }

                var conversationRef = JsonConvert.DeserializeObject<ConversationReference>(user.ConversationRef);

                MicrosoftAppCredentials.TrustServiceUrl(conversationRef.ServiceUrl);
                var connector = new ConnectorClient(new Uri(conversationRef.ServiceUrl), appId, appPassword);
                
                var userAccount = new ChannelAccount(id: user.UserId);
                var res = connector.Conversations.CreateDirectConversation(conversationRef.Bot, userAccount);

                // conversationRef.GetPostToUserMessage() では Slack にポストできなかったので、
                // 普通に CreateMessageActivity した。
                var message = Activity.CreateMessageActivity();
                message.From = conversationRef.Bot;
                message.Recipient = userAccount;
                message.Conversation = new ConversationAccount(id: res.Id);
                
                message.Text = $"{user.NickName} さん、お疲れさまです。{nowHour}時{nowStepedMinute:00}分 です、今日のお仕事は終わりましたか？\n\n" +
                    $"--\n\ny:終わった\n\nn:終わってない\n\nd:今日は徹夜";
                message.Locale = "ja-Jp";

                //message.Attachments.Add(new Attachment()
                //{
                //    ContentType = AdaptiveCard.ContentType,
                //    Content = MakeAdaptiveCard()
                //});

                connector.Conversations.SendToConversation((Activity)message);

                await conversationStateRepo.UpsertState(
                    user.PartitionKey, conversationRef.User.Id, AskingState.AskingEoW, $"{nowHour:00}{nowStepedMinute:00}",
                    nowUserTzDateText);

                Log($"メッセージを送信しました。 ({message.Text})");
            }
        }

        private void Log(string text)
        {
            _log.Info(text);
            Console.WriteLine(text);
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
