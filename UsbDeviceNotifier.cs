using System;
using System.Windows;
using System.Windows.Interop;

namespace ApkInstaller;

public class UsbDeviceNotifier
{
	private const int DbtDeviceArrival = 32768;

	private const int DbtDeviceRemoveComplete = 32772;

	private const int WmDeviceChange = 537;

	private readonly Window? _window;

	public event EventHandler? UsbDeviceChanged;

	public UsbDeviceNotifier(Window window)
	{
		_window = window;
		HwndSource.FromHwnd(new WindowInteropHelper(_window).Handle).AddHook(HwndHandler);
	}

	private nint HwndHandler(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
	{
		if (msg == 537)
		{
			int num = (int)wParam;
			if (num == 32768 || num == 32772)
			{
				this.UsbDeviceChanged?.Invoke(this, EventArgs.Empty);
			}
		}
		return IntPtr.Zero;
	}
}
