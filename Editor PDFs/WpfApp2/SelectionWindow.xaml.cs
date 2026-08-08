using System.Windows;

namespace WpfApp2
{
    public partial class SelectionWindow : Window
    {
        public SelectionWindow()
        {
            InitializeComponent();
        }

        private void BtnEditar_Click(object sender, RoutedEventArgs e)
        {
            MainWindow editarWindow = new MainWindow();
            editarWindow.Show();
            this.Close();
        }

        private void BtnAst_Click(object sender, RoutedEventArgs e)
        {
            AstGeneratorWindow astWindow = new AstGeneratorWindow();
            astWindow.Show();
            this.Close();
        }
    }
}