using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic
{
    [Serializable]
    public enum AskingState : int
    {
        None = 0,
        AskingEoW = 1, // 終業したか聞いてる最中
        Punched = 2, // 終業を受信した
        DoNotAskToday = 3, // 今日はもう聞かないで
        TodayIsOff = 4 // 今日は休日
    }
}
