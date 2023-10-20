using BetterSerialMonitor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace BetterSerialMonitor
{
    /// <summary>
    /// View model for the main window. I have made it a singleton class since there should really only ever be
    /// one instance of this object.
    /// </summary>
    public class MainWindowViewModel : NotifyPropertyChangedObject
    {
        #region Static methods

        public static string ByteArrayToString(byte[] ba)
        {
            /*StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();*/
            return BitConverter.ToString(ba);
        }

        #endregion

        #region Private variables

        Model _model = Model.GetInstance();

        #endregion

        #region Singleton class

        private static MainWindowViewModel _instance = null;
        private static object _instance_lock = new object();

        private MainWindowViewModel()
        {
            Model.GetInstance().PropertyChanged += this.ExecuteReactionsToModelPropertyChanged;
        }
        
        public static MainWindowViewModel GetInstance ()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new MainWindowViewModel();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region Properties

        [ReactToModelPropertyChanged(new string[] { "SerialConnection" })]
        public bool IsPortDisconnected
        {
            get
            {
                return (Model.GetInstance().SerialConnection == null);
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SerialConnection" })]
        public bool IsPortConnected
        {
            get
            {
                return (Model.GetInstance().SerialConnection != null);
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SerialConnection" })]
        public string ConnectionStatusText
        {
            get
            {
                if (Model.GetInstance().SerialConnection == null)
                {
                    return "Not connected";
                }
                else
                {
                    return "Connected";
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SerialConnection" })]
        public string ConnectButtonContent
        {
            get
            {
                if (Model.GetInstance().SerialConnection == null)
                {
                    return "Connect";
                }
                else
                {
                    return "Disconnect";
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "SerialConnection" })]
        public SolidColorBrush ConnectButtonForegroundColor
        {
            get
            {
                if (Model.GetInstance().SerialConnection == null)
                {
                    return new SolidColorBrush(Colors.Green);
                }
                else
                {
                    return new SolidColorBrush(Colors.Red);
                }
            }
        }

        [ReactToModelPropertyChanged(new string[] { "AvailableDevices" })]
        public List<string> AvailablePorts
        {
            get
            {
                List<string> result = new List<string>();
                var devices = Model.GetInstance().AvailableDevices;
                foreach (var d in devices)
                {
                    result.Add(d.Description + " (" + d.DeviceID + ")");
                }

                return result;
            }
        }

        [ReactToModelPropertyChanged(new string[] { "AvailableBaudRates" })]
        public List<string> AvailableBaudRates
        {
            get
            {
                List<string> result = new List<string>();
                var br = Model.GetInstance().AvailableBaudRates;
                foreach (var b in br)
                {
                    result.Add(b.ToString());
                }

                return result;
            }
        }

        public List<string> DataTypeOptions
        {
            get
            {
                return Enum.GetNames(typeof(Model.TypesToInclude)).ToList();
            }
        }

        public List<string> EndiannessOptions
        {
            get
            {
                return Enum.GetNames(typeof(Model.Endianness)).ToList();
            }
        }

        public int SelectedPortIndex { get; set; } = 0;

        public int SelectedBaudRateIndex { get; set; } = 0;

        public int DataTypeOptionsIndex { get; set; } = 0;

        public int EndiannessOptionsIndex { get; set; } = 0;

        [ReactToModelPropertyChanged(new string[] { "CurrentMessage" })]
        public string Message_ASCII
        {
            get
            {
                var b = Model.GetInstance().CurrentMessage.ToArray();
                return System.Text.Encoding.ASCII.GetString(b);
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentMessage" })]
        public string Message_Bytes
        {
            get
            {
                return ByteArrayToString(Model.GetInstance().CurrentMessage.ToArray());
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentMessage" })]
        public string MessageLength
        {
            get
            {
                return Model.GetInstance().CurrentMessage.Count.ToString();
            }
        }

        [ReactToModelPropertyChanged(new string[] { "CurrentReceiveBuffer" })]
        public string CurrentReceiveBufferText
        {
            get
            {
                return Model.GetInstance().CurrentReceiveBuffer;
            }
        }

        #endregion

        #region Methods

        public void ClearBuffer ()
        {
            Model.GetInstance().ClearBuffer();
        }

        public void ClearCurrentMessage ()
        {
            Model.GetInstance().ClearCurrentMessage();
        }

        public void AddTextToMessage (string text)
        {
            Model.GetInstance().AddTextToMessage(text);
        }

        public void AddBytesToMessage (string text)
        {
            Model.GetInstance().AddBytesToMessage(text);
        }

        public void RefreshAvailablePorts ()
        {
            Model.GetInstance().RefreshAvailablePorts();
        }

        public void ConnectToDevice ()
        {
            var br = Model.GetInstance().AvailableBaudRates[SelectedBaudRateIndex];
            var com_port = Model.GetInstance().AvailableDevices[SelectedPortIndex].DeviceID;
            Model.GetInstance().ConnectToDevice(com_port, br);
        }

        public void Disconnect ()
        {
            Model.GetInstance().Disconnect();
        }

        public void SendImmediateMessage (string text)
        {
            Model.GetInstance().SendImmediateMessage(text);
        }

        public void SendCurrentMessage ()
        {
            Model.GetInstance().SendCurrentMessage();
        }

        public void AddAsDataType(string text)
        {
            Model.TypesToInclude t = (Model.TypesToInclude) DataTypeOptionsIndex;
            Model.Endianness end = (Model.Endianness) EndiannessOptionsIndex;
            Model.GetInstance().AddAsDataType(text, t, end);
        }

        #endregion
    }
}
