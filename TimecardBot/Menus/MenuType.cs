using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Menus
{
    public enum MenuType
    {
        [AliasAttribute("なし")]
        None,

        [AliasAttribute("ユーザー登録")]
        RegistUser, // ユーザー登録

        [AliasAttribute("日報のダウンロード")]
        DownloadTimecard,

        [AliasAttribute("日報の編集")]
        ModityTimecard,

        [AliasAttribute("このボットについて")]
        AboutThis,

        [AliasAttribute("その他")]
        Others,

        [AliasAttribute("閉じる")]
        Cancel // キャンセル
    }
}