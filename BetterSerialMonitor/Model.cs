using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TxBDC_Common;

namespace BetterSerialMonitor
{
    /// <summary>
    /// Model class. This is a singleton class because there should really only ever be one of these.
    /// </summary>
    public class Model : NotifyPropertyChangedObject
    {
        #region Private variables

        private List<int> _available_baud_rates = new List<int>() { 300, 1200, 2400, 4800, 9600, 19200, 38400, 57600, 74880, 115200 };
        private List<byte> _current_message = new List<byte>();

        private string _current_receive_buffer = string.Empty;
        private object _current_receive_buffer_lock = new object();

        private List<USBDeviceInfo> _available_devices = new List<USBDeviceInfo>();
        private object _available_devices_lock = new object();

        private BackgroundWorker refresh_ports_worker = new BackgroundWorker();
        private BackgroundWorker read_serial_data_worker = new BackgroundWorker();
        
        private SerialPort _serial_port = null;
        private object _serial_port_lock = new object();

        #endregion

        #region Singleton class

        private static Model _instance = null;
        private static object _instance_lock = new object();

        private Model ()
        {
            refresh_ports_worker.DoWork += Refresh_ports_worker_DoWork;
            refresh_ports_worker.RunWorkerCompleted += Refresh_ports_worker_RunWorkerCompleted;

            read_serial_data_worker.DoWork += Read_serial_data_worker_DoWork;
            read_serial_data_worker.ProgressChanged += Read_serial_data_worker_ProgressChanged;
            read_serial_data_worker.WorkerReportsProgress = true;
            read_serial_data_worker.WorkerSupportsCancellation = true;

            refresh_ports_worker.RunWorkerAsync();
        }
        
        public static Model GetInstance ()
        {
            if (_instance == null)
            {
                lock (_instance_lock)
                {
                    if (_instance == null)
                    {
                        _instance = new Model();
                    }
                }
            }

            return _instance;
        }

        #endregion

        #region Public properties

        /// <summary>
        /// List of available baud rates
        /// </summary>
        public List<int> AvailableBaudRates
        {
            get
            {
                return _available_baud_rates;
            }
        }

        /// <summary>
        /// A list of bytes in the current message
        /// </summary>
        public List<byte> CurrentMessage
        {
            get
            {
                return _current_message;
            }
            private set
            {
                _current_message = value;
                NotifyPropertyChanged("CurrentMessage");
            }
        }

        /// <summary>
        /// The current buffer of text received over the serial line
        /// </summary>
        public string CurrentReceiveBuffer
        {
            get
            {
                return _current_receive_buffer;
            }
            private set
            {
                _current_receive_buffer = value;
            }
        }

        /// <summary>
        /// Returns the list of available devices that we can communicate with over serial
        /// </summary>
        public List<USBDeviceInfo> AvailableDevices
        {
            get
            {
                lock (_available_devices_lock)
                {
                    return _available_devices.ToList();
                }
            }
        }

        /// <summary>
        /// The serial port object
        /// </summary>
        public SerialPort SerialConnection
        {
            get
            {
                return _serial_port;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Clear the current receive buffer
        /// </summary>
        public void ClearBuffer ()
        {
            lock (_current_receive_buffer_lock)
            {
                _current_receive_buffer = string.Empty;
                NotifyPropertyChanged("CurrentReceiveBuffer");
            }
        }

        /// <summary>
        /// Clears the current message
        /// </summary>
        public void ClearCurrentMessage()
        {
            CurrentMessage.Clear();
            NotifyPropertyChanged("CurrentMessage");
        }

        /// <summary>
        /// Adds text to the current message
        /// </summary>
        public void AddTextToMessage (string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var txt_bytes = Encoding.ASCII.GetBytes(text).ToList();
                CurrentMessage.AddRange(txt_bytes);
                NotifyPropertyChanged("CurrentMessage");
            }
        }

        /// <summary>
        /// Parses a text string for bytes the user would like to add to the current message
        /// Bytes should be separated by spaces.
        /// If a byte starts with "0x", it will be parsed as a hexidecimal value.
        /// Otherwise, it will be parsed as a decimal value.
        /// </summary>
        public void AddBytesToMessage (string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                var string_pieces = text.Split(new char[] { ' ' }).ToList();
                if (string_pieces.Count > 0)
                {
                    for (int i = 0; i < string_pieces.Count; i++)
                    {
                        var this_piece = string_pieces[i];
                        if (this_piece.StartsWith("0x", StringComparison.CurrentCultureIgnoreCase))
                        {
                            //Parse as a hexidecimal value
                            this_piece = this_piece.Substring(2);
                            bool hex_parsed_successfully = Byte.TryParse(this_piece,
                                NumberStyles.HexNumber, CultureInfo.CurrentCulture, out byte hex_result);
                            if (hex_parsed_successfully)
                            {
                                CurrentMessage.Add(hex_result);
                            }
                        }
                        else
                        {
                            //Parse as a decimal value
                            bool decimal_parsed_successfully = Byte.TryParse(this_piece, out byte decimal_result);
                            if (decimal_parsed_successfully)
                            {
                                CurrentMessage.Add(decimal_result);
                            }
                        }
                    }

                    NotifyPropertyChanged("CurrentMessage");
                }
            }
        }

