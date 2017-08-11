using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TimecardBot.Commands
{
    [Serializable]
    public struct Command
    {
        public CommandType Type { get; }

        public string Message { get; }

        public Command(CommandType type, string message) : this()
        {
            Type = type;
            Message = message;
        }

        public override string ToString()
        {
            return Type.ToAlias();
        }

        public static Command Make(CommandType type, string message = default(string))
        {
            return new Command(type, message);
        }
    }
}