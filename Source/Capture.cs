// SPDX-License-Identifier: MIT

using System;using System.IO;using System.Linq;using System.Drawing;using System.Drawing.Imaging;using System.Drawing.Drawing2D;using System.Windows.Forms;using System.Runtime.InteropServices;using System.Threading.Tasks;using System.Collections.Generic;using System.Text;

static class Native {
 [DllImport("user32.dll")]internal static extern bool ReleaseCapture();
 [DllImport("user32.dll",EntryPoint="SendMessage")]static extern IntPtr SendRaw(IntPtr h,int m,IntPtr w,IntPtr l);internal static void TypeForCheck(TextBoxBase box,string text){foreach(char c in text)SendRaw(box.Handle,0x102,new IntPtr(c),new IntPtr(1));}internal static void DragWindow(IntPtr h){SendRaw(h,0xA1,new IntPtr(2),IntPtr.Zero);}

 [DllImport("user32.dll")]internal static extern bool ShowWindow(IntPtr h,int command);

 [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,int message,IntPtr value,string text);internal static void Cue(TextBoxBase box,string text){var rich=box as SpellTextBox;if(rich!=null){rich.Placeholder=text;rich.Invalidate();return;}SendMessage(box.Handle,0x1501,new IntPtr(1),text);}



 [StructLayout(LayoutKind.Sequential)]internal struct RECT {internal int Left,Top,Right,Bottom;internal Rectangle Rect {get{return Rectangle.FromLTRB(Left,Top,Right,Bottom);}}}

 internal delegate bool EnumProc(IntPtr h,IntPtr p);

 [DllImport("user32.dll")]internal static extern bool RegisterHotKey(IntPtr h,int id,uint mod,uint key);

 [DllImport("user32.dll")]internal static extern bool UnregisterHotKey(IntPtr h,int id);

 [DllImport("user32.dll")]internal static extern IntPtr GetForegroundWindow();
 [DllImport("user32.dll")]internal static extern uint GetWindowThreadProcessId(IntPtr h,out uint process);

 [DllImport("user32.dll")]internal static extern bool GetWindowRect(IntPtr h,out RECT r);

 [DllImport("user32.dll")]internal static extern bool GetClientRect(IntPtr h,out RECT r);

 [DllImport("user32.dll")]internal static extern bool ClientToScreen(IntPtr h,ref Point p);

 [DllImport("user32.dll")]internal static extern bool IsWindowVisible(IntPtr h);

 [DllImport("user32.dll")]internal static extern bool IsIconic(IntPtr h);

 [DllImport("user32.dll")]internal static extern bool IsWindow(IntPtr h);

 [DllImport("user32.dll",CharSet=CharSet.Unicode)]internal static extern int GetWindowText(IntPtr h,StringBuilder b,int n);

 [DllImport("user32.dll")]internal static extern bool EnumWindows(EnumProc callback,IntPtr p);

 [DllImport("user32.dll")]internal static extern bool SetForegroundWindow(IntPtr h);

 [DllImport("user32.dll")]internal static extern IntPtr SetParent(IntPtr h,IntPtr parent);

 [DllImport("user32.dll",EntryPoint="SetWindowLongPtr")]internal static extern IntPtr SetStyle(IntPtr h,int n,IntPtr v);

 [DllImport("user32.dll")]internal static extern bool MoveWindow(IntPtr h,int x,int y,int w,int height,bool paint);

 [DllImport("user32.dll")]internal static extern IntPtr GetParent(IntPtr h);

}

sealed partial class Companion {

 internal bool captureBusy;Rectangle lastRegion;IntPtr lastWindow;string lastKind;internal readonly List<int> hotkeys=new List<int>();

 internal static Keys ParseShortcut(string text){if(String.IsNullOrWhiteSpace(text))return Keys.None;var keys=(Keys)Enum.Parse(typeof(Keys),text);var code=keys&Keys.KeyCode;if(code==Keys.None||(keys&(Keys.Control|Keys.Alt|Keys.Shift))==0)throw new Exception("Use a modifier such as Ctrl+Alt with a letter or function key.");return keys;}

