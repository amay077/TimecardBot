using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.DataModels
{
    [Serializable]
    public sealed class DownloadTimecardOrder
    {
        [Prompt("{&} を YYYYMM の形式または今月、先月などと入力してください。")]
        [Describe("年月")]
        public string YearMonth { get; set; }

        public static IForm<DownloadTimecardOrder> BuildForm()
        {
            return new FormBuilder<DownloadTimecardOrder>()
                .Message("指定した年月のタイムカードを表示します。")
                .Field(nameof(YearMonth))
                .Confirm(async order =>
                {
                    return new PromptAttribute(
                        $"{order.YearMonth} のタイムカードをダウンロードします。\n\n" +
                        "--\n\n" +
                        "よろしいですか？ {||}");
                })
                .AddRemainingFields()
                .Build();
        }

    }
}