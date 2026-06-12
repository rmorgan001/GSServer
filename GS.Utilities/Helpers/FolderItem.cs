using System;

namespace GS.Utilities.Helpers
{
    public class FolderItem: ObservableObject, IDisposable
    {
        private string _name;
        private string _path;
        private string _counts;
        private bool _isSelected;

        public string Name
        {
            get => _name;
            set { _name = value; OnPropertyChanged(); }
        }
        
        public string NameCount
        {
            get => _counts;
            set { _counts = value; OnPropertyChanged(); }
        }

        public string Path
        {
            get => _path;
            set { _path = value; OnPropertyChanged(); }
        }
        
        // This property binds directly to the CheckBox's IsChecked property
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }
        
        public void Dispose()
        {
            throw new NotImplementedException();
        }
    }
}
