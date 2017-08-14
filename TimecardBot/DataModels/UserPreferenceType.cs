using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using TimecardBot.Commands;
using TimecardLogic.DataModels;

namespace TimecardBot.DataModels
{
    public enum UserPreferenceType
    {
        [Alias("ニックネーム")]
        NickName,

        [Alias("終業時刻")]
        EndOfWorkTime,

        [Alias("確認終了時刻")]
        EndOfConfirmTime,

        [Alias("休みの曜日")]
        DayOfWeekEnables,

        [Alias("タイムゾーン")]
        TimeZoneId,

        [Alias("中止")]
        Cancel
    }
}