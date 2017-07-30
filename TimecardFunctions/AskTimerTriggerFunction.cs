using System;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace TimecardFunctions
{
    public static class AskTimerTriggerFunction
    {
        [FunctionName("AskTimerTrigger")]
        public static void Run([TimerTrigger("0 */30 * * * *")]TimerInfo myTimer, TraceWriter log)
        {
            log.Info($"C# Timer trigger function executed at: {DateTime.Now}");

            // Skype でメッセージ送信
            var sender = new MessageSender(log);
            sender.Send();
            log.Info($"C# Timer trigger messege sent at: {DateTime.Now}");

        }
    }
}