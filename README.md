# Installment Reminder

An Android application built with .NET MAUI and C# to manage
installments, attach invoice images, and schedule payment reminders.

Developed as a freelance project with an Arabic right-to-left interface.

## Screenshots

### Create an installment
<img width="691" height="1536" alt="1" src="https://github.com/user-attachments/assets/4a3c7394-9df9-4a6a-adf9-362df254032e" />

### Save and schedule notifications
<img width="691" height="1536" alt="2" src="https://github.com/user-attachments/assets/8ba21fd5-0230-4016-ade9-688e2205fc86" />

### View unpaid installments
<img width="691" height="1536" alt="3" src="https://github.com/user-attachments/assets/35ad85a6-8939-418e-877b-ab4e736103c9" />

### Confirm payment
<img width="691" height="1536" alt="4" src="https://github.com/user-attachments/assets/be9fcfe4-2d1e-40bc-9414-2440a5859b0d" />

### Paid installment
<img width="691" height="1536" alt="5" src="https://github.com/user-attachments/assets/95e21e6e-ca7c-43e9-b62d-36902eeb5529" />

## Features

- Create installments with a title, amount, and payment due date.
- Set a separate reminder date and time.
- Attach an invoice image using the device file picker.
- Store installment records locally using SQLite.
- Save invoice images in the application's local storage.
- View saved installments and their payment status.
- Schedule separate notifications for reminders and payment due dates.
- Mark installments as paid and cancel their scheduled notifications.
- Validate amounts, due dates, and reminder times.
- Handle Android notification and exact-alarm permissions.

## Technology Stack

- C#
- .NET 10
- .NET MAUI
- XAML
- SQLite using sqlite-net-pcl
- Plugin.LocalNotification
- Dependency Injection
- Async/Await

## Project Structure

- `Models/` — Installment data model.
- `Data/` — SQLite database access.
- `Views/MainPage.xaml` — Installment creation interface.
- `Views/MainPage.xaml.cs` — Form validation, user feedback, and save workflow.
- `Views/InstallmentsPage.xaml` — Saved installments interface.
- `Views/InstallmentsPage.xaml.cs` — Loading records and marking payments as complete.
- `Services/ReminderService.cs` — Notification permissions, scheduling, and cancellation.
- `Services/ReminderPermissionStatus.cs` — Permission results displayed by the page.
- `Services/InvoiceImageService.cs` — Invoice image selection and local file storage.
- `AppShell.xaml` / `AppShell.xaml.cs` — Home page and navigation routes.
- `Platforms/Android/` — Android configuration and permissions.
- `MauiProgram.cs` — Application configuration and service registration.

Pages receive the database and services through constructor injection. The database
and services are shared instances; pages are transient. Shell resolves the saved
installments page through its registered route. Services do not reference pages;
permission results are returned to the UI so it can show the appropriate message.

SQLite remains in `installments.db3`, images remain in `Invoices/`, and notification
IDs remain `Id` for reminders and `-Id` for payment dates, preserving existing data.

## Getting Started

1. Install the .NET 10 SDK and the .NET MAUI development workload.
2. Clone this repository.
3. Open `InstallmentReminder.slnx` in a compatible version of Visual Studio.
4. Restore NuGet packages.
5. Select an Android emulator or a connected Android device.
6. Build and run the application.
7. Grant notification and alarm permissions when prompted.

To build the Android target from the command line:

```powershell
dotnet build InstallmentReminder/InstallmentReminder.csproj -f net10.0-android
```

## Manual Verification

- Launch the app and verify the Arabic form and date/time defaults.
- Try an empty title, invalid amount, and past reminder; verify validation messages.
- Cancel image selection, then select an image and verify its preview.
- Save a future installment, granting notification/alarm permissions as needed.
- Open saved installments, return, and reopen to check navigation and persistence.
- Mark an installment paid; verify its status and cancellation of both notifications.
- Restart the app and confirm existing installments and invoice images still load.

## Notification Workflow

When a new installment is saved, the app schedules two notifications:

1. A reminder at the selected reminder time.
2. A payment notification at the installment's due time.

Marking the installment as paid cancels both notifications.

Notification delivery depends on the device's permissions and system settings.

## Data Storage

Installment records and invoice images are stored locally on the device.
The app does not currently provide cloud synchronization or user accounts.

## Platform Status

Android is the primary tested platform.
The project also includes other .NET MAUI target configurations.

## Author

Ahmed El Srogy

GitHub: https://github.com/ahmedsrogy11
