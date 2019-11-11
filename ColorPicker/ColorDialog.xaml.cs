using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ColorPicker
{
    /// <summary>
    /// Holds a ColorPicker control, and exposes
    /// the ColorPicker.SelectedColor
    /// </summary>
    public partial class ColorDialog
    {
        #region Ctor
        public ColorDialog()
        {
            InitializeComponent();
        }
        #endregion

        #region Public Properties
        public Color SelectedColor
        {
            get => colorPicker.SelectedColor;
            set => colorPicker.SelectedColor = value;
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Closes the dialog on Enter key pressed
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Close();
            }
        }

        /// <summary>
        /// User is happy with choice
        /// </summary>
        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        /// <summary>
        /// User is not happy with choice
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
        #endregion
    }
}
