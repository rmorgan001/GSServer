﻿#region "copyright"

/*
    Copyright © 2016 - 2021 Stefan Berg <isbeorn86+NINA@googlemail.com> and the N.I.N.A. contributors

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    This Source Code Form is subject to the terms of the Mozilla Public
    License, v. 2.0. If a copy of the MPL was not distributed with this
    file, You can obtain one at http://mozilla.org/MPL/2.0/.
*/

#endregion "copyright"

using GS.Utilities.Helpers;
using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace NINA.Utility
{

    public abstract class AsyncCommandBase : ObservableObject, IAsyncCommand
    {

        public abstract bool CanExecute(object parameter);

        public abstract Task ExecuteAsync(object parameter);

        public async void Execute(object parameter)
        {
            await ExecuteAsync(parameter);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        protected void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public interface IAsyncCommand : ICommand
    {

        Task ExecuteAsync(object parameter);
    }

    public class AsyncCommand<TResult> : AsyncCommandBase
    {
        private readonly Func<object, Task<TResult>> _command;
        private NotifyTaskCompletion<TResult> _execution;

        /// <summary>
        /// Encapsulated the representation for the validation of the execute method
        /// </summary>
        private Predicate<object> _canExecute;

        /// <summary>
        /// Defines if command can be executed (default behaviour)
        /// </summary>
        /// <param name="parameter">The parameter.</param>
        /// <returns>Always true</returns>
        private static bool DefaultCanExecute(object parameter)
        {
            return true;
        }

        public AsyncCommand(Func<object, Task<TResult>> command)
        {
            _command = command;
            _canExecute = DefaultCanExecute;
        }

        public AsyncCommand(Func<object, Task<TResult>> command, Predicate<object> canExecute)
        {
            _command = command;
            _canExecute = canExecute;
        }

        public AsyncCommand(Func<Task<TResult>> command)
        {
            _command = (object o) => {
                return command();
            };
            _canExecute = DefaultCanExecute;
        }

        public AsyncCommand(Func<Task<TResult>> command, Predicate<object> canExecute)
        {
            _command = (object o) => {
                return command();
            };
            _canExecute = canExecute;
        }

        public override bool CanExecute(object parameter)
        {
            return (this._canExecute != null && this._canExecute(parameter)) && (Execution == null || Execution.IsCompleted);
        }

        public override async Task ExecuteAsync(object parameter)
        {
            Execution = new NotifyTaskCompletion<TResult>(_command(parameter));
            RaiseCanExecuteChanged();
            if (!Execution.IsCompleted)
            {
                await Execution.TaskCompletion;
            }
            RaiseCanExecuteChanged();
        }

        // Raises PropertyChanged
        public NotifyTaskCompletion<TResult> Execution { get { return _execution; } private set { _execution = value; OnPropertyChanged(); } }
    }
}