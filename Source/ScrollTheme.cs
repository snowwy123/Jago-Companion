// SPDX-License-Identifier: MIT
using System;using System.Windows.Forms;using System.Runtime.InteropServices;using System.Collections.Generic;
static class NativeScrollTheme {
 static readonly Dictionary<Control,bool> current=new Dictionary<Control,bool>();
 [DllImport("uxtheme.dll",CharSet=CharSet.Unicode)]static extern int SetWindowTheme(IntPtr window,string theme,string classes);
 [DllImport("uxtheme.dll",EntryPoint="#133")]static extern bool AllowDarkModeForWindow(IntPtr window,bool dark);
 [DllImport("uxtheme.dll",EntryPoint="#135")]static extern int SetPreferredAppMode(int mode);
 static NativeScrollTheme(){try{if(Environment.OSVersion.Version.Build>=18362)SetPreferredAppMode(1);}catch{}}
 static void Set(Control c,bool dark){try{try{AllowDarkModeForWindow(c.Handle,dark);}catch{}SetWindowTheme(c.Handle,dark?"DarkMode_Explorer":"Explorer",null);}catch{}}
 internal static void Apply(Control c,bool dark){if(!(c is TextBoxBase||c is ListBox||c is ScrollableControl))return;if(!current.ContainsKey(c)){c.Disposed+=(s,e)=>current.Remove(c);c.HandleCreated+=(s,e)=>{bool value;if(!c.IsDisposed&&current.TryGetValue(c,out value))Set(c,value);};}current[c]=dark;if(c.IsHandleCreated)Set(c,dark);}
}
