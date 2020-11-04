using ASCOM.Utilities;
using ColorPicker;
using DarkSkyApi;
using DarkSkyApi.Models;
using GS.Principles;
using GS.Server.SkyTelescope;
using GS.Shared;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using GS.Server.Controls.Dialogs;

namespace GS.Server.Notes
{
    /// <inheritdoc cref="NotesV" />
    /// <summary>
    /// Interaction logic for notes
    /// </summary>
    [ComVisible(false)]
    public sealed partial class NotesV : INotifyPropertyChanged
    {
        //3f69788eb1c931a32cc83059783a60f5
        private static string _fileName;
        private static readonly string _myDocs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string _filePath = Path.Combine(_myDocs, "GSServer\\");
        private readonly Util _util = new Util();
        private const string _newline = "\u2028";
        private static SolidColorBrush _fontForeGroundColor;
        private static SolidColorBrush _fontBackGroundColor;

        public NotesV()
        {
            try
            {
                InitializeComponent();

                //sets the line spacing
                if (rtbEditor.Document.Blocks.FirstBlock is Paragraph p) p.LineHeight = 1;

                if (rtbEditor.Foreground == null)
                {
                    _fontForeGroundColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FFFFFFFF"));
                }
                else
                {
                    _fontForeGroundColor = (SolidColorBrush)rtbEditor.Foreground;
                }
                Paintbrush.Foreground = _fontForeGroundColor;


                if (rtbEditor.Background == null)
                {
                    _fontBackGroundColor = (SolidColorBrush)(new BrushConverter().ConvertFrom("#FF000000"));
                }
                else
                {
                    _fontBackGroundColor = (SolidColorBrush)rtbEditor.Background;
                }
                ColorLens.Background = _fontBackGroundColor;



                if (!string.IsNullOrEmpty(SkyServer.Notes))
                {
                    using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(SkyServer.Notes)))
                    {
                        var range = new TextRange(rtbEditor?.Document.ContentStart, rtbEditor?.Document.ContentEnd);
                        range.Load(ms, DataFormats.Rtf);
                    }
                }

                // cbSkySettings.ItemsSource = AllSkySettings();

                _fileName = _filePath + "Notes.rtf";
                cbFontFamily.ItemsSource = Fonts.SystemFontFamilies.OrderBy(f => f.Source);
                cbFontSize.ItemsSource = new List<double>() { 8, 9, 10, 11, 12, 14, 16, 18, 20, 22, 24, 26, 28, 36, 48, 72 };
                cbFontFamily.SelectedItem = rtbEditor?.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
                cbFontSize.Text = rtbEditor?.Selection.GetPropertyValue(TextElement.FontSizeProperty).ToString();
                rtbEditor?.Focus();
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        #region Notes

        private bool _isDialogOpen;
        public bool IsDialogOpen
        {
            get => _isDialogOpen;
            set
            {
                if (_isDialogOpen == value) return;
                _isDialogOpen = value;
                OnPropertyChanged();
            }
        }

        private object _dialogContent;
        public object DialogContent
        {
            get => _dialogContent;
            set
            {
                if (_dialogContent == value) return;
                _dialogContent = value;
                OnPropertyChanged();
            }
        }

        private string _DialogMsg;
        public string DialogMsg
        {
            get => _DialogMsg;
            set
            {
                if (_DialogMsg == value) return;
                _DialogMsg = value;
                OnPropertyChanged();
            }
        }

        public void OpenDialog1(string msg)
        {
            if (msg == null) return;
            DialogMsg = msg;
            DialogContent = new DialogOK();
            IsDialogOpen = true;
        }

