using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BetterSerialMonitor
{
    /// <summary>
    /// A basic class to hold some USB device information
    /// </summary>
    public class USBDeviceInfo
    {
        #region Constructor

        public USBDeviceInfo(string device_description, string com_port, bool busy)
        {
            Description = device_description;
            DeviceID = com_port;
            SerialObject = new SerialPort(DeviceID, 115200);
            IsPortBusy = busy;
        }

        #endregion

        #region Public fields

        public string DeviceID = string.Empty;
        public string Description = string.Empty;
        public SerialPort SerialObject = null;
        public bool IsPortBusy = false;

        #endregion

    }
}
