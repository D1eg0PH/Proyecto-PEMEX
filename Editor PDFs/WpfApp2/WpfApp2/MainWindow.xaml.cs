using ImageMagick;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Quality;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;


namespace WpfApp2
{
    public partial class MainWindow : Window
    {
        private PdfDocument _pdfDocument;
        private byte[] _pdfBytes;
        private int _currentPageIndex = 0;
        private List<List<EditorElement>> _pageElements = new List<List<EditorElement>>();
        private TextBox _activeTextBox;
        private bool _isAddingText = false;
        private bool _isAddingImage = false;
        private string _imagePathToInsert;
        private const int RenderDpi = 300; // DPI alto para calidad

        public MainWindow()
        {
            InitializeComponent();
            InitializeFontCombos();
            GlobalFontSettings.FontResolver = new CustomFontResolver();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            editCanvas.SizeChanged += (s, args) => RenderCurrentPage();
        }

        private void InitializeFontCombos()
        {
            foreach (var font in Fonts.SystemFontFamilies)
            {
                cmbFontFamily.Items.Add(font.Source);
            }
            cmbFontFamily.SelectedIndex = 0;

            for (int i = 8; i <= 48; i += 2)
            {
                cmbFontSize.Items.Add(i);
            }
            cmbFontSize.SelectedIndex = 4; // 16

            cmbFontColor.Items.Add("Negro");
            cmbFontColor.Items.Add("Rojo");
            cmbFontColor.Items.Add("Azul");
            cmbFontColor.Items.Add("Verde");
            cmbFontColor.SelectedIndex = 0;
        }

        #region Botones

