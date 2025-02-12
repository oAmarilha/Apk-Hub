using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ApkInstaller.Helper_classes;

namespace ApkInstaller.Feature_classes
{
    /// <summary>
    /// Interaction logic for DeviceInformation.xaml
    /// </summary>
    public partial class DeviceInformation : Window, IComponentConnector
    {
        readonly DeviceInfo _selectedDevice;
        private static readonly Dictionary<string, string> androidName = new()
        {
            {"7", "N" },
            {"8", "O" },
            {"9", "P" },
            {"10", "Q" },
            {"11", "R" },
            {"12", "S" },
            {"13", "T" },
            {"14", "U" },
            {"15", "V" }
        };
        public DeviceInformation(MainWindow mainWindow, DeviceInfo selectedDevice)
        {
            InitializeComponent();
            Owner = mainWindow;
            _selectedDevice = selectedDevice;
            PopulateDeviceInfo();
        }

        private void PopulateDeviceInfo()
        {
            AndroidVersion.Text = _selectedDevice.AndroidVersion;
            BuildMode.Text = _selectedDevice.BuildMode;
            CscCode.Text = _selectedDevice.CscCode;
            ModelCode.Text = _selectedDevice.DeviceName;
            OsName.Text = androidName.GetValueOrDefault(_selectedDevice.AndroidVersion);
            Manufacturer.Text = _selectedDevice.Manufacturer;
            SdkVersion.Text = _selectedDevice.SdkVersion;
            SerialNo.Text = _selectedDevice.SerialNo;
        }
    }
}
