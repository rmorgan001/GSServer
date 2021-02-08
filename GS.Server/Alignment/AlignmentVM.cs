/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com),
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
using MaterialDesignThemes.Wpf;
using NStarAlignment.DataTypes;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace GS.Server.Alignment
{
    public class AlignmentVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields

        public string TopName => "Align";

        public string BottomName => "Align";

        public int Uid => 9;

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
                SelectedAlignmentPoint = AlignmentPoints[e.NewStartingIndex + e.NewItems.Count-1];
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

        public double ProximityLimit
        {
            get => AlignmentSettings.ProximityLimit;
            set
            {
                AlignmentSettings.ProximityLimit = value;
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
                           param => {
                               SkyServer.AlignmentModel.ClearAlignmentPoints();
                           },
                           param => AlignmentPoints.Count > 0)
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
                           param =>
                           {
                               SkyServer.AlignmentModel.RemoveAlignmentPoint(SelectedAlignmentPoint);
                           },
                           param => SelectedAlignmentPoint != null)
                       );
            }
        }
        #endregion

        #region Dialog 

        private string _dialogMsg;
        public string DialogMsg
        {
            get => _dialogMsg;
            set
            {
                if (_dialogMsg == value) return;
                _dialogMsg = value;
                OnPropertyChanged();
            }
        }

        private string _dialogCaption;
        public string DialogCaption
        {
            get => _dialogCaption;
            set
            {
                if (_dialogCaption == value) return;
                _dialogCaption = value;
                OnPropertyChanged();
            }
        }

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
                    param => OpenDialog(null)
                );
            }
        }
        private void OpenDialog(string msg, string caption = null)
        {
            if (msg != null) DialogMsg = msg;
            DialogCaption = caption ?? Application.Current.Resources["msgDialog"].ToString();
            DialogContent = new DialogOK();
            IsDialogOpen = true;

            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Telescope,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod().Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = $"{msg}"
            };
            MonitorLog.LogToMonitor(monitorItem);

        }

        private ICommand _clickOkDialogCommand;
        public ICommand ClickOkDialogCommand
        {
            get
            {
                var command = _clickOkDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickOkDialogCommand = new RelayCommand(
                    param => ClickOkDialog()
                );
            }
        }
        private void ClickOkDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _clickCancelDialogCommand;
        public ICommand ClickCancelDialogCommand
        {
            get
            {
                var command = _clickCancelDialogCommand;
                if (command != null)
                {
                    return command;
                }

                return _clickCancelDialogCommand = new RelayCommand(
                    param => ClickCancelDialog()
                );
            }
        }
        private void ClickCancelDialog()
        {
            IsDialogOpen = false;
        }

        private ICommand _runMessageDialog;
        public ICommand RunMessageDialogCommand
        {
            get
            {
                var dialog = _runMessageDialog;
                if (dialog != null)
                {
                    return dialog;
                }

                return _runMessageDialog = new RelayCommand(
                    param => ExecuteMessageDialog()
                );
            }
        }
        private async void ExecuteMessageDialog()
        {
            //let's set up a little MVVM, cos that's what the cool kids are doing:
            var view = new ErrorMessageDialog
            {
                DataContext = new ErrorMessageDialogVM()
            };

            //show the dialog
            await DialogHost.Show(view, "RootDialog", ClosingMessageEventHandler);
        }
        private void ClosingMessageEventHandler(object sender, DialogClosingEventArgs eventArgs)
        {
            Console.WriteLine(@"You can intercept the closing event, and cancel here.");
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