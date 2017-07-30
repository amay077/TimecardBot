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

namespace TimecardFunctions
{
    class MessageSender
    {
        public async void Send()
        {
            //var serviceUrl = "https://smba.trafficmanager.net/apis/";
            //var serviceUrl = "https://timecardbot20170730023129.azurewebsites.net/api";
            var appId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            var appPassword = ConfigurationManager.AppSettings["MicrosoftAppPassword"];

            var usersRepo = new UsersRepository();
            var users = await usersRepo.GetAllUsers();
            foreach (var user in users)
            {
                int startHour, startMinute;
                int endHour, endMinute;

                ParseHHMM(user.AskEndOfWorkStartTime, out startHour, out startMinute);
                ParseHHMM(user.AskEndOfWorkEndTime, out endHour, out endMinute);

                var nowUtc = DateTime.Now.ToUniversalTime();
                var tzUser = TimeZoneInfo.FindSystemTimeZoneById(user.TimeZoneId);
                var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzUser);

                var startTotalMinute = startHour * 60 + startMinute;
                var endTotalMinute = endHour * 60 + endMinute;
                var nowTotalMinute = nowUserTz.Hour * 60 + nowUserTz.Minute;

                if (startTotalMinute <= nowTotalMinute && nowTotalMinute <= endTotalMinute)
                {
                    var conversationRef = JsonConvert.DeserializeObject<ConversationReference>(user.ConversationRef);

                    MicrosoftAppCredentials.TrustServiceUrl(conversationRef.ServiceUrl); 
                    var connector = new ConnectorClient(new Uri(conversationRef.ServiceUrl), appId, appPassword);

                    var message = conversationRef.GetPostToUserMessage();
                    message = message.CreateReply();

                    var minute = nowUserTz.Minute / 30 * 30;
                    message.Text = $"{user.NickName} さん、お疲れさまです。{nowUserTz.Hour}時{minute:00}分 です、今日のお仕事は終わりましたか？" +
                        $"（y:終わった／n:終わってない／d:今日は徹夜）";
                    message.Locale = "ja-Jp";

                    //message.Attachments.Add(new Attachment()
                    //{
                    //    ContentType = AdaptiveCard.ContentType,
                    //    Content = MakeAdaptiveCard()
                    //});

                    connector.Conversations.ReplyToActivity(message);
                }
            }
        }

        private bool ParseHHMM(string hhmm, out int hour, out int minute)
        {
            hour = 0;
            minute = 0;

            if (hhmm.Length != 4)
            {
                return false;
            }

            hour = int.Parse(hhmm.Substring(0, 2));
            minute = int.Parse(hhmm.Substring(2));
            return true;
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
