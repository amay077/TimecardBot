using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Menus
{
    [Serializable]
    public struct Menu
    {
        public MenuType Type { get; set; }

        public override string ToString()
        {
            return Type.ToAlias();
        }

        public static Menu Make(MenuType type)
        {
            return new Menu { Type = type };
        }
    }
}