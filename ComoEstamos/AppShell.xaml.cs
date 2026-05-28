using ComoEstamos.Pages;

namespace ComoEstamos
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ContasPage), typeof(ContasPage));
        }
    }
}
