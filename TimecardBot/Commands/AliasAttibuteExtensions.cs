using System;
using System.Linq;
using System.Reflection;

namespace TimecardBot.Commands
{
    public static class AttibuteExtensions
    {
        public static string ToAlias<T>(this T hasAlias) where T : struct
        {
            Type type = hasAlias.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException();
            }

            var memberInfo = type.GetMember(hasAlias.ToString()).Single();
            var attribute = memberInfo.GetCustomAttribute<AliasAttribute>();
            return attribute?.Label ?? "<null>";
        }
        public static string[] ToWords<T>(this T words) where T : struct
        {
            Type type = words.GetType();
            if (!type.IsEnum)
            {
                throw new ArgumentException();
            }

            var memberInfo = type.GetMember(words.ToString()).Single();
            var attribute = memberInfo.GetCustomAttribute<CommandAttribute>();
            return attribute?.Words ?? new string[0];
        }
    }
}