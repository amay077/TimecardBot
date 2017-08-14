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

        public async Task<User> Update(User user, UserPreferenceType prefType, string text)
        {
            var userEntity = await _userRepo.GetUserEntityById(user.UserId);

            switch (prefType)
            {
                case UserPreferenceType.NickName:
                    userEntity.NickName = text;
                    break;
                case UserPreferenceType.EndOfWorkTime:
                    userEntity.AskEndOfWorkStartTime = text;
                    break;
                case UserPreferenceType.EndOfConfirmTime:
                    userEntity.AskEndOfWorkEndTime = text;
                    break;
                case UserPreferenceType.DayOfWeekEnables:
                    {
                        // 有効な曜日群の抽出（例： 日火土→ 0101110）
                        var dayOfWeelEnables = User.WEEKDAYS.Select(d => text.Contains(d) ? "0" : "1").Aggregate((x, y) => x + y);
                        userEntity.DayOfWeekEnables = dayOfWeelEnables;
                    }
                    break;
                case UserPreferenceType.TimeZoneId:
                    userEntity.TimeZoneId = text;
                    break;
                case UserPreferenceType.Cancel:
                default:
                    return user;
            }

            await _userRepo.UpdateUser(userEntity);
            return userEntity.ToModel();
        }
    }
}