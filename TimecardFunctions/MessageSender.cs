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
                int startHour, startMinute;
                int endHour, endMinute;

                var tzUser = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzUser); // ユーザーのタイムゾーンでの現在時刻
                var nowUserTzText = $"{nowUserTz.Year:0000}/{nowUserTz.Month:00}/{nowUserTz.Day:00}";  // ユーザーTZ現在時刻を文字列化

                Util.ParseHHMM(user.AskEndOfWorkStartTime, out startHour, out startMinute);
                Util.ParseHHMM(user.AskEndOfWorkEndTime, out endHour, out endMinute);

                var startTotalMinute = startHour * 60 + startMinute;
                var endTotalMinute = endHour * 60 + endMinute;
                var nowTotalMinute = nowUserTz.Hour * 60 + nowUserTz.Minute;

                var stateEntity = await conversationStateRepo.GetStatusByUserId(user.UserId);

                var currentTargetDate = stateEntity?.TargetDate ?? "2000/01/01";

                // ターゲット日付と現在時刻が同じで、
                // 打刻済/今日はもう聞かないで/休日だったら何もしない
                var currentState = stateEntity?.State ?? AskingState.None;
                bool containsTimeRange = true;
                if (!disableFilter)
                {
                    if (string.CompareOrdinal(nowUserTzText, currentTargetDate) == 0 &&
                        currentState == AskingState.DoNotAskToday || currentState == AskingState.Punched || currentState == AskingState.TodayIsOff)
                    {
                        _log.Info($"ターゲット日付({currentTargetDate})とユーザーTZ現在日付({nowUserTzText})が同じで、State が {currentState} なので何もしない");
                        continue;
                    }

                    containsTimeRange = startTotalMinute <= nowTotalMinute && nowTotalMinute <= endTotalMinute;

                    // 聞き取り終了時刻を過ぎていたらStateをNoneにする
                    // AskingEoW のまま y を打たれると打刻できてしまうので。
                    if (nowTotalMinute > endTotalMinute)
                    {
                        await conversationStateRepo.UpsertState(
                            user.PartitionKey, user.UserId, AskingState.None, $"{endHour:00}{endMinute:00}",
                            nowUserTzText);
                    }
                }

                if (containsTimeRange)
                {
                    var conversationRef = JsonConvert.DeserializeObject<ConversationReference>(user.ConversationRef);

                    MicrosoftAppCredentials.TrustServiceUrl(conversationRef.ServiceUrl); 
                    var connector = new ConnectorClient(new Uri(conversationRef.ServiceUrl), appId, appPassword);

                    var message = conversationRef.GetPostToUserMessage();
                    message = message.CreateReply();

                    var hour = nowUserTz.Hour;
                    var minute = nowUserTz.Minute / 30 * 30;
                    message.Text = $"{user.NickName} さん、お疲れさまです。{hour}時{minute:00}分 です、今日のお仕事は終わりましたか？" +
                        $"（y:終わった／n:終わってない／d:今日は徹夜）";
                    message.Locale = "ja-Jp";

                    //message.Attachments.Add(new Attachment()
                    //{
                    //    ContentType = AdaptiveCard.ContentType,
                    //    Content = MakeAdaptiveCard()
                    //});

                    await connector.Conversations.ReplyToActivityAsync(message);

                    await conversationStateRepo.UpsertState(
                        user.PartitionKey, conversationRef.User.Id, AskingState.AskingEoW, $"{hour:00}{minute:00}",
                        nowUserTzText);
                }
                else
                {
                    _log.Info($"現在時刻({nowUserTz}) が {user.AskEndOfWorkStartTime} から {user.AskEndOfWorkEndTime} の範囲外なので何もしない");
                }
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
