/* Copyright(C) 2019-2026 Rob Morgan (robert.morgan.e@gmail.com)

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
using GS.Server.Helpers;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Provides a singleton view model for managing observatory sites, including selection and CRUD operations.
    /// </summary>
    /// <remarks>Allows addition and deletion of named observatories. Selecting an observatory
    /// immediately applies its latitude, longitude, elevation and temperature to <see cref="SkySettings"/>.</remarks>
    public sealed class ObservatoryViewModel : ObservableObject
    {
        #region Singleton

        private static readonly Lazy<ObservatoryViewModel> _instance =
            new Lazy<ObservatoryViewModel>(() => new ObservatoryViewModel());

        public static ObservatoryViewModel Instance => _instance.Value;

        private ObservatoryViewModel()
        {
        }

        #endregion

        #region Properties

        private Observatory _selection;
        private Observatory _settingsSelection;

        /// <summary>
        /// The full collection of saved observatories.
        /// </summary>
        public ObservableCollection<Observatory> Observatories => SkySettings.Observatories;

        /// <summary>
        /// The currently active observatory. Setting this value applies its
        /// lat/long/elevation to <see cref="SkySettings"/> and persists the active name.
        /// </summary>
        public Observatory Selection
        {
            get => _selection;
            set
            {
                if (value == null || ReferenceEquals(_selection, value)) return;
                _selection = value;
                _settingsSelection = value;
                // Apply geographic coordinates to SkySettings so existing DMS controls update.
                SkySettings.Latitude = value.Latitude;
                SkySettings.Longitude = value.Longitude;
                SkySettings.Elevation = value.Elevation;
                SkySettings.Temperature = value.Temperature;
                SkySettings.ObservatoryName = value.Name;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The observatory selected in the settings/management ComboBox (used for delete operations).
        /// </summary>
        public Observatory SettingsSelection
        {
            get => _settingsSelection;
            set
            {
                if (ReferenceEquals(_settingsSelection, value)) return;
                _settingsSelection = value;
                OnPropertyChanged();
            }
        }

        #endregion

        #region CRUD Methods

        /// <summary>
        /// Adds a new observatory using the current <see cref="SkySettings"/> lat/long/elevation values.
        /// </summary>
        public void AddObservatory(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Observatory name cannot be empty", nameof(name));

            var trimmedName = name.Trim();

            if (Observatories.Any(o => string.Equals(o.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Observatory '{trimmedName}' already exists");
            }

            var obs = new Observatory(trimmedName, SkySettings.Latitude, SkySettings.Longitude, SkySettings.Elevation, SkySettings.Temperature);
            Observatories.Add(obs);
            SkySettings.SaveObservatories(Observatories.ToList());
            SettingsSelection = obs;
            // Update the default view's current item for any CollectionViewSources bound to this collection
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Observatories);
            if (view != null)
            {
                view.MoveCurrentTo(obs);
            }

            Selection = obs;
        }

        /// <summary>
        /// Updates the specified observatory's coordinates from the current <see cref="SkySettings"/> values
        /// and persists the change.
        /// </summary>
        public void UpdateObservatory(Observatory observatory)
        {
            if (observatory == null) return;

            var current = Observatories.FirstOrDefault(o => o.Name == observatory.Name);
            var index = Observatories.IndexOf(current);
            if (index < 0) return;

            // Remember if this position is currently selected BEFORE RemoveAt changes the binding
            var wasSettingsSelection = SettingsSelection.Name == observatory.Name;
            var wasSelection = Selection.Name == observatory.Name;

            // Remove old object from collection (clears WPF ComboBox cache)
            // NOTE: This will trigger IsSynchronizedWithCurrentItem binding and change SettingsSelection/Selection
            Observatories.RemoveAt(index);

            // Create new object to avoid mutating hash code while in collection
            var updated = new Observatory(observatory.Name, observatory.Latitude, observatory.Longitude, observatory.Elevation, observatory.Temperature);

            // Insert at same position to maintain order
            Observatories.Insert(index, updated);

            // Restore references based on what they were BEFORE RemoveAt
            if (wasSettingsSelection)
                SettingsSelection = updated;
            if (wasSelection)
                Selection = updated;

            // Update the CollectionView's current item for ComboBoxes with IsSynchronizedWithCurrentItem="True"
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Observatories);
            if (view != null)
            {
                view.MoveCurrentTo(updated);
            }

            SkySettings.SaveObservatories(Observatories.ToList());
        }

        /// <summary>
        /// Removes the specified observatory from the collection.
        /// </summary>
        public void DeleteObservatory(Observatory observatory)
        {
            if (observatory == null) return;

            var index = Observatories.IndexOf(observatory);
            Observatories.Remove(observatory);
            SkySettings.SaveObservatories(Observatories.ToList());

            if (Observatories.Count == 0)
            {
                SettingsSelection = null;
                Selection = null;
            }
            else
            {
                var newIndex = Math.Max(0, index - 1);
                if (newIndex >= Observatories.Count)
                {
                    newIndex = Observatories.Count - 1;
                }

                SettingsSelection = Observatories[newIndex];
                if (ReferenceEquals(Selection, observatory))
                {
                    Selection = Observatories.FirstOrDefault();
                }
            }
        }

        #endregion
    }
}
