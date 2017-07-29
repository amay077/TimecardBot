using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Autofac;
using System.Linq;

namespace TimecardBot.Dialogs
{
    [Serializable]
    public class TestDialog : IDialog<object>
    {
        private bool _firstRespond = false;
        private int _choisedOperation = 0;

        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var activity = await result as Activity;
            var message = activity.Text;

            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
            {
                var client = scope.Resolve<IConnectorClient>();
                var activityMembers = await client.Conversations.GetConversationMembersAsync(activity.Conversation.Id);

                string members = string.Join(
                    "\n ",
                    activityMembers.Select(
                        member => ($"* Member: {member.Name} (Id: {member.Id})")));

                await context.PostAsync($"2. These are the members of this conversation: \n" +
                    $"ServiceUrl: {activity.ServiceUrl}\n" +
                    $" * Conversation-ID: {activity.Conversation.Id} \n" +
                    $" * Recipient: {activity.Recipient.Name} (Id: {activity.Recipient.Id}) \n" +
                    $" {members}");
            }

            //if (!_firstRespond)
            //{
            //    // return our reply to the user
            //    await context.PostAsync($"こんにちは、何をしますか？(1.かう, 2.うる)");
            //    _firstRespond = true;
            //}
            //else
            //{
            //    if (int.TryParse(message, out _choisedOperation))
            //    {
            //        if (_choisedOperation == 1)
            //        {
            //            await context.PostAsync($"なにをかいますか？");
            //        }
            //        else if (_choisedOperation == 2)
            //        {
            //            await context.PostAsync($"なにをうりますか？");
            //        }
            //    }
            //    else
            //    {
            //        await context.PostAsync($"え？なんですって？何をしますか？(1.かう, 2.うる)");
            //    }
            //}

            context.Wait(MessageReceivedAsync);
        }
    }
}