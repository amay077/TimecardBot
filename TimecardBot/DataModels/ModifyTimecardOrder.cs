using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.DataModels
{
    [Serializable]
    public sealed class ModifyTimecardOrder
    {
        [Prompt("編集する {&} を YYYYMMDD の形式で入力してください。")]
        [Describe("日付")]
        public string Date { get; set; }

        [Prompt("登録する {&} を HHMM の形式で入力してください。削除する場合は なし と入力して下さい。")]
        [Describe("終業時刻")]
        public string EoWTime{ get; set; }

        public static IForm<ModifyTimecardOrder> BuildForm()
        {
            return new FormBuilder<ModifyTimecardOrder>()
                .Message("指定した日付のタイムカードを編集（追加、更新または削除）します。")
                .Field(nameof(Date))
                .Field(nameof(EoWTime))
                .Confirm(async order =>
                {
                    return new PromptAttribute(
                        $"{order.Date} のタイムカードを {order.EoWTime} にします。\n\n" +
                        "--\n\n" +
                        "よろしいですか？ {||}");
                })
                .AddRemainingFields()
                .Build();
        }
    }
}