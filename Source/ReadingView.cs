// SPDX-License-Identifier: MIT
using System;using System.Drawing;using System.Windows.Forms;
sealed partial class Companion {
 internal void ReadPage(){if(current==null||!SaveNow())return;ReadTextPage(this,current.Title,editor.Text,System.IO.Path.GetDirectoryName(current.Path));}
 internal void ReadTextPage(Form parent,string title,string text,string folder){using(var f=Dialog("Read page · "+title,760,650)){var web=new WebBrowser{Dock=DockStyle.Fill,ScriptErrorsSuppressed=true,AllowWebBrowserDrop=false,IsWebBrowserContextMenuEnabled=false};web.DocumentText=Store.Markdown(text,folder);web.Navigating+=(s,e)=>{if(e.Url!=null&&e.Url.ToString()!="about:blank")e.Cancel=true;};f.Controls.Add(web);ThemeControls(f);f.ShowDialog(parent);}}
}
