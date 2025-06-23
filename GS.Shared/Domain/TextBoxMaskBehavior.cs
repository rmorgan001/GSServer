using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;

namespace GS.Shared.Domain
{
    #region Documentation Tags
    /// <summary>
    ///     WPF mask able TextBox class. Just specify the TextBoxMaskBehavior.Mask attached property to a TextBox. 
    ///     It protect your TextBox from unwanted non-numeric symbols and make it easy to modify your numbers.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Class Information:
    ///	    <list type="bullet">
    ///         <item name="authors">Authors: Ruben Hakopian</item>
    ///         <item name="date">February 2009</item>
    ///         <item name="originalURL">http://www.rubenhak.com/?p=8</item>
    ///     </list>
    /// </para>
    /// </remarks>
    #endregion
    public class TextBoxMaskBehaviour
    {
        #region MinimumValue Property

        public static double GetMinimumValue(DependencyObject obj)
        {
            return (double)obj.GetValue(MinimumValueProperty);
        }

        public static void SetMinimumValue(DependencyObject obj, double value)
        {
            obj.SetValue(MinimumValueProperty, value);
        }

        public static readonly DependencyProperty MinimumValueProperty =
            DependencyProperty.RegisterAttached(
                "MinimumValue",
                typeof(double),
                typeof(TextBoxMaskBehaviour),
                new FrameworkPropertyMetadata(double.NaN, MinimumValueChangedCallback)
                );

        private static void MinimumValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var aThis = (d as TextBox);
            ValidateTextBox(aThis);
        }
        #endregion

        #region MaximumValue Property

        public static double GetMaximumValue(DependencyObject obj)
        {
            return (double)obj.GetValue(MaximumValueProperty);
        }

        public static void SetMaximumValue(DependencyObject obj, double value)
        {
            obj.SetValue(MaximumValueProperty, value);
        }

        public static readonly DependencyProperty MaximumValueProperty =
            DependencyProperty.RegisterAttached(
                "MaximumValue",
                typeof(double),
                typeof(TextBoxMaskBehaviour),
                new FrameworkPropertyMetadata(double.NaN, MaximumValueChangedCallback)
                );

