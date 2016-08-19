using System.ComponentModel;
using USB_Data_Stealer.Core.Logging;

namespace USB_Data_Stealer.Core
{
    partial class UsbStealer
    {
        public class FileTransfererSettings : INotifyPropertyChanged
        {
            private bool _copyBySize;
            private string[] _precedenceExtensions;

            public bool CopyBySize
            {
                get { return _copyBySize; }
                set
                {
                    if (_copyBySize == value) return;
                    _copyBySize = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("CopyBySize"));
                    Logger.Append(new Message(Message.EventTypes.Info,
                        "Copy by Size changed to: " + CopyBySize + "."));
                }
            }

            public string[] PrecedenceExtensions
            {
                get { return _precedenceExtensions ?? new string[0]; }
                set
                {
                    if (PrecedenceExtensions == value) return;
                    _precedenceExtensions = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("PrecedenceExtensions"));
                    Logger.Append(new Message(Message.EventTypes.Info,
                        "Precedence Extensions changed to: " + string.Join(" ", PrecedenceExtensions) + "."));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            private void OnPropertyChanged(PropertyChangedEventArgs e)
            {
                PropertyChanged?.Invoke(this, e);
            }
        }
    }
}