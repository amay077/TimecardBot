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
using Microsoft.Bot.Builder.ConnectorEx;
using Newtonsoft.Json;
using TimecardLogic.Entities;
using TimecardLogic.DataModels;
using TimecardLogic;
using TimecardBot.Menus;

namespace TimecardBot.Dialogs
{
    [Serializable]
    public class MainDialog : IDialog<object>
    {
        protected int count = 1;
        private User _currentUser;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var activity = await argument as Activity;

            // 現在の会話のユーザーIDを得る
            if (_currentUser == null)
            {
                var userId = await GetFirstMember(activity);
                var userRepo = new UsersRepository();
                _currentUser = await userRepo.GetUserById(userId);
            }

            var message = await argument;

            if (message.EqualsIntent("menu", "メニュー"))
            {
                PromptDialog.Choice<Menu>(context, MenuProcessAsync,
                    BuildMenus(),  "タイムカードボットのメインメニューです。操作を選択して下さい。");
            }
            else if (message.EqualsIntent("reset", "リセット"))
            {
                PromptDialog.Confirm(context, ResetCountAsync, "リセットしますか?");
            }
            else
            {
                var conversationStateRepo = new ConversationStateRepository();
                var stateEntity = await conversationStateRepo.GetStatusByUserId(_currentUser?.UserId ?? string.Empty);

                //if (stateEntity == null)
                //{
                //    stateEntity = new ConversationStateEntity("debug_tenant", _currentUser.UserId)
                //    {
                //        State = AskingState.AskingEoW,
                //        TargetDate = "2017/07/30",
                //        TargetTime = "2000",
                //    };
                //}

                // 終業かを問い合わせ中なら、
                // （y:終わった／n:終わってない／d:今日は徹夜）に応答する。
                if ((stateEntity?.State ?? AskingState.None) == AskingState.AskingEoW)
                {
                    if (message.EqualsIntent("y", "yes", "ok", "はい")) // y:はい
                    {
                        int eowHour, eowMinute;
                        Util.ParseHHMM(stateEntity.TargetTime, out eowHour, out eowMinute);

                        // 該当日のタイムカードの終業時刻を更新
                        int year, month, day;
                        Util.ParseYYYYMMDD(stateEntity.TargetDate, out year, out month, out day);
                        var monthlyTimecardRepo = new MonthlyTimecardRepository();
                        await monthlyTimecardRepo.UpsertTimecardRecord(_currentUser.UserId, year, month, day, eowHour, eowMinute);

                        // 打刻済みにして更新
                        stateEntity.State = AskingState.Punched;
                        await conversationStateRepo.UpsertState(stateEntity);

                        await context.PostAsync($"お疲れさまでした。{month}月{day}日 の終業時刻は {eowHour}時{eowMinute:00}分 を記録しました。");
                    }
                    else if (message.EqualsIntent("d")) // d:もう聞かないで
                    {
                        await context.PostAsync($"分かりました。今日はもう聞きません。");

                        // 今日はもう聞かないにして更新
                        stateEntity.State = AskingState.DoNotAskToday;
                        await conversationStateRepo.UpsertState(stateEntity);
                    }
                    else if (message.EqualsIntent("n", "no", "ng", "いいえ", "だめ")) // n:まだ
                    {
                        await context.PostAsync($"失礼しました。また３０分後に聞きます。");
                    }
                    else
                    {
                        await context.PostAsync($"認識できないコマンドです。 menu とタイプするとメニューを表示します。");
                    }
                }
                else
                {
                    var text = "";
                    if (_currentUser == null)
                    {
                        text = "初めての方は、 menu とタイプしてメニューを表示し、「ユーザー登録」を選択してください。";
                    }
                    else
                    {
                        text = " menu とタイプするとメニューを表示します。";
                    }

                    await context.PostAsync($"こんにちわ {_currentUser?.NickName ?? "ゲスト"} さん。" + text);
                }

                //await context.PostAsync(string.Format("{0}:{1}って言ったね。", this.count++, message.Text));
                context.Wait(MessageReceivedAsync);
            }
        }

        private IEnumerable<Menu> BuildMenus()
        {
            var menus = new List<Menu>();

            if (_currentUser == null)
            {
                // 未登録ユーザー
                menus.Add(Menu.Make(MenuType.RegistUser));
                menus.Add(Menu.Make(MenuType.AboutThis));
                menus.Add(Menu.Make(MenuType.Cancel));
            }
            else
            {
                // 登録済みユーザー
                menus.Add(Menu.Make(MenuType.DownloadTimecard));
                menus.Add(Menu.Make(MenuType.ModityTimecard));
                menus.Add(Menu.Make(MenuType.PostFeedback));
                menus.Add(Menu.Make(MenuType.AboutThis));
                menus.Add(Menu.Make(MenuType.UnregistUser));
                menus.Add(Menu.Make(MenuType.Cancel));
            }

            return menus;
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

        public async Task MenuProcessAsync(IDialogContext context, IAwaitable<Menu> argument)
        {
            var confirm = await argument;
            if (confirm.Type == MenuType.RegistUser)
            {
                var userRepo = new UsersRepository();
                if (_currentUser != null)
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
            else if (confirm.Type == MenuType.UnregistUser)
            {
                if (_currentUser == null)
                {
                    await context.PostAsync("ユーザー登録されていません。");
                }
                else
                {
                    PromptDialog.Confirm(context, UnregistUserConfirmAsync, "ユーザーを削除してよいですか？");
                    return;
                }
            }
            else if (confirm.Type == MenuType.Cancel)
            {
                await context.PostAsync("メニューを閉じました。");
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
                var conversationRef = context.Activity.ToConversationReference();

                var userRepo = new UsersRepository();
                var tzTokyo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
                var userId = await GetFirstMember(context.Activity as Activity);
                await userRepo.AddUser(userId, "Mike", "1900", "2400", tzTokyo.Id, JsonConvert.SerializeObject(conversationRef));
                _currentUser = await userRepo.GetUserById(userId);

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
                await userRepo.DeleteUser(_currentUser.UserId);
                await context.PostAsync("ユーザーを削除しました。");
                _currentUser = null;
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
                _currentUser = null;
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