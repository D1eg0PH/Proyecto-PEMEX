using System;
using System.Windows;

namespace WpfApp2
{
    public partial class App : Application
    {
        [STAThread]
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainWindow mainWindow;

            // Verificar si se pasó un argumento (ruta del PDF)
            if (e.Args.Length > 0 && !string.IsNullOrWhiteSpace(e.Args[0]))
                mainWindow = new MainWindow(e.Args[0]);
            else
                mainWindow = new MainWindow();

            mainWindow.Show();
        }
    }
}
