using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot
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