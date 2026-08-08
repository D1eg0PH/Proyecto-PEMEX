using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using Ookii.Dialogs.Wpf; // Para VistaFolderBrowserDialog

namespace WpfApp2
{
    public partial class AstGeneratorWindow : Window
    {
        public string FechaActual => DateTime.Now.ToString("dd/MM/yyyy");

        private readonly Dictionary<int, Dictionary<string, float[]>> coordenadas =
            new Dictionary<int, Dictionary<string, float[]>>()
            {
        { 1, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 2, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 3, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 4, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 5, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 6, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 7, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 8, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }},
        { 9, new Dictionary<string, float[]> {
            { "jefe",     new float[] { 95, 76 } },
            { "ejecutor", new float[] { 365, 76 } },
            { "numero1",  new float[] { 360, 600 } },  // PRIMERA vez
            { "numero2",  new float[] { 450, 600 } },  // SEGUNDA vez (ajusta esta posición)
            { "fecha",    new float[] { 470, 690 } }
        }}
        };

        public AstGeneratorWindow()
        {
            InitializeComponent();
            TxtFecha.Text = FechaActual;
        }

        private void BtnGenerar_Click(object sender, RoutedEventArgs e)
        {
            string jefe = TxtJefe.Text.Trim();
            string ejecutor = TxtEjecutor.Text.Trim();
            string numero = TxtNumero.Text.Trim();
            string fecha = FechaActual;

            if (string.IsNullOrEmpty(jefe) || string.IsNullOrEmpty(ejecutor) || string.IsNullOrEmpty(numero))
            {
                MessageBox.Show("Por favor, completa todos los campos.");
                return;
            }

            string plantillasDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plantillas");
            if (!Directory.Exists(plantillasDir))
            {
                MessageBox.Show($"No se encontró la carpeta:\n{plantillasDir}");
                return;
            }

            var archivosFaltantes = new List<string>();
            for (int i = 1; i <= 9; i++)
            {
                string path = Path.Combine(plantillasDir, $"ast{i}.pdf");
                if (!File.Exists(path)) archivosFaltantes.Add($"ast{i}.pdf");
            }

            if (archivosFaltantes.Count > 0)
            {
                MessageBox.Show($"Faltan plantillas:\n{string.Join("\n", archivosFaltantes)}");
                return;
            }

            var folderDialog = new VistaFolderBrowserDialog();
            if (folderDialog.ShowDialog() != true) return;

            string carpetaSalida = folderDialog.SelectedPath;

            try
            {
                for (int i = 1; i <= 9; i++)
                {
                    string plantillaPath = Path.Combine(plantillasDir, $"ast{i}.pdf");
                    string salidaPath = Path.Combine(carpetaSalida, $"AST_{numero}_Plantilla{i}.pdf");

                    using (PdfReader reader = new PdfReader(plantillaPath))
                    using (PdfWriter writer = new PdfWriter(salidaPath))
                    using (PdfDocument pdfDoc = new PdfDocument(reader, writer))
                    using (Document doc = new Document(pdfDoc))
                    {
                        var coords = coordenadas[i];
                        AddText(doc, "Ing. " + jefe, coords["jefe"][0], coords["jefe"][1], true);  // subrayado                      
                        AddText(doc, ejecutor, coords["ejecutor"][0], coords["ejecutor"][1], true);  // subrayado
                        // NÚMERO DOS VECES
                        AddText(doc, numero, coords["numero1"][0], coords["numero1"][1]);
                        AddText(doc, numero, coords["numero2"][0], coords["numero2"][1]);

                        AddText(doc, fecha, coords["fecha"][0], coords["fecha"][1]);
                    }
                }

                MessageBox.Show($"9 documentos AST generados exitosamente en:\n{carpetaSalida}");
            }
            catch (Exception ex)
            {
                string error = $"Error: {ex.GetType().Name}\n" +
                               $"Mensaje: {ex.Message}\n" +
                               (ex.InnerException != null ? $"Inner: {ex.InnerException.Message}" : "");

                MessageBox.Show(error, "Error Detallado", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // MÉTODO CORREGIDO: repite texto en TODAS las páginas
        private void AddText(Document doc, string text, float x, float y, bool subrayar = false)
        {
            PdfDocument pdfDoc = doc.GetPdfDocument();
            int totalPages = pdfDoc.GetNumberOfPages();

            for (int page = 1; page <= totalPages; page++)
            {
                Paragraph p = new Paragraph(text)
                    .SetFontSize(12)
                    .SetFixedPosition(page, x, y, 300);

                // APLICAR SUBRAYADO SOLO SI SE INDICA
                if (subrayar)
                {
                    p.SetUnderline(1, -2); // grosor 1, posición -2 puntos bajo el texto
                }

                doc.Add(p);
            }
        }
    }
}