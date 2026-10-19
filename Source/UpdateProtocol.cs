// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Runtime.InteropServices;using System.Text;using System.Threading;
static class UpdateProtocol {
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern uint RegisterWindowMessage(string text);
 [DllImport("user32.dll",SetLastError=true)]static extern IntPtr SendMessageTimeout(IntPtr window,uint message,IntPtr value,IntPtr flags,uint options,uint timeout,out IntPtr result);
 [DllImport("user32.dll")]static extern bool EnumWindows(Callback callback,IntPtr value);delegate bool Callback(IntPtr window,IntPtr value);
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr window,out uint pid);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr window,StringBuilder title,int max);
 [DllImport("user32.dll")]static extern bool ShowWindow(IntPtr window,int command);
 [DllImport("user32.dll")]static extern bool SetForegroundWindow(IntPtr window);
 internal static readonly uint QuitMessage=RegisterWindowMessage("Jago.Companion.SaveAndQuit.v1");
 internal static IntPtr MainWindow(Process process){IntPtr found=IntPtr.Zero;EnumWindows((window,value)=>{uint pid;GetWindowThreadProcessId(window,out pid);if(pid==(uint)process.Id){var title=new StringBuilder(256);GetWindowText(window,title,256);if(title.ToString()=="Jago Companion"){found=window;return false;}}return true;},IntPtr.Zero);return found;}
 internal static Process[] Running(string executable){string path=Path.GetFullPath(executable);return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(path)).Where(p=>{try{return !p.HasExited&&p.Id!=Process.GetCurrentProcess().Id&&Path.GetFullPath(p.MainModule.FileName).Equals(path,StringComparison.OrdinalIgnoreCase);}catch{return false;}}).ToArray();}
 internal static bool RequestClose(Process process){try{IntPtr window=MainWindow(process);if(window==IntPtr.Zero)return process.HasExited;IntPtr result;if(SendMessageTimeout(window,QuitMessage,IntPtr.Zero,IntPtr.Zero,2,2500,out result)!=IntPtr.Zero&&result.ToInt32()==1)return process.WaitForExit(6000);if(SendMessageTimeout(window,0x11,IntPtr.Zero,new IntPtr(1),2,2500,out result)!=IntPtr.Zero&&result.ToInt32()==1){SendMessageTimeout(window,0x16,new IntPtr(1),new IntPtr(1),2,2500,out result);return process.WaitForExit(6000);}return process.HasExited;}catch{return false;}}
 internal static void Reveal(Process process){try{var window=MainWindow(process);ShowWindow(window,5);SetForegroundWindow(window);}catch{}}
}
