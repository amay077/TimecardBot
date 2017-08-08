﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Commands
{
    public enum CommandType
    {
        [AliasAttribute("なし")]
        [Command()]
        None,

        [AliasAttribute("今日は休み")]
        [Command("今日は休み", "今日は有給", "今日は有休", "休み", "有休", "有給")]
        PunchTodayIsOff,

        [AliasAttribute("ユーザー登録")]
        [Command("ユーザー登録", "regist user", "regist")]
        RegistUser,

        [AliasAttribute("退会")]
        [Command("ユーザー退会", "unregist user", "unregist")]
        UnregistUser,

        [AliasAttribute("日報のダウンロード")]
        [Command("日報のダウンロード", "ダウンロード", "download")]
        DownloadTimecard,

        [AliasAttribute("日報の編集")]
        [Command("日報の編集", "編集", "edit")]
        ModityTimecard,

        [AliasAttribute("このボットについて")]
        [Command("このボットについて", "説明", "about")]
        AboutThis,

        [AliasAttribute("意見を送る")]
        [Command("意見を送る", "post feedback", "feedback", "要望", "報告")]
        PostFeedback,

        [AliasAttribute("メニュー")]
        [Command("メニュー", "menu")]
        Menu,

        [AliasAttribute("その他のメニュー")]
        [Command("その他のメニュー", "other menu")]
        Others,

        [AliasAttribute("仕事終わった")]
        [Command("仕事終わった", "y", "yes", "ok", "はい")]
        AnswerToEoW,

        [AliasAttribute("仕事終わってない")]
        [Command("仕事終わってない", "n", "no", "ng", "いいえ", "だめ")]
        AnswerToNotEoW,

        [AliasAttribute("今日はもう聞かないで")]
        [Command("今日はもう聞かないで", "d")]
        AnswerToDoNotAskToday,

        [AliasAttribute("閉じる")]
        Cancel, // キャンセル

                    [AliasAttribute("今日も一日")]
        [Command("今日も一日")]
        EasterEggGanbaruzoi
    }
}