        private static void MaximumValueChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var aThis = (d as TextBox);
            ValidateTextBox(aThis);
        }
        #endregion

        #region Mask Property

        public static MaskType GetMask(DependencyObject obj)
        {
            return (MaskType)obj.GetValue(MaskProperty);
        }

        public static void SetMask(DependencyObject obj, MaskType value)
        {
            obj.SetValue(MaskProperty, value);
        }

        public static readonly DependencyProperty MaskProperty =
            DependencyProperty.RegisterAttached(
                "Mask",
                typeof(MaskType),
                typeof(TextBoxMaskBehaviour),
                new FrameworkPropertyMetadata(MaskChangedCallback)
                );

        private static void MaskChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is TextBox box)
            {
                box.PreviewTextInput -= TextBox_PreviewTextInput;
                DataObject.RemovePastingHandler(box, TextBoxPastingEventHandler);
            }

            var aThis = (d as TextBox);
            if (aThis == null)
                return;

            if ((MaskType)e.NewValue != MaskType.Any)
            {
                aThis.PreviewTextInput += TextBox_PreviewTextInput;
                DataObject.AddPastingHandler(aThis, TextBoxPastingEventHandler);
            }

            ValidateTextBox(aThis);
        }

        #endregion

        #region Private Static Methods

        private static void ValidateTextBox(TextBox aThis)
        {
            if (GetMask(aThis) != MaskType.Any)
            {
                aThis.Text = ValidateValue(GetMask(aThis), aThis.Text);
            }
        }

        private static void TextBoxPastingEventHandler(object sender, DataObjectPastingEventArgs e)
        {
            var aThis = (sender as TextBox);
            var clipboard = e.DataObject.GetData(typeof(string)) as string;
            clipboard = ValidateValue(GetMask(aThis), clipboard);
            if (!string.IsNullOrEmpty(clipboard))
            {
                if (aThis != null) aThis.Text = clipboard;
            }
            e.CancelCommand();
            e.Handled = true;
        }

        private static void TextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            var aThis = (sender as TextBox);
            var isValid = IsSymbolValid(GetMask(aThis), e.Text);
            e.Handled = !isValid;
            if (isValid)
            {
                if (aThis != null)
                {
                    var caret = aThis.CaretIndex;
                    var text = aThis.Text;
                    var textInserted = false;
                    var selectionLength = 0;

                    if (aThis.SelectionLength > 0)
                    {
                        text = text.Substring(0, aThis.SelectionStart) +
                               text.Substring(aThis.SelectionStart + aThis.SelectionLength);
                        caret = aThis.SelectionStart;
                    }

                    if (e.Text == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
                    {
                        while (true)
                        {
                            var ind = text.IndexOf(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator, StringComparison.Ordinal);
                            if (ind == -1)
                                break;

                            text = text.Substring(0, ind) + text.Substring(ind + 1);
                            if (caret > ind)
                                caret--;
                        }

                        if (caret == 0)
                        {
                            text = "0" + text;
                            caret++;
                        }
                        else
                        {
                            if (caret == 1 && string.Empty + text[0] == NumberFormatInfo.CurrentInfo.NegativeSign)
                            {
                                text = NumberFormatInfo.CurrentInfo.NegativeSign + "0" + text.Substring(1);
                                caret++;
                            }
                        }

                        if (caret == text.Length)
                        {
                            selectionLength = 1;
                            textInserted = true;
                            text = text + NumberFormatInfo.CurrentInfo.NumberDecimalSeparator + "0";
                            caret++;
                        }
                    }
                    else if (e.Text == NumberFormatInfo.CurrentInfo.NegativeSign)
                    {
                        textInserted = true;
                        if (aThis.Text.Contains(NumberFormatInfo.CurrentInfo.NegativeSign))
                        {
                            text = text.Replace(NumberFormatInfo.CurrentInfo.NegativeSign, string.Empty);
                            if (caret != 0)
                                caret--;
                        }
                        else
                        {
                            text = NumberFormatInfo.CurrentInfo.NegativeSign + aThis.Text;
                            caret++;
                        }
                    }

                    if (!textInserted)
                    {
                        text = text.Substring(0, caret) + e.Text +
                               ((caret < aThis.Text.Length) ? text.Substring(caret) : string.Empty);

                        caret++;
                    }

                    try
                    {
                        var val = Convert.ToDouble(text);
                        var newVal = ValidateLimits(GetMinimumValue(aThis), GetMaximumValue(aThis), val);
                        if (Math.Abs(val - newVal) > 0.0000000000001)
                        {
                            text = newVal.ToString(CultureInfo.InvariantCulture);
                        }
                        else if (val == 0)
                        {
                            if (!text.Contains(NumberFormatInfo.CurrentInfo.NumberDecimalSeparator))
                                text = "0";
                        }
                    }
                    catch
                    {
                        text = "0";
                    }

                    while (text.Length > 1 && text[0] == '0' && string.Empty + text[1] != NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
                    {
                        text = text.Substring(1);
                        if (caret > 0)
                            caret--;
                    }

                    while (text.Length > 2 && string.Empty + text[0] == NumberFormatInfo.CurrentInfo.NegativeSign && text[1] == '0' && string.Empty + text[2] != NumberFormatInfo.CurrentInfo.NumberDecimalSeparator)
                    {
                        text = NumberFormatInfo.CurrentInfo.NegativeSign + text.Substring(2);
                        if (caret > 1)
                            caret--;
                    }

                    if (caret > text.Length)
                        caret = text.Length;

                    aThis.Text = text;
                    aThis.CaretIndex = caret;
                    aThis.SelectionStart = caret;
                    aThis.SelectionLength = selectionLength;
                }

                e.Handled = true;
            }
        }

        private static string ValidateValue(MaskType mask, string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            value = value.Trim();
            switch (mask)
            {
                case MaskType.Integer:
                    var resultI = long.TryParse(value, out _);
                    return resultI ? value : string.Empty;
                case MaskType.Decimal:
                    var resultD = double.TryParse(value, out _);
                        return resultD ? value : string.Empty;
            }
            return value;
        }

        private static double ValidateLimits(double min, double max, double value)
        {
            if (!min.Equals(double.NaN))
            {
                if (value < min)
                    return min;
            }

            if (!max.Equals(double.NaN))
            {
                if (value > max)
                    return max;
            }

            return value;
        }

        private static bool IsSymbolValid(MaskType mask, string str)
        {
            switch (mask)
            {
                case MaskType.Any:
                    return true;

                case MaskType.Integer:
                    if (str == NumberFormatInfo.CurrentInfo.NegativeSign)
                        return true;
                    break;

                case MaskType.Decimal:
                    if (str == NumberFormatInfo.CurrentInfo.NumberDecimalSeparator ||
                        str == NumberFormatInfo.CurrentInfo.NegativeSign)
                        return true;
                    break;
            }

            if (mask.Equals(MaskType.Integer) || mask.Equals(MaskType.Decimal))
            {
                foreach (var ch in str)
                {
                    if (!Char.IsDigit(ch))
                        return false;
                }

                return true;
            }

            return false;
        }

        #endregion
    }

    public enum MaskType
    {
        Any,
        Integer,
        Decimal
    }
}
