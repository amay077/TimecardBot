using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Commands
{
    public class CommandAttribute : Attribute
    {
        public string[] Words { get; }

        public CommandAttribute(params string[] words)
        {
            Words = words;
        }
    }
}