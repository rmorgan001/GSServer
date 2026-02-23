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
using System;
using System.Collections.ObjectModel;
using System.Linq;
using GS.Server.Helpers;

namespace GS.Server.SkyTelescope
{
    /// <summary>
    /// Provides a singleton view model for managing park positions, including selection and CRUD operations.
    /// </summary>
    /// <remarks>This class allows for the addition, updating, and deletion of park positions, while
    /// maintaining the current selection state. It ensures that changes to the park positions are saved persistently
    /// and updates the UI bindings accordingly.</remarks>
    public sealed class ParkPositionViewModel : ObservableObject
    {
        #region Singleton

        private static readonly Lazy<ParkPositionViewModel> _instance =
            new Lazy<ParkPositionViewModel>(() => new ParkPositionViewModel());

        public static ParkPositionViewModel Instance => _instance.Value;

        private ParkPositionViewModel()
        {
        }

        #endregion

        #region Properties

        private ParkPosition _selection;
        private ParkPosition _settingsSelection;

        public ObservableCollection<ParkPosition> Positions => SkySettings.ParkPositions;

        public ParkPosition Selection
        {
            get => _selection;
            set
            {
                if (value == null || ReferenceEquals(_selection, value)) return;
                _selection = value;
                SkyServer.ParkSelected = value;
                OnPropertyChanged();
            }
        }

        public ParkPosition SettingsSelection
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

        public void AddPosition(string name, double x, double y)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Park position name cannot be empty", nameof(name));
            }

            var trimmedName = name.Trim();

            if (Positions.Any(p => string.Equals(p.Name, trimmedName, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Park position '{trimmedName}' already exists");
            }

            var pp = new ParkPosition { Name = trimmedName, X = x, Y = y };
            Positions.Add(pp);
            SkySettings.SaveParkPositions(Positions.ToList());
            SettingsSelection = pp;
            // Update the default view's current item for any CollectionViewSources bound to this collection
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Positions);
            if (view != null)
            {
                view.MoveCurrentTo(pp);
            }

            // Also update Selection for StandardButtonBar
            Selection = pp;
        }

        public void UpdatePosition(ParkPosition position, double newX, double newY)
        {
            if (position == null) return;

            var index = Positions.IndexOf(position);
            if (index < 0) return;

            // Remember if this position is currently selected BEFORE RemoveAt changes the binding
            var wasSettingsSelection = ReferenceEquals(SettingsSelection, position);
            var wasSelection = ReferenceEquals(Selection, position);

            // Remove old object from collection (clears WPF ComboBox cache)
            // NOTE: This will trigger IsSynchronizedWithCurrentItem binding and change SettingsSelection/Selection
            Positions.RemoveAt(index);

            // Create new object to avoid mutating hash code while in collection
            var updated = new ParkPosition(position.Name, newX, newY);

            // Insert at same position to maintain order
            Positions.Insert(index, updated);

            // Restore references based on what they were BEFORE RemoveAt
            if (wasSettingsSelection)
                SettingsSelection = updated;
            if (wasSelection)
                Selection = updated;

            // Update the CollectionView's current item for ComboBoxes with IsSynchronizedWithCurrentItem="True"
            var view = System.Windows.Data.CollectionViewSource.GetDefaultView(Positions);
            if (view != null)
            {
                view.MoveCurrentTo(updated);
            }

            SkySettings.SaveParkPositions(Positions.ToList());
        }

        public void DeletePosition(ParkPosition position)
        {
            if (position == null) return;

            var index = Positions.IndexOf(position);
            Positions.Remove(position);
            SkySettings.SaveParkPositions(Positions.ToList());

            if (Positions.Count == 0)
            {
                SettingsSelection = null;
                Selection = null;
            }
            else
            {
                var newIndex = Math.Max(0, index - 1);
                if (newIndex >= Positions.Count)
                {
                    newIndex = Positions.Count - 1;
                }

                SettingsSelection = Positions[newIndex];
                if (ReferenceEquals(Selection, position))
                {
                    Selection = Positions.FirstOrDefault();
                }
            }
        }

        #endregion
    }
}
