﻿using System;
using System.Threading;
using System.Windows;
using GS.Shared;

namespace GS.Utilities
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : IDisposable
    {
        // give the mutex a  unique name
        private const string MutexName = "Green Swamp Utility";
        // declare the mutex
        private readonly Mutex _mutex;
        // overload the constructor
        private readonly bool createdNew;

        public App()
        {
            // overloaded mutex constructor which outs a boolean
            // telling if the mutex is new or not.
            // see http://msdn.microsoft.com/en-us/library/System.Threading.Mutex.aspx
            _mutex = new Mutex(true, MutexName, out createdNew);
            if (createdNew) return;
            // if the mutex already exists, notify and quit
            MessageBox.Show("GS Utility is already running", "Error", MessageBoxButton.OK, MessageBoxImage.Exclamation);
            Current.Shutdown(0);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            if (!createdNew) return;
            base.OnStartup(e);
            Languages.SetLanguageDictionary(false, LanguageApp.GSChartViewer);

        }


        #region Dispose
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        // NOTE: Leave out the finalizer altogether if this class doesn't
        // own unmanaged resources itself, but leave the other methods
        // exactly as they are.
        ~App()
        {
            // Finalizer calls Dispose(false)
            Dispose(false);
        }
        // The bulk of the clean-up code is implemented in Dispose(bool)
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                _mutex?.Dispose();
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