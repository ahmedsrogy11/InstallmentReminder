using InstallmentReminder.Models;
using InstallmentReminder.Data;
using Microsoft.Maui.Storage;
using System.Globalization;

#if ANDROID || IOS
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
#endif

namespace InstallmentReminder;

public partial class MainPage : ContentPage
{
    private readonly InstallmentDatabase _database;
    private bool _isSaving;
    private string? _selectedImagePath;

    public MainPage(InstallmentDatabase database)
    {
        InitializeComponent();
        _database = database;

        DueDatePicker.Date = DateTime.Today.AddDays(2);
        DueTimePicker.Time = new TimeSpan(12, 0, 0);

        ReminderDatePicker.Date = DateTime.Today.AddDays(1);
        ReminderTimePicker.Time = new TimeSpan(12, 0, 0);
    }

    private async void OnShowInstallmentsClicked(
        object? sender, EventArgs e)
    {
        await Navigation.PushAsync(
            new InstallmentsPage(_database));
    }

    private async void OnPickImageClicked(
        object? sender, EventArgs e)
    {
        try
        {
            var photo = await FilePicker.Default.PickAsync(
                new PickOptions
                {
                    PickerTitle = "اختر صورة الفاتورة",
                    FileTypes = FilePickerFileType.Images
                });

            if (photo == null)
                return;

            string extension = Path.GetExtension(photo.FileName);
            string fileName = $"{Guid.NewGuid():N}{extension}";

            string folderPath = Path.Combine(
                FileSystem.AppDataDirectory,
                "Invoices");

            Directory.CreateDirectory(folderPath);

            string destinationPath = Path.Combine(
                folderPath,
                fileName);

            using (var source = await photo.OpenReadAsync())
            using (var destination = File.Create(destinationPath))
            {
                await source.CopyToAsync(destination);
            }

            _selectedImagePath = destinationPath;

            InvoiceImage.Source = ImageSource.FromFile(destinationPath);
            InvoiceImage.IsVisible = true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "خطأ",
                "تعذر اختيار الصورة أو حفظها.",
                "موافق");
        }
    }

    private async void OnPreviewClicked(
        object? sender, EventArgs e)
    {
        if (_isSaving)
            return;

        string title = TitleEntry.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(title))
        {
            await DisplayAlertAsync(
                "تنبيه",
                "اكتب اسم القسط.",
                "موافق");

            return;
        }

        bool isValidAmount = decimal.TryParse(
            AmountEntry.Text,
            NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowLeadingWhite |
            NumberStyles.AllowTrailingWhite,
            CultureInfo.InvariantCulture,
            out decimal amount);

        if (!isValidAmount || amount <= 0)
        {
            await DisplayAlertAsync(
                "تنبيه",
                "اكتب مبلغًا أكبر من صفر، مثل 500 أو 500.50.",
                "موافق");

            return;
        }

        if (DueDatePicker.Date is not DateTime dueDate ||
            DueTimePicker.Time is not TimeSpan dueTime ||
            ReminderDatePicker.Date is not DateTime reminderDate ||
            ReminderTimePicker.Time is not TimeSpan reminderTime)
        {
            await DisplayAlertAsync(
                "تنبيه",
                "اختار تاريخ ووقت الاستحقاق والتذكير.",
                "موافق");

            return;
        }

        DateTime dueAt = dueDate.Date + dueTime;
        DateTime reminderAt = reminderDate.Date + reminderTime;

        if (dueAt <= DateTime.Now)
        {
            await DisplayAlertAsync(
                "تنبيه",
                "ميعاد القسط لازم يكون في المستقبل.",
                "موافق");

            return;
        }

        if (reminderAt <= DateTime.Now || reminderAt >= dueAt)
        {
            await DisplayAlertAsync(
                "تنبيه",
                "ميعاد التذكير لازم يكون في المستقبل وقبل ميعاد القسط.",
                "موافق");

            return;
        }

        _isSaving = true;
        SaveButton.IsEnabled = false;

        bool saved = false;

        try
        {
            // نطلب الأذونات قبل حفظ القسط.
            bool permissionsReady =
                await EnsureReminderPermissionsAsync();

            if (!permissionsReady)
                return;

            // قد يكون الوقت مرّ أثناء الموافقة على الأذونات.
            if (reminderAt <= DateTime.Now)
            {
                await DisplayAlertAsync(
                    "تنبيه",
                    "ميعاد التذكير مرّ. اختار ميعادًا جديدًا.",
                    "موافق");

                return;
            }

            var installment = new Installment
            {
                Title = title,
                Amount = amount,
                DueDate = dueAt,
                ReminderAt = reminderAt,
                ImagePath = _selectedImagePath,
                IsPaid = false
            };

            int affectedRows = await _database.SaveAsync(installment);

            if (affectedRows != 1)
            {
                await DisplayAlertAsync(
                    "تنبيه",
                    "لم يتم تأكيد حفظ القسط.",
                    "موافق");

                return;
            }

            saved = true;

            // الحفظ نجح، فنمسح المدخلات.
            TitleEntry.Text = string.Empty;
            AmountEntry.Text = string.Empty;

            _selectedImagePath = null;
            InvoiceImage.Source = null;
            InvoiceImage.IsVisible = false;

            // جدولة إشعار التذكير وإشعار ميعاد الدفع.
            string reminderResult =
                await ScheduleReminderAsync(installment);

            await DisplayAlertAsync(
                "تم الحفظ",
                $"تم حفظ القسط برقم {installment.Id}.\n" +
                reminderResult,
                "موافق");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "خطأ",
                saved
                    ? "القسط محفوظ، لكن تعذر إكمال العملية. راجع قائمة الأقساط قبل إضافته مرة أخرى."
                    : "تعذر إكمال حفظ القسط. راجع قائمة الأقساط قبل إعادة المحاولة.",
                "موافق");
        }
        finally
        {
            _isSaving = false;
            SaveButton.IsEnabled = true;
        }
    }

    private async Task<bool> EnsureReminderPermissionsAsync()
    {
#if ANDROID || IOS
        var notifications = LocalNotificationCenter.Current;

        if (!await notifications.AreNotificationsEnabled())
        {
            await notifications.RequestNotificationPermission(
                new NotificationPermission
                {
                    Android =
                    {
                        RequestPermissionToScheduleExactAlarm = false
                    }
                });
        }

        if (!await notifications.AreNotificationsEnabled())
        {
            await DisplayAlertAsync(
                "الإشعارات غير مفعلة",
                "فعّل إشعارات التطبيق من إعدادات الجهاز، ثم اضغط حفظ مرة أخرى. لم يتم حفظ القسط بعد.",
                "موافق");

            return false;
        }
#endif

#if ANDROID
        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var context = global::Android.App.Application.Context;

            var alarmManager =
                context.GetSystemService(
                    global::Android.Content.Context.AlarmService)
                as global::Android.App.AlarmManager;

            if (alarmManager == null)
            {
                await DisplayAlertAsync(
                    "خطأ",
                    "تعذر الوصول لخدمة التذكيرات على الجهاز.",
                    "موافق");

                return false;
            }

            if (!alarmManager.CanScheduleExactAlarms())
            {
                await DisplayAlertAsync(
                    "السماح بالتذكيرات",
                    "هتفتح إعدادات المنبهات والتذكيرات. فعّل السماح للتطبيق، وبعدها ارجع واضغط حفظ مرة أخرى. لم يتم حفظ القسط بعد.",
                    "فتح الإعدادات");

                var intent = new global::Android.Content.Intent(
                    global::Android.Provider.Settings
                        .ActionRequestScheduleExactAlarm);

                intent.SetData(
                    global::Android.Net.Uri.Parse(
                        $"package:{context.PackageName}"));

                intent.AddFlags(
                    global::Android.Content.ActivityFlags.NewTask);

                context.StartActivity(intent);

                return false;
            }
        }
#endif

        await Task.CompletedTask;
        return true;
    }

    private async Task<string> ScheduleReminderAsync(
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
}