using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace WpfApp2
{
    public partial class AgregarUsuarioWindow : Window
    {
        private string _firmaPath = null;
        private Usuario _usuarioEditar = null;

        public AgregarUsuarioWindow(Usuario usuario = null)
        {
            InitializeComponent();
            _usuarioEditar = usuario;

            if (usuario != null)
            {
                Title = "Editar Usuario";
                txtNombre.Text = usuario.Nombre;
                txtRol.Text = usuario.Rol;
                lblFirmaPath.Text = "Firma actual cargada";
                _firmaPath = "db"; // Marca que ya hay firma
            }
        }

        private void BtnSeleccionarFirma_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Imágenes PNG|*.png",
                Title = "Seleccionar Firma"
            };

            if (dlg.ShowDialog() == true)
            {
                _firmaPath = dlg.FileName;
                lblFirmaPath.Text = Path.GetFileName(_firmaPath);
                lblFirmaPath.Foreground = System.Windows.Media.Brushes.Black;
            }
        }


        private void BtnGuardar_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNombre.Text) ||
                string.IsNullOrWhiteSpace(txtRol.Text) ||
                string.IsNullOrEmpty(txtContrasena.Password))
            {
                MessageBox.Show("Nombre, rol y contraseña son obligatorios.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            byte[] firmaBytes;
            if (_firmaPath == "db") // Editar: mantener firma actual
            {
                firmaBytes = DatabaseManager.ObtenerFirma(_usuarioEditar.Id, txtContrasena.Password);
                if (firmaBytes == null)
                {
                    MessageBox.Show("Contraseña actual incorrecta.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else if (!string.IsNullOrEmpty(_firmaPath))
            {
                firmaBytes = File.ReadAllBytes(_firmaPath);
            }
            else
            {
                MessageBox.Show("Selecciona una firma.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (_usuarioEditar == null)
            {
                // NUEVO
                DatabaseManager.AgregarUsuario(txtNombre.Text, txtRol.Text, txtContrasena.Password, firmaBytes);
            }
            else
            {
                // EDITAR
                DatabaseManager.ActualizarUsuario(_usuarioEditar.Id, txtNombre.Text, txtRol.Text, txtContrasena.Password, firmaBytes);
            }

            MessageBox.Show(_usuarioEditar == null ? "Usuario agregado." : "Usuario actualizado.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
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
