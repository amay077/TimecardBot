using Microsoft.Bot.Builder.FormFlow;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.DataModels
{
    public enum EndOfWorkTimeType : int
    {
        //[Describe("16時")]
        午後４時 = 16,
        //[Describe("17時")]
        午後５時 = 17,
        //[Describe("18時")]
        午後６時 = 18,
        //[Describe("19時")]
        午後７時 = 19,
        //[Describe("20時")]
        午後８時 = 20,
    }
}