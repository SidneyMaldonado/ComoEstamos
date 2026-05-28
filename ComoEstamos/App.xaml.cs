using Microsoft.Extensions.DependencyInjection;

namespace ComoEstamos
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell())
            {
                Width = 390,
                Height = 844,
                MinimumWidth = 390,
                MaximumWidth = 390,
            };
        }
    }
}