        private async Task DarkSkyTask()
        {
            try
            {
                using (new WaitCursor())
                {
                    var client = new DarkSkyService(Settings.Settings.DarkSkyKey);
                    var exclusionList = new List<Exclude> { Exclude.Minutely, Exclude.Hourly, Exclude.Daily, Exclude.Daily };
                    var task = client.GetWeatherDataAsync(SkySettings.Latitude, SkySettings.Longitude, Unit.Auto, exclusionList);
                    const int timeout = 10000;
                    if (await Task.WhenAny(task, Task.Delay(timeout)) == task)
                    {
                        var result = task.Result;
                        if (result != null)
                        {
                            var unit = (Unit)Enum.Parse(typeof(Unit), $"{result.Flags.Units.Trim().ToUpper()}");

                            var str = $"Dark Sky Weather Service ({client.ApiCallsMade}): Units {unit} {_newline}";
                            str += $"DateTime: {ConvertDSField(result, unit, "Time")} {_newline}";
                            str += $"Temperature: {ConvertDSField(result, unit, "Temperature")} {_newline}";
                            str += $"ApparentTemperature: {ConvertDSField(result, unit, "ApparentTemperature")} {_newline}";
                            str += $"CloudCover: {ConvertDSField(result, unit, "CloudCover")} {_newline}";
                            str += $"DewPoint: {ConvertDSField(result, unit, "DewPoint")} {_newline}";
                            str += $"Humidity: {ConvertDSField(result, unit, "Humidity")} {_newline}";
                            str += $"NearestStormBearing: {ConvertDSField(result, unit, "NearestStormBearing")} {_newline}";
                            str += $"NearestStormDistance: {ConvertDSField(result, unit, "NearestStormDistance")} {_newline}";
                            str += $"Ozone: {ConvertDSField(result, unit, "Ozone")} {_newline}";
                            str += $"PrecipitationType: {ConvertDSField(result, unit, "PrecipitationType")} {_newline}";
                            str += $"PrecipitationIntensity: {ConvertDSField(result, unit, "PrecipitationIntensity")} {_newline}";
                            str += $"PrecipitationProbability: {ConvertDSField(result, unit, "PrecipitationProbability")} {_newline}";
                            str += $"Pressure: {ConvertDSField(result, unit, "Pressure")} {_newline}";
                            str += $"Visibility: {ConvertDSField(result, unit, "Visibility")} {_newline}";
                            str += $"UVIndex: {ConvertDSField(result, unit, "UVIndex")} {_newline}";
                            str += $"WindSpeed: {ConvertDSField(result, unit, "WindSpeed")} {_newline}";
                            str += $"WindGust: {ConvertDSField(result, unit, "WindGust")} {_newline}";
                            str += $"WindBearing: {ConvertDSField(result, unit, "WindBearing")} {_newline}";
                            str += $"Summary: {ConvertDSField(result, unit, "Summary")} {_newline}";
                            if (rtbEditor == null) return;
                            rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                            rtbEditor.CaretPosition?.InsertTextInRun(str);
                            return;
                        }
                        OpenDialog1($"{Application.Current.Resources["notNoData"]}");
                    }
                    else
                    {
                        OpenDialog1($"{Application.Current.Resources["notTimeout"]}");
                    }
                }

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private string ConvertDSField(Forecast forCast, Unit unit, string field)
        {
            string a;
            string b = null;
            switch (field.ToLower())
            {
                case "time":
                    a = $"{Time.UnixTimeStampToDateTime(forCast.Currently.Time.ToUnixTime()) }";
                    break;
                case "temperature":
                    a = $"{forCast.Currently.Temperature}";
                    switch (unit)
                    {
                        case Unit.US:
                            b = "f";
                            break;
                        case Unit.SI:
                        case Unit.CA:
                        case Unit.UK:
                        case Unit.UK2:
                            b = "c";
                            break;
                    }
                    break;
                case "apparenttemperature":
                    a = $"{forCast.Currently.ApparentTemperature}";
                    switch (unit)
                    {
                        case Unit.US:
                            b = "f";
                            break;
                        case Unit.SI:
                        case Unit.CA:
                        case Unit.UK:
                        case Unit.UK2:
                            b = "c";
                            break;
                    }
                    break;
                case "cloudcover":
                    a = $"{forCast.Currently.CloudCover * 100}";
                    b = "%";
                    break;
                case "dewpoint":
                    a = $"{forCast.Currently.DewPoint}";
                    switch (unit)
                    {
                        case Unit.US:
                            b = "f";
                            break;
                        case Unit.SI:
                        case Unit.CA:
                        case Unit.UK:
                        case Unit.UK2:
                            b = "c";
                            break;
                    }
                    break;
                case "humidity":
                    a = $"{forCast.Currently.Humidity * 100}";
                    b = "%";
                    break;
                case "neareststormbearing":
                    a = $"{forCast.Currently.NearestStormBearing}";
                    b = "°";
                    break;
                case "neareststormdistance":
                    a = $"{forCast.Currently.NearestStormDistance}";
                    switch (unit)
                    {
                        case Unit.UK2:
                        case Unit.US:
                            b = "mi";
                            break;
                        case Unit.SI:
                        case Unit.CA:
                        case Unit.UK:
                            b = "k";
                            break;
                    }
                    break;
                case "ozone":
                    a = $"{forCast.Currently.Ozone}";
                    b = "du";
                    break;
                case "precipitationtype":
                    a = $"{forCast.Currently.PrecipitationType}";
                    break;
                case "precipitationintensity":
                    a = $"{forCast.Currently.PrecipitationIntensity}";
                    switch (unit)
                    {
                        case Unit.US:
                            b = "ip/h";
                            break;
                        case Unit.SI:
                        case Unit.CA:
                        case Unit.UK2:
                        case Unit.UK:
                            b = "mm/h";
                            break;
                    }
                    break;
                case "precipitationprobability":
                    a = $"{forCast.Currently.PrecipitationProbability * 100}";
                    b = "%";
                    break;
                case "pressure":
                    a = $"{forCast.Currently.Pressure}";
                    switch (unit)
                    {
                        case Unit.US:
                            b = "mbar";
                            break;
                        case Unit.SI:
                        case Unit.CA:
                        case Unit.UK2:
                        case Unit.UK:
                            b = "hPa";
                            break;
                    }
                    break;
                case "visibility":
                    a = $"{forCast.Currently.Visibility}";
                    switch (unit)
                    {
                        case Unit.UK2:
                        case Unit.US:
                            b = "mi";
                            break;
                        case Unit.SI:
                        case Unit.CA:
                        case Unit.UK:
                            b = "km";
                            break;
                    }
                    break;
                case "uvindex":
                    a = $"{forCast.Currently.UVIndex}";
                    break;
                case "windspeed":
                    a = $"{forCast.Currently.WindSpeed}";
                    switch (unit)
                    {
                        case Unit.UK2:
                        case Unit.US:
                            b = "mph";
                            break;
                        case Unit.CA:
                            b = "km/h";
                            break;
                        case Unit.SI:
                        case Unit.UK:
                            b = "mps";
                            break;
                    }
                    break;
                case "windgust":
                    a = $"{forCast.Currently.WindGust}";
                    switch (unit)
                    {
                        case Unit.UK2:
                        case Unit.US:
                            b = "mph";
                            break;
                        case Unit.CA:
                            b = "km/h";
                            break;
                        case Unit.SI:
                        case Unit.UK:
                            b = "mps";
                            break;
                    }
                    break;
                case "windbearing":
                    a = $"{forCast.Currently.WindBearing}";
                    b = "°";
                    break;
                case "summary":
                    a = $"{forCast.Currently.Summary}";
                    break;
                default:
                    a = "Blank";
                    break;
            }
            return $"{a}{b}";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region top menu bar

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var dlg = new OpenFileDialog { Filter = "Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*" };
                if (dlg.ShowDialog() != true) return;
                using (var stream = File.Open(dlg.FileName, FileMode.Open))
                {
                    var range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                    range.Load(stream, DataFormats.Rtf);
                }
                _fileName = dlg.FileName;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);

                OpenDialog1(ex.Message);
            }
        }

