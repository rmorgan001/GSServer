/* Copyright(C) 2019-2025 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Shared;
using GS.Shared.Command;
using GS.Utilities.Controls.Dialogs;
using NINA.Model.MyFocuser;
using NINA.Utility;
using NINA.ViewModel.Equipment.Focuser;
using System;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace GS.Server.Focuser
{
    public sealed class FocuserVM : ObservableObject, IPageVM, IDisposable
    {
        #region Fields ...
        public string TopName => "Focuser";
        public string BottomName => "Focuser";
        public int Uid => 11;

        readonly DispatcherTimer _focuserTimer;

        public static FocuserVM _focuserVM;
        #endregion

        #region Properties ...

        private bool _connected;
        public bool Connected
        {
            get => _connected;
            set
            {
                _connected = value;
                OnPropertyChanged();
            }
        }

        private int _position;
        public int Position
        {
            get => _position;
            set
            {
                _position = value;
                OnPropertyChanged();
            }
        }

        private bool _isMoving;
        public bool IsMoving
        {
            get => _isMoving;
            set
            {
                _isMoving = value;
                OnPropertyChanged();
            }
        }

        public bool ShowConnect
        {
            get
            {
                return FocuserChooserVM.SelectedDevice != null
                    && (FocuserChooserVM.SelectedDevice is IFocuser)
                    && (this.Focuser == null);
            }
        }

        public bool ShowDisconnect
        {
            get
            {
                return (Focuser?.Connected == true);
            }
        }

        public bool ShowSetup
        {
            get
            {
                return (FocuserChooserVM.SelectedDevice != null
                    && FocuserChooserVM.SelectedDevice is IFocuser);
            }
        }


        public int StepSize
        {
            get => Properties.Focuser.Default.StepSize;
            set
            {
                Properties.Focuser.Default.StepSize = value;
                OnPropertyChanged();
            }
        }

        public bool ReverseDirection
        {
            get => Properties.Focuser.Default.ReverseDirection;
            set
            {
                Properties.Focuser.Default.ReverseDirection = value;
                OnPropertyChanged();
            }
        }
        #endregion


        private FocuserChooserVM _focuserChooserVM;

        public FocuserChooserVM FocuserChooserVM
        {
            get
            {
                if (_focuserChooserVM == null)
                {
                    _focuserChooserVM = new FocuserChooserVM();
                    _focuserChooserVM.PropertyChanged += FocuserChooserVM_PropertyChanged;
                }
                return _focuserChooserVM;
            }
            set
            {
                _focuserChooserVM = value;
            }
        }

        private void FocuserChooserVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedDevice")
            {
                RefreshButtonVisibilities();
                Properties.Focuser.Default.DeviceId = FocuserChooserVM.SelectedDevice?.Id ?? "";
            }
        }

        private void RefreshButtonVisibilities()
        {
            OnPropertyChanged("ShowSetup");
            OnPropertyChanged("ShowConnect");
            OnPropertyChanged("ShowDisconnect");
        }

        public FocuserVM()
        {
            var monitorItem = new MonitorEntry
            {
                Datetime = HiResDateTime.UtcNow,
                Device = MonitorDevice.Ui,
                Category = MonitorCategory.Interface,
                Type = MonitorType.Information,
                Method = MethodBase.GetCurrentMethod()
                    .Name,
                Thread = Thread.CurrentThread.ManagedThreadId,
                Message = " Loading FocuserVM"
            };
            MonitorLog.LogToMonitor(monitorItem);

            _focuserVM = this;

            FocuserSettings.Load();

            FocuserChooserVM.GetEquipment(Properties.Focuser.Default.DeviceId);

            ChooseFocuserCommand = new AsyncCommand<bool>(() => ChooseFocuser());
            CancelChooseFocuserCommand = new RelayCommand(CancelChooseFocuser);
            DisconnectCommand = new RelayCommand(DisconnectDiag);
            RefreshFocuserListCommand = new RelayCommand(RefreshFocuserList, o => !(Focuser?.Connected == true));
            MoveFocuserInCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal((ReverseDirection ? 1: -1) * StepSize), (p) => Connected && !IsMoving);
            MoveFocuserOutCommand = new AsyncCommand<int>(() => MoveFocuserRelativeInternal((ReverseDirection ? -1 : 1)* StepSize), (p) => Connected && !IsMoving);

            _focuserTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(Properties.Focuser.Default.DevicePollingInterval) };
            _focuserTimer.Tick += FocuserTimer_Tick;
        }

        private void FocuserTimer_Tick(object sender, EventArgs e)
        {
            if (_focuser != null && _focuser.Connected)
            {
                UpdateFocuserValues();
            }
        }

        private void HaltFocuser()
        {
            var monitorItem = new MonitorEntry
            { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Driver, Type = MonitorType.Information, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"Halting focuser" };
            MonitorLog.LogToMonitor(monitorItem);

            if (Focuser?.Connected == true)
            {
                try
                {
                    Focuser.Halt();
                }
                catch (Exception ex)
                {
                    monitorItem = new MonitorEntry
                    { Datetime = Principles.HiResDateTime.UtcNow, Device = MonitorDevice.Focuser, Category = MonitorCategory.Driver, Type = MonitorType.Error, Method = MethodBase.GetCurrentMethod()?.Name, Thread = Thread.CurrentThread.ManagedThreadId, Message = $"{ex.Message}" };
                    MonitorLog.LogToMonitor(monitorItem);
                }
            }
        }

        private CancellationTokenSource _cancelMove;

        private Task<int> MoveFocuserRelativeInternal(int position)
        {
            _cancelMove?.Dispose();
            _cancelMove = new CancellationTokenSource();
            return MoveFocuserRelative(position, _cancelMove.Token);
        }

        public async Task<int> MoveFocuser(int position, CancellationToken ct)
        {
            int pos = -1;

            await Task.Run(async () =>
            {
                try
                {
                    using (ct.Register(() => HaltFocuser()))
                    {
                        var monitorItem = new MonitorEntry
                        {
                            Datetime = Principles.HiResDateTime.UtcNow,
                            Device = MonitorDevice.Focuser,
                            Category = MonitorCategory.Driver,
                            Type = MonitorType.Information,
                            Method = MethodBase.GetCurrentMethod()?.Name,
                            Thread = Thread.CurrentThread.ManagedThreadId,
                            Message = $"Moving Focuser to position { position }"
                        };
                        MonitorLog.LogToMonitor(monitorItem);
                        while (Focuser.Position != position)
                        {
                            this.IsMoving = true;
                            ct.ThrowIfCancellationRequested();
                            await Focuser.Move(position, ct);
                        }

                        this.Position = Focuser?.Position ?? 0;
                        pos = this.Position;
                    }
                }
                catch (OperationCanceledException)
                {
                }
                finally
                {
                    IsMoving = false;
                    // progress.Report(new FocuserStatus() { Status = string.Empty });
                }
            });
            return pos;
        }

        public async Task<int> MoveFocuserRelative(int offset, CancellationToken ct)
        {
            int pos = -1;
            if (Focuser?.Connected == true)
            {
                pos = this.Position + offset;
                pos = await MoveFocuser(pos, ct);
            }
            return pos;
        }

        private CancellationTokenSource _cancelChooseFocuserSource;

        private readonly SemaphoreSlim ss = new SemaphoreSlim(1, 1);

        private async Task<bool> ChooseFocuser()
        {
            await ss.WaitAsync();
            try
            {
                _focuserTimer.Stop();
                Disconnect();
                if (FocuserChooserVM.SelectedDevice.Id == "No_Device")
                {
                    Properties.Focuser.Default.DeviceId = FocuserChooserVM.SelectedDevice.Id;
                    return false;
                }

                var focuser = (IFocuser)FocuserChooserVM.SelectedDevice;
                _cancelChooseFocuserSource?.Dispose();
                _cancelChooseFocuserSource = new CancellationTokenSource();
                if (focuser != null)
                {
                    try
                    {
                        var connected = await focuser?.Connect(_cancelChooseFocuserSource.Token);
                        _cancelChooseFocuserSource.Token.ThrowIfCancellationRequested();
                        if (connected)
                        {
                            this.Focuser = focuser;
                            UpdateFocuserValues();
                            _focuserTimer.Start();

                            TargetPosition = this.Position;
                            Properties.Focuser.Default.DeviceId = Focuser.Id;

                            // Logger.Info($"Successfully connected Focuser. Id: {Focuser.Id} Name: {Focuser.Name} Driver Version: {Focuser.DriverVersion}");
                            RefreshButtonVisibilities();
                            return true;
                        }
                        else
                        {
                            this.Focuser = null;
                            return false;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        if (this.Connected) { Disconnect(); }
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            finally
            {
                ss.Release();
            }
        }

        private void CancelChooseFocuser(object o)
        {
            _cancelChooseFocuserSource?.Cancel();
        }

        private void UpdateFocuserValues()
        {
            if (Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    this.Connected = _focuser?.Connected ?? false;
                    this.Position = _focuser?.Position ?? 0;
                    this.IsMoving = _focuser?.IsMoving ?? false;
                });
            }
        }


        private int _targetPosition;

        public int TargetPosition
        {
            get
            {
                return _targetPosition;
            }
            set
            {
                _targetPosition = value;
                OnPropertyChanged();
            }
        }

        public void DisconnectDiag(object o)
        {
            TwoButtonMessageDialogVM messageVm = new TwoButtonMessageDialogVM()
            {
                Caption = "Disconnect Focuser",
                Message = "Click <Accept> to disconnect the focuser",
                ButtonOneCaption = "Accept",
                ButtonTwoCaption = "Cancel",
                OnButtonOneClicked = async () =>
                {
                    await Task.Run(() => Disconnect());
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

        public void Disconnect()
        {
            _focuserTimer.Stop();
            Focuser?.Disconnect();
            Focuser = null;
            OnPropertyChanged(nameof(Focuser));
            UpdateFocuserValues();
            RefreshButtonVisibilities();
            // Logger.Info("Disconnected Focuser");
        }

        public void RefreshFocuserList(object obj)
        {
            FocuserChooserVM.GetEquipment(Properties.Focuser.Default.DeviceId);
        }

        private IFocuser _focuser;

        public IFocuser Focuser
        {
            get
            {
                return _focuser;
            }
            private set
            {
                _focuser = value;
                OnPropertyChanged();
            }
        }

        #region Commands ...
        // private IProgress<FocuserStatus> progress;

        public ICommand RefreshFocuserListCommand { get; private set; }
        public IAsyncCommand ChooseFocuserCommand { get; private set; }
        public ICommand CancelChooseFocuserCommand { get; private set; }
        public ICommand DisconnectCommand { get; private set; }

        public ICommand MoveFocuserInCommand { get; private set; }
        public ICommand MoveFocuserOutCommand { get; private set; }


        private RelayCommand _resetStepSize;

        public RelayCommand ResetStepSize
        {
            get
            {
                return _resetStepSize
                       ?? (_resetStepSize = new RelayCommand(
                           param =>
                           {
                               StepSize = 10;
                           })
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
        ~FocuserVM()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            FocuserSettings.Save();
            if (disposing)
            {

            }
        }
        #endregion
    }
}
