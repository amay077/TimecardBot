using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Commands
{
    public class AliasAttribute : Attribute
    {
        public string Label { get; }

        public AliasAttribute(string label)
        {
            Label = label;

        }
    }
}