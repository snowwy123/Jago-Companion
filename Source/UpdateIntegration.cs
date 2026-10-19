// SPDX-License-Identifier: MIT
using System;using System.IO;using System.Linq;using System.Windows.Forms;using System.Diagnostics;using System.Collections.Generic;
sealed partial class Companion {
 internal bool QuitForUpdate(){if(captureBusy){Tell("Finish or cancel the capture before updating Companion.");return false;}if(!SaveNow())return false;foreach(var memo in memos.ToArray())if(!memo.Flush())return false;try{foreach(var capture in captureEditors.ToArray())if(capture.UnsavedEdits){capture.SavedFile=SaveCapture(capture.image);capture.UnsavedEdits=false;}exiting=true;RememberWindow();BeginInvoke((Action)(()=>{foreach(var capture in captureEditors.ToArray())capture.Close();Close();}));return true;}catch(Exception e){exiting=false;Tell("Could not prepare update: "+e.Message);return false;}}
}
