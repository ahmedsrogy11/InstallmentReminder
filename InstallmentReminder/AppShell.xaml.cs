namespace InstallmentReminder
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(Views.InstallmentsPage), typeof(Views.InstallmentsPage));
        }
    }
}
