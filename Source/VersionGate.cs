// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Linq;using System.Diagnostics;using System.Collections.Generic;using System.Windows.Forms;
static class VersionGate {
 internal static string OlderExecutable;
 internal static bool CurrentInstance(string executable){
  string shared=Environment.GetEnvironmentVariable("JAGO_SUITE_ROOT")??Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Jago Studio Suite"),folder=Path.Combine(shared,"OpenStudios");
  var records=new List<string>{Path.Combine(Program.Config,"workscape-live.json")};if(Directory.Exists(folder))records.AddRange(Directory.GetFiles(folder,"*.json"));foreach(string file in records)try{
   var data=Store.Json.Deserialize<Dictionary<string,object>>(File.ReadAllText(file));
   if(Convert.ToString(data["studio"])!="companion")continue;
   if(data.ContainsKey("config")&&!Convert.ToString(data["config"]).Equals(Program.Config,StringComparison.OrdinalIgnoreCase))continue;
   using(var p=Process.GetProcessById(Convert.ToInt32(data["pid"]))){
    if(p.HasExited||p.Id==Process.GetCurrentProcess().Id)continue;
    string actual=Path.GetFullPath(p.MainModule.FileName),recorded=Path.GetFullPath(Convert.ToString(data["exe"]));
    if(!actual.Equals(recorded,StringComparison.OrdinalIgnoreCase))continue;
    if(data.ContainsKey("build")&&Convert.ToString(data["build"])==Program.BuildHash)return true;
    OlderExecutable=actual;return false;
   }
  }catch{} // A stale record must not prevent checking the other live windows.
  if(UpdateProtocol.Running(executable).Length==0)return true;
  OlderExecutable=executable;return false;
 }
 internal static bool CloseOlder(string executable){
  string target=OlderExecutable??executable,helper=Path.Combine(Path.GetDirectoryName(executable),"Jago-Update.exe");
  if(!File.Exists(helper)){MessageBox.Show("An older Companion is still running. Save your writing, choose Quit Companion from its tray menu, then open this update again.","Older Companion is open");return false;}
  try{using(var p=Process.Start(new ProcessStartInfo(helper,"--app \""+target+"\""){UseShellExecute=false})){p.WaitForExit();return p.ExitCode==0;}}
  catch(Exception e){MessageBox.Show("Close the older Companion from its tray menu before opening this update.\n\n"+e.Message,"Companion update");return false;}
 }
}
