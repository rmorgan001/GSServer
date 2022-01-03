using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using GS.Shared.Command;
using GS.Utilities.Helpers;

namespace GS.Utilities.Controls.Dialogs
{
    public class TwoButtonMessageDialogVM : ObservableObject
    {
        /// <summary>
        /// An action to perform when button one is clicked
        /// </summary>
        public Action OnButtonOneClicked;

        /// <summary>
        /// An action to perform when button two is clicked
        /// </summary>
        public Action OnButtonTwoClicked;

        private string _caption = "";

        /// <summary>
        /// Message box caption
        /// </summary>
        public string Caption
        {
            get => _caption;
            set
            {
                _caption = value;
                OnPropertyChanged();
            }
        }

        private string _message = "";

        /// <summary>
        /// The message to display
        /// </summary>
        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }


        private string _buttonOneCaption = "OK";

        /// <summary>
        /// Caption for button two (default is OK)
        /// </summary>
        public string ButtonOneCaption
        {
            get => _buttonOneCaption;
            set
            {
                _buttonOneCaption = value;
                OnPropertyChanged();
            }
        }

        public bool ButtonOneIsDefault { get; set; } = true;

        public bool ButtonOneIsCancel { get; set; } = false;


        private string _buttonTwoCaption = "Cancel";

        /// <summary>
        /// Caption for button two, if null the button will not be displayed.
        /// </summary>
        public string ButtonTwoCaption
        {
            get => _buttonTwoCaption;
            set
            {
                _buttonTwoCaption = value;
                OnPropertyChanged();
            }
        }

        public bool ButtonTwoIsVisible
        {
            get => (_buttonTwoCaption != null);
        }

        public bool ButtonTwoIsDefault { get; set; } = false;

        public bool ButtonTwoIsCancel { get; set; } = true;


        public TwoButtonMessageDialogVM()
        {
        }


        private ICommand _buttonOneClickedCommand;
        public ICommand ButtonOneClickedCommand
        {
            get
            {
                return _buttonOneClickedCommand ?? (_buttonOneClickedCommand = new RelayCommand(
                    param =>
                    {
                        OnButtonOneClicked?.Invoke();
                    }
                ));
            }
        }

        private ICommand _buttonTwoClickedCommand;
        public ICommand ButtonTwoClickedCommand
        {
            get
            {
                return _buttonTwoClickedCommand ?? (_buttonTwoClickedCommand = new RelayCommand(
                    param =>
                    {
                        OnButtonTwoClicked?.Invoke();
                    }
                ));
            }
        }
    }
}
