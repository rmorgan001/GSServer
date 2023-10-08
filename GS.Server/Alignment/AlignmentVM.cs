/* Copyright(C) 2019-2022 Rob Morgan (robert.morgan.e@gmail.com),
    Phil Crompton (phil@unitysoftware.co.uk)

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */
using GS.Principles;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Shared.Command;
using GS.Utilities.Controls.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Drawing.Geometries;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Application = System.Windows.Application;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using System.Diagnostics.Eventing.Reader;
using System.Windows.Threading;

namespace GS.Server.Alignment
{
    public class AlignmentVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields

        public string TopName => "Align";

        public string BottomName => "Align";

        public int Uid => 10;

        private readonly SkyTelescopeVM _skyTelescopeVM;

        DispatcherTimer _alertTimer;

        #endregion


        private readonly object _alignmentPointsLock = new object();

        //public ObservableCollection<AlignmentPoint> AlignmentPoints { get; } = new ObservableCollection<AlignmentPoint>();

        public ObservableCollection<AlignmentPoint> AlignmentPoints => SkyServer.AlignmentModel.AlignmentPoints;

        private AlignmentPoint _selectedAlignmentPoint;

        public AlignmentPoint SelectedAlignmentPoint
        {
            get => _selectedAlignmentPoint;
            set
            {
                if (_selectedAlignmentPoint == value) return;
                _selectedAlignmentPoint = value;
                // Update for chart
                SelectedAlignmentPointList.Clear();
                SelectedAlignmentPointList.Add(value);

                OnPropertyChanged();
                DeleteSelectedPointCommand.RaiseCanExecuteChanged();
            }
        }

        /// <summary>
        /// Used for highlighting the selected alignment point in the chart
        /// </summary>
        public ObservableCollection<AlignmentPoint> SelectedAlignmentPointList { get; } =
            new ObservableCollection<AlignmentPoint>();

        public bool IsAlignmentOn
        {
            get => AlignmentSettings.IsAlignmentOn;
            set
            {
                AlignmentSettings.IsAlignmentOn = value;
                OnPropertyChanged();
            }
        }

        public string AlertBadge
        {
            get => AlignmentSettings.AlertBadge;
        }

        public bool IsAlertOn
        {
            get => AlignmentSettings.IsAlertOn;
        }

        public double ProximityLimitArcSeconds
        {
            get => AlignmentSettings.ProximityLimit * 3600;
            set
            {
                AlignmentSettings.ProximityLimit = value / 3600;
                OnPropertyChanged();
                AlignmentSettings.Save();
            }

        }

        public AlignmentBehaviourEnum AlignmentBehaviour
        {
            get => AlignmentSettings.AlignmentBehaviour;
            set
            {
                AlignmentSettings.AlignmentBehaviour = value;
                OnPropertyChanged();
                AlignmentSettings.Save();
            }

        }

        public ActivePointsEnum ActivePoints
        {
            get => AlignmentSettings.ActivePoints;
            set
            {
                AlignmentSettings.ActivePoints = value;
                OnPropertyChanged();
                AlignmentSettings.Save();
            }

        }

        public ThreePointAlgorithmEnum ThreePointAlgorithm
        {
            get => AlignmentSettings.ThreePointAlgorithm;
            set
            {
                AlignmentSettings.ThreePointAlgorithm = value;
                OnPropertyChanged();
                AlignmentSettings.Save();
            }

        }

        public IList<int> AlignmentWarningThresholdList { get; }


        public int AlignmentWarningThreshold
        {
            get => AlignmentSettings.AlignmentWarningThreshold;
            set
            {
                AlignmentSettings.AlignmentWarningThreshold = value;
                OnPropertyChanged();
                AlignmentSettings.Save();
            }

        }


        public bool ClearModelOnStartup
        {
            get => AlignmentSettings.ClearModelOnStartup;
            set
            {
                AlignmentSettings.ClearModelOnStartup = value;
                OnPropertyChanged();
            }
        }

        #region Plotting properties ...

