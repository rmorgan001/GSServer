using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ColorPicker
{
    /// <summary>
    /// A simple WPF color picker. The basic idea is to
    /// ise a Color swatch image and then pick out a single
    /// pixel and use that pixels RGB values along with the
    /// Alpha slider to form a SelectedColor.
    /// 
    /// This class borrows an idea or two from the following source(s)
    /// 
    /// AlphaSlider, and Preview box
    /// Based on an article by ShawnVN's Blog
    /// http://weblogs.asp.net/savanness/archive/2006/12/05/colorcomb-yet-another-color-picker-dialog-for-wpf.aspx
    /// 
    /// 1*1 pixel copy
    /// Based on an article by Lee Brimelow 
    /// http://thewpfblog.com/?p=62
    /// </summary>
    /// 
	/// <summary>
	/// Interaktionslogik für "MainControl.xaml"
	/// </summary>
    public partial class ColorPickerControl
    {
        #region Data
        private readonly DrawingAttributes drawingAttributes = new DrawingAttributes();
        private Color selectedColor = Colors.Transparent;
        private Boolean IsMouseDown;
        #endregion

        #region Ctor
        public ColorPickerControl()
        {
            InitializeComponent();
            Loaded += ColorPicker_Loaded;
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// gets or privately sets the selected
        /// Color. When the Color is set 
        /// the CreateAlphaLinearBrush()/UpdateTextBoxes()
        /// and UpdateInk() methods are called
        /// </summary>
        public Color SelectedColor
        {
            get => selectedColor;
            set
            {
                if (selectedColor == value) return;
                selectedColor = value;
                CreateAlphaLinearBrush();
                UpdateTextBoxes();
                UpdateInk();
            }
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Start with a default Color of black
        /// </summary>
        private void ColorPicker_Loaded(object sender, RoutedEventArgs e)
        {
            //SelectedColor = Colors.Black;
        }

        /// <summary>
        /// Creates a new LinearGradientBrush background for the
        /// Alpha area slider. This is based on the current color
        /// </summary>
        private void CreateAlphaLinearBrush()
        {
            var startColor = Color.FromArgb(
                    0,
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B);

            var endColor = Color.FromArgb(
                    255,
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B);

            var alphaBrush =
                new LinearGradientBrush(startColor, endColor,
                    new Point(0, 0), new Point(1, 0));

            AlphaBorder.Background = alphaBrush;
        }


        /// <summary>
        /// apply the new Swatch image based on user requested swatch
        /// </summary>
        private void Swatch_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Image img) ColorImage.Source = img.Source;
        }


        /// <summary>
        /// Simply grab a 1*1 pixel from the current color image, and
        /// use that and copy the new 1*1 image pixels to a byte array and
        /// then construct a Color from that.
        /// </summary>
        private void CanvImage_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsMouseDown)
                return;

            try
            {
                var cb = new CroppedBitmap(ColorImage.Source as BitmapSource ?? throw new InvalidOperationException(),
                    new Int32Rect((int)Mouse.GetPosition(CanvImage).X,
                        (int)Mouse.GetPosition(CanvImage).Y, 1, 1));

                var pixels = new byte[4];

                try
                {
                    cb.CopyPixels(pixels, 4, 0);
                }
                catch (Exception)
                {
                    //Ooops
                }

                //Ok now, so update the mouse cursor position and the SelectedColor
                ellipsePixel.SetValue(Canvas.LeftProperty, Mouse.GetPosition(CanvImage).X - 5);
                ellipsePixel.SetValue(Canvas.TopProperty, Mouse.GetPosition(CanvImage).Y - 5);
                CanvImage.InvalidateVisual();
                SelectedColor = Color.FromArgb((byte)AlphaSlider.Value, pixels[2], pixels[1], pixels[0]);
            }
            catch (Exception)
            {
                //not much we can do
            }
        }

        /// <summary>
        /// Update text box values based on SelectedColor
        /// </summary>
        private void UpdateTextBoxes()
        {
            txtAlpha.Text = SelectedColor.A.ToString();
            txtAlphaHex.Text = SelectedColor.A.ToString("X");
            txtRed.Text = SelectedColor.R.ToString();
            txtRedHex.Text = SelectedColor.R.ToString("X");
            txtGreen.Text = SelectedColor.G.ToString();
            txtGreenHex.Text = SelectedColor.G.ToString("X");
            txtBlue.Text = SelectedColor.B.ToString();
            txtBlueHex.Text = SelectedColor.B.ToString("X");
            txtAll.Text = $"#{txtAlphaHex.Text}{txtRedHex.Text}{txtGreenHex.Text}{txtBlueHex.Text}";
        }

        /// <summary>
        /// Updates Ink stroked based on SelectedColor
        /// </summary>
        private void UpdateInk()
        {
            drawingAttributes.Color = SelectedColor;
            drawingAttributes.StylusTip = StylusTip.Ellipse;
            drawingAttributes.Width = 5;

            // Update DA on previewPresenter
            foreach (var s in previewPresenter.Strokes)
            {
                s.DrawingAttributes = drawingAttributes;
            }
        }

        /// <summary>
        /// Update SelectedColor Aplha based on Slider value
        /// </summary>
        private void AlphaSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            SelectedColor =
                Color.FromArgb(
                    (byte)AlphaSlider.Value,
                    SelectedColor.R,
                    SelectedColor.G,
                    SelectedColor.B);
        }

        /// <summary>
        /// Change IsMouseDown state
        /// </summary>
        private void CanvImage_MouseDown(object sender, MouseButtonEventArgs e)
        {
            IsMouseDown = true;
        }

        /// <summary>
        /// Change IsMouseDown state
        /// </summary>
        private void CanvImage_MouseUp(object sender, MouseButtonEventArgs e)
        {
            IsMouseDown = false;
        }
        #endregion
    }
}