using InstallmentReminder.Models;

#if ANDROID || IOS
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
#endif

namespace InstallmentReminder.Services;

public class ReminderService
{
    public async Task<ReminderPermissionStatus> RequestPermissionAsync()
    {
#if ANDROID || IOS
        var notifications = LocalNotificationCenter.Current;
        if (!await notifications.AreNotificationsEnabled())
        {
            await notifications.RequestNotificationPermission(
                new NotificationPermission
                {
                    Android = { RequestPermissionToScheduleExactAlarm = false }
                });
        }

        if (!await notifications.AreNotificationsEnabled())
            return ReminderPermissionStatus.NotificationsDisabled;
#endif

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var context = global::Android.App.Application.Context;
            var alarms = context.GetSystemService(
                global::Android.Content.Context.AlarmService)
                as global::Android.App.AlarmManager;
            if (alarms == null)
                return ReminderPermissionStatus.AlarmServiceUnavailable;
            if (!alarms.CanScheduleExactAlarms())
                return ReminderPermissionStatus.ExactAlarmRequired;
        }
#endif

        await Task.CompletedTask;
        return ReminderPermissionStatus.Granted;
    }

    public void OpenAlarmSettings()
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            return;
        var context = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(
            global::Android.Provider.Settings.ActionRequestScheduleExactAlarm);
        intent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
        intent.AddFlags(global::Android.Content.ActivityFlags.NewTask);
        context.StartActivity(intent);
#endif
    }

    public bool Cancel(Installment installment)
    {
#if ANDROID || IOS
        // Attempt both cancellations even when the first one fails.
        bool reminderCancelled = CancelNotification(installment.Id);
        bool dueCancelled = CancelNotification(-installment.Id);
        return reminderCancelled && dueCancelled;
#else
        return true;
#endif
    }

    public async Task<string> ScheduleAsync(
        Installment installment)
    {
#if ANDROID || IOS
        if (installment.Id <= 0 || installment.IsPaid)
        {
            return "لم تتم جدولة الإشعارات: القسط غير صالح أو تم سداده.";
        }

        // لكل قسط رقمان مختلفان للإشعارات:
        // مثال: القسط 5 -> التذكير 5، وميعاد الدفع -5.
        // نحتفظ برقم التذكير القديم لتوافق الأقساط السابقة.
        string reminderResult = await ScheduleOneNotificationAsync(
            installment.Id,
            $"تذكير بقسط: {installment.Title}",
            $"المبلغ: {installment.Amount:N2} ريال\n" +
            $"الاستحقاق: {installment.DueDate:dd/MM/yyyy HH:mm}",
            installment.ReminderAt,
            installment.Id,
            "التذكير");

        // نحاول جدولة الدفع حتى لو فشلت جدولة التذكير.
        string dueResult = await ScheduleOneNotificationAsync(
            -installment.Id,
            $"حان موعد سداد: {installment.Title}",
            $"مطلوب سداد {installment.Amount:N2} ريال.\n" +
            "سجّل السداد في التطبيق بعد الدفع.",
            installment.DueDate,
            installment.Id,
            "ميعاد الدفع");

        return reminderResult + "\n" + dueResult;
#else
        await Task.CompletedTask;

        return "تم حفظ المواعيد كبيانات. الإشعارات في هذا الكود مفعّلة لأندرويد وآيفون فقط.";
#endif
    }

#if ANDROID || IOS
    private async Task<string> ScheduleOneNotificationAsync(
        int notificationId,
        string title,
        string description,
        DateTime scheduledAt,
        int installmentId,
        string label)
    {
        try
        {
            if (scheduledAt <= DateTime.Now)
            {
                return $"لم تتم جدولة إشعار {label}: الموعد مرّ.";
            }

            var notification = new NotificationRequest
            {
                NotificationId = notificationId,
                Title = title,
                Description = description,
                ReturningData = installmentId.ToString(),

                Schedule =
                {
                    NotifyTime = new DateTimeOffset(
                        DateTime.SpecifyKind(
                            scheduledAt,
                            DateTimeKind.Local))
                }
            };

            bool scheduled =
                await LocalNotificationCenter.Current.Show(notification);

            return scheduled
                ? $"تمت جدولة إشعار {label}: {scheduledAt:dd/MM/yyyy HH:mm}."
                : $"لم تتم جدولة إشعار {label}. راجع أذونات الإشعارات والتذكيرات؛ القسط محفوظ.";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            return $"تعذرت جدولة إشعار {label}؛ القسط محفوظ.";
        }
    }
#endif

#if ANDROID || IOS
    private static bool CancelNotification(int notificationId)
    {
        try
        {
            return Plugin.LocalNotification.LocalNotificationCenter
                .Current.Cancel(notificationId);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            return false;
        }
    }
#endif
}