 internal void RegisterShortcuts(){ReleaseHotkeys();if(Program.Test!=null||Environment.GetEnvironmentVariable("JAGO_WORKSPACE_TEST_ROOT")!=null)return;var missing=new List<string>();var pairs=new[]{Tuple.Create(1,store.Pref.NoteKey,"Quick note"),Tuple.Create(2,store.Pref.RegionKey,"Capture region"),Tuple.Create(3,store.Pref.WindowKey,"Capture window")};foreach(var pair in pairs){try{Keys k=ParseShortcut(pair.Item2);if(k==Keys.None)continue;uint m=0x4000;if((k&Keys.Control)!=0)m|=2;if((k&Keys.Alt)!=0)m|=1;if((k&Keys.Shift)!=0)m|=4;if(Native.RegisterHotKey(Handle,pair.Item1,m,(uint)(k&Keys.KeyCode)))hotkeys.Add(pair.Item1);else missing.Add(pair.Item3);}catch{missing.Add(pair.Item3);}}if(missing.Count>0)Tell("Shortcut unavailable: "+String.Join(", ",missing)+". Change it in Settings; buttons still work.");}

 internal void ReleaseHotkeys(){foreach(int k in hotkeys)Native.UnregisterHotKey(Handle,k);hotkeys.Clear();}

 protected override void WndProc(ref Message m){if((uint)m.Msg==UpdateProtocol.QuitMessage){m.Result=new IntPtr(QuitForUpdate()?1:0);return;}if(m.Msg==0x312){int id=m.WParam.ToInt32();if(id==1)NewNote("Quick note","");else if(id==2)BeginCapture("region");else if(id==3)BeginCapture("active");return;}if(m.Msg==0x84&&store!=null&&!store.Pref.Locked&&!IsTabbed&&FormBorderStyle==FormBorderStyle.None){base.WndProc(ref m);long xy=m.LParam.ToInt64();Point p=PointToClient(new Point((short)(xy&0xffff),(short)((xy>>16)&0xffff)));int edge=7;if(p.Y>=ClientSize.Height-edge)m.Result=new IntPtr(p.X<=edge?16:p.X>=ClientSize.Width-edge?17:15);else if(p.X<=edge)m.Result=new IntPtr(10);else if(p.X>=ClientSize.Width-edge)m.Result=new IntPtr(11);return;}if(m.Msg==0x84&&store!=null&&store.Pref.Locked&&!IsTabbed){base.WndProc(ref m);int hit=m.Result.ToInt32();if(hit==2||hit>=10&&hit<=17)m.Result=new IntPtr(1);return;}base.WndProc(ref m);}

 internal static Bitmap ScreenShot(Rectangle area){area=Rectangle.Intersect(SystemInformation.VirtualScreen,area);if(area.Width<2||area.Height<2||((long)area.Width*area.Height)>40000000)throw new Exception("Choose a visible area under 40 million pixels.");var b=new Bitmap(area.Width,area.Height,PixelFormat.Format32bppArgb);try{using(var g=Graphics.FromImage(b))g.CopyFromScreen(area.Location,Point.Empty,area.Size,CopyPixelOperation.SourceCopy);return b;}catch{b.Dispose();throw;}}

 internal bool IsOwnWindow(IntPtr h){uint pid;Native.GetWindowThreadProcessId(h,out pid);return pid==(uint)System.Diagnostics.Process.GetCurrentProcess().Id;}
 internal Rectangle WindowRect(IntPtr h){if(h==IntPtr.Zero||!Native.IsWindow(h)||Native.IsIconic(h))throw new Exception("Choose an open, visible application window. Restore minimised windows first.");Native.RECT r;if(store.Pref.Frame){Native.GetWindowRect(h,out r);return Rectangle.Intersect(r.Rect,SystemInformation.VirtualScreen);}Native.GetClientRect(h,out r);Point p=Point.Empty;Native.ClientToScreen(h,ref p);return Rectangle.Intersect(new Rectangle(p,r.Rect.Size),SystemInformation.VirtualScreen);}

