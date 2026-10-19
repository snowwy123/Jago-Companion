// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Linq;using System.Reflection;using System.Text;using System.Runtime.InteropServices;using System.Diagnostics;using System.Windows.Forms;using System.Collections.Generic;
sealed partial class Companion {
 ExplorerPreview previewKeys;Process filePreview;ToolStripMenuItem previewToggle;
 internal void SetupFilePreview(){
  previewToggle=new ToolStripMenuItem("Explorer previews · Space"){Checked=store.Pref.FilePreviewEnabled,CheckOnClick=true};
  previewToggle.CheckedChanged+=(s,e)=>SetFilePreview(previewToggle.Checked);
  if(tray!=null)tray.ContextMenuStrip.Items.Insert(5,previewToggle);
  var menu=new ToolStripDropDownButton("Files");menu.DropDownItems.Add("Preview file…",null,(s,e)=>PickPreviewFile());menu.DropDownItems.Add("Explorer previews…",null,(s,e)=>Settings());top.Items.Insert(Math.Min(4,top.Items.Count),menu);
  SetFilePreview(store.Pref.FilePreviewEnabled);
 }
 internal void SetFilePreview(bool enabled){
  if(previewKeys!=null){previewKeys.Dispose();previewKeys=null;}
  if(enabled){try{previewKeys=new ExplorerPreview(this,paths=>LaunchFilePreview(paths,true));}catch(Exception e){enabled=false;Tell("Explorer preview could not start: "+e.Message);}}
  store.Pref.FilePreviewEnabled=enabled;if(previewToggle!=null&&previewToggle.Checked!=enabled)previewToggle.Checked=enabled;store.SavePrefs();
 }
 internal void PickPreviewFile(){using(var pick=new OpenFileDialog{Title="Preview files",Multiselect=true,Filter="All files (*.*)|*.*"})if(pick.ShowDialog(this)==DialogResult.OK)LaunchFilePreview(pick.FileNames,false);}
 internal void LaunchFilePreview(string[] paths,bool toggle){Guard(()=>{
  if(filePreview!=null){try{if(!filePreview.HasExited){filePreview.CloseMainWindow();if(toggle&&paths.SequenceEqual(lastPreview,StringComparer.OrdinalIgnoreCase))return;}}catch{}finally{filePreview.Dispose();filePreview=null;}}
  paths=paths.Where(File.Exists).Distinct(StringComparer.OrdinalIgnoreCase).Take(128).ToArray();if(paths.Length==0){Tell("Select a local file in Explorer to preview it.");return;}
  var helper=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Jago-Preview.exe");if(!File.Exists(helper)){Tell("The preview helper is missing. Use the complete Companion package.");return;}
  var info=new ProcessStartInfo(helper,"--quick --file "+Quote(paths[0])+" --theme "+Quote(store.Pref.Theme)){UseShellExecute=false};
  info.EnvironmentVariables["JAGO_PREVIEW_CACHE"]=Path.Combine(Program.Config,"FilePreviewCache");string queue=Store.Json.Serialize(paths);if(queue.Length<24000)info.EnvironmentVariables["JAGO_PREVIEW_FILES"]=queue;
  filePreview=Process.Start(info);lastPreview=paths;
 });}
 string[] lastPreview=new string[0];
 internal void StopFilePreview(){if(previewKeys!=null){previewKeys.Dispose();previewKeys=null;}if(filePreview!=null){try{if(!filePreview.HasExited)filePreview.CloseMainWindow();}catch{}filePreview.Dispose();filePreview=null;}}
}
// No file access or COM work is performed in the keyboard hook; Explorer selection is read on the UI queue.
sealed class ExplorerPreview:IDisposable {
 delegate IntPtr Hook(int code,IntPtr w,IntPtr l);readonly Hook callback;readonly Control owner;readonly Action<string[]> open;IntPtr hook;bool held;volatile bool disposed;int querying;
 internal ExplorerPreview(Control owner,Action<string[]> open){this.owner=owner;this.open=open;callback=Key;hook=SetWindowsHookEx(13,callback,GetModuleHandle(null),0);if(hook==IntPtr.Zero)throw new System.ComponentModel.Win32Exception();}
 internal static bool Eligible(string window,string focus,bool caret,bool modifiers){return !caret&&!modifiers&&(window=="CabinetWClass"||window=="ExploreWClass")&&(focus=="DirectUIHWND"||focus=="SysListView32");}
 static string Class(IntPtr h){var b=new StringBuilder(256);GetClassName(h,b,b.Capacity);return b.ToString();}
 IntPtr Key(int code,IntPtr w,IntPtr l){try{if(code>=0&&Marshal.ReadInt32(l)==32){bool up=w.ToInt32()==0x101||w.ToInt32()==0x105;if(up){if(held){held=false;return new IntPtr(1);}}else if(w.ToInt32()==0x100){if(held)return new IntPtr(1);var foreground=GetForegroundWindow();uint pid;uint thread=GetWindowThreadProcessId(foreground,out pid);var info=new GuiInfo{size=Marshal.SizeOf(typeof(GuiInfo))};bool mods=new[]{0x10,0x11,0x12,0x5b,0x5c}.Any(k=>(GetAsyncKeyState(k)&0x8000)!=0);if(GetGUIThreadInfo(thread,ref info)&&Eligible(Class(foreground),Class(info.focus),info.caret!=IntPtr.Zero,mods)){held=true;owner.BeginInvoke((Action)(()=>{if(!disposed&&GetForegroundWindow()==foreground){ReadSelection(foreground);}}));return new IntPtr(1);}}}}catch{}return CallNextHookEx(hook,code,w,l);}
 void ReadSelection(IntPtr foreground){if(System.Threading.Interlocked.CompareExchange(ref querying,1,0)!=0)return;var worker=new System.Threading.Thread(()=>{try{var paths=Selection(foreground);if(!disposed&&!owner.IsDisposed&&paths.Length>0)owner.BeginInvoke((Action)(()=>{if(!disposed&&GetForegroundWindow()==foreground)open(paths);}));}catch{}finally{System.Threading.Interlocked.Exchange(ref querying,0);}});worker.IsBackground=true;worker.SetApartmentState(System.Threading.ApartmentState.STA);worker.Start();}
 internal static string[] Selection(IntPtr foreground){var objects=new List<object>();try{object shell=Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));objects.Add(shell);object windows=Get(shell,"Windows",true);objects.Add(windows);int count=Convert.ToInt32(Get(windows,"Count",false));for(int i=0;i<count;i++){object window=windows.GetType().InvokeMember("Item",BindingFlags.InvokeMethod,null,windows,new object[]{i});objects.Add(window);if(new IntPtr(Convert.ToInt64(Get(window,"HWND",false)))!=foreground)continue;object document=Get(window,"Document",false);objects.Add(document);object selected=Get(document,"SelectedItems",true);objects.Add(selected);var paths=new List<string>();int n=Math.Min(128,Convert.ToInt32(Get(selected,"Count",false)));for(int j=0;j<n;j++){object item=selected.GetType().InvokeMember("Item",BindingFlags.InvokeMethod,null,selected,new object[]{j});objects.Add(item);string path=Convert.ToString(Get(item,"Path",false));if(File.Exists(path))paths.Add(Path.GetFullPath(path));}return paths.ToArray();}}catch{}finally{for(int i=objects.Count-1;i>=0;i--)if(objects[i]!=null&&Marshal.IsComObject(objects[i]))try{Marshal.ReleaseComObject(objects[i]);}catch{}}return new string[0];}
 static object Get(object o,string name,bool method){return o.GetType().InvokeMember(name,method?BindingFlags.InvokeMethod:BindingFlags.GetProperty,null,o,null);}
 public void Dispose(){disposed=true;held=false;if(hook!=IntPtr.Zero){UnhookWindowsHookEx(hook);hook=IntPtr.Zero;}}
 [StructLayout(LayoutKind.Sequential)]struct Rect{public int l,t,r,b;}
 [StructLayout(LayoutKind.Sequential)]struct GuiInfo{public int size,flags;public IntPtr active,focus,capture,menu,move,caret;public Rect rect;}
 [DllImport("user32.dll")]static extern IntPtr SetWindowsHookEx(int id,Hook proc,IntPtr module,uint thread);
 [DllImport("user32.dll")]static extern bool UnhookWindowsHookEx(IntPtr hook);
 [DllImport("user32.dll")]static extern IntPtr CallNextHookEx(IntPtr hook,int code,IntPtr w,IntPtr l);
 [DllImport("kernel32.dll",CharSet=CharSet.Auto)]static extern IntPtr GetModuleHandle(string name);
 [DllImport("user32.dll")]static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]static extern uint GetWindowThreadProcessId(IntPtr h,out uint pid);
 [DllImport("user32.dll")]static extern bool GetGUIThreadInfo(uint thread,ref GuiInfo info);
 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetClassName(IntPtr h,StringBuilder name,int count);
 [DllImport("user32.dll")]static extern short GetAsyncKeyState(int key);
}
