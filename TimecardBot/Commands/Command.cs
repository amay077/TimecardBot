using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Commands
{
    [Serializable]
    public struct Command<E> where E : struct
    {
        public E Type { get; set; }

        public override string ToString()
        {
            return Type.ToAlias();
        }

        public static Command<E> Make(E type)
        {
            return new Command<E> { Type = type };
        }
    }
}