using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private readonly FeedbackRepository _feedbackRepo = new FeedbackRepository();

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

        public async Task<(int year, int month, int day, int hour, int minute)> PunchEoW(string hhmm)
        {
            var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
            var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser); // ユーザーのタイムゾーンでの現在時刻

            int hour = 0;
            int minute = 0;
            Util.ParseHHMM(hhmm, out hour, out minute);

            await PunchEoW(nowUserTz.Year, nowUserTz.Month, nowUserTz.Day, hour, minute);
            return (nowUserTz.Year, nowUserTz.Month, nowUserTz.Day, hour, minute);
        }

        public async Task PunchEoW(int year, int month, int day, int hour, int minute)
        {
            // 指定された日付の終業時刻を、指定された時刻で更新する
            var monthlyTimecardRepo = new MonthlyTimecardRepository();
            await monthlyTimecardRepo.UpsertTimecardRecord(_currentUser.UserId, year, month, day, hour, minute);

            // 当日ならもう聞かないようにステータスを打刻済みに更新
            var stateEntity = await _conversationStateRepo.GetStatusByUserId(_currentUser.UserId);
            if (stateEntity != null && stateEntity.TargetDate.Equals($"{year:0000}/{month:00}/{day:00}"))
            {
                stateEntity.State = AskingState.Punched;
                await _conversationStateRepo.UpsertState(stateEntity);
            }

            return;
        }


        public async Task PunchDoNotAskToday(ConversationStateEntity stateEntity)
        {
            // 今日はもう聞かないにして更新
            stateEntity.State = AskingState.DoNotAskToday;
            await _conversationStateRepo.UpsertState(stateEntity);
        }

        public async Task PostFeedback(string feedback)
        {
            await _feedbackRepo.AddFeedback(_currentUser.UserId, feedback);
        }

        public async Task<string> DumpTimecard(string yyyymm)
        {
            int year = 0;
            int month = 0;

            // ユーザーのタイムゾーンでの現在時刻
            var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
            var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser);

            if (yyyymm.Contains("今月"))
            {
                year = nowUserTz.Year;
                month = nowUserTz.Month;
            }
            else if (yyyymm.Contains("先月"))
            {
                nowUserTz = nowUserTz.AddMonths(-1);
                year = nowUserTz.Year;
                month = nowUserTz.Month;
            }
            else
            {
                Util.ParseYYYYMM(yyyymm, out year, out month);
            }

            var records = await _monthlyTimecardRepo.GetTimecardRecordByYearMonth(_currentUser.UserId, year, month);

            if (records == null || records.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            builder.Append("日付, 終業時刻");
            builder.Append("\n\n");

            foreach (var rec in records)
            {
                builder.Append($"{rec.Day}, {rec.EoWTime}");
                builder.Append("\n\n");
            }

            return builder.ToString();
        }

        public async Task ModifyTimecard(string yyyymmdd, string eoWTime)
        {
            int year = 0;
            int month = 0;
            int day = 0;
            Util.ParseYYYYMMDD(yyyymmdd, out year, out month, out day);

            // なしと言われたら該当日のタイムカードを削除
            if (eoWTime.Contains("なし"))
            {
                await _monthlyTimecardRepo.DeleteTimecardRecord(_currentUser.UserId, year, month, day);

                // ユーザーのタイムゾーンでの現在時刻
                var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
                var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser);

                // 削除日が今日だったら、また聞き始めるようにステータスを削除する
                if (nowUserTz.Year == year && nowUserTz.Month == month && nowUserTz.Day == day)
                {
                    var stateEntity = await _conversationStateRepo.GetStatusByUserId(_currentUser.UserId);
                    if (stateEntity != null)
                    {
                        stateEntity.State = AskingState.None;
                        await _conversationStateRepo.UpsertState(stateEntity);
                    }
                }
            }
            else
            {
                // 該当日のタイムカードを更新または追加
                int hour = 0;
                int minute = 0;

                Util.ParseHHMM(eoWTime, out hour, out minute);
                await PunchEoW(year, month, day, hour, minute);
            }
        }
    }
}