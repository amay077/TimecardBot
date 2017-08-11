using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.DataModels
{
    [Serializable]
    public sealed class FeedbackOrder
    {
        [Prompt("{&} を入力してください。")]
        [Describe("ご意見や不具合の内容")]
        public string Body { get; set; }

        public static IForm<FeedbackOrder> BuildForm()
        {
            return new FormBuilder<FeedbackOrder>()
                .Message("このボットに対するご意見を募集しています。")
                .Field(nameof(Body))
                //.Field(nameof(Holidays))
                .Confirm(async order =>
                {
                    return new PromptAttribute(
                        "次の内容で送信します。\n\n" +
                        "--\n\n" +
                        $"{order.Body}\n\n" +
                        "--\n\n" +
                        "よろしいですか？ {||}");
                }
)
                .AddRemainingFields()
                .Build();
        }
    }
}