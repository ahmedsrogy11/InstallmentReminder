using InstallmentReminder.Data;
using InstallmentReminder.Models;

namespace InstallmentReminder;

public partial class InstallmentsPage : ContentPage
{
    private readonly InstallmentDatabase _database;
    private bool _isSaving;

    public InstallmentsPage(InstallmentDatabase database)
    {
        InitializeComponent();
        _database = database;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await LoadInstallmentsAsync();
    }

    private async Task LoadInstallmentsAsync()
    {
        try
        {
            var installments = await _database.GetAllAsync();

            InstallmentsList.ItemsSource = installments;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "خطأ",
                "تعذر تحميل الأقساط. ارجع وافتح الصفحة مرة أخرى.",
                "موافق");
        }
    }

    private async void OnMarkPaidClicked(
        object? sender, EventArgs e)
    {
        if (_isSaving)
            return;

        if (sender is not Button button ||
            button.BindingContext is not Installment installment)
        {
            return;
        }

        if (installment.IsPaid)
            return;

        _isSaving = true;

        bool paymentSaved = false;

        try
        {
            bool confirmed = await DisplayAlertAsync(
                "تأكيد السداد",
                $"هل تم سداد قسط «{installment.Title}»؟",
                "نعم، تم السداد",
                "إلغاء");

            if (!confirmed)
                return;

            button.IsEnabled = false;
            installment.IsPaid = true;

            try
            {
                int affectedRows =
                    await _database.SaveAsync(installment);

                if (affectedRows != 1)
                {
                    throw new InvalidOperationException(
                        "لم يتم تحديث القسط.");
                }
            }
            catch
            {
                // نرجع الحالة القديمة لو الحفظ فشل.
                installment.IsPaid = false;
                throw;
            }

            paymentSaved = true;

#if ANDROID || IOS
            // نلغي إشعار التذكير وإشعار ميعاد الدفع.
            // نحاول إلغاء كل واحد حتى لو فشل إلغاء الآخر.
            bool reminderCancelled =
                CancelNotification(installment.Id);

            bool dueCancelled =
                CancelNotification(-installment.Id);

            if (!reminderCancelled || !dueCancelled)
            {
                await DisplayAlertAsync(
                    "تنبيه",
                    "تم حفظ السداد، لكن لم يتم تأكيد إلغاء كل الإشعارات؛ قد يظهر إشعار لهذا القسط.",
                    "موافق");
            }
#endif

            await LoadInstallmentsAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            await DisplayAlertAsync(
                "خطأ",
                paymentSaved
                    ? "تم حفظ السداد، لكن تعذر إكمال العملية. ارجع وافتح قائمة الأقساط مرة أخرى."
                    : "تعذر تسجيل السداد. حاول مرة أخرى.",
                "موافق");
        }
        finally
        {
            _isSaving = false;
            button.IsEnabled = true;
        }
    }

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