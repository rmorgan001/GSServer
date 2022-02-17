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
using GS.Server.Controls.Dialogs;
using GS.Server.Helpers;
using GS.Server.Main;
using GS.Server.SkyTelescope;
using GS.Shared;
using GS.Shared.Command;
using GS.Utilities.Controls.Dialogs;
using MaterialDesignThemes.Wpf;
using NStarAlignment.DataTypes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;

namespace GS.Server.Alignment
{
    public class AlignmentVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields

        public string TopName => "Align";

        public string BottomName => "Align";

        public int Uid => 10;

        private readonly SkyTelescopeVM _skyTelescopeVM;

        #endregion

        public AlignmentVM()
        {
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
                OnPropertyChanged();
                DeleteSelectedPointCommand.RaiseCanExecuteChanged();
            }
        }

        public bool IsAlignmentOn
        {
            get => AlignmentSettings.IsAlignmentOn;
            set
            {
                AlignmentSettings.IsAlignmentOn = value;
                OnPropertyChanged();
            }
        }

        public double ProximityLimitArcSeconds
        {
            get => AlignmentSettings.ProximityLimit * 3600;
            set
            {
                AlignmentSettings.ProximityLimit = value / 3600;
                OnPropertyChanged();
            }

        }

        public double NearbyLimit
        {
            get => AlignmentSettings.NearbyLimit;
            set
            {
                AlignmentSettings.NearbyLimit = value;
                OnPropertyChanged();
            }

        }

        public IList<int> SampleSizeList { get; } = new List<int>(Enumerable.Range(2, 8));

        public int SampleSize
        {
            get => AlignmentSettings.SampleSize;
            set
            {
                AlignmentSettings.SampleSize = value;
                OnPropertyChanged();
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

        private RelayCommand _resetNearbyLimit;

        public RelayCommand ResetNearbyLimit
        {
            get
            {
                return _resetNearbyLimit
                       ?? (_resetNearbyLimit = new RelayCommand(
                           param =>
                           {
                               NearbyLimit = 45.0;
                           })
                       );
            }
        }

        private RelayCommand _resetSampleSize;

        public RelayCommand ResetSampleSize
        {
            get
            {
                return _resetSampleSize
                       ?? (_resetSampleSize = new RelayCommand(
                           param =>
                           {
                               SampleSize = 3;
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
            var dlg = new SaveFileDialog { Filter = $"{Application.Current.Resources["aliPointModelFileFilter"]}" };
            if (dlg.ShowDialog() != true) return;
            SkyServer.AlignmentModel.SaveAlignmentPoints(dlg.FileName);
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

                _skyTelescopeVM?.Dispose();
            }

        }

        #endregion
    }
}