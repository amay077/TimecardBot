using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TimecardFunctions
{
    static class TaskExtensions
    {
        public static T RunAsSync<T>(this Task<T> self)
        {
            return self.Result;
        }

        public static void RunAsSync(this Task self)
        {
            self.Wait();
        }
    }
}
