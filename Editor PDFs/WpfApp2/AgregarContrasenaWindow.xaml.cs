using System.Windows;

namespace WpfApp2
{
    public partial class IngresarContrasenaWindow : Window
    {
        public string Contrasena => txtContrasena.Password;

        public IngresarContrasenaWindow()
        {
            InitializeComponent();
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(txtContrasena.Password))
            {
                MessageBox.Show("Ingresa la contraseña.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DialogResult = true;
            Close();
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}