using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.ConnectorEx;
using TimecardLogic.DataModels;
using TimecardLogic;
using Microsoft.Bot.Builder.FormFlow;
using TimecardBot.DataModels;
using TimecardBot.Usecases;
using TimecardBot.Commands;
using System.Diagnostics;

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
                var usecase = new UserUsecase();
                var userId = await activity.GetFirstMember();
                _currentUser = await usecase.GetUser(userId);
            }

            var message = await argument;

            Trace.WriteLine($"{_currentUser?.UserId ?? "unknown user"} posted '{message.Text}'.");

            var resolver = new CommandResolver();
            var commandType = resolver.Resolve(message.Text);
            Trace.WriteLine($"Resolved command is {commandType}");
            //await context.PostAsync($"Resolved command is {commandType}");

            await HandleCommandTypeAsync(context, commandType);
        }

        private async Task HandleCommandTypeAsync(IDialogContext context, CommandType commandType)
        {
            switch (commandType)
            {
                case CommandType.None:
                    break;
                case CommandType.PunchTodayIsOff:
                    break;
                case CommandType.RegistUser:
                    {
                        if (_currentUser != null)
                        {
                            await context.PostAsync("あなたは既にユーザー登録されています。");
                        }
                        else
                        {
                            var dlg = FormDialog.FromForm(RegistUserOrder.BuildForm, FormOptions.PromptInStart);
                            context.Call(dlg, RegistUserProcess);
                            return;
                        }
                    }
                    break;
                case CommandType.UnregistUser:
                    break;
                case CommandType.DownloadTimecard:
                    {
                        await context.PostAsync("タイムカードのダウンロードはただいま実装中です。");
                    }
                    break;
                case CommandType.ModityTimecard:
                    {
                        await context.PostAsync("タイムカードの編集はただいま実装中です。");
                    }
                    break;
                case CommandType.AboutThis:
                    {
                        var interval = 3000;
                        await context.PostAsync("私は、終業時間を毎日EXCELに記録するのが面倒なアナタのためのボットです。");
                        await Task.Delay(interval);
                        await context.PostAsync("ユーザー登録しておくと、終業時間を過ぎたら私がアナタに「仕事はおわりましたか？」と聞きます。");
                        await Task.Delay(interval);
                        await context.PostAsync("アナタが「はい」と応えたら、私はその時刻を終業時間として記録します。");
                        await Task.Delay(interval);
                        await context.PostAsync("「いいえ」と応える、または無視すると、私は３０分後に再び聞きます。");
                        await Task.Delay(interval);
                        await context.PostAsync("毎日私の問いに応えるだけで、月末には上司に提出するための日報ができています。ぜひ私に登録してみてください。");
                    }
                    break;
                case CommandType.PostFeedback:
                    break;
                case CommandType.Menu:
                    {
                        PromptDialog.Choice<Command<CommandType>>(context, SubMenuProcessAsync,
                            BuildMenus(), "タイムカードボットのメインメニューです。操作を選択して下さい。");
                    }
                    break;
                case CommandType.Others:
                    {
                        PromptDialog.Choice(context, SubMenuProcessAsync,
                            new Command<CommandType>[]
                            {
                            Command<CommandType>.Make(CommandType.PostFeedback),
                            Command<CommandType>.Make(CommandType.UnregistUser),
                            Command<CommandType>.Make(CommandType.Cancel)
                            }, "その他の機能です。操作を選択して下さい。");
                    }
                    break;
                case CommandType.Cancel:
                    break;
                case CommandType.EasterEggGanbaruzoi:
                    {
                        var usecase = new EasterEgg();
                        await usecase.PostGanbaruzoi(context);
                    }
                    break;
                default:
                    {
                        var usecase = new MainUsecase(_currentUser);
                        var stateEntity = await usecase.GetCurrentUserStatus();

                        // 「今日は休み」と言われたら、 AskingEoW でなくともその日は休日にする
                        if (_currentUser != null && commandType == CommandType.PunchTodayIsOff)
                        {
                            // 今日を休みに更新
                            await usecase.PunchTodayIsOff(stateEntity);
                            await context.PostAsync($"今日はお休みなのですね、分かりました。今日はもう聞きません。よい休日をお過ごし下さい。");
                        }
                        // 終業かを問い合わせ中なら、
                        // （y:終わった／n:終わってない／d:今日は徹夜）に応答する。
                        else if ((stateEntity?.State ?? AskingState.None) == AskingState.AskingEoW)
                        {
                            if (commandType == CommandType.AnswerToEoW) // y:はい
                            {
                                // 聞かれた時刻で、終業時刻を更新
                                var eowDateTime = await usecase.PunchEoW(stateEntity);
                                await context.PostAsync($"お疲れさまでした。{eowDateTime.month}月{eowDateTime.day}日 の" +
                                    $"終業時刻は {eowDateTime.hour}時{eowDateTime.minute:00}分 を記録しました。");
                            }
                            else if (commandType == CommandType.AnswerToDoNotAskToday) // d:もう聞かないで
                            {
                                // 今日はもう聞かないにして更新
                                await usecase.PunchDoNotAskToday(stateEntity);
                                await context.PostAsync($"分かりました。今日はもう聞きません。");
                            }
                            else if (commandType == CommandType.AnswerToDoNotAskToday) // n:まだ
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

                        context.Wait(MessageReceivedAsync);
                    }
                    break;
            }
        }

        private IEnumerable<Command<CommandType>> BuildMenus()
        {
            var menus = new List<Command<CommandType>>();

            if (_currentUser == null)
            {
                // 未登録ユーザー
                menus.Add(Command<CommandType>.Make(CommandType.RegistUser));
                menus.Add(Command<CommandType>.Make(CommandType.AboutThis));
                menus.Add(Command<CommandType>.Make(CommandType.Cancel));
            }
            else
            {
                // 登録済みユーザー
                menus.Add(Command<CommandType>.Make(CommandType.DownloadTimecard));
                menus.Add(Command<CommandType>.Make(CommandType.ModityTimecard));
                menus.Add(Command<CommandType>.Make(CommandType.AboutThis));
                menus.Add(Command<CommandType>.Make(CommandType.Others));
                menus.Add(Command<CommandType>.Make(CommandType.Cancel));
            }

            return menus;
        }

        //public async Task MenuProcessAsync(IDialogContext context, IAwaitable<Menu<MenuType>> argument)
        //{
        //    var confirm = await argument;

        //    switch (confirm.Type)
        //    {
        //        case MenuType.RegistUser:
        //            if (_currentUser != null)
        //            {
        //                await context.PostAsync("あなたは既にユーザー登録されています。");
        //            }
        //            else
        //            {
        //                var dlg = FormDialog.FromForm(RegistUserOrder.BuildForm, FormOptions.PromptInStart);
        //                context.Call(dlg, RegistUserProcess);
        //                return;
        //            }
        //            break;
        //        case MenuType.DownloadTimecard:
        //            await context.PostAsync("タイムカードのダウンロードはただいま実装中です。");
        //            break;
        //        case MenuType.ModityTimecard:
        //            await context.PostAsync("タイムカードの編集はただいま実装中です。");
        //            break;
        //        case MenuType.AboutThis: // このボットについて
        //            var interval = 3000;
        //            await context.PostAsync("私は、終業時間を毎日EXCELに記録するのが面倒なアナタのためのボットです。");
        //            await Task.Delay(interval);
        //            await context.PostAsync("ユーザー登録しておくと、終業時間を過ぎたら私がアナタに「仕事はおわりましたか？」と聞きます。");
        //            await Task.Delay(interval);
        //            await context.PostAsync("アナタが「はい」と応えたら、私はその時刻を終業時間として記録します。");
        //            await Task.Delay(interval);
        //            await context.PostAsync("「いいえ」と応える、または無視すると、私は３０分後に再び聞きます。");
        //            await Task.Delay(interval);
        //            await context.PostAsync("毎日私の問いに応えるだけで、月末には上司に提出するための日報ができています。ぜひ私に登録してみてください。");
        //            break;
        //        case MenuType.Others:
        //            PromptDialog.Choice<Menu<SubMenuType>>(context, SubMenuProcessAsync,
        //                new Menu<SubMenuType>[]
        //                {
        //                    Menu<SubMenuType>.Make(SubMenuType.PostFeedback),
        //                    Menu<SubMenuType>.Make(SubMenuType.UnregistUser),
        //                    Menu<SubMenuType>.Make(SubMenuType.Cancel)
        //                }, "その他の機能です。操作を選択して下さい。");
        //            return;
        //        case MenuType.Cancel:
        //            await context.PostAsync("メニューを閉じました。");
        //            break;
        //        default:
        //            await context.PostAsync("無効なメニューが選択されました。");
        //            break;
        //    }
        //    context.Wait(MessageReceivedAsync);
        //}

        private async Task RegistUserProcess(IDialogContext context, IAwaitable<RegistUserOrder> result)
        {
            var order = await result;
            var conversationRef = context.Activity.ToConversationReference();
            var userId = await (context.Activity as Activity).GetFirstMember();

            var usecase = new UserUsecase();
            _currentUser = await usecase.RegistUser(userId, order, conversationRef);

            await context.PostAsync($"ユーザーを登録しました。\n\nこれから毎日、{order.EndOfWorkTime}になったら仕事が終わったかを聞きますので、よろしくお願いします。");

            context.Wait(MessageReceivedAsync);
        }

        public async Task SubMenuProcessAsync(IDialogContext context, IAwaitable<Command<CommandType>> argument)
        {
            var commandType = (await argument).Type;
            await HandleCommandTypeAsync(context, commandType);


            //var choice = await argument;
            //switch (choice.Type)
            //{
            //    case SubMenuType.UnregistUser:
            //        if (_currentUser == null)
            //        {
            //            await context.PostAsync("ユーザー登録されていません。");
            //        }
            //        else
            //        {
            //            PromptDialog.Confirm(context, UnregistUserConfirmAsync, "ユーザーを削除してよいですか？");
            //            return;
            //        }
            //        break;
            //    case SubMenuType.PostFeedback:
            //        await context.PostAsync("フィードバックの送信はただいま実装中です。");
            //        break;
            //    case SubMenuType.Cancel:
            //        await context.PostAsync("メニューを閉じました。");
            //        break;
            //    default:
            //        await context.PostAsync("無効なメニューが選択されました。");
            //        break;
            //}
            //context.Wait(MessageReceivedAsync);
        }

        public async Task UnregistUserConfirmAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                PromptDialog.Confirm(context, UnregistUserAsync, "退会すると記録されているデータが全て削除されます。本当に削除してよろしいですか？（これが最後の確認です）");
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
                var usecase = new UserUsecase();
                await usecase.DeleteUser(_currentUser);
                _currentUser = null;
                await context.PostAsync("ユーザーを削除しました。またのご利用をお待ちしております。");
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