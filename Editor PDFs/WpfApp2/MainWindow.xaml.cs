using ImageMagick;
using iText.StyledXmlParser.Jsoup.Nodes;
using Microsoft.Win32;
using PdfSharp.Drawing;
using PdfSharp.Fonts;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;

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
        private bool _isAddingSignature = false;
        private byte[] _firmaAInsertar;
        private const int RenderDpi = 300;
        private double _scale;
        private bool isAddingCerradoPM = false;

        private Stack<EditorElement> _undoStack = new Stack<EditorElement>();
        private Stack<EditorElement> _redoStack = new Stack<EditorElement>();


        private List<MemoryStream> _imageStreams = new List<MemoryStream>();

        private List<BitmapImage> _renderedPages = new List<BitmapImage>();


        public MainWindow()
        {
            InitializeComponent();
            InitializeFontCombos();
            GlobalFontSettings.FontResolver = new CustomFontResolver();
            // En tu MainWindow()
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

        }

        // Constructor que recibe ruta del PDF
        public MainWindow(string pdfPath) : this()
        {
            if (!string.IsNullOrWhiteSpace(pdfPath) && File.Exists(pdfPath))
            {
                OpenPdf(pdfPath);
            }
            else if (!string.IsNullOrWhiteSpace(pdfPath))
            {
                MessageBox.Show($"El archivo no existe:\n{pdfPath}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                double zoom = e.Delta > 0 ? 0.1 : -0.1;
                double newScale = scaleTransform.ScaleX + zoom;

                // Limitar zoom entre 0.2 (20%) y 2.0 (200%)
                if (newScale >= 0.2 && newScale <= 2.0)
                {
                    scaleTransform.ScaleX = newScale;
                    scaleTransform.ScaleY = newScale;
                }

                e.Handled = true;
            }
        }

        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            UndoLastAction();
        }
        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            RedoLastAction();
        }

        private void BtnCerrarOrden_Click(object sender, RoutedEventArgs e)
        {
            isAddingCerradoPM = true;
            Cursor = Cursors.Pen;
            MessageBox.Show("Haz clic en el PDF para insertar el sello 'CERRADO PM SAP'.", "Sello", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void cmbFontFamily_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_activeTextBox != null && cmbFontFamily.SelectedItem != null)
                _activeTextBox.FontFamily = new FontFamily(cmbFontFamily.SelectedItem.ToString());
        }


        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                if (e.Key == Key.Z)
                {
                    UndoLastAction();
                    e.Handled = true;
                }
                else if (e.Key == Key.Y)
                {
                    RedoLastAction();
                    e.Handled = true;
                }
            }
        }


        private void UndoLastAction()
        {
            if (_pageElements[_currentPageIndex].Count > 0)
            {
                var last = _pageElements[_currentPageIndex][_pageElements[_currentPageIndex].Count - 1];
                _pageElements[_currentPageIndex].RemoveAt(_pageElements[_currentPageIndex].Count - 1);

                if (last.UIElement != null && editCanvas.Children.Contains(last.UIElement))
                    editCanvas.Children.Remove(last.UIElement);

                _redoStack.Push(last); // Permite rehacer
            }
        }

        private void RedoLastAction()
        {
            if (_redoStack.Count > 0)
            {
                var el = _redoStack.Pop();
                _pageElements[_currentPageIndex].Add(el);

                if (el.UIElement != null && !editCanvas.Children.Contains(el.UIElement))
                    editCanvas.Children.Add(el.UIElement);

                _undoStack.Push(el);
            }
        }


        private void cmbFontColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_activeTextBox != null && cmbFontColor.SelectedItem != null)
                _activeTextBox.Foreground = GetBrushFromColorName(cmbFontColor.SelectedItem.ToString());
        }


        private void cmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Solo cambia si hay un TextBox en edición y una opción seleccionada
            if (_activeTextBox != null && cmbFontSize.SelectedItem != null)
            {
                _activeTextBox.FontSize = Convert.ToDouble(cmbFontSize.SelectedItem) * _scale;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) { }

        private void InitializeFontCombos()
        {
            foreach (var font in Fonts.SystemFontFamilies)
                cmbFontFamily.Items.Add(font.Source);
            cmbFontFamily.SelectedIndex = 0;
            for (int i = 8; i <= 48; i += 2)
                cmbFontSize.Items.Add(i);
            cmbFontSize.SelectedIndex = 4;
            cmbFontColor.Items.Add("Negro"); cmbFontColor.Items.Add("Rojo");
            cmbFontColor.Items.Add("Azul"); cmbFontColor.Items.Add("Verde");
            cmbFontColor.SelectedIndex = 0;
        }

        #region Botones
        private void BtnOpenPdf_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "PDF|*.pdf" };
            if (dlg.ShowDialog() == true) OpenPdf(dlg.FileName);
        }

        private void BtnAddText_Click(object sender, RoutedEventArgs e)
        {
            _isAddingText = true;
            _isAddingSignature = false;
            textPropertiesPanel.Visibility = Visibility.Visible;
            Cursor = Cursors.Pen;
        }

        private void BtnAddSignature_Click(object sender, RoutedEventArgs e)
        {
            var ventana = new SeleccionarUsuarioWindow();
            if (ventana.ShowDialog() == true && ventana.FirmaObtenida != null)
            {
                _firmaAInsertar = ventana.FirmaObtenida;
                _isAddingSignature = true;
                Cursor = Cursors.Pen;
                MessageBox.Show("Haz clic en el PDF para insertar la firma.", "Firma", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSavePdf_Click(object sender, RoutedEventArgs e)
        {
            if (_pdfDocument == null) return;
            var dlg = new SaveFileDialog { Filter = "PDF|*.pdf", FileName = "PDF_Editado.pdf" };
            if (dlg.ShowDialog() == true)
            {
                SavePdfWithChanges(dlg.FileName);
                MessageBox.Show("PDF guardado correctamente.", "Éxito", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        #endregion

        #region Renderizado
        private void OpenPdf(string filePath)
        {
            // 1. Valida existencia y lectura correcta del PDF
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                MessageBox.Show("No se encontró el archivo PDF.");
                return;
            }

            _pdfBytes = File.ReadAllBytes(filePath);

            if (_pdfBytes == null || _pdfBytes.Length == 0)
            {
                MessageBox.Show("El archivo PDF está vacío o corrupto.");
                return;
            }

            // 2. Abrir documento PDF para edición/guardado (PdfSharp)
            _pdfDocument = PdfReader.Open(new MemoryStream(_pdfBytes), PdfDocumentOpenMode.Import);

            // 3. Limpiar listas y paneles previos
            _renderedPages.Clear();
            _pageElements.Clear();
            editCanvas.Children.Clear();
            cmbPages.Items.Clear();


            // 4. Renderizar TODAS las páginas del PDF a memoria (caché)
            try
            {
                using (var collection = new MagickImageCollection())
                {
                    var settings = new MagickReadSettings { Density = new Density(RenderDpi) };
                    collection.Read(_pdfBytes, settings); // aquí está fallando en el server

                    for (int i = 0; i < collection.Count; i++)
                    {
                        var img = collection[i];
                        var bitmap = new BitmapImage();
                        using (var ms = new MemoryStream())
                        {
                            img.Write(ms, MagickFormat.Png);
                            ms.Position = 0;
                            bitmap.BeginInit();
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }
                        _renderedPages.Add(bitmap);

                        _pageElements.Add(new List<EditorElement>());
                        cmbPages.Items.Add($"Página {i + 1}");
                    }
                }
            }
            catch (ImageMagick.MagickDelegateErrorException ex)
            {
                MessageBox.Show(
                    "Error de Magick.NET al leer/renderizar el PDF:\n\n" +
                    ex.Message,
                    "Error Magick.NET",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error al renderizar el PDF:\n\n" + ex.ToString(),
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }


            // 5. Selecciona la primera página y renderiza
            _currentPageIndex = 0;
            cmbPages.SelectedIndex = 0;
            RenderCurrentPage();
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
            if (_pdfDocument == null || _renderedPages.Count == 0) return;

            var bitmap = _renderedPages[_currentPageIndex];
            editCanvas.Children.Clear();
            pdfPageImage.Source = bitmap;
            pdfPageImage.Width = bitmap.PixelWidth;
            pdfPageImage.Height = bitmap.PixelHeight;
            editCanvas.Width = bitmap.PixelWidth;
            editCanvas.Height = bitmap.PixelHeight;
            editCanvas.Children.Add(pdfPageImage);
            _scale = RenderDpi / 72.0;

            foreach (var el in _pageElements[_currentPageIndex])
                el.Render(editCanvas, _scale, 1.0);

            textPropertiesPanel.Visibility = Visibility.Collapsed;
            _isAddingText = false;
            Cursor = Cursors.Arrow;
        }


        #endregion

        #region Insertar
        private void EditCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var pos = e.GetPosition(editCanvas);

            // 🔍 DEBUG temporal
            System.Diagnostics.Debug.WriteLine($"Click en canvas: X={pos.X}, Y={pos.Y}");
            System.Diagnostics.Debug.WriteLine($"Canvas size: {editCanvas.ActualWidth} x {editCanvas.ActualHeight}");
            System.Diagnostics.Debug.WriteLine($"Scale: {_scale}");

            if (_isAddingText)
            {
                _activeTextBox = new TextBox
                {
                    Width = 200,
                    Height = 30,
                    FontFamily = new FontFamily(cmbFontFamily.SelectedItem.ToString()),
                    // CAMBIO CLAVE: Multiplica el tamaño de fuente por _scale
                    FontSize = Convert.ToDouble(cmbFontSize.SelectedItem) * _scale,
                    Foreground = GetBrushFromColorName(cmbFontColor.SelectedItem.ToString()),
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(1),
                    Text = "Escribe aquí..."
                };

                Canvas.SetLeft(_activeTextBox, pos.X);
                Canvas.SetTop(_activeTextBox, pos.Y);
                editCanvas.Children.Add(_activeTextBox);
                _activeTextBox.Focus();
                _activeTextBox.SelectAll();
                _isAddingText = false;
                Cursor = Cursors.Arrow;
                e.Handled = true;
            }
            else if (isAddingCerradoPM)
            {
                // Fijamos un sello grande y en negrita
                var sello = new TextBlock
                {
                    Text = "CERRADO " +
                    "PM SAP",
                    FontSize = 10 * _scale, // Puedes ajustar el tamaño
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black,
                    Background = Brushes.Transparent
                };
                Canvas.SetLeft(sello, pos.X);
                Canvas.SetTop(sello, pos.Y);
                editCanvas.Children.Add(sello);

                // Agrega también a lista para guardar en PDF (ajusta según tus modelos)
                var el = new TextElement
                {
                    UIElement = sello,
                    Text = sello.Text,
                    X = pos.X / _scale,
                    Y = pos.Y / _scale,
                    FontName = "Arial Black",
                    FontSize = 10, // Tamaño en puntos PDF
                    Color = Colors.Black
                };
                _pageElements[_currentPageIndex].Add(el);

                isAddingCerradoPM = false;
                Cursor = Cursors.Arrow;
                e.Handled = true;
            }

            else if (_isAddingSignature && _firmaAInsertar != null)
            {
                var bitmap = new BitmapImage();
                using (var ms = new MemoryStream(_firmaAInsertar))
                {
                    bitmap.BeginInit(); bitmap.StreamSource = ms;
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit(); bitmap.Freeze();
                }
                var image = new Image { Source = bitmap, Width = bitmap.PixelWidth * 0.5, Height = bitmap.PixelHeight * 0.5 };
                Canvas.SetLeft(image, pos.X - image.Width / 2);
                Canvas.SetTop(image, pos.Y - image.Height / 2);
                editCanvas.Children.Add(image);
                var el = new ImageElement
                {
                    UIElement = image,
                    ImageBytes = _firmaAInsertar,
                    X = (pos.X - image.Width / 2) / _scale,
                    Y = (pos.Y - image.Height / 2) / _scale,
                    Width = image.Width / _scale,
                    Height = image.Height / _scale
                };
                _pageElements[_currentPageIndex].Add(el);
                _isAddingSignature = false;
                _firmaAInsertar = null;
                Cursor = Cursors.Arrow;
                e.Handled = true;
            }
        }

        private void BtnAcceptText_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTextBox == null || _activeTextBox.Parent == null) return;
            var tb = new TextBlock
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
            Canvas.SetLeft(tb, left); Canvas.SetTop(tb, top);
            editCanvas.Children.Add(tb);
            var el = new TextElement
            {
                UIElement = tb,
                Text = _activeTextBox.Text,
                X = left / _scale,
                Y = top / _scale,
                FontName = _activeTextBox.FontFamily.Source,
                FontSize = (float)(_activeTextBox.FontSize / _scale),
                Color = ((SolidColorBrush)_activeTextBox.Foreground).Color
            };
            _pageElements[_currentPageIndex].Add(el);
            _activeTextBox = null;
            textPropertiesPanel.Visibility = Visibility.Collapsed;
        }

        private void BtnCancelText_Click(object sender, RoutedEventArgs e)
        {
            if (_activeTextBox != null && _activeTextBox.Parent != null)
                editCanvas.Children.Remove(_activeTextBox);
            _activeTextBox = null;
            textPropertiesPanel.Visibility = Visibility.Collapsed;
            _isAddingText = false;
            Cursor = Cursors.Arrow;
        }
        #endregion

        #region Guardado



        private void SavePdfWithChanges(string outputPath)
        {
            var doc = new PdfDocument();
            _imageStreams.Clear();
            try
            {
                for (int i = 0; i < _pdfDocument.PageCount; i++)
                {
                    var page = _pdfDocument.Pages[i];
                    var newPage = doc.AddPage(page);
                    using (var gfx = XGraphics.FromPdfPage(newPage))
                    {
                        foreach (var el in _pageElements[i])
                        {
                            if (el is TextElement t)
                            {
                                var brush = new XSolidBrush(XColor.FromArgb(t.Color.A, t.Color.R, t.Color.G, t.Color.B));
                                var font = new XFont(t.FontName ?? "Arial", t.FontSize, XFontStyleEx.Regular);

                                // ✅ CAMBIO: Sin inversión de Y
                                double x = t.X;
                                double y = t.Y;
                                gfx.DrawString(t.Text, font, brush, x, y);
                            }
                            else if (el is ImageElement img)
                            {
                                if (img.ImageBytes != null)
                                {
                                    var ms = new MemoryStream(img.ImageBytes);
                                    _imageStreams.Add(ms);
                                    var ximg = XImage.FromStream(ms);

                                    // ✅ CAMBIO: Sin inversión de Y
                                    double x = img.X;
                                    double y = img.Y;
                                    gfx.DrawImage(ximg, x, y, img.Width, img.Height);
                                }
                            }
                        }
                    }
                }
                doc.Save(outputPath);
            }
            finally
            {
                doc.Close();
                foreach (var ms in _imageStreams) ms.Dispose();
                _imageStreams.Clear();
            }
        }

        #endregion

        private Brush GetBrushFromColorName(string name)
        {
            switch (name)
            {
                case "Rojo": return Brushes.Red;
                case "Azul": return Brushes.Blue;
                case "Verde": return Brushes.Green;
                default: return Brushes.Black;
            }
        }
    }

    #region Elementos
    public abstract class EditorElement
    {
        public UIElement UIElement { get; set; }
        public abstract void Render(Canvas canvas, double scale, double zoom);
    }
    public class TextElement : EditorElement
    {
        public string Text { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public Color Color { get; set; }
        public override void Render(Canvas canvas, double scale, double zoom)
        {
            if (UIElement is TextBlock tb)
            {
                // ✅ Esta línea es clave para que se vea igual que en el PDF
                tb.FontSize = FontSize * scale;
                Canvas.SetLeft(tb, X * scale);
                Canvas.SetTop(tb, Y * scale);
                if (tb.Parent == null) canvas.Children.Add(tb);
            }
        }

    }
    public class ImageElement : EditorElement
    {
        public byte[] ImageBytes { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public override void Render(Canvas canvas, double scale, double zoom)
        {
            if (UIElement is Image img)
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