 internal IntPtr PickWindow(){using(var f=Dialog("Capture an application",460,230)){var picker=new LiveAppPicker{Dock=DockStyle.Top};var use=new Button{Text="Capture selected app",Dock=DockStyle.Bottom,Height=38,DialogResult=DialogResult.OK};f.Controls.Add(picker);f.Controls.Add(use);ThemeControls(f);return f.ShowDialog(this)==DialogResult.OK?picker.Chosen:IntPtr.Zero;}}
 internal async void BeginCapture(string kind){if(kind=="configured")kind=ConfiguredCaptureAction();if(kind=="video"){VideoSnippet();return;}if(kind=="chooser"){CaptureHome();return;}await Capture(kind);}
 internal new async Task Capture(string kind){if(captureBusy)return;captureBusy=true;if(!new[]{"region","edit-region","window","active","screen","repeat","area","remembered-window"}.Contains(kind)){captureBusy=false;return;}bool edit=kind.StartsWith("edit-");if(edit)kind=kind.Substring(5);bool visible=Visible,tabbed=IsTabbed,hadResult=false;IntPtr foreground=Native.GetForegroundWindow();try{if(kind=="repeat"){if(lastKind==null)throw new Exception("Make a capture first, then Repeat remembers that region or window.");kind=lastKind;}IntPtr target=kind=="window"?PickWindow():kind=="active"?(IsOwnWindow(foreground)?lastWindow:foreground):kind=="remembered-window"?lastWindow:IntPtr.Zero;if(kind=="window"&&target==IntPtr.Zero)return;if(!SaveNow())return;if(!tabbed)Hide();if(captureButton!=null)captureButton.Hide();await Task.Delay(180);if(target!=IntPtr.Zero){LiveWindows.BringForward(target);await Task.Delay(250);}if(store.Pref.Delay>0)await Task.Delay(store.Pref.Delay*1000);Bitmap result=null;if(kind=="region"){using(var screen=ScreenShot(SystemInformation.VirtualScreen))using(var selection=new RegionPicker(screen,SystemInformation.VirtualScreen)){if(selection.ShowDialog()==DialogResult.OK){lastRegion=selection.RegionArea;result=screen.Clone(new Rectangle(lastRegion.X-SystemInformation.VirtualScreen.X,lastRegion.Y-SystemInformation.VirtualScreen.Y,lastRegion.Width,lastRegion.Height),PixelFormat.Format32bppArgb);lastKind="area";}}}else if(kind=="area"){result=ScreenShot(lastRegion);}else if(kind=="screen"){result=Program.Test!=null&&File.Exists(Path.Combine(Program.Test,"capture-fixture.png"))?new Bitmap(Path.Combine(Program.Test,"capture-fixture.png")):ScreenShot(SystemInformation.VirtualScreen);lastKind="screen";}else{if(target==IntPtr.Zero)target=lastWindow;if(target==Handle)throw new Exception("Choose the application you want to capture, or use its active-window shortcut.");lastWindow=target;result=ScreenShot(WindowRect(target));lastKind="remembered-window";}if(result!=null){hadResult=true;using(result){if(!edit&&store.Pref.AfterCapture!="Preview and annotate"){string file=store.Pref.AfterCapture!="Copy only"?SaveCapture(result):null;Clipboard.SetImage(result);if(file!=null)CaptureReady(file);Tell(file==null?"Capture copied to clipboard":"Capture saved and copied · "+Path.GetFileName(file));if(store.Pref.CaptureNotice&&Program.Test==null)tray.ShowBalloonTip(2500,"Capture ready",file==null?"Image copied to your clipboard.":"Image saved locally and copied to your clipboard.",ToolTipIcon.Info);}else{string file=SaveCapture(result);lastCapture=file;Clipboard.SetImage(result);var preview=new CaptureEditor(result,this,file);preview.Show();}}}}catch(Exception e){Tell(e.Message);if(tray!=null&&Program.Test==null)tray.ShowBalloonTip(4000,"Capture needs a check",e.Message,ToolTipIcon.Info);}finally{captureBusy=false;if(captureButton!=null&&store.Pref.CaptureButton)captureButton.Show();if(!tabbed&&visible&&!hadResult)Show();}}

