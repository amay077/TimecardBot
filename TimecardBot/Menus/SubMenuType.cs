using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Menus
{
    public enum SubMenuType
    {
        [AliasAttribute("なし")]
        None,

        [AliasAttribute("退会")]
        UnregistUser, // ユーザー退会

        [AliasAttribute("意見を送る")]
        PostFeedback,

        [AliasAttribute("閉じる")]
        Cancel // キャンセル
    }
}