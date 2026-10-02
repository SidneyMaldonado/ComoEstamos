using ComoEstamos.Pages;

namespace ComoEstamos
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(ContaPage.Rota, typeof(ContaPage));
        }
    }
}
