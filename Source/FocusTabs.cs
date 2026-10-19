// SPDX-License-Identifier: MIT
using System;using System.Windows.Forms;
sealed partial class Companion {
 ToolStripButton allNotesTab,currentNoteTab;bool layingFocus,hostCompact;
 internal bool CompactLayout {get{return IsTabbed?hostCompact:store.Pref.Compact;}}
 internal void SetupFocusTabs(){allNotesTab=new ToolStripButton("All notes");currentNoteTab=new ToolStripButton("Current note");allNotesTab.Click+=(s,e)=>FocusPage(false);currentNoteTab.Click+=(s,e)=>FocusPage(true);InstallFocusTabs();foreach(var strip in new[]{top,quick,format,viewTools,memoTools})if(strip!=null){strip.AutoSize=true;strip.CanOverflow=true;strip.LayoutStyle=ToolStripLayoutStyle.HorizontalStackWithOverflow;}viewTools.SizeChanged+=(s,e)=>UpdateWritingHeader();Resize+=(s,e)=>ApplyFocusLayout();Shown+=(s,e)=>ApplyFocusLayout();}
 internal void InstallFocusTabs(){if(allNotesTab==null)return;quick.Items.Insert(0,currentNoteTab);quick.Items.Insert(0,allNotesTab);allNotesTab.Overflow=currentNoteTab.Overflow=ToolStripItemOverflow.Never;}
 internal void FocusPage(bool focus){store.Pref.FocusNote=focus;ApplyFocusLayout();store.SavePrefs();}
 internal void ApplyFocusLayout(){if(allNotesTab==null||layingFocus)return;layingFocus=true;try{bool narrow=ClientSize.Width<600,hidePage=narrow&&!store.Pref.FocusNote,hideList=!hidePage&&(narrow||store.Pref.FocusNote);split.SuspendLayout();try{if(!hideList&&split.Panel1Collapsed)split.Panel1Collapsed=false;if(!hidePage&&split.Panel2Collapsed)split.Panel2Collapsed=false;if(hideList&&!split.Panel1Collapsed)split.Panel1Collapsed=true;if(hidePage&&!split.Panel2Collapsed)split.Panel2Collapsed=true;}finally{split.ResumeLayout();}UpdateFocusTabs();}finally{layingFocus=false;}}
 internal void UpdateFocusTabs(){if(allNotesTab==null)return;allNotesTab.Checked=!store.Pref.FocusNote;currentNoteTab.Checked=store.Pref.FocusNote;}
}
