// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Diagnostics;using System.Windows.Forms;
sealed partial class Companion {
 internal void VideoSnippet(){Guard(()=>{if(!SaveNow())return;string helper=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Jago-Recorder.exe");if(!File.Exists(helper)){Tell("Video recorder is missing. Use the complete Companion preview package.");return;}if(captureButton!=null)captureButton.Hide();var recording=Process.Start(new ProcessStartInfo(helper,"--folder "+Quote(CaptureRoot)+" --pipe "+Quote(Program.Pipe)+" --theme "+Quote(store.Pref.Theme)){UseShellExecute=false});recording.EnableRaisingEvents=true;recording.Exited+=(s,e)=>{if(!IsDisposed&&IsHandleCreated)BeginInvoke((Action)(()=>{if(store.Pref.CaptureButton)ShowCaptureButton();}));};if(!IsTabbed)Hide();});}
 internal void PreviewVideo(string file){string helper=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Jago-Preview.exe");if(!File.Exists(helper)){Open(file);return;}if(captureButton!=null)captureButton.Hide();var recording=Process.Start(new ProcessStartInfo(helper,"--file "+Quote(file)+" --theme "+Quote(store.Pref.Theme)+" --pipe "+Quote(Program.Pipe)){UseShellExecute=false});}
 internal static string Quote(string value){return "\""+value.Replace("\"","")+"\"";}
}