 internal string SaveCapture(Bitmap image){string folder=Path.Combine(CaptureRoot,DateTime.Now.ToString("yyyy-MM"));Directory.CreateDirectory(folder);string file=Path.Combine(folder,"Capture-"+DateTime.Now.ToString("yyyyMMdd-HHmmss-fff")+"-"+Guid.NewGuid().ToString("N").Substring(0,6)+(store.Pref.CaptureFormat=="JPEG"?".jpg":".png"));image.Save(file,store.Pref.CaptureFormat=="JPEG"?ImageFormat.Jpeg:ImageFormat.Png);Tell("Capture saved · "+file);return file;}

 internal void CaptureShelf(){using(var f=Dialog("Your captures",760,570)){var list=new ListBox{Dock=DockStyle.Left,Width=280};var preview=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};var files=SafeFiles(CaptureRoot,"*",0).Where(x=>new[]{".png",".jpg",".jpeg",".mp4"}.Contains(Path.GetExtension(x).ToLowerInvariant())).OrderByDescending(File.GetLastWriteTimeUtc).Take(500).ToArray();foreach(string file in files)list.Items.Add(Path.GetFileName(file));Point dragStart=Point.Empty;list.MouseDown+=(s,e)=>dragStart=e.Location;list.MouseMove+=(s,e)=>{if(e.Button==MouseButtons.Left&&list.SelectedIndex>=0&&Math.Abs(e.X-dragStart.X)+Math.Abs(e.Y-dragStart.Y)>8){list.DoDragDrop(new DataObject(DataFormats.FileDrop,new[]{files[list.SelectedIndex]}),DragDropEffects.Copy);dragStart=e.Location;}};list.DoubleClick+=(s,e)=>{if(list.SelectedIndex>=0){string selected=files[list.SelectedIndex];if(Path.GetExtension(selected)==".mp4")Open(selected);else using(var img=new Bitmap(selected))using(var edit=new CaptureEditor(img,this))edit.ShowDialog();}};list.SelectedIndexChanged+=(s,e)=>{if(preview.Image!=null){preview.Image.Dispose();preview.Image=null;}if(list.SelectedIndex>=0&&Path.GetExtension(files[list.SelectedIndex])!=".mp4")using(var img=Image.FromFile(files[list.SelectedIndex]))preview.Image=new Bitmap(img);};var row=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=90,AutoScroll=true};var add=new Button{Text="Attach to current note",AutoSize=true};add.Click+=(s,e)=>{if(list.SelectedIndex<0)return;EnsureNote();editor.AppendText("\n"+store.AttachmentLink(current,CaptureAttachment(files[list.SelectedIndex]))+"\n");SaveNow();f.Close();SetView("Notes");};row.Controls.Add(add);var open=new Button{Text="Open captures folder",AutoSize=true};open.Click+=(s,e)=>Open(CaptureRoot);row.Controls.Add(open);var reveal=new Button{Text="Show selected file",AutoSize=true};reveal.Click+=(s,e)=>{if(list.SelectedIndex>=0)Reveal(files[list.SelectedIndex]);};row.Controls.Add(reveal);var play=new Button{Text="Open / play",AutoSize=true};play.Click+=(s,e)=>{if(list.SelectedIndex>=0)Open(files[list.SelectedIndex]);};row.Controls.Add(play);var path=new Button{Text="Copy folder for Library",AutoSize=true};path.Click+=(s,e)=>Clipboard.SetText(CaptureRoot);row.Controls.Add(path);var library=new Button{Text="Open in Library",AutoSize=true};library.Click+=(s,e)=>OpenCapturesLibrary();row.Controls.Add(library);f.Controls.Add(preview);f.Controls.Add(list);f.Controls.Add(row);ThemeControls(f);f.ShowDialog(this);if(preview.Image!=null)preview.Image.Dispose();}}

}

