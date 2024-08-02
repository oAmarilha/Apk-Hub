using System.Diagnostics;
using System.Threading.Tasks;

namespace ApkInstaller;

public static class ProcessExtensions
{
	public static Task<int> WaitForExitAsync(this Process process)
	{
		TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();
		process.EnableRaisingEvents = true;
		process.Exited += delegate
		{
			tcs.TrySetResult(process.ExitCode);
		};
		return tcs.Task;
	}
}
