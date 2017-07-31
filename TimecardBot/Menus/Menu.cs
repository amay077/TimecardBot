using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Menus
{
    [Serializable]
    public struct Menu<E> where E:struct
    {
        public E Type { get; set; }

        public override string ToString()
        {
            return Type.ToAlias();
        }

        public static Menu<E> Make(E type)
        {
            return new Menu<E> { Type = type };
        }
    }
}