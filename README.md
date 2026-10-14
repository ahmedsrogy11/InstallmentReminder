# Installment Reminder

An Android application built with .NET MAUI and C# to manage
installments, attach invoice images, and schedule payment reminders.

Developed as a freelance project with an Arabic right-to-left interface.

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
- `MainPage.xaml` — Installment creation interface.
- `MainPage.xaml.cs` — Validation, image selection, saving, and notifications.
- `InstallmentsPage.xaml` — Saved installments interface.
- `InstallmentsPage.xaml.cs` — Loading records and marking payments as complete.
- `Platforms/Android/` — Android configuration and permissions.
- `MauiProgram.cs` — Application configuration and service registration.

## Getting Started

1. Install the .NET 10 SDK and the .NET MAUI development workload.
2. Clone this repository.
3. Open `InstallmentReminder.slnx` in a compatible version of Visual Studio.
4. Restore NuGet packages.
5. Select an Android emulator or a connected Android device.
6. Build and run the application.
7. Grant notification and alarm permissions when prompted.

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