sealed class RegionPicker:Form {readonly Bitmap screen;internal readonly Rectangle virtualScreen;Point start,end;bool down;internal Rectangle RegionArea;

 internal RegionPicker(Bitmap image,Rectangle bounds){screen=image;virtualScreen=bounds;FormBorderStyle=FormBorderStyle.None;StartPosition=FormStartPosition.Manual;AutoScaleMode=AutoScaleMode.None;Bounds=bounds;Shown+=(s,e)=>{Bounds=virtualScreen;Activate();};TopMost=true;ShowInTaskbar=false;Cursor=Cursors.Cross;DoubleBuffered=true;KeyPreview=true;MouseDown+=(s,e)=>{if(e.Button==MouseButtons.Right){DialogResult=DialogResult.Cancel;Close();return;}start=end=e.Location;down=true;Invalidate();};MouseMove+=(s,e)=>{if(down){end=e.Location;Invalidate();}};MouseUp+=(s,e)=>{end=e.Location;var r=Selection();if(r.Width>=2&&r.Height>=2){RegionArea=new Rectangle(r.X+bounds.X,r.Y+bounds.Y,r.Width,r.Height);DialogResult=DialogResult.OK;Close();}};KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;Close();}};}

 Rectangle Selection(){return Rectangle.FromLTRB(Math.Min(start.X,end.X),Math.Min(start.Y,end.Y),Math.Max(start.X,end.X),Math.Max(start.Y,end.Y));}

 protected override void OnPaint(PaintEventArgs e){e.Graphics.DrawImageUnscaled(screen,0,0);using(var shade=new SolidBrush(Color.FromArgb(120,0,0,0)))e.Graphics.FillRectangle(shade,ClientRectangle);var r=Selection();if(r.Width>0&&r.Height>0){e.Graphics.DrawImage(screen,r,r,GraphicsUnit.Pixel);using(var pen=new Pen(Color.FromArgb(145,217,244),2))e.Graphics.DrawRectangle(pen,r);TextRenderer.DrawText(e.Graphics,r.Width+" × "+r.Height,Font,new Point(r.Left+4,Math.Max(4,r.Top-28)),Color.White,Color.FromArgb(32,35,41));}TextRenderer.DrawText(e.Graphics,"Drag to capture · Esc cancels",new Font("Segoe UI",13),new Point(24,24),Color.White,Color.FromArgb(32,35,41));}

}

sealed partial class CaptureEditor:Form {internal Bitmap image;readonly Companion owner;readonly List<Bitmap> history=new List<Bitmap>();readonly PictureBox canvas=new PictureBox{Dock=DockStyle.Fill,SizeMode=PictureBoxSizeMode.Zoom};internal readonly LinkLabel hint=new LinkLabel{Dock=DockStyle.Bottom,Height=30};internal string tool="Arrow";Point start,last,end;bool down;Color color=Color.FromArgb(205,83,47);readonly ToolStrip bar=new StableToolStrip{GripStyle=ToolStripGripStyle.Hidden,LayoutStyle=ToolStripLayoutStyle.Flow};

