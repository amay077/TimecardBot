using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;

namespace TimecardBot
{
    public static class AliasAttibuteExtensions
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
    }
}