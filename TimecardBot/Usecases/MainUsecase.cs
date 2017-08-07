using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using TimecardLogic;
using TimecardLogic.DataModels;
using TimecardLogic.Entities;
using TimecardLogic.Repositories;

namespace TimecardBot.Usecases
{
    public sealed class MainUsecase
    {
        private readonly User _currentUser;
        private readonly ConversationStateRepository _conversationStateRepo = new ConversationStateRepository();
        private readonly MonthlyTimecardRepository _monthlyTimecardRepo = new MonthlyTimecardRepository();

        public MainUsecase(User currentUser)
        {
            _currentUser = currentUser;
        }

        public async Task PunchTodayIsOff(ConversationStateEntity stateEntity)
        {
            // 今日は休み にして更新
            stateEntity.State = AskingState.TodayIsOff;
            await _conversationStateRepo.UpsertState(stateEntity);

            // もし終業時刻が登録済みだったら削除する
            var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
            var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser); // ユーザーのタイムゾーンでの現在時刻

            await _monthlyTimecardRepo.DeleteTimecardRecord(_currentUser.UserId, nowUserTz.Year, nowUserTz.Month, nowUserTz.Day);
        }

        public async Task<ConversationStateEntity> GetCurrentUserStatus()
        {
            return await _conversationStateRepo.GetStatusByUserId(_currentUser?.UserId ?? string.Empty);
        }

        public async Task<(int year, int month, int day, int hour, int minute)> 
            PunchEoW(ConversationStateEntity stateEntity)
        {
            int eowHour, eowMinute;
            Util.ParseHHMM(stateEntity.TargetTime, out eowHour, out eowMinute);

            // 該当日のタイムカードの終業時刻を更新
            int year, month, day;
            Util.ParseYYYYMMDD(stateEntity.TargetDate, out year, out month, out day);
            var monthlyTimecardRepo = new MonthlyTimecardRepository();
            await monthlyTimecardRepo.UpsertTimecardRecord(_currentUser.UserId, year, month, day, eowHour, eowMinute);

            // 打刻済みにして更新
            stateEntity.State = AskingState.Punched;
            await _conversationStateRepo.UpsertState(stateEntity);

            return ( year, month, day, eowHour, eowMinute );
        }

        public async Task PunchDoNotAskToday(ConversationStateEntity stateEntity)
        {
            // 今日はもう聞かないにして更新
            stateEntity.State = AskingState.DoNotAskToday;
            await _conversationStateRepo.UpsertState(stateEntity);
        }
    }
}