/* Copyright(C) 2019-2021  Rob Morgan (robert.morgan.e@gmail.com)

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
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace GS.ChartViewer.Helpers
{
    public abstract class ObservableObject : INotifyPropertyChanged
    {
        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] != null) return;
            var msg = "Invalid property name: " + propertyName;

            if (ThrowOnInvalidPropertyName)
                throw new Exception(msg);
            else
                Debug.Fail(msg);
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        private bool ThrowOnInvalidPropertyName => false;

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raises the PropertyChange event for the property specified
        /// </summary>
        /// <param name="propertyName">Property name to update. Is case-sensitive.</param>
        public void RaisePropertyChanged(string propertyName)
        {
            VerifyPropertyName(propertyName);
            OnPropertyChanged(propertyName);
        }

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            VerifyPropertyName(propertyName);

            var handler = PropertyChanged;
            if (handler == null) return;
            var e = new PropertyChangedEventArgs(propertyName);
            handler(this, e);
        }

        #endregion // INotifyPropertyChanged Members
    }
}
