using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            await _monthlyTimecardRepo.DeleteTimecardRecord(_currentUser.UserId, Yyyymmdd.FromDate(nowUserTz));
        }

        public async Task<ConversationStateEntity> GetCurrentUserStatus()
        {
            return await _conversationStateRepo.GetStatusByUserId(_currentUser?.UserId ?? string.Empty);
        }

        public async Task<(Yyyymmdd ymd, Hhmm hm)> 
            PunchEoW(ConversationStateEntity stateEntity)
        {
            var hhmm = Hhmm.Parse(stateEntity.TargetTime);
            // 該当日のタイムカードの終業時刻を更新
            var ymd = Yyyymmdd.Parse(stateEntity.TargetDate, _currentUser.TimeZoneId);

            if (ymd.isEmpty || hhmm.IsEmpty)
            {
                Trace.WriteLine($"PunchEoW parse ymd, hhmm failed - {ymd}, {hhmm}");
                return (ymd, hhmm);
            }

            var monthlyTimecardRepo = new MonthlyTimecardRepository();
            await monthlyTimecardRepo.UpsertTimecardRecord(_currentUser.UserId, ymd, hhmm);

            // 打刻済みにして更新
            stateEntity.State = AskingState.Punched;
            await _conversationStateRepo.UpsertState(stateEntity);

            return (ymd, hhmm);
        }

        public async Task<(Yyyymmdd ymd, Hhmm hm)> PunchEoW(string hhmmText)
        {
            var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
            var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser); // ユーザーのタイムゾーンでの現在時刻

            var hhmm = Hhmm.Parse(hhmmText);
            var ymd = Yyyymmdd.FromDate(nowUserTz);

            // パース失敗していたら処理しない
            if (hhmm.IsEmpty || ymd.isEmpty)
            {
                Trace.WriteLine($"PunchEoW parse hhmm failed - {hhmmText}");
                return (ymd, hhmm);
            }

            await PunchEoW(ymd, hhmm);
            return (ymd, hhmm);
        }

        public async Task PunchEoW(Yyyymmdd ymd, Hhmm hm)
        {
            // 指定された日付の終業時刻を、指定された時刻で更新する
            var monthlyTimecardRepo = new MonthlyTimecardRepository();
            await monthlyTimecardRepo.UpsertTimecardRecord(_currentUser.UserId, ymd, hm);

            // 当日ならもう聞かないようにステータスを打刻済みに更新
            var stateEntity = await _conversationStateRepo.GetStatusByUserId(_currentUser.UserId);
            var targetYmd = Yyyymmdd.Parse(stateEntity.TargetDate, _currentUser.TimeZoneId);
            if (stateEntity != null && ymd.Equals(targetYmd))
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

        public async Task<(Yyyymm ym, string csv)> DumpTimecard(string yyyymm)
        {
            var ym = Yyyymm.Parse(yyyymm, _currentUser.TimeZoneId);
            if (ym.IsEmpty)
            {
                Trace.WriteLine($"DumpTimecard parse ym failed - {yyyymm}");
                return (ym, string.Empty);
            }
            var dumped = await DumpTimecard(ym);

            return (ym, dumped);
        }

        public async Task<string> DumpTimecard(Yyyymm ym)
        {
            var records = await _monthlyTimecardRepo.GetTimecardRecordByYearMonth(_currentUser.UserId, ym);

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
            var ymd = Yyyymmdd.Parse(yyyymmdd, _currentUser.TimeZoneId);

            if (ymd.isEmpty)
            {
                Trace.WriteLine($"ModifyTimecard parse ymd failed - {yyyymmdd}");
                return;
            }

            // なしと言われたら該当日のタイムカードを削除
            if (eoWTime.Contains("なし"))
            {
                await _monthlyTimecardRepo.DeleteTimecardRecord(_currentUser.UserId, ymd);

                // ユーザーのタイムゾーンでの現在時刻
                var tzUser = TimeZoneInfo.FindSystemTimeZoneById(_currentUser.TimeZoneId);
                var nowUserTz = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzUser);
                var today = Yyyymmdd.FromDate(nowUserTz);

                // 削除日が今日だったら、また聞き始めるようにステータスを削除する
                if (ymd.Equals(today))
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
                var hhmm = Hhmm.Parse(eoWTime);
                if (hhmm.IsEmpty)
                {
                    Trace.WriteLine($"ModifyTimecard parse hhmm failed - {hhmm}");
                    return;
                }

                await PunchEoW(ymd, hhmm);
            }
        }
    }
}