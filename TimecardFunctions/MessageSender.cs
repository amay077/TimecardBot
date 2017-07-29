using Microsoft.Bot.Connector;
using TimecardLogic.Repositories;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardFunctions
{
    class MessageSender
    {
        public async void Send()
        {
            var serviceUrl = "https://smba.trafficmanager.net/apis/";
            var appId = ConfigurationManager.AppSettings["MicrosoftAppId"];
            var appPassword = ConfigurationManager.AppSettings["MicrosoftAppPassword"];

            MicrosoftAppCredentials.TrustServiceUrl(serviceUrl); // https://codedump.io/share/43fLSEl1kzYX/1/bot-framework-unauthorized-when-creating-a-conversation
            var connector = new ConnectorClient(new Uri(serviceUrl), appId, appPassword);
            var botAccount = new ChannelAccount(id: ConfigurationManager.AppSettings["SkypeBotAccountId"]);

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
                    var userAccount = new ChannelAccount(id: user.UserId);
                    var res = connector.Conversations.CreateDirectConversation(botAccount, userAccount);


                    IMessageActivity message = Activity.CreateMessageActivity();
                    message.From = botAccount;
                    message.Recipient = userAccount;
                    message.Conversation = new ConversationAccount(id: res.Id);
                    var minute = nowUserTz.Minute / 30 * 30;
                    message.Text = $"{user.NickName} さん、お疲れさまです。{nowUserTz.Hour}時{minute:00}分 です、今日のお仕事は終わりましたか？";
                    message.Locale = "ja-Jp";
                    connector.Conversations.SendToConversation((Activity)message);
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
    }
}
