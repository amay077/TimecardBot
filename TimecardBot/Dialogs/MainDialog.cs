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
    public class MainDialog2 : IDialog<object>
    {
        private User _currentUser;

        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(ReceivedMessageAsync);
        }

        public async Task ReceivedMessageAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            try
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
                var command = resolver.Resolve(message.Text);
                Trace.WriteLine($"Resolved command is {command.Type} with {command.Message}");

                await CommandAsync(context, command);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"ReceivedMessageAsync failed - {ex.Message} - {ex.StackTrace}");
                await context.PostAsync($"ReceivedMessageAsync failed - {ex.Message}");
                context.Wait(ReceivedMessageAsync);
            }
        }

        private async Task CommandAsync(IDialogContext context, Command command)
        {
            var handleMessage = false;
            switch (command.Type)
            {
                case CommandType.RegistUser:
                    handleMessage = await CommandRegistUserAsync(context, command);
                    break;
                case CommandType.UnregistUser:
                    handleMessage = await CommandUnregistUserAsync(context, command);
                    break;
                case CommandType.Preference:
                    handleMessage = await CommandPreferenceAsync(context, command);
                    break;
                case CommandType.DownloadTimecard:
                    handleMessage = await CommandDownloadTimecardAsync(context, command);
                    break;
                case CommandType.DownloadTimecardThisMonth:
                    handleMessage = await CommandDownloadTimecardThisMonthAsync(context, command);
                    break;
                case CommandType.DownloadTimecardPreviousMonth:
                    handleMessage = await CommandDownloadTimecardPreviousMonthAsync(context, command);
                    break;
                case CommandType.ModifyTimecard:
                    handleMessage = await CommandModifyTimecardAsync(context, command);
                    break;
                case CommandType.AboutThis:
                    handleMessage = await CommandAboutThisAsync(context, command);
                    break;
                case CommandType.PostFeedback:
                    handleMessage = await CommandPostFeedbackAsync(context, command);
                    break;
                case CommandType.Menu:
                    handleMessage = await CommandMenuAsync(context, command);
                    break;
                case CommandType.Others:
                    handleMessage = await CommandOthersAsync(context, command);
                    break;
                case CommandType.Cancel:
                    handleMessage = await CommandCommandAsync(context, command);
                    break;
                case CommandType.EasterEggGanbaruzoi:
                    handleMessage = await CommandEasterEggGanbaruzoiAsync(context, command);
                    break;
                case CommandType.PunchTodayIsOff:
                    handleMessage = await CommandPunchTodayIsOffAsync(context, command);
                    break;
                case CommandType.AnswerToEoW:
                    handleMessage = await CommandAnswerToEoWAsync(context, command);
                    break;
                case CommandType.AnswerToEoWWithTime:
                    handleMessage = await CommandAnswerToEoWWithTimeAsync(context, command);
                    break;
                case CommandType.AnswerToNotEoW:
                    handleMessage = await CommandAnswerToAnswerToNotEoWAsync(context, command);
                    break;
                case CommandType.AnswerToDoNotAskToday:
                    handleMessage = await CommandAnswerToDoNotAskTodayAsync(context, command);
                    break;
                case CommandType.None:
                default:
                    {
                        var text = (_currentUser == null) ?
                            "初めての方は、 menu とタイプしてメニューを表示し、「ユーザー登録」を選択してください。" :
                            "menu とタイプするとメニューを表示します。";

                        await context.PostAsync($"こんにちわ {_currentUser?.NickName ?? "ゲスト"} さん。" + text);
                        handleMessage = false;
                    }
                    break;
            }

            if (!handleMessage)
            {
                context.Wait(ReceivedMessageAsync);
            }
        }

        private async Task<bool> CommandPostFeedbackAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var dlg = FormDialog.FromForm(FeedbackOrder.BuildForm, FormOptions.PromptInStart);
            context.Call(dlg, ReceivePostFeedbackOrderAsync);

            return true;
        }

        private async Task ReceivePostFeedbackOrderAsync(IDialogContext context, IAwaitable<FeedbackOrder> argument)
        {
            var order = await argument;
            var usecase = new MainUsecase(_currentUser);
            await usecase.PostFeedback(order.Body);

            await context.PostAsync("送信しました、今後の改善のネタにします。ご意見ありがとうございます。");
        }

        private async Task<bool> CommandCommandAsync(IDialogContext context, Command command)
        {
            await context.PostAsync("メニューを閉じました。");
            return false;
        }

        private async Task<bool> CommandAnswerToDoNotAskTodayAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var usecase = new MainUsecase(_currentUser);
            var stateEntity = await usecase.GetCurrentUserStatus();

            // 終業かを問い合わせ中なら、
            // （y:終わった／n:終わってない／d:今日は徹夜）に応答する。
            if ((stateEntity?.State ?? AskingState.None) == AskingState.AskingEoW)
            {
                // 今日はもう聞かないにして更新
                await usecase.PunchDoNotAskToday(stateEntity);
                await context.PostAsync($"分かりました。今日はもう聞きません。");
            }
            else
            {
                await context.PostAsync($"今は仕事の終わりを聞いていません。");
            }

            return false;
        }

        private async Task<bool> CommandAnswerToEoWAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var usecase = new MainUsecase(_currentUser);
            var stateEntity = await usecase.GetCurrentUserStatus();

            // 終業かを問い合わせ中なら、
            // （y:終わった／n:終わってない／d:今日は徹夜）に応答する。
            if ((stateEntity?.State ?? AskingState.None) == AskingState.AskingEoW)
            {
                // 聞かれた時刻で、終業時刻を更新
                var eowDateTime = await usecase.PunchEoW(stateEntity);
                await context.PostAsync($"お疲れさまでした。{eowDateTime.ymd.FormatMd()} の" +
                    $"終業時刻は {eowDateTime.hm.Format()} を記録しました。");
            }
            else
            {
                await context.PostAsync($"今は仕事の終わりを聞いていません。終業時刻を記録するには タイムカードの編集 とタイプして下さい。");
            }

            return false;
        }

        private async Task<bool> CommandAnswerToEoWWithTimeAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var usecase = new MainUsecase(_currentUser);

            // 聞かれた時刻で、終業時刻を更新
            var eowDateTime = await usecase.PunchEoW(command.Message);
            await context.PostAsync($"お疲れさまでした。{eowDateTime.ymd.FormatMd()} の" +
                $"終業時刻は {eowDateTime.hm.Format()} を記録しました。");

            return false;
        }
        private async Task<bool> CommandAnswerToAnswerToNotEoWAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            await context.PostAsync($"失礼しました、引き続きがんばってください、３０分後にまた聞きます。");
            return false;
        }

        private async Task<bool> CommandPunchTodayIsOffAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var usecase = new MainUsecase(_currentUser);
            var stateEntity = await usecase.GetCurrentUserStatus();

            // 「今日は休み」と言われたら、 AskingEoW でなくともその日は休日にする
            if (_currentUser != null)
            {
                // 今日を休みに更新
                await usecase.PunchTodayIsOff(stateEntity);
                await context.PostAsync($"今日はお休みなのですね、分かりました。今日はもう聞きません。よい休日をお過ごし下さい。");
            }
            else
            {
                await context.PostAsync($"今は仕事の終わりを聞いていません。よい休日をお過ごし下さい。");
            }

            return false;
        }

        private async Task<bool> CommandEasterEggGanbaruzoiAsync(IDialogContext context, Command command)
        {
            var usecase = new EasterEgg();
            await usecase.PostGanbaruzoi(context);
            return false;
        }

        private async Task<bool> CommandOthersAsync(IDialogContext context, Command command)
        {
            PromptDialog.Choice(context, SubMenuProcessAsync,
                new Command[]
                {
                    Command.Make(CommandType.PostFeedback),
                    Command.Make(CommandType.UnregistUser),
                    Command.Make(CommandType.Cancel)
                }, "その他の機能です。操作を選択して下さい。");
            return true;
        }

        private async Task<bool> CommandMenuAsync(IDialogContext context, Command command)
        {
            var menus = new List<Command>();

            if (_currentUser == null)
            {
                // 未登録ユーザー
                menus.Add(Command.Make(CommandType.RegistUser));
                menus.Add(Command.Make(CommandType.AboutThis));
                menus.Add(Command.Make(CommandType.Cancel));
            }
            else
            {
                // 登録済みユーザー
                menus.Add(Command.Make(CommandType.DownloadTimecard));
                menus.Add(Command.Make(CommandType.ModifyTimecard));
                menus.Add(Command.Make(CommandType.AboutThis));
                menus.Add(Command.Make(CommandType.Others));
                menus.Add(Command.Make(CommandType.Cancel));
            }

            PromptDialog.Choice<Command>(context, SubMenuProcessAsync,
                menus, "タイムカードボットのメインメニューです。操作を選択して下さい。");
            return true;
        }

        private async Task<bool> CommandRegistUserAsync(IDialogContext context, Command command)
        {
            if (_currentUser != null)
            {
                await context.PostAsync("あなたは既にユーザー登録されています。");
                return false;
            }

            var dlg = FormDialog.FromForm(RegistUserOrder.BuildForm, FormOptions.PromptInStart);
            context.Call(dlg, ReceivedRegistUserOrderAsync);
            return true;
        }

        private async Task ReceivedRegistUserOrderAsync(IDialogContext context, IAwaitable<RegistUserOrder> result)
        {
            var order = await result;
            var conversationRef = context.Activity.ToConversationReference();
            var userId = await (context.Activity as Activity).GetFirstMember();

            var usecase = new UserUsecase();
            _currentUser = await usecase.RegistUser(userId, order, conversationRef);

            await context.PostAsync($"ユーザーを登録しました。\n\nこれから毎日、{order.EndOfWorkTime}になったら仕事が終わったかを聞きますので、よろしくお願いします。");

            context.Wait(ReceivedMessageAsync);
        }

        private async Task<bool> CommandDownloadTimecardAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var dlg = FormDialog.FromForm(DownloadTimecardOrder.BuildForm, FormOptions.PromptInStart);
            context.Call(dlg, ReceiveDownloadTimecardOrderAsync);

            return true;
        }

        private async Task ReceiveDownloadTimecardOrderAsync(IDialogContext context, IAwaitable<DownloadTimecardOrder> result)
        {
            var order = await result;
            var usecase = new MainUsecase(_currentUser);
            var res = await usecase.DumpTimecard(order.YearMonth);

            if (string.IsNullOrEmpty(res.csv))
            {
                await context.PostAsync($"{res.ym.Format()} のタイムカードはデータがありません。");
            }
            else
            {
                await context.PostAsync($"{res.ym.Format()} のタイムカードです。");
                await context.PostAsync(res.csv);
            }
        }

        private async Task<bool> CommandDownloadTimecardThisMonthAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            // ユーザーのタイムゾーンでの現在時刻
            var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
            var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser);
            var ym = Yyyymm.FromDate(nowUserTz);

            var usecase = new MainUsecase(_currentUser);
            var dumped = await usecase.DumpTimecard(ym);

            if (string.IsNullOrEmpty(dumped))
            {
                await context.PostAsync($"{ym.Format()} のタイムカードはデータがありません。");
            }
            else
            {
                await context.PostAsync($"{ym.Format()} のタイムカードです。");
                await context.PostAsync(dumped);
            }

            return false;
        }


        private async Task<bool> CommandDownloadTimecardPreviousMonthAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            // ユーザーのタイムゾーンでの現在時刻
            var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
            var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser).AddMonths(-1); // 先月
            var ym = Yyyymm.FromDate(nowUserTz.AddMonths(-1));

            var usecase = new MainUsecase(_currentUser);
            var dumped = await usecase.DumpTimecard(ym);

            if (string.IsNullOrEmpty(dumped))
            {
                await context.PostAsync($"{ym.Format()}bのタイムカードはデータがありません。");
            }
            else
            {
                await context.PostAsync($"{ym.Format()} のタイムカードです。");
                await context.PostAsync(dumped);
            }

            return false;
        }

        private async Task<bool> CommandModifyTimecardAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var dlg = FormDialog.FromForm(ModifyTimecardOrder.BuildForm, FormOptions.PromptInStart);
            context.Call(dlg, ReceiveModifyTimecardOrderAsync);

            return true;
        }

        private async Task ReceiveModifyTimecardOrderAsync(IDialogContext context, IAwaitable<ModifyTimecardOrder> result)
        {
            var order = await result;

            var usecase = new MainUsecase(_currentUser);
            await usecase.ModifyTimecard(order.Date, order.EoWTime);
            await context.PostAsync("タイムカードを変更しました。");
        }

        private async Task<bool> CommandAboutThisAsync(IDialogContext context, Command command)
        {
            var interval = 3000;
            await context.PostAsync("私は、終業時間を毎日EXCELに記録するのが面倒なアナタのためのボットです。");
            await Task.Delay(interval);
            await context.PostAsync("ユーザー登録しておくと、終業時間を過ぎたら私がアナタに「仕事はおわりましたか？」と聞きます。");
            await Task.Delay(interval);
            await context.PostAsync("アナタが「はい」と応えたら、私はその時刻を終業時間としてタイムカードに記録します。");
            await Task.Delay(interval);
            await context.PostAsync("「いいえ」と応える、または無視すると、私は３０分後に再び聞きます。");
            await Task.Delay(interval);
            await context.PostAsync("毎日私の問いに応えるだけで、月末には上司に提出するための日報ができています。ぜひ私に登録してみてください。");
            return false;
        }

        public async Task SubMenuProcessAsync(IDialogContext context, IAwaitable<Command> argument)
        {
            var command = (await argument);
            await CommandAsync(context, command);
        }

        public async Task<bool> CommandUnregistUserAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されていません。");
                return false;
            }
            else
            {
                PromptDialog.Confirm(context, ReceivedUnregistUserAsync, "ユーザーを削除してよいですか？");
                return true;
            }
        }

        private async Task<bool> CommandPreferenceAsync(IDialogContext context, Command command)
        {
            if (_currentUser == null)
            {
                await context.PostAsync("ユーザー登録されている人のみ使える機能です。");
                return false;
            }

            var usecase = new UserUsecase();

            await context.PostAsync($"現在のユーザー設定は次のとおりです。\n\n--\n\n{_currentUser.ToDescribeString()}");

            PromptDialog.Confirm(context, ReceivedPreferenceAsync, "変更しますか？");
            return true;
        }

        private async Task ReceivedPreferenceAsync(IDialogContext context, IAwaitable<bool> result)
        {
            var confirm = await result;
            if (confirm)
            {
                var dlg = FormDialog.FromForm(ChangeUserPreferenceOrder.BuildForm, FormOptions.PromptInStart);
                context.Call(dlg, ReceiveChangeUserPreferenceOrderAsync);
            }
            else
            {
                await context.PostAsync("ユーザー設定の変更を中止しました。");
            }
        }

        private async Task ReceiveChangeUserPreferenceOrderAsync(IDialogContext context, IAwaitable<ChangeUserPreferenceOrder> result)
        {
            var order = await result;
            await context.PostAsync("ユーザー設定を変更しました。");
        }

        public async Task ReceivedUnregistUserAsync(IDialogContext context, IAwaitable<bool> argument)
        {
            var confirm = await argument;
            if (confirm)
            {
                PromptDialog.Confirm(context, ReceivedUnregistUserConfirmAsync, 
                    "退会すると記録されているデータが全て削除されます。本当に削除してよろしいですか？（これが最後の確認です）");
            }
            else
            {
                await context.PostAsync("ユーザー削除を中止しました。");
                context.Wait(ReceivedMessageAsync);
            }
        }

        public async Task ReceivedUnregistUserConfirmAsync(IDialogContext context, IAwaitable<bool> argument)
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
            context.Wait(ReceivedMessageAsync);
        }
    }
}