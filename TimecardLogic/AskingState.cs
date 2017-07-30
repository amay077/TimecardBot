using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardLogic
{
    [Serializable]
    public enum AskingState
    {
        None,
        AskingEoW, // 終業したか聞いてる最中
        Punched, // 終業を受信した
        DoNotAskToday, // 今日はもう聞かないで
        TodayIsOff // 今日は休日
    }
}