        /// <summary>
        /// Refreshes the list of available ports
        /// </summary>
        public void RefreshAvailablePorts ()
        {
            if (!refresh_ports_worker.IsBusy)
            {
                refresh_ports_worker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// Connects to a serial port for communication
        /// </summary>
        public void ConnectToDevice (string port, int baud_rate)
        {
            lock (_serial_port_lock)
            {
                try
                {
                    if (_serial_port == null)
                    {
                        _serial_port = new SerialPort(port, baud_rate);
                        _serial_port.DtrEnable = true;
                        _serial_port.Open();

                        //Cancel any previous existing worker that is reading serial data
                        if (read_serial_data_worker.IsBusy)
                        {
                            read_serial_data_worker.CancelAsync();
                        }
                        
                        //Start a new worker
                        read_serial_data_worker.RunWorkerAsync();
                    }
                }
                catch (Exception)
                {
                    _serial_port = null;
                }
            }
            
            NotifyPropertyChanged("SerialConnection");
        }

        /// <summary>
        /// Disconnect from the current device
        /// </summary>
        public void Disconnect ()
        {
            lock (_serial_port_lock)
            {
                try
                {
                    if (_serial_port != null)
                    {
                        _serial_port.Close();
                    }
                }
                catch (Exception)
                {
                    //empty
                }
            }

            _serial_port = null;
            read_serial_data_worker.CancelAsync();

            NotifyPropertyChanged("SerialConnection");
        }

        /// <summary>
        /// Sends a string over the serial line
        /// </summary>
        public void SendImmediateMessage (string text)
        {
            lock (_serial_port_lock)
            {
                if (_serial_port != null && _serial_port.IsOpen)
                {
                    _serial_port.Write(text);
                }
            }
        }

        /// <summary>
        /// Sends the current message across the serial line
        /// </summary>
        public void SendCurrentMessage ()
        {
            lock (_serial_port_lock)
            {
                if (_serial_port != null && _serial_port.IsOpen)
                {
                    _serial_port.Write(CurrentMessage.ToArray(), 0, CurrentMessage.Count);
                }
            }

            CurrentMessage.Clear();
            NotifyPropertyChanged("CurrentMessage");
        }

        #endregion

        #region Private methods

        private void Refresh_ports_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            lock (_available_devices_lock)
            {
                //Create a list to hold information from USB devices
                List<USBDeviceInfo> devices = new List<USBDeviceInfo>();

                //Query all connected devices
                var searcher = new ManagementObjectSearcher(@"SELECT * FROM WIN32_SerialPort");
                var collection = searcher.Get();

                //Grab the information we need
                foreach (var device in collection)
                {
                    string id = (string)device.GetPropertyValue("DeviceID");
                    string desc = (string)device.GetPropertyValue("Description");
                    USBDeviceInfo d = new USBDeviceInfo(desc, id, false);
                    devices.Add(d);
                }

                //Dispose of the collection of queried devices
                collection.Dispose();

                //Set the list of available devices
                _available_devices = devices;
            }
        }

        private void Refresh_ports_worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            NotifyPropertyChanged("AvailableDevices");
        }

        private void Read_serial_data_worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            NotifyPropertyChanged("CurrentReceiveBuffer");
        }

        private void Read_serial_data_worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!read_serial_data_worker.CancellationPending)
            {
                lock (_serial_port_lock)
                {
                    if (_serial_port != null && _serial_port.IsOpen && _serial_port.BytesToRead > 0)
                    {
                        var input_data = _serial_port.ReadExisting();
                        CurrentReceiveBuffer += input_data;
                    }
                }

                //Report progress to the GUI
                read_serial_data_worker.ReportProgress(0);

                //Sleep for a bit so we don't consume the whole CPU
                Thread.Sleep(150);
            }
        }

        #endregion
    }
}