        private ScatterSeries<AlignmentPoint> _unsyncedScatterSeries = new ScatterSeries<AlignmentPoint>
        {
            Stroke = new SolidColorPaint(SKColors.LightCoral) { StrokeThickness = 1 },
            Fill = null,
            Values = SkyServer.AlignmentModel.AlignmentPoints,
            Mapping = (alignmentPoint, point) =>
            {
                point.PrimaryValue = alignmentPoint.UnsyncedCartesian.x;
                point.SecondaryValue = -alignmentPoint.UnsyncedCartesian.y;
            },
            GeometrySize = 10,
        };

        private ScatterSeries<AlignmentPoint, SquareGeometry> _syncedScatterSeries =
            new ScatterSeries<AlignmentPoint, SquareGeometry>
            {
                Stroke = new SolidColorPaint(SKColors.LightGreen) { StrokeThickness = 1 },
                Fill = null,
                Values = SkyServer.AlignmentModel.AlignmentPoints,
                Mapping = (alignmentPoint, point) =>
                {
                    point.PrimaryValue = alignmentPoint.SyncedCartesian.x;
                    point.SecondaryValue = -alignmentPoint.SyncedCartesian.y;
                },
                GeometrySize = 10
            };


        public ObservableCollection<ISeries> ChartData { get; } = new ObservableCollection<ISeries>()
        {
            // Current telescope position
            new LineSeries<CartesCoord>
            {
                Stroke = new SolidColorPaint(SKColors.Green) { StrokeThickness = 4 },
                Fill = null,
                Values = SkyServer.AlignmentModel.CurrentPoint,
                Mapping = (coord, point) =>
                {
                    point.PrimaryValue = coord.x ;
                    point.SecondaryValue = -coord.y ;
                },
                GeometrySize = 15,
                GeometryFill = new SolidColorPaint(SKColors.Red.WithAlpha(90)),
                GeometryStroke = new SolidColorPaint(SKColors.Red.WithAlpha(90))
            },
            // Triangle for 3 points synched
            new LineSeries<AlignmentPoint>
            {
                Values = SkyServer.AlignmentModel.ChartTrianglePoints,
                Mapping = (alignmentPoint, point) =>
                {
                    point.PrimaryValue = alignmentPoint.SyncedCartesian.x ;
                    point.SecondaryValue = -alignmentPoint.SyncedCartesian.y ;
                },
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Green) {StrokeThickness = 1},
                LineSmoothness=0,
                GeometrySize=0
            },
            // Triangle for 3 points un-synched
            new LineSeries<AlignmentPoint>
            {
                Values = SkyServer.AlignmentModel.ChartTrianglePoints,
                Mapping = (alignmentPoint, point) =>
                {
                    point.PrimaryValue = alignmentPoint.UnsyncedCartesian.x;
                    point.SecondaryValue = -alignmentPoint.UnsyncedCartesian.y;
                },
                Fill = null,
                Stroke = new SolidColorPaint(SKColors.Red) {StrokeThickness = 1},
                LineSmoothness=0,
                GeometrySize=0
            },
            // Single point highlighted.
            new LineSeries<CartesCoord>
            {
                Stroke = null,
                Fill = null,
                Values = SkyServer.AlignmentModel.ChartNearestPoint,
                Mapping = (alignmentPoint, point) =>
                {
                    point.PrimaryValue = alignmentPoint.x;
                    point.SecondaryValue = -alignmentPoint.y;
                },
                GeometrySize = 30,
                GeometryFill = null,
                GeometryStroke = new SolidColorPaint(SKColors.Green)
            }
            // Additional series added in method CompleteChartSeriesInit
        };

        const double ChartLimit = 12.0E6;

        public List<Axis> ChartXAxes { get; } = new List<Axis>()
        {
            new Axis { IsVisible = false, MinLimit = -ChartLimit, MaxLimit = ChartLimit},
        };

        public List<Axis> ChartYAxes { get; }= new List<Axis>()
        {
            new Axis{IsVisible = false, MinLimit = -ChartLimit*1.2, MaxLimit = ChartLimit*1.2},
        };

        public LiveChartsCore.Measure.Margin ChartMargin { get; set; } = new LiveChartsCore.Measure.Margin(25);
        #endregion

        public AlignmentVM()
        {
            AlignmentWarningThresholdList = new List<int>(Enumerable.Range(1, 10));

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Server,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()
                    .Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = " Loading AlignmentVM"
            };
            MonitorLog.LogToMonitor(monitorItem);

            if (_skyTelescopeVM == null) _skyTelescopeVM = SkyTelescopeVM._skyTelescopeVM;

            BindingOperations.EnableCollectionSynchronization(AlignmentPoints, _alignmentPointsLock);
            SkyServer.AlignmentModel.AlignmentPoints.CollectionChanged += AlignmentPoints_CollectionChanged;
            AlignmentSettings.StaticPropertyChanged += AlignmentSettings_StaticPropertyChanged;

            CompleteChartSeriesInit();

            _alertTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(15)
            };
            _alertTimer.Tick += AlertTimer_Tick;

        }

        private void CompleteChartSeriesInit()
        {
            _unsyncedScatterSeries.DataPointerDown += ScatterSeries_DataPointerDown;
            _syncedScatterSeries.DataPointerDown += ScatterSeries_DataPointerDown;
            ChartData.Add(_unsyncedScatterSeries);
            ChartData.Add(_syncedScatterSeries);
            ChartData.Add(new ScatterSeries<AlignmentPoint>
            {
                Stroke = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 2 },
                Fill = null,
                Values = SelectedAlignmentPointList,
                Mapping = (alignmentPoint, point) =>
                {
                    point.PrimaryValue = alignmentPoint.UnsyncedCartesian.x;
                    point.SecondaryValue = -alignmentPoint.UnsyncedCartesian.y;
                },
                GeometrySize = 14,
            });
            // Synced selected point
            ChartData.Add(new ScatterSeries<AlignmentPoint, SquareGeometry>
            {
                Stroke = new SolidColorPaint(SKColors.LightGray) { StrokeThickness = 2 },
                Fill = null,
                Values = SelectedAlignmentPointList,
                Mapping = (alignmentPoint, point) =>
                {
                    point.PrimaryValue = alignmentPoint.SyncedCartesian.x;
                    point.SecondaryValue = -alignmentPoint.SyncedCartesian.y;
                },
                GeometrySize = 14
            });

        }


        private void AlertTimer_Tick(object sender, EventArgs e)
        {
            SpeakAlert();
        }

        private void SpeakAlert()
        {
            Synthesizer.Beep(BeepType.Default);
            Synthesizer.Speak(Application.Current.Resources["vceAlignmentAlert"].ToString());
        }
        private void AlignmentSettings_StaticPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsAlignmentOn")
            {
                RaisePropertyChanged(e.PropertyName);
            }
            else if(e.PropertyName == "AlertBadge")
            {
                RaisePropertyChanged(e.PropertyName);
            }
            else if (e.PropertyName == "IsAlertOn")
            {
                CancelAlertCommand.RaiseCanExecuteChanged();
                RaisePropertyChanged(e.PropertyName);
                if (AlignmentSettings.IsAlertOn)
                {
                    SpeakAlert();
                    _alertTimer.Start();
                }
                else
                {
                    _alertTimer.Stop();
                }
            }
        }

        private void AlignmentPoints_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                SelectedAlignmentPoint = AlignmentPoints[e.NewStartingIndex + e.NewItems.Count - 1];
            }
            else
            {
                if (!AlignmentPoints.Contains(SelectedAlignmentPoint))
                {
                    SelectedAlignmentPoint = AlignmentPoints.FirstOrDefault();
                }
            }

            ClearAllPointsCommand.RaiseCanExecuteChanged();
            DeleteSelectedPointCommand.RaiseCanExecuteChanged();
        }

        private bool stopDoubleProcessing = false;
        private void ScatterSeries_DataPointerDown(LiveChartsCore.Kernel.Sketches.IChartView chart, IEnumerable<LiveChartsCore.Kernel.ChartPoint<AlignmentPoint, CircleGeometry, LabelGeometry>> points)
        {
            if (points != null)
            {
                stopDoubleProcessing = true;
                AlignmentPoint selectedPoint = points.FirstOrDefault()?.Model;
                if (selectedPoint == SelectedAlignmentPoint)
                {
                    //De-select
                    SelectedAlignmentPoint = null;
                }
                else
                {
                    SelectedAlignmentPoint = selectedPoint;
                }
            }
        }

        private void ScatterSeries_DataPointerDown(LiveChartsCore.Kernel.Sketches.IChartView chart, IEnumerable<LiveChartsCore.Kernel.ChartPoint<AlignmentPoint, SquareGeometry, LabelGeometry>> points)
        {
            if (points != null)
            {
                if (stopDoubleProcessing)
                {
                    stopDoubleProcessing = false;
                    return;
                }
                AlignmentPoint  selectedPoint = points.FirstOrDefault()?.Model;
                if (selectedPoint == SelectedAlignmentPoint)
                {
                    //De-select
                    SelectedAlignmentPoint = null;
                }
                else
                {
                    SelectedAlignmentPoint = selectedPoint;
                }
            }
        }


        #region Commands ...

        private RelayCommand _clearAllPointsCommand;

        public RelayCommand ClearAllPointsCommand
        {
            get
            {
                return _clearAllPointsCommand
                       ?? (_clearAllPointsCommand = new RelayCommand(
                           param => ClearAllAlignmentPoints(),
                           param => AlignmentPoints.Count > 0)
                       );
            }
        }


        private void ClearAllAlignmentPoints()
        {
            TwoButtonMessageDialogVM messageVm = new TwoButtonMessageDialogVM()
            {
                Caption = $"{Application.Current.Resources["aliConfirmClearAllCaption"]}",
                Message = $"{Application.Current.Resources["aliConfirmClearAllMessage"]}",
                ButtonOneCaption = $"{Application.Current.Resources["aliAcceptButtonCaption"]}",
                ButtonTwoCaption = $"{Application.Current.Resources["aliCancelButtonCaption"]}",
                OnButtonOneClicked = () =>
                {
                    SkyServer.AlignmentModel.ClearAlignmentPoints();
                    IsDialogOpen = false;
                },
                OnButtonTwoClicked = () =>
                {
                    IsDialogOpen = false;
                }
            };
            DialogContent = new TwoButtonMessageDialog(messageVm);
            IsDialogOpen = true;

        }

        private RelayCommand _resetProximityLimit;

        public RelayCommand ResetProximityLimit
        {
            get
            {
                return _resetProximityLimit
                       ?? (_resetProximityLimit = new RelayCommand(
                           param =>
                           {
                               ProximityLimitArcSeconds = 1000;
                           })
                       );
            }
        }



        private RelayCommand _deleteSelectedPointCommand;

        public RelayCommand DeleteSelectedPointCommand
        {
            get
            {
                return _deleteSelectedPointCommand
                       ?? (_deleteSelectedPointCommand = new RelayCommand(
                           param => DeleteSelectedPoint(),
                           param => SelectedAlignmentPoint != null)
                       );
            }
        }

        private void DeleteSelectedPoint()
        {
            TwoButtonMessageDialogVM messageVm = new TwoButtonMessageDialogVM()
            {
                Caption = $"{Application.Current.Resources["aliConfirmDeleteCaption"]}",
                Message = $"{Application.Current.Resources["aliConfirmDeleteMessage"]}",
                ButtonOneCaption = $"{Application.Current.Resources["aliAcceptButtonCaption"]}",
                ButtonTwoCaption = $"{Application.Current.Resources["aliCancelButtonCaption"]}",
                OnButtonOneClicked = () =>
                {
                    SkyServer.AlignmentModel.RemoveAlignmentPoint(SelectedAlignmentPoint);
                    IsDialogOpen = false;
                },
                OnButtonTwoClicked = () =>
                {
                    IsDialogOpen = false;
                }
            };
            DialogContent = new TwoButtonMessageDialog(messageVm);
            IsDialogOpen = true;

        }

        private RelayCommand _exportCommand;

        public RelayCommand ExportCommand
        {
            get
            {
                return _exportCommand
                       ?? (_exportCommand = new RelayCommand(
                           param =>
                           {
                               ExportPointModel();
                           },
                           param => AlignmentPoints.Count > 0)
                       );
            }
        }

        private void ExportPointModel()
        {
            var dlg = new SaveFileDialog { Filter = $"{Application.Current.Resources["aliPointModelFileFilter"]}|Test data (*.datarows)|*.datarows" };
            if (dlg.ShowDialog() != true) return;
            if (dlg.FileName.EndsWith(".datarows"))
            {
                // Export test data suitable for unit testing the code
                SkyServer.AlignmentModel.ExportAlignmentPointTestData(dlg.FileName);
            }
            else
            {
                SkyServer.AlignmentModel.SaveAlignmentPoints(dlg.FileName);
            }
        }

        private RelayCommand _importCommand;

        public RelayCommand ImportCommand
        {
            get
            {
                return _importCommand
                       ?? (_importCommand = new RelayCommand(
                           param =>
                           {
                               if (AlignmentPoints.Count > 0)
                               {
                                   ConfirmImport();
                               }
                               else
                               {
                                   ImportPointModel();
                               }
                           })
                       );
            }
        }

        private void ConfirmImport()
        {
            TwoButtonMessageDialogVM messageVm = new TwoButtonMessageDialogVM()
            {
                Caption = $"{Application.Current.Resources["aliConfirmImportCaption"]}",
                Message = $"{Application.Current.Resources["aliConfirmImportMessage"]}",
                ButtonOneCaption = $"{Application.Current.Resources["aliAcceptButtonCaption"]}",
                ButtonTwoCaption = $"{Application.Current.Resources["aliCancelButtonCaption"]}",
                OnButtonOneClicked = () =>
                {
                    ImportPointModel();
                    IsDialogOpen = false;
                },
                OnButtonTwoClicked = () =>
                {
                    IsDialogOpen = false;
                }
            };
            DialogContent = new TwoButtonMessageDialog(messageVm);
            IsDialogOpen = true;
        }



        private void ImportPointModel()
        {
            var fileDialog = new OpenFileDialog()
            {
                FileName = "*.GssPointModel",
                Filter = $"{Application.Current.Resources["aliPointModelFileFilter"]}",
                Multiselect = false,
            };
            string filename = fileDialog.ShowDialog() != true ? null : fileDialog.FileName;
            if (filename != null)
            {
                SkyServer.AlignmentModel.LoadAlignmentPoints(filename);
                SkyServer.AlignmentModel.SaveAlignmentPoints(); // Save to default configuration file.
            }

        }

        private RelayCommand _cancelAlertCommand;

        public RelayCommand CancelAlertCommand
        {
            get
            {
                return _cancelAlertCommand
                       ?? (_cancelAlertCommand = new RelayCommand(
                           param => { AlignmentSettings.IsAlertOn = false; },
                           param => AlignmentSettings.IsAlertOn)
                       );
            }
        }

        #endregion

        #region Dialog 
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

        private ICommand _openDialogCommand;
        public ICommand OpenDialogCommand
        {
            get
            {
                var command = _openDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _openDialogCommand = new RelayCommand(
                    param => OpenDialog("My test message", "My test caption")
                );
            }
        }
        private void OpenDialog(string msg, string caption = null)
        {
            TwoButtonMessageDialogVM messageVm = new TwoButtonMessageDialogVM()
            {
                Caption = caption,
                Message = msg,
                ButtonOneCaption = "Accept",
                ButtonTwoCaption = "Cancel",
                OnButtonOneClicked = () =>
                {
                    IsDialogOpen = false;
                },
                OnButtonTwoClicked = () =>
                {
                    IsDialogOpen = false;
                }
            };
            DialogContent = new TwoButtonMessageDialog(messageVm);
            IsDialogOpen = true;
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
        ~AlignmentVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }

        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                WeakEventManager<AlignmentPointCollection, NotifyCollectionChangedEventArgs>.RemoveHandler(SkyServer.AlignmentModel.AlignmentPoints, "CollectionChanged", AlignmentPoints_CollectionChanged);
                AlignmentSettings.StaticPropertyChanged -= AlignmentSettings_StaticPropertyChanged;
                _unsyncedScatterSeries.DataPointerDown -= ScatterSeries_DataPointerDown;
                _syncedScatterSeries.DataPointerDown -= ScatterSeries_DataPointerDown;
                _skyTelescopeVM?.Dispose();
            }

        }

        #endregion
    }
}