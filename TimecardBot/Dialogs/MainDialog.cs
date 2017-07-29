using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using TimecardLogic.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace TimecardBot.Dialogs
{
    [Serializable]
    public class MainDialog : IDialog<object>
    {
        protected int count = 1;
        private string _userId;

        public async Task StartAsync(IDialogContext context)
        {

            context.Wait(MessageReceivedAsync);
        }
        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument as Activity;

            // 現在の会話のユーザーIDを得る
            _userId = await GetFirstMember(activity);

            var message = await argument;

            if (string.CompareOrdinal(message.Text, "menu") == 0)
            {
                PromptDialog.Choice<MenuType>(context, MenuProcessAsync,
                    new MenuType[] { MenuType.RegistUser, MenuType.UnregistUser },  "メニューを表示します");
            }
            else if (message.Text == "reset")
            {
                PromptDialog.Confirm(context, ResetCountAsync, "リセットしますか?");
            }
            else
            {
                await context.PostAsync(string.Format("{0}:{1}って言ったね。", this.count++, message.Text));
                context.Wait(MessageReceivedAsync);
            }
        }

        /// <summary>
        /// 会話の最初のメンバーを取得する（一人しか居ない想定）
        /// </summary>
        /// <param name="activity"></param>
        /// <returns></returns>
        private async Task<string> GetFirstMember(Activity activity)
        {
            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
            {
                var client = scope.Resolve<IConnectorClient>();
                var activityMembers = await client.Conversations.GetConversationMembersAsync(activity.Conversation.Id);

                return activityMembers.Select(x=>x.Id).FirstOrDefault();
            }
        }

        public async Task MenuProcessAsync(IDialogContext context, IAwaitable<MenuType> argument)
        {
            var confirm = await argument;
            if (confirm == MenuType.RegistUser)
            {
                var userRepo = new UsersRepository();
                if (await userRepo.ExistUserId(_userId))
                {
                    await context.PostAsync("あなたは既にユーザー登録されています。");
                }
                else
                {
                    //PromptDialog.Confirm(context, ResetCountAsync, "リセットしますか?");

                    PromptDialog.Confirm(context, RegistUserAsync, "ユーザー登録を行ってよいですか？");
                    return;
                }
            }
            else if (confirm == MenuType.UnregistUser)
            {
                PromptDialog.Confirm(context, UnregistUserConfirmAsync, "ユーザーを削除してよいですか？");
                return;
            }
            else
            {
                await context.PostAsync("無効なメニューが選択されました。");
            }
            context.Wait(MessageReceivedAsync);
        }

        public async Task RegistUserAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                var userRepo = new UsersRepository();
                var tzTokyo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                await userRepo.AddUser(_userId, "Mike", "1900", "2400", tzTokyo.Id);
                await context.PostAsync("ユーザーを登録しました。");
            }
            else
            {
                await context.PostAsync("ユーザー登録しませんでした。");
            }
            context.Wait(MessageReceivedAsync);
        }

        public async Task UnregistUserConfirmAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                PromptDialog.Confirm(context, UnregistUserAsync, "本当に削除してよろしいですか？");
                return;
            }
            else
            {
                await context.PostAsync("ユーザー削除を中止しました。");
            }
            context.Wait(MessageReceivedAsync);
        }

        public async Task UnregistUserAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                var userRepo = new UsersRepository();
                await userRepo.DeleteUser(_userId);
                await context.PostAsync("ユーザーを削除しました。");
            }
            else
            {
                await context.PostAsync("ユーザー削除を中止しました。");
            }
            context.Wait(MessageReceivedAsync);
        }

        public async Task ResetCountAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                this.count = 1;
                await context.PostAsync("会話数をリセットしました。");
            }
            else
            {
                await context.PostAsync("会話数リセットを中止しました。");
            }
            context.Wait(MessageReceivedAsync);
        }
    }
}