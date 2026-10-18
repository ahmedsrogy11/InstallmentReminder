using Microsoft.Maui.Storage;

namespace InstallmentReminder.Services;

public class InvoiceImageService
{
    public async Task<string?> PickAndSaveAsync()
    {
        var photo = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "اختر صورة الفاتورة",
            FileTypes = FilePickerFileType.Images
        });

        if (photo == null)
            return null;

        string folderPath = Path.Combine(FileSystem.AppDataDirectory, "Invoices");
        Directory.CreateDirectory(folderPath);
        string fileName = $"{Guid.NewGuid():N}{Path.GetExtension(photo.FileName)}";
        string destinationPath = Path.Combine(folderPath, fileName);

        using var source = await photo.OpenReadAsync();
        using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination);
        return destinationPath;
    }
}
