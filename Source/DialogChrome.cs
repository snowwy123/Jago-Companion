// SPDX-License-Identifier: MIT
using System;using System.Drawing;using System.Windows.Forms;
sealed partial class Companion {
 internal void DialogChrome(Form form){form.FormBorderStyle=FormBorderStyle.None;form.Padding=new Padding(18,4,18,18);form.Load+=(s,e)=>{var header=new Panel{Dock=DockStyle.Top,Height=40};var title=new Label{Text=form.Text,Dock=DockStyle.Fill,Font=new Font("Segoe UI",12,FontStyle.Bold),TextAlign=ContentAlignment.MiddleLeft,AutoEllipsis=true};var close=new Button{Text="Close",Dock=DockStyle.Right,Width=62,FlatStyle=FlatStyle.Flat};close.Click+=(ss,ee)=>form.Close();title.MouseDown+=(ss,ee)=>{if(ee.Button==MouseButtons.Left){Native.ReleaseCapture();Native.DragWindow(form.Handle);}};header.Controls.Add(title);header.Controls.Add(close);form.Controls.Add(header);ThemeControls(header);};}
}
