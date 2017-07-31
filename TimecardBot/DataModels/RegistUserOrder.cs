using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using static TimecardBot.MessagesController;

namespace TimecardBot.DataModels
{
    [Serializable]
    public sealed class RegistUserOrder
    {
        [Prompt("{&} を入力してください。")]
        [Describe("ニックネーム")]
        public string NickName { get; set; }

        [Prompt("{&} を選択してください。{||}")]
        [Describe("終業時刻")]
        public EndOfWorkTimeType EndOfWorkTime;


        public static IForm<RegistUserOrder> BuildForm()
        {
            return new FormBuilder<RegistUserOrder>()
                .Message("ユーザー登録を行います。次の情報を入力または選択してください。")
                .Field(nameof(NickName))
                .Field(nameof(EndOfWorkTime))
                .Confirm(async order => 
                {
                    return new PromptAttribute(
                        "以下の情報でユーザー登録します。\n\n  \n\n" +
                        $"・ニックネーム: {order.NickName}\n\n" +
                        $"・終業時刻: {order.EndOfWorkTime}\n\n  \n\n" +
                        "よろしいですか？ {||}")
;
                }
)
                .AddRemainingFields()
                .Build();
        }
    }
}