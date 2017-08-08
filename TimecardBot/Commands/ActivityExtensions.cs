using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System.Linq;
using System.Threading.Tasks;

namespace TimecardBot.Commands
{
    internal static class ActivityExtensions
    {
        /// <summary>
        /// 会話の最初のメンバーを取得する（一人しか居ない想定）
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        public static async Task<string> GetFirstMember(this Activity activity)
        {
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
            {
                var client = scope.Resolve<IConnectorClient>();
                var activityMembers = await client.Conversations.GetConversationMembersAsync(activity.Conversation.Id);

                return activityMembers.Select(x => x.Id).FirstOrDefault();
            }
        }
    }
}