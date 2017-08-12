using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.DataModels
{
    [Serializable]
    public sealed class ChangeUserPreferenceOrder
    {
        [Optional]
        [Prompt("{&} を入力してください。変更しない場合は s を入力して下さい。")]
        [Describe("ニックネーム")]
        public string NickName { get; set; }

        [Optional]
        [Prompt("{&} を hhmm の形式で入力してください。変更しない場合は s を入力して下さい。")]
        [Describe("終業時刻（確認開始時刻）")]
        public string EndOfWorkTime;

        [Optional]
        [Prompt("{&} を hhmm の形式で入力してください。変更しない場合は s を入力して下さい。")]
        [Describe("確認終了時刻")]
        public string EndOfConfirmTime;

        [Optional]
        [Prompt("{&} を「土日」「月水金」のように入力してください。設定しない場合は「なし」と入力してください。変更しない場合は s を入力して下さい。")]
        [Describe("休みの曜日")]
        public string DayOfWeekEnables { get; set; }

        [Optional]
        [Prompt("{&} を入力入力してください。変更しない場合は s を入力して下さい。")]
        [Describe("タイムゾーン")]
        public string TimeZoneId { get; set; }

        public static IForm<ChangeUserPreferenceOrder> BuildForm()
        {
            return new FormBuilder<ChangeUserPreferenceOrder>()
                .Message("ユーザー設定の変更を行います。次の情報を入力または選択してください。")
                .Field(nameof(NickName))
                .Field(nameof(EndOfWorkTime))
                .Field(nameof(EndOfConfirmTime))
                .Field(nameof(DayOfWeekEnables))
                .Field(nameof(TimeZoneId))
                .Confirm(async order =>
                {
                    return new PromptAttribute(
                        "以下の情報でユーザー設定を更新します。\n\n" +
                        $"・ニックネーム: {(order.NickName.Equals("s") ? "変更なし" : order.NickName)}\n\n" +
                        $"・終業時刻（確認開始時刻）: {(order.EndOfWorkTime.Equals("s") ? "変更なし" : order.EndOfWorkTime)}\n\n" +
                        $"・確認終了時刻: {(order.EndOfWorkTime.Equals("s") ? "変更なし" : order.EndOfWorkTime)}\n\n" +
                        $"・休みの曜日: {(order.DayOfWeekEnables.Equals("s") ? "変更なし" : order.DayOfWeekEnables)}\n\n" +
                        $"・タイムゾーン: {(order.TimeZoneId.Equals("s") ? "変更なし": order.TimeZoneId)}\n\n" +
                        "よろしいですか？ {||}");
                })
                .AddRemainingFields()
                .Build();
        }
    }
}