 internal bool UnsavedEdits;internal string SavedFile;
 internal CaptureEditor(Bitmap source,Companion parent,string savedFile=null){SavedFile=savedFile;parent.captureEditors.Add(this);owner=parent;image=new Bitmap(source);Text="Capture · annotate, copy or save";Size=new Size(950,720);MinimumSize=new Size(640,450);StartPosition=FormStartPosition.CenterScreen;Font=parent.Font;Icon=parent.Icon;var tools=new ThemeMenuChoice{DropDownStyle=ComboBoxStyle.DropDownList};tools.Items.AddRange(new object[]{"Arrow","Pen","Rectangle","Highlight","Solid redact","Crop","Text"});tools.SelectedItem=tool;tools.SelectedIndexChanged+=(s,e)=>{CommitTextDraft();tool=tools.Text;hint.Text=tool=="Solid redact"?"Solid redaction is flattened into saved/copied pixels. Undo remains available here.":"Drag on the image · "+tool;};bar.Items.Add(tools);Add("Colour",()=>{var picked=owner.PickColour(this,color);if(picked.HasValue)color=picked.Value;});Add("Undo",Undo);Add("Select / copy text",()=>{CommitTextDraft();new ImageTextWindow(image,owner).Show();});Add("Copy image",()=>{Clipboard.SetImage(image);hint.Text="Copied image to clipboard";});Add("Show in folder",()=>{if(SavedFile==null){SavedFile=owner.SaveCapture(image);UnsavedEdits=false;}owner.Reveal(SavedFile);});Add("Library",owner.OpenCapturesLibrary);Add("Open notebook",owner.ShowWindow);Add("Save edited copy",()=>{string f=owner.SaveCapture(image);SavedFile=f;UnsavedEdits=false;owner.lastCapture=f;hint.Text="Saved · "+Path.GetFileName(f);});Add("Save & attach",()=>{string file=owner.SaveCapture(image);SavedFile=file;UnsavedEdits=false;owner.lastCapture=file;owner.EnsureNote();owner.editor.AppendText("\n"+owner.store.AttachmentLink(owner.current,owner.CaptureAttachment(file))+"\n");owner.SaveNow();Close();owner.ShowWindow();});Add("Save as…",()=>{using(var pick=new SaveFileDialog{Filter="PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg",FileName="Capture.png"})if(pick.ShowDialog(this)==DialogResult.OK){image.Save(pick.FileName,pick.FilterIndex==2?ImageFormat.Jpeg:ImageFormat.Png);SavedFile=pick.FileName;UnsavedEdits=false;owner.lastCapture=pick.FileName;hint.Text="Saved · "+Path.GetFileName(SavedFile);}});Add("Float image reference",()=>{var pin=new ImagePin(image,parent);pin.Show();});hint.Cursor=Cursors.Hand;hint.Click+=(s,e)=>{if(SavedFile==null)SavedFile=owner.SaveCapture(image);owner.Reveal(SavedFile);};Controls.Add(canvas);Controls.Add(hint);Controls.Add(bar);bar.Dock=DockStyle.Top;canvas.Image=image;canvas.MouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Left||!ImageBox().Contains(e.Location))return;if(tool=="Text"){if(textDraft!=null&&TextDraftBox().Contains(e.Location)){movingText=true;textDragOffset=new Point(ToImage(e.Location).X-textAt.X,ToImage(e.Location).Y-textAt.Y);canvas.Capture=true;}else AddText(ToImage(e.Location));return;}Push();start=last=end=ToImage(e.Location);down=true;};canvas.MouseMove+=(s,e)=>{if(movingText){var point=ToImage(e.Location);textAt=new Point(Math.Max(0,point.X-textDragOffset.X),Math.Max(0,point.Y-textDragOffset.Y));canvas.Invalidate();return;}if(!down)return;end=ToImage(e.Location);if(tool=="Pen"){using(var g=Graphics.FromImage(image))using(var p=new Pen(color,Math.Max(2,image.Width/350f))){p.StartCap=p.EndCap=LineCap.Round;g.DrawLine(p,last,end);}last=end;}canvas.Invalidate();};canvas.MouseUp+=(s,e)=>{if(movingText){movingText=false;canvas.Capture=false;return;}if(!down)return;down=false;end=ToImage(e.Location);Apply(start,end);canvas.Image=image;canvas.Invalidate();};canvas.Paint+=(s,e)=>{if(down&&tool!="Pen"){Point a=FromImage(start),b=FromImage(end);using(var p=new Pen(color,2)){if(tool=="Arrow"){p.CustomEndCap=new AdjustableArrowCap(4,6);e.Graphics.DrawLine(p,a,b);}else e.Graphics.DrawRectangle(p,Rect(a,b));}}};canvas.Paint+=(s,e)=>PaintTextDraft(e.Graphics);FormClosing+=(s,e)=>CommitTextDraft();FormClosed+=(s,e)=>{parent.captureEditors.Remove(this);canvas.Image=null;image.Dispose();foreach(var old in history)old.Dispose();};parent.ThemeControls(this);hint.Text=SavedFile==null?"Copy or save when ready · Nothing uploads":"Saved · "+Path.GetFileName(SavedFile)+" · Click to show in folder";}

 void Add(string label,Action a){var b=new ToolStripButton(label);if(label=="Save edited copy")b.ToolTipText="Save another image in your captures folder. The original stays available.";if(label=="Float image reference")b.ToolTipText="Keep a separate image window on top while you work. Close it to dismiss.";b.Click+=(s,e)=>{try{if(label=="Undo")CancelTextDraft();else CommitTextDraft();a();}catch(Exception ex){hint.Text=ex.Message;}};bar.Items.Add(b);}

 Rectangle ImageBox(){double s=Math.Min((double)canvas.Width/image.Width,(double)canvas.Height/image.Height);int w=(int)(image.Width*s),h=(int)(image.Height*s);return new Rectangle((canvas.Width-w)/2,(canvas.Height-h)/2,w,h);}

 Point ToImage(Point p){var r=ImageBox();return new Point(Math.Max(0,Math.Min(image.Width-1,(int)((p.X-r.X)*(double)image.Width/r.Width))),Math.Max(0,Math.Min(image.Height-1,(int)((p.Y-r.Y)*(double)image.Height/r.Height))));}

 Point FromImage(Point p){var r=ImageBox();return new Point(r.X+(int)(p.X*(double)r.Width/image.Width),r.Y+(int)(p.Y*(double)r.Height/image.Height));}

 static Rectangle Rect(Point a,Point b){return Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));}

 internal void Push(){UnsavedEdits=true;while(history.Count>0&&(history.Count>=8||((long)image.Width*image.Height*(history.Count+1))>50000000)){history[0].Dispose();history.RemoveAt(0);}history.Add(new Bitmap(image));}

 internal void Undo(){if(history.Count==0)return;image.Dispose();image=history[history.Count-1];history.RemoveAt(history.Count-1);canvas.Image=image;canvas.Invalidate();}

 internal void Apply(Point a,Point b){Rectangle r=Rect(a,b);if(tool=="Crop"){if(r.Width<2||r.Height<2)return;var next=image.Clone(r,PixelFormat.Format32bppArgb);image.Dispose();image=next;return;}using(var g=Graphics.FromImage(image)){g.SmoothingMode=SmoothingMode.AntiAlias;if(tool=="Solid redact"){using(var brush=new SolidBrush(Color.Black))g.FillRectangle(brush,r);}else if(tool=="Highlight"){using(var brush=new SolidBrush(Color.FromArgb(85,color)))g.FillRectangle(brush,r);}else if(tool=="Rectangle"){using(var p=new Pen(color,Math.Max(2,image.Width/350f)))g.DrawRectangle(p,r);}else if(tool=="Arrow"){using(var p=new Pen(color,Math.Max(3,image.Width/300f))){p.CustomEndCap=new AdjustableArrowCap(4,6);g.DrawLine(p,a,b);}}}}

 protected override bool ProcessCmdKey(ref Message msg,Keys key){if(key==Keys.Escape&&textDraft!=null){CancelTextDraft();return true;}if(key==(Keys.Control|Keys.Enter)&&textDraft!=null){CommitTextDraft();return true;}if(key==(Keys.Control|Keys.Z)){CancelTextDraft();Undo();return true;}if(key==(Keys.Control|Keys.C)&&textDraft==null){Clipboard.SetImage(image);return true;}return base.ProcessCmdKey(ref msg,key);}

}

sealed class ImagePin:Form {readonly Image copy;internal ImagePin(Image source,Companion parent){copy=new Bitmap(source);Text="Pinned capture · close to dismiss";Size=new Size(Math.Min(600,source.Width+20),Math.Min(600,source.Height+50));TopMost=true;Icon=parent.Icon;var p=new PictureBox{Dock=DockStyle.Fill,Image=copy,SizeMode=PictureBoxSizeMode.Zoom};Controls.Add(p);FormClosed+=(s,e)=>copy.Dispose();}}