        private void SaveAs_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                var dlg = new SaveFileDialog { Filter = "Rich Text Format (*.rtf)|*.rtf|All files (*.*)|*.*" };
                if (dlg.ShowDialog() != true) return;
                using (var stream = File.Open(dlg.FileName, FileMode.OpenOrCreate))
                {
                    var range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                    range.Save(stream, DataFormats.Rtf);
                }
                _fileName = dlg.FileName;
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private void Save_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                using (var stream = File.Open(_fileName, FileMode.Create))
                {
                    var range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                    range.Save(stream, DataFormats.Rtf);
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private void BtPrint_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new PrintDialog { PageRangeSelection = PageRangeSelection.AllPages, UserPageRangeEnabled = true };
                if (dlg.ShowDialog() == true)
                {
                    //use either one of the below    
                    //dlg.PrintVisual(rtbEditor, "printing as visual");
                    //dlg.PrintDocument((((IDocumentPaginatorSource)rtbEditor.Document).DocumentPaginator), "printing as paginator");

                    using (var ms = new MemoryStream())
                    {
                        var range = new TextRange(rtbEditor?.Document.ContentStart, rtbEditor?.Document.ContentEnd);
                        range.Save(ms, DataFormats.Rtf);

                        var copy = new FlowDocument();
                        range = new TextRange(copy.ContentStart, copy.ContentEnd);
                        range.Load(ms, DataFormats.Rtf);

                        var paginator = ((IDocumentPaginatorSource)copy).DocumentPaginator;
                        paginator = new DocumentPaginatorWrapper(paginator, new Size(96 * 8.0, 96 * 11), new Size(48, 48));
                        dlg.PrintDocument(paginator, "GS Server printing as paginator");

                        //original way but margins too big
                        //dlg.PrintDocument((((IDocumentPaginatorSource)copy).DocumentPaginator), "printing as paginator");
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void TbLeft_OnClick(object sender, RoutedEventArgs e)
        {
            switch (tbLeft.IsChecked)
            {
                case true:
                    tbRight.IsChecked = false;
                    tbCenter.IsChecked = false;
                    //tbLeft.IsChecked = false;
                    break;
            }
        }

        private void TbCenter_OnClick(object sender, RoutedEventArgs e)
        {
            switch (tbCenter.IsChecked)
            {
                case true:
                    tbRight.IsChecked = false;
                    //tbCenter.IsChecked = false;
                    tbLeft.IsChecked = false;
                    break;
            }
        }

        private void TbRight_OnClick(object sender, RoutedEventArgs e)
        {
            switch (tbRight.IsChecked)
            {
                case true:
                    //tbRight.IsChecked = false;
                    tbCenter.IsChecked = false;
                    tbLeft.IsChecked = false;
                    break;
            }
        }

        private void CbFontFamily_DropDownClosed(object sender, EventArgs e)
        {
            rtbEditor?.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, cbFontFamily.SelectedItem);
            rtbEditor?.Focus();
        }

        private void CbFontSize_DropDownClosed(object sender, EventArgs e)
        {
            rtbEditor?.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, cbFontSize.SelectedItem.ToString());
            rtbEditor?.Focus();
        }

        private void BtForground_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var colorDialog = new ColorDialog
                {
                    Owner = Window.GetWindow(this),
                    SelectedColor = _fontForeGroundColor.Color
                };
                var b = colorDialog.ShowDialog();
                if (b == null) return;
                if ((bool)!b) return;
                if (rtbEditor == null) return;
                _fontForeGroundColor = new SolidColorBrush(colorDialog.SelectedColor);
                rtbEditor?.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, _fontForeGroundColor);
                rtbEditor?.Focus();
                Paintbrush.Foreground = _fontForeGroundColor;

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void BtBackground_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var colorDialog = new ColorDialog
                {
                    Owner = Window.GetWindow(this),
                    SelectedColor = _fontBackGroundColor.Color
                };
                var b = colorDialog.ShowDialog();
                if (b == null) return;
                if ((bool)!b) return;
                if (rtbEditor != null)
                {
                    _fontBackGroundColor = new SolidColorBrush(colorDialog.SelectedColor);
                    rtbEditor?.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, _fontBackGroundColor);
                    rtbEditor?.Focus();
                }
                ColorLens.Background = new SolidColorBrush(colorDialog.SelectedColor);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        #endregion

