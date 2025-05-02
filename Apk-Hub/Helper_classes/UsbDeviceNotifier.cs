using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ApkInstaller.Helper_classes;

public class UsbDeviceNotifier
{
    private const int DbtDeviceArrival = 0x8000;            // Device connected
    private const int DbtDeviceRemoveComplete = 0x8004;      // Device disconnected
    private const int WmDeviceChange = 0x0219;               // Device change event
    private const int DbtDevtypDeviceInterface = 5;          // Device type is a device interface

    private static readonly Guid GuidDevinterfaceMtpDevice = new Guid("6AC27878-A6FA-4155-BA32-235D98C0DD1D");

    private readonly Window _window;
    private nint _notificationHandle;

    public event EventHandler? UsbDeviceChanged;

    public UsbDeviceNotifier(Window window)
    {
        _window = window;
        var windowHandle = new WindowInteropHelper(_window).Handle;
        HwndSource.FromHwnd(windowHandle)?.AddHook(HwndHandler);

        RegisterForUsbEvents(windowHandle);
    }

    private nint HwndHandler(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WmDeviceChange)
        {
            switch ((int)wParam)
            {
                case DbtDeviceArrival:
                case DbtDeviceRemoveComplete:
                    UsbDeviceChanged?.Invoke(this, EventArgs.Empty);
                    handled = true;
                    break;
            }
        }
        return nint.Zero;
    }

    private void RegisterForUsbEvents(nint windowHandle)
    {
        var deviceInterface = new DevBroadcastDeviceinterface
        {
            dbcc_size = Marshal.SizeOf(typeof(DevBroadcastDeviceinterface)),
            dbcc_devicetype = DbtDevtypDeviceInterface,
            dbcc_classguid = GuidDevinterfaceMtpDevice
        };

        nint buffer = Marshal.AllocHGlobal(deviceInterface.dbcc_size);
        Marshal.StructureToPtr(deviceInterface, buffer, true);

        _notificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);

        if (_notificationHandle == nint.Zero)
        {
            throw new Exception("Failed to register for USB device notifications.");
        }
    }

    public void UnregisterDeviceNotification()
    {
        if (_notificationHandle != nint.Zero)
        {
            UnregisterDeviceNotification(_notificationHandle);
            _notificationHandle = nint.Zero;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint RegisterDeviceNotification(nint hRecipient, nint notificationFilter, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool UnregisterDeviceNotification(nint handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct DevBroadcastDeviceinterface
    {
        public int dbcc_size;
        public int dbcc_devicetype;
        public int dbcc_reserved;
        public Guid dbcc_classguid;
        public short dbcc_name;
    }
}
