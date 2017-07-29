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
                var userAccount = new ChannelAccount(id: user.UserId);
                var res = connector.Conversations.CreateDirectConversation(botAccount, userAccount);

                var nowUtc = DateTime.Now.ToUniversalTime();
                var tzTokyo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                var nowTokyo = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, tzTokyo);

                IMessageActivity message = Activity.CreateMessageActivity();
                message.From = botAccount;
                message.Recipient = userAccount;
                message.Conversation = new ConversationAccount(id: res.Id);
                message.Text = $"{nowTokyo} ({tzTokyo.StandardName}) です、こんにちわ、{user.NickName} さん。";
                message.Locale = "ja-Jp";
                connector.Conversations.SendToConversation((Activity)message);
            }
        }
    }
}
