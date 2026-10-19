// SPDX-License-Identifier: MIT
using System;
// Keeps slider gestures independent of the asynchronous media decoder.
internal sealed class PreviewSeeking {
 internal bool Pending { get; private set; }
 internal bool Dragging { get; private set; }
 internal double Target { get; private set; }
 internal bool Playing = true;
 internal bool Ended { get; private set; }
 internal string ButtonText { get { return Ended?"Replay":Playing?"Pause":"Play"; } }
 readonly Action pause, resume; readonly Action<double> position; readonly Action<bool> seeking;
 internal PreviewSeeking(Action pause, Action resume, Action<double> position, Action<bool> seeking) { this.pause=pause;this.resume=resume;this.position=position;this.seeking=seeking; }
 internal void Begin() { Dragging=true;Prepare(); }
 void Prepare() { if(Pending)return;Pending=true;seeking(true);pause(); }
 internal void Request(double fraction) { Ended=false;Prepare();Target=Math.Max(0,Math.Min(1,fraction)); }
 internal void Release() { Dragging=false; }
 // First settle tick commits once; the second allows the decoder to settle before audio resumes.
 internal bool Commit() { if(!Pending||Dragging)return false;position(Target);return true; }
 internal void Finish() { if(!Pending||Dragging)return;Pending=false;seeking(false);if(Playing)resume(); }
 internal bool End(bool loop) { if(Pending)return false;Ended=true;Playing=false;if(!loop)return false;Playing=true;Request(0);return true; }
 internal void Toggle() { if(Ended){Playing=true;Request(0);return;}Playing=!Playing;if(!Pending){if(Playing)resume();else pause();} }
 internal void Cancel() { Pending=false;Dragging=false;Playing=false;Ended=false;seeking(false); }
}
