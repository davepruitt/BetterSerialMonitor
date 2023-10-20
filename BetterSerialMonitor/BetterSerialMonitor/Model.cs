using BetterSerialMonitor.Utilities;
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

namespace BetterSerialMonitor
{
    /// <summary>
    /// Model class. This is a singleton class because there should really only ever be one of these.
    /// </summary>
    public class Model : NotifyPropertyChangedObject
    {
        #region Private variables

        public enum Endianness
        {
            LitteEndian,
            BigEndian
        }

        public enum TypesToInclude
        {
            Bool,
            Sbyte,
            Byte,
            Int16,
            UInt16,
            Int32,
            UInt32,
            Int64,
            UInt64,
            Float,
            Double,
            Decimal
        }
        
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

        private int MaxBufferLength = 1000;

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
        /// Adds text as a specific datatype
        /// </summary>
        /// <param name="text"></param>
        public void AddAsDataType (string text, TypesToInclude t, Endianness e)
        {
            byte[] result_bytes = null;

            switch (t)
            {
                case Model.TypesToInclude.Bool:

                    var bool_parse_success = bool.TryParse(text, out bool bool_result);
                    if (bool_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(bool_result);
                    }

                    break;
                case Model.TypesToInclude.Sbyte:

                    var sbyte_parse_success = sbyte.TryParse(text, out sbyte sbyte_result);
                    if (sbyte_parse_success)
                    {
                        unchecked
                        {
                            result_bytes = new byte[] { (byte)sbyte_result };
                        }
                    }

                    break;
                case Model.TypesToInclude.Byte:

                    var byte_parse_success = byte.TryParse(text, out byte byte_result);
                    if (byte_parse_success)
                    {
                        result_bytes = new byte[] { byte_result };
                    }
                    
                    break;
                case Model.TypesToInclude.Int16:

                    var short_parse_success = Int16.TryParse(text, out Int16 short_result);
                    if (short_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(short_result);
                    }

                    break;
                case Model.TypesToInclude.UInt16:

                    var ushort_parse_success = UInt16.TryParse(text, out UInt16 ushort_result);
                    if (ushort_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(ushort_result);
                    }

                    break;
                case Model.TypesToInclude.Int32:

                    var int_parse_success = Int32.TryParse(text, out Int32 int_result);
                    if (int_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(int_result);
                    }

                    break;
                case Model.TypesToInclude.UInt32:

                    var uint_parse_success = UInt32.TryParse(text, out UInt32 uint_result);
                    if (uint_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(uint_result);
                    }

                    break;
                case Model.TypesToInclude.Int64:

                    var long_parse_success = Int64.TryParse(text, out Int64 long_result);
                    if (long_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(long_result);
                    }

                    break;
                case Model.TypesToInclude.UInt64:

                    var ulong_parse_success = UInt64.TryParse(text, out UInt64 ulong_result);
                    if (ulong_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(ulong_result);
                    }

                    break;
                case Model.TypesToInclude.Float:

                    var float_parse_success = Single.TryParse(text, out Single single_result);
                    if (float_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(single_result);
                    }

                    break;
                case Model.TypesToInclude.Double:

                    var double_parse_success = Double.TryParse(text, out Double double_result);
                    if (double_parse_success)
                    {
                        result_bytes = BitConverter.GetBytes(double_result);
                    }

                    break;
                case Model.TypesToInclude.Decimal:

                    var decimal_parse_success = Decimal.TryParse(text, out Decimal decimal_result);
                    if (decimal_parse_success)
                    {
                        Int32[] bits = decimal.GetBits(decimal_result);
                        List<byte> b = new List<byte>();
                        foreach (Int32 i in bits)
                        {
                            b.AddRange(BitConverter.GetBytes(i));
                        }

                        result_bytes = b.ToArray();
                    }

                    break;

            }

            if (result_bytes != null)
            {
                if ((BitConverter.IsLittleEndian && e == Endianness.BigEndian) ||
                    (!BitConverter.IsLittleEndian && e == Endianness.LitteEndian))
                {
                    result_bytes = result_bytes.Reverse().ToArray();
                }

                CurrentMessage.AddRange(result_bytes);
            }

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
                        if (CurrentReceiveBuffer.Length > MaxBufferLength)
                        {
                            CurrentReceiveBuffer = CurrentReceiveBuffer.Substring(CurrentReceiveBuffer.Length - MaxBufferLength);
                        }
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
