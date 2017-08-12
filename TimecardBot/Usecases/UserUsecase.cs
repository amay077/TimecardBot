using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TimecardLogic.DataModels;
using TimecardLogic.Repositories;
using TimecardBot.DataModels;
using Microsoft.Bot.Connector;

namespace TimecardBot.Usecases
{
    public sealed class UserUsecase
    {
        private readonly UsersRepository _userRepo = new UsersRepository();

        public async Task<User> RegistUser(string userId, RegistUserOrder order, ConversationReference conversationRef)
        {
            // 有効な曜日群の抽出（例： 日火土→ 0101110）
            var dayOfWeelEnables = User.WEEKDAYS.Select(d => order.DayOfWeekEnables.Contains(d) ? "0" : "1").Aggregate((x, y) => x + y);

            // 休日群の抽出
            //var holidays = order.Holidays.Split(',', ' ')
            //    ?.Select(x => x.Trim())
            //    ?.Where(x=> 
            //    {
            //        DateTime dummy;
            //        return DateTime.TryParse(x, out dummy);
            //    })
            //    ?.Distinct()
            //    ?.ToList() ?? default(IList<string>);
            var holidays = default(IList<string>);

            var tzTokyo = TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time");
            await _userRepo.AddUser(userId, order.NickName, $"{((int)order.EndOfWorkTime):00}00", "2400", tzTokyo.Id,
                JsonConvert.SerializeObject(conversationRef), dayOfWeelEnables, holidays);
            return await _userRepo.GetUserById(userId);
        }

        public async Task<User> GetUser(string userId)
        {
            return await _userRepo.GetUserById(userId);
        }

        public async Task DeleteUser(User currentUser)
        {
            await _userRepo.DeleteUser(currentUser.UserId);
        }
    }
}