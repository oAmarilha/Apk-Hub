using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ApkInstaller;

public class UsbDeviceNotifier
{
    private const int DbtDeviceArrival = 0x8000;            // Device connected
    private const int DbtDeviceRemoveComplete = 0x8004;      // Device disconnected
    private const int WmDeviceChange = 0x0219;               // Device change event

    private const int DbtDevtypDeviceInterface = 5;          // Device type is a device interface
    private static readonly Guid GuidDevinterfaceUsbDevice = new Guid("A5DCBF10-6530-11D2-901F-00C04FB951ED");

    private readonly Window _window;
    private IntPtr _notificationHandle;

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
        return IntPtr.Zero;
    }

    private void RegisterForUsbEvents(IntPtr windowHandle)
    {
        var deviceInterface = new DevBroadcastDeviceinterface
        {
            dbcc_size = Marshal.SizeOf(typeof(DevBroadcastDeviceinterface)),
            dbcc_devicetype = DbtDevtypDeviceInterface,
            dbcc_classguid = GuidDevinterfaceUsbDevice
        };

        IntPtr buffer = Marshal.AllocHGlobal(deviceInterface.dbcc_size);
        Marshal.StructureToPtr(deviceInterface, buffer, true);

        _notificationHandle = RegisterDeviceNotification(windowHandle, buffer, 0);

        if (_notificationHandle == IntPtr.Zero)
        {
            throw new Exception("Failed to register for USB device notifications.");
        }
    }

    public void UnregisterDeviceNotification()
    {
        if (_notificationHandle != IntPtr.Zero)
        {
            UnregisterDeviceNotification(_notificationHandle);
            _notificationHandle = IntPtr.Zero;
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr notificationFilter, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern bool UnregisterDeviceNotification(IntPtr handle);

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
