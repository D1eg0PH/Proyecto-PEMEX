using System.Windows;
using System.Windows.Controls;

namespace WpfApp2
{
    public partial class SeleccionarUsuarioWindow : Window
    {
        public byte[] FirmaObtenida { get; private set; }
        public Usuario UsuarioSeleccionado { get; private set; }

        public SeleccionarUsuarioWindow()
        {
            InitializeComponent();
            CargarUsuarios();
        }

        private void CargarUsuarios()
        {
            lstUsuarios.ItemsSource = DatabaseManager.ObtenerUsuarios();
        }

        private void BtnAgregarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new AgregarUsuarioWindow();
            if (ventana.ShowDialog() == true)
            {
                CargarUsuarios(); // Recarga la lista
            }
        }

        private void BtnAceptar_Click(object sender, RoutedEventArgs e)
        {
            if (lstUsuarios.SelectedItem is Usuario usuario)
            {
                // VENTANA PARA INGRESAR CONTRASEÑA
                var ventanaContrasena = new IngresarContrasenaWindow();
                if (ventanaContrasena.ShowDialog() == true)
                {
                    byte[] firma = DatabaseManager.ObtenerFirma(usuario.Id, ventanaContrasena.Contrasena);
                    if (firma != null)
                    {
                        UsuarioSeleccionado = usuario;
                        FirmaObtenida = firma; // ← GUARDAMOS LA FIRMA
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Contraseña incorrecta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecciona un usuario.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnEditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Usuario usuario)
            {
                var ventana = new AgregarUsuarioWindow(usuario); // ← Sobrecarga
                if (ventana.ShowDialog() == true)
                {
                    CargarUsuarios();
                }
            }
        }

        private void BtnEliminarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Usuario usuario)
            {
                var resultado = MessageBox.Show(
                    $"¿Eliminar a {usuario.Nombre}?",
                    "Confirmar",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    DatabaseManager.EliminarUsuario(usuario.Id);
                    CargarUsuarios();
                }
            }
        }

        private void BtnCancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}