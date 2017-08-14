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

        [Prompt("{&} を「土日」「月水金」のように入力してください。。設定しない場合は「なし」と入力してください。")]
        [Describe("仕事が休みの曜日")]
        public string DayOfWeekEnables { get; set; }

        // FIXME 毎年ある祝日か、単発の休日かの管理が面倒なので、とりま未使用
        //[Prompt("{&} （定休日、祝日など）をカンマまたはスペース区切りで「1/1,2/11」「5/3 5/5 7/20」のように入力してください。設定しない場合は「なし」と入力してください。")]
        ////[Describe("その他の休日")]
        //public string Holidays { get; set; }

        public static IForm<RegistUserOrder> BuildForm()
        {
            return new FormBuilder<RegistUserOrder>()
                .Message("ユーザー登録を行います。次の情報を入力または選択してください。" +
                "中止する場合は「中止」、「やめる」または「cancel」とタイプしてください。")
                .Field(nameof(NickName))
                .Field(nameof(EndOfWorkTime))
                .Field(nameof(DayOfWeekEnables))
                //.Field(nameof(Holidays))
                .Confirm(async order => 
                {
                    return new PromptAttribute(
                        "以下の情報でユーザー登録します。\n\n" +
                        $"・ニックネーム: {order.NickName}\n\n" +
                        $"・終業時刻: {order.EndOfWorkTime}\n\n" +
                        $"・仕事が休みの曜日: {order.DayOfWeekEnables}\n\n" +
                        //$"・その他の休日: {order.Holidays}\n\n" +
                        "よろしいですか？ {||}");
                }
)
                .AddRemainingFields()
                .Build();
        }
    }
}