        private void BtnOpenPdf_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Archivos PDF|*.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                OpenPdf(dlg.FileName);
            }
        }

        private void BtnAddText_Click(object sender, RoutedEventArgs e)
        {
            _isAddingText = true;
            _isAddingImage = false;
            textPropertiesPanel.Visibility = Visibility.Visible;
            Cursor = Cursors.Pen;
        }

        private void BtnAddSignature_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Imágenes|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dlg.ShowDialog() == true)
            {
                _imagePathToInsert = dlg.FileName;
                _isAddingImage = true;
                _isAddingText = false;
                Cursor = Cursors.Hand;
            }
        }

        private void BtnSavePdf_Click(object sender, RoutedEventArgs e)
        {

            if (_pdfDocument == null) return;

            SaveFileDialog dlg = new SaveFileDialog
            {
                Filter = "Archivos PDF|*.pdf",
                FileName = "PDF_Editado.pdf"
            };

            if (dlg.ShowDialog() == true)
            {
                MessageBox.Show($"Elementos a guardar en página actual: {_pageElements[_currentPageIndex].Count}");
                SavePdfWithChanges(dlg.FileName);
                MessageBox.Show("PDF guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        #endregion

        #region Apertura y renderizado

        private void OpenPdf(string filePath)
        {
            try
            {
                _pdfBytes = File.ReadAllBytes(filePath);
                using (var stream = new MemoryStream(_pdfBytes))
                {
                    _pdfDocument = PdfReader.Open(stream, PdfDocumentOpenMode.Import);  // Cambiado a Import
                }
                _pageElements.Clear();

                for (int i = 0; i < _pdfDocument.PageCount; i++)
                {
                    _pageElements.Add(new List<EditorElement>());
                }

                cmbPages.Items.Clear();
                for (int i = 1; i <= _pdfDocument.PageCount; i++)
                {
                    cmbPages.Items.Add(i);
                }
                cmbPages.SelectedIndex = 0;

                RenderCurrentPage();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al abrir PDF: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void CmbPages_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbPages.SelectedIndex >= 0)
            {
                _currentPageIndex = cmbPages.SelectedIndex;
                RenderCurrentPage();
            }
        }


        private void RenderCurrentPage()
        {
            if (_pdfDocument == null || _pdfBytes == null) return;

            var page = _pdfDocument.Pages[_currentPageIndex];

            // Renderizar página a imagen usando Magick.NET
            using (var collection = new MagickImageCollection())
            {
                var settings = new MagickReadSettings { Density = new Density(RenderDpi, DensityUnit.PixelsPerInch) };
                collection.Read(_pdfBytes, settings);

                using (var pageImage = collection[_currentPageIndex])
                {
                    using (var memory = new MemoryStream())
                    {
                        pageImage.Write(memory, MagickFormat.Png);
                        memory.Position = 0;

                        var bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        bitmap.StreamSource = memory;
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.EndInit();
                        bitmap.Freeze();

                        // Actualizar la imagen de fondo (no la agregamos, ya está en el Canvas)
                        pdfPageImage.Source = bitmap;
                        pdfPageImage.Width = bitmap.PixelWidth;
                        pdfPageImage.Height = bitmap.PixelHeight;
                    }
                }
            }

            double scale = RenderDpi / 72.0; // Points son 72 DPI
            editCanvas.Width = page.Width.Point * scale;
            editCanvas.Height = page.Height.Point * scale;

            Canvas.SetLeft(pdfPageImage, 0);
            Canvas.SetTop(pdfPageImage, 0);

            // Limpiar solo los elementos de edición (overlays), manteniendo la imagen de fondo (índice 0)
            if (editCanvas.Children.Count > 1)
            {
                editCanvas.Children.RemoveRange(1, editCanvas.Children.Count - 1);
            }

            // Renderizar elementos editados de la página actual
            foreach (var element in _pageElements[_currentPageIndex])
            {
                element.Render(editCanvas, scale);
            }
        }
        #endregion

        #region Inserción de elementos

        private void EditCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_pdfDocument == null) return;

            var position = e.GetPosition(pdfPageImage);
            double scale = RenderDpi / 72.0;

            if (_isAddingText)
            {
                if (_activeTextBox != null) return; // Evitar agregar múltiples si ya hay uno activo

                _activeTextBox = new TextBox
                {
                    Width = 200,
                    Height = 30,
                    FontFamily = new FontFamily(cmbFontFamily.SelectedItem?.ToString() ?? "Arial"),
                    FontSize = Convert.ToDouble(cmbFontSize.SelectedItem),
                    Foreground = GetBrushFromColorName(cmbFontColor.SelectedItem?.ToString()),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    AcceptsReturn = false
                };

                Canvas.SetLeft(_activeTextBox, position.X);
                Canvas.SetTop(_activeTextBox, position.Y);
                editCanvas.Children.Add(_activeTextBox);
                _activeTextBox.Focus();
                _activeTextBox.SelectAll();

                // NO ocultar panel aquí: deja que el usuario edite y clickee Aceptar/Cancelar
                // _isAddingText permanece true para bloquear más clicks hasta confirmar
            }
            else if (_isAddingImage && !string.IsNullOrEmpty(_imagePathToInsert))
            {
                // Código de imagen sin cambios...
            }
        }
        private void BtnAcceptText_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTextBox != null && _activeTextBox.Parent != null)
            {
                var textBlock = new TextBlock
                {
                    Text = _activeTextBox.Text,
                    FontFamily = _activeTextBox.FontFamily,
                    FontSize = _activeTextBox.FontSize,
                    Foreground = _activeTextBox.Foreground,
                    Background = Brushes.Transparent
                };

                double left = Canvas.GetLeft(_activeTextBox);
                double top = Canvas.GetTop(_activeTextBox);

                editCanvas.Children.Remove(_activeTextBox);
                Canvas.SetLeft(textBlock, left);
                Canvas.SetTop(textBlock, top);
                editCanvas.Children.Add(textBlock);

                double scale = RenderDpi / 72.0;

                var textElement = new TextElement
                {
                    UIElement = textBlock,
                    Text = _activeTextBox.Text,
                    X = left / scale,
                    Y = top / scale,
                    FontName = _activeTextBox.FontFamily.Source,
                    FontSize = (float)_activeTextBox.FontSize,
                    Color = ((SolidColorBrush)_activeTextBox.Foreground).Color
                };

                _pageElements[_currentPageIndex].Add(textElement);
                _activeTextBox = null;
            }

            textPropertiesPanel.Visibility = Visibility.Collapsed;
            _isAddingText = false;  // Reset para permitir agregar más
            Cursor = Cursors.Arrow;
        }
        private void BtnCancelText_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTextBox != null && _activeTextBox.Parent != null)
            {
                editCanvas.Children.Remove(_activeTextBox);
                _activeTextBox = null;
            }

            textPropertiesPanel.Visibility = Visibility.Collapsed;
            _isAddingText = false;  // Reset
            Cursor = Cursors.Arrow;
        }

        #endregion

        #region Guardado

        private void SavePdfWithChanges(string outputPath)
        {
            var outputDoc = new PdfDocument();

            for (int i = 0; i < _pdfDocument.PageCount; i++)
            {
                var page = _pdfDocument.Pages[i];
                var newPage = outputDoc.AddPage(page);

                using (var gfx = XGraphics.FromPdfPage(newPage))
                {
                    var elements = _pageElements[i];

                    foreach (var el in elements)
                    {
                        if (el is TextElement textEl)
                        {
                            var xColor = XColor.FromArgb(textEl.Color.A, textEl.Color.R, textEl.Color.G, textEl.Color.B);
                            var xBrush = new XSolidBrush(xColor);
                            var xFont = new XFont(textEl.FontName, textEl.FontSize, XFontStyleEx.Regular);

                            gfx.DrawString(textEl.Text, xFont, xBrush, textEl.X, textEl.Y + textEl.FontSize); // Ajuste para baseline
                        }
                        else if (el is ImageElement imgEl)
                        {
                            using (var xImage = XImage.FromFile(imgEl.ImagePath))
                            {
                                gfx.DrawImage(xImage, imgEl.X, imgEl.Y, imgEl.Width, imgEl.Height);
                            }
                        }
                    }
                }
            }

            outputDoc.Save(outputPath);
            outputDoc.Close();
        }

        #endregion

        #region Utilidades

        private System.Windows.Media.Brush GetBrushFromColorName(string colorName)
        {
            switch (colorName)
            {
                case "Rojo":
                    return Brushes.Red;
                case "Azul":
                    return Brushes.Blue;
                case "Verde":
                    return Brushes.Green;
                default:
                    return Brushes.Black;
            }
        }

        #endregion
    }

    #region Clases de elementos

    public abstract class EditorElement
    {
        public UIElement UIElement { get; set; }
        public abstract void Render(Canvas canvas, double scale);
    }


    

    public class TextElement : EditorElement
    {
        public string Text { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public System.Windows.Media.Color Color { get; set; }

        public override void Render(Canvas canvas, double scale)
        {
            if (UIElement is TextBlock tb)
            {
                Canvas.SetLeft(tb, X * scale);
                Canvas.SetTop(tb, Y * scale);
                if (tb.Parent == null) canvas.Children.Add(tb);
            }
        }
    }

    public class ImageElement : EditorElement
    {
        public string ImagePath { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public override void Render(Canvas canvas, double scale)
        {
            if (UIElement is System.Windows.Controls.Image img)
            {
                img.Width = Width * scale;
                img.Height = Height * scale;
                Canvas.SetLeft(img, X * scale);
                Canvas.SetTop(img, Y * scale);
                if (img.Parent == null) canvas.Children.Add(img);
            }
        }
    }

    #endregion


}

