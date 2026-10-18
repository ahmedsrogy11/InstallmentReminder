using InstallmentReminder.Models;
using InstallmentReminder.Data;
using InstallmentReminder.Services;
using System.Globalization;

namespace InstallmentReminder.Views;

public partial class MainPage : ContentPage
{
    private readonly InstallmentDatabase _database;
    private readonly ReminderService _reminders;
    private readonly InvoiceImageService _invoiceImages;
    private bool _isSaving;
    private string? _selectedImagePath;

    public MainPage(
        InstallmentDatabase database,
        ReminderService reminders,
        InvoiceImageService invoiceImages)
    {
        InitializeComponent();
        _database = database;
        _reminders = reminders;
        _invoiceImages = invoiceImages;

        DueDatePicker.Date = DateTime.Today.AddDays(2);
        DueTimePicker.Time = new TimeSpan(12, 0, 0);

        ReminderDatePicker.Date = DateTime.Today.AddDays(1);
        ReminderTimePicker.Time = new TimeSpan(12, 0, 0);
    }

    private async void OnShowInstallmentsClicked(
        object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(InstallmentsPage));
    }

    private async void OnPickImageClicked(
        object? sender, EventArgs e)
    {
        try
        {
            string? destinationPath = await _invoiceImages.PickAndSaveAsync();
            if (destinationPath == null)
                return;

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

    private async void OnSaveClicked(
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
                await _reminders.ScheduleAsync(installment);

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
        var status = await _reminders.RequestPermissionAsync();
        switch (status)
        {
            case ReminderPermissionStatus.Granted:
                return true;
            case ReminderPermissionStatus.NotificationsDisabled:
                await DisplayAlertAsync(
                    "الإشعارات غير مفعلة",
                    "فعّل إشعارات التطبيق من إعدادات الجهاز، ثم اضغط حفظ مرة أخرى. لم يتم حفظ القسط بعد.",
                    "موافق");
                return false;
            case ReminderPermissionStatus.ExactAlarmRequired:
                await DisplayAlertAsync(
                    "السماح بالتذكيرات",
                    "هتفتح إعدادات المنبهات والتذكيرات. فعّل السماح للتطبيق، وبعدها ارجع واضغط حفظ مرة أخرى. لم يتم حفظ القسط بعد.",
                    "فتح الإعدادات");
                _reminders.OpenAlarmSettings();
                return false;
            default:
                await DisplayAlertAsync(
                    "خطأ", "تعذر الوصول لخدمة التذكيرات على الجهاز.", "موافق");
                return false;
        }
    }
}