        #region bottom menu bar

        private void BtDate_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notDate"]}: {DateTime.Now:D}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void BtTime_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notTime"]}: {DateTime.Now:T}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void Ra_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notRa"]}: {_util.HoursToHMS(SkyServer.RightAscensionXForm, "h ", ":", "", 2)}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void Dec_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notDec"]}: {_util.DegreesToDMS(SkyServer.DeclinationXForm, "° ", ":", "", 2)}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void BtAlt_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notAlt"]}: {_util.DegreesToDMS(SkyServer.Altitude, "° ", ":", "", 2)}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void BtAz_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notAz"]}: {_util.DegreesToDMS(SkyServer.Azimuth, "° ", ":", "", 2)}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private void BtMoon_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var astro = new ASCOM.Astrometry.AstroUtils.AstroUtils();
                var angle = astro.MoonPhase(JDate.Utc2Jd2(DateTime.Now));
                string phase = null;
                switch (angle)
                {
                    case var n when (n >= -180.0 && n <= -135.0):
                        phase = $"{Application.Current.Resources["notFullMoon"]}";
                        break;
                    case var n when (n >= -135.0 && n <= -90.0):
                        phase = $"{Application.Current.Resources["notWaningGibbous"]}";
                        break;
                    case var n when (n >= -90.0 && n <= -45.0):
                        phase = $"{Application.Current.Resources["notLastQuarter"]}";
                        break;
                    case var n when (n >= -45.0 && n <= 0.0):
                        phase = $"{Application.Current.Resources["notWaningCrescent"]}";
                        break;
                    case var n when (n >= 0.0 && n <= 45.0):
                        phase = $"{Application.Current.Resources["notNewMoon"]}";
                        break;
                    case var n when (n >= 45.0 && n <= 90.0):
                        phase = $"{Application.Current.Resources["notWaxingCrescent"]}";
                        break;
                    case var n when (n >= 90.0 && n <= 135.0):
                        phase = $"{Application.Current.Resources["notFirstQuarter"]}";
                        break;
                    case var n when (n >= 135.0 && n <= 180.0):
                        phase = $"{Application.Current.Resources["notWaxingGibbous"]}";
                        break;
                }

                var str = $" {Application.Current.Resources["notMoon"]}: {phase} ";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private void BtLog_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $"{Application.Current.Resources["notLogEntry"]}: {Application.Current.Resources["notDate"]} {DateTime.Now:D} {Application.Current.Resources["notTime"]} {DateTime.Now:T}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private void BtAlignmentMode_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $"{ Application.Current.Resources["notAlignmentMode"]}: { SkySettings.AlignmentMode}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void BtEqSystem_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $"{ Application.Current.Resources["notCoordinateType"]}: {SkySettings.EquatorialCoordinateType}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void BtObservatory_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $"{ Application.Current.Resources["notObservatory"]} { Application.Current.Resources["notLat"]}: {_util.DegreesToDMS(SkySettings.Latitude, "° ", ":", "", 2)} { Application.Current.Resources["notLong"]}: {_util.DegreesToDMS(SkySettings.Longitude, "° ", ":", "", 2)}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private async void BtWeather_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                await DarkSkyTask();
                // Jerome Cheng jcheng31 https://github.com/jcheng31/DarkSkyApi
                // current conditions url "https://api.darksky.net/forecast/{0}/{1},{2}?units={3}&extend={4}&exclude={5}&lang={6}"
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void BtSidereal_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notSiderealTime"]}: {_util.HoursToHMS(SkyServer.SiderealTime)}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        private void Ha_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var str = $" { Application.Current.Resources["notHa"]}: {_util.HoursToHMS(Coordinate.Ra2Ha12(SkyServer.RightAscensionXForm,SkyServer.SiderealTime))}";
                if (rtbEditor == null) return;
                rtbEditor.CaretPosition = rtbEditor?.CaretPosition.GetPositionAtOffset(0, LogicalDirection.Forward);
                rtbEditor.CaretPosition?.InsertTextInRun(str);
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }

        }

        #endregion

        #region Events

        private void RtbEditor_SelectionChanged(object sender, RoutedEventArgs e)
        {
            try
            {
                var temp = rtbEditor.Selection.GetPropertyValue(TextElement.FontWeightProperty);
                tbBold.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontWeights.Bold));
                temp = rtbEditor.Selection.GetPropertyValue(TextElement.FontStyleProperty);
                tbItalic.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(FontStyles.Italic));
                temp = rtbEditor.Selection.GetPropertyValue(Inline.TextDecorationsProperty);
                tbUnderline.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextDecorations.Underline));

                temp = rtbEditor.Selection.GetPropertyValue(FlowDocument.TextAlignmentProperty);
                tbLeft.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextAlignment.Left));
                tbCenter.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextAlignment.Center));
                tbRight.IsChecked = (temp != DependencyProperty.UnsetValue) && (temp.Equals(TextAlignment.Right));

                temp = rtbEditor.Selection.GetPropertyValue(TextElement.FontFamilyProperty);
                if (temp != null) cbFontFamily.SelectedItem = temp;
                temp = rtbEditor.Selection.GetPropertyValue(TextElement.FontSizeProperty);
                int.TryParse(temp.ToString(), out var siz);
                if (siz > 0) cbFontSize.Text = siz.ToString();
                rtbEditor.SpellCheck.IsEnabled = tbSpell.IsChecked == true;

                var fg = rtbEditor.Selection.GetPropertyValue(TextElement.ForegroundProperty);
                if (fg is SolidColorBrush fgbrush)
                {
                    _fontForeGroundColor = fgbrush;
                    Paintbrush.Foreground = _fontForeGroundColor;
                }

                var bg = rtbEditor.Selection.GetPropertyValue(TextElement.BackgroundProperty);
                if (bg is SolidColorBrush bgbrush)
                {
                    _fontBackGroundColor = bgbrush;
                    ColorLens.Background = _fontBackGroundColor;
                }

                using (var ms = new MemoryStream())
                {
                    var range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                    range.Save(ms, DataFormats.Rtf);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var sr = new StreamReader(ms))
                    {
                        SkyServer.Notes = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private void RtbEditor_OnKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                //var range = new TextRange(rtbEditor?.Selection.Start, rtbEditor?.Selection.End);
                //range.ApplyPropertyValue(TextElement.FontFamilyProperty, cbFontFamily.SelectedItem);

                //var fs = cbFontSize.SelectedItem;
                //if (fs != null) { range.ApplyPropertyValue(TextElement.FontSizeProperty, fs.ToString()); }
                //else { cbFontSize.SelectedItem = rtbEditor?.Selection.GetPropertyValue(TextElement.FontFamilyProperty); }

                rtbEditor?.Selection.ApplyPropertyValue(TextElement.FontFamilyProperty, cbFontFamily.SelectedItem);
                rtbEditor?.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, _fontForeGroundColor);
                rtbEditor?.Selection.ApplyPropertyValue(TextElement.BackgroundProperty, _fontBackGroundColor);
                rtbEditor?.Selection.ApplyPropertyValue(TextElement.FontSizeProperty, cbFontSize.SelectedItem.ToString());

            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                {
                    Datetime = HiResDateTime.UtcNow,
                    Device = MonitorDevice.Server,
                    Category = MonitorCategory.Notes,
                    Type = MonitorType.Error,
                    Method = MethodBase.GetCurrentMethod().Name,
                    Thread = Thread.CurrentThread.ManagedThreadId,
                    Message = $"{ex.Message}, {ex.StackTrace}"
                };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        private void TbSpell_OnClick(object sender, RoutedEventArgs e)
        {
            rtbEditor.SpellCheck.IsEnabled = tbSpell.IsChecked == true;
            rtbEditor?.Focus();
        }

        private void NotesV_OnLoaded(object sender, RoutedEventArgs e)
        {
            rtbEditor?.Focus();
        }

        private void RtbEditor_OnUnloaded(object sender, RoutedEventArgs e)
        {
            try
            {
                using (var ms = new MemoryStream())
                {
                    var range = new TextRange(rtbEditor.Document.ContentStart, rtbEditor.Document.ContentEnd);
                    range.Save(ms, DataFormats.Rtf);
                    ms.Seek(0, SeekOrigin.Begin);
                    using (var sr = new StreamReader(ms))
                    {
                        SkyServer.Notes = sr.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                var monitorItem = new MonitorEntry
                { Datetime = HiResDateTime.UtcNow, Device = MonitorDevice.Server, Category = MonitorCategory.Notes, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod().Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}, {ex.StackTrace}" };
                MonitorLog.LogToMonitor(monitorItem);
                OpenDialog1(ex.Message);
            }
        }

        #endregion

        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~NotesV()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _util?.Dispose();
            }
            // free native resources if there are any.
            //if (nativeResource != IntPtr.Zero)
            //{
            //    Marshal.FreeHGlobal(nativeResource);
            //    nativeResource = IntPtr.Zero;
            //}
        }
        #endregion

    }
}
