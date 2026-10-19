# Developing Companion

Run Build.ps1 with the Windows .NET Framework C# compiler installed. It builds the native Companion and a separate tiny Capture launcher and on-demand recording process. No third-party rendering engine is kept running. Brand includes editable SVGs and high-resolution PNGs.

Sources are separated into notes/files, native UI, capture/annotation, floating appearance, Workscape messaging and Library collection export. Data is written as UTF-8 with atomic replacement. Existing external edits produce a recovered copy instead of being overwritten. Shared theme colours match the suite palette.

Run Source/Tests/run-checks.py with Python. The app's --test mode isolates its configuration and home, avoids startup/hotkey changes, and captures only a synthetic test window. --data selects a separate configuration directory. JAGO_SUITE_ROOT isolates integration records. Never run verification against real user homes.

The accompanying Workscape source adds optional native Companion discovery, a narrow panel and release-to-tray acknowledgements. Workscape and Companion exchange local command/snapshot files. An already owned Companion is not taken from another Workscape.

Installer/Companion.iss builds the separate per-user preview installer. Startup is optional. User data lives outside the installed program folder and remains on uninstall. Portable copies use the same local configuration unless --data selects another profile.

## Research references

- ShareX capture/edit workflow: https://getsharex.com/docs/image-editor
- Zettlr writing controls: https://docs.zettlr.com/en/editor/
- Windows global shortcut handling: https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey
- Native screen copying: https://learn.microsoft.com/en-us/dotnet/api/system.drawing.graphics.copyfromscreen
- Future unobscured window capture: https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture

These informed workflow choices; no code was copied from those applications. Native image window capture uses visible pixels. ScreenRecorderLib 7.0.1 (MIT) supplies video recording with Media Foundation; its pinned dependency manifest and licence are in Dependencies/ScreenRecorderLib.

Recording reference: https://github.com/sskodje/ScreenRecorderLib
WASAPI loopback reference: https://learn.microsoft.com/en-us/windows/win32/coreaudio/loopback-recording

Library captures use a shared capture-settings.json in JAGO_SUITE_ROOT. The native capture-inbox command preserves Library's current project. Preview installer identity remains JagoCompanionPreview for upgrades. Never run upgrade checks against an existing user installation; compile isolated test identities and verify retained personal files.

## Preview upgrade and interface checks

The 0.3 preview includes a separate Jago-Update.exe helper. It locates the exact installed executable, requests save-and-quit and avoids forced termination. Older previews can fall back to native session-close messages; failure leaves a clear Quit-and-retry route. Never test installers using the real preview AppId or user data.

Native Windows spelling uses ISpellChecker with installed languages. It is optional, debounced and checks a bounded area around the visible text. Corrections use normal text undo. No dictionaries are downloaded.

The Capture launcher uses a Windows Jump List for taskbar actions and a separate launcher preference file. --no-shell disables shell registration during isolated tests. Main notebook and sticky appearance settings are separate; page titles and task-entry drafts are retained in the home's .jago folder.

References: https://learn.microsoft.com/en-us/windows/win32/api/spellcheck/nf-spellcheck-ispellcheckerfactory-createspellchecker and https://learn.microsoft.com/en-us/dotnet/api/system.windows.shell.jumplist

## Background opacity and menu lifetime

NoteBackground keeps foreground text/editing opaque and fades a separate no-activate background plate. It mirrors window bounds/visibility/pinning, forwards background mouse clicks and wheel input to the underlying editing controls, and disables desktop transparency when tabbed. Rounded sticky regions are mirrored. Do not restore whole-window Opacity for notes: it also fades text. Test editing, movement, capture hiding, focus and tab/detach transitions together.

Editor context menus are reused until the control is disposed. Do not dispose a menu synchronously in Closed: native menu message dispatch can still refer to it. PreparePopup installs one lifetime-managed outside-click watcher per menu. StableToolStrip avoids switching root menus just because another toolbar was hovered.

Native scrollbar appearance uses Windows theme APIs with guarded dark-mode hooks and a fallback on older systems. Verify the actual Windows/DPI setup manually as well as native image checks. Heading/bold/task actions write readable Markdown rather than a separate rich-text format. Text capture annotations are raster edits with undo in the editor.

## Recorder and preview

Jago-Preview.exe hosts the Windows WPF MediaElement on demand, without adding a resident browser runtime. It uses local media codecs and offers an external-player fallback when decoding fails. Build/package/install the player alongside the recorder.

RecordingInk uses a per-pixel layered window: opaque marks above an almost invisible input surface. Draw toggles click-through; the annotation controls are excluded from capture where Windows supports display affinity. Repaint is limited to 30 Hz and active changes. Microphone audio uses the pinned recorder library's default capture source; it stays disabled unless selected. Use only synthetic fixtures during recording tests.

LiveWindows is shared by app capture/recording/attachment. It filters hidden, cloaked, owned, helper and tool windows; each opening refreshes live choices. App-area recording is a fixed desktop rectangle, supporting the ink overlay. It is not a promise to track a moved app.

Resize hooks cover child controls and transparent backdrop edges; locking and tabbed mode disable desktop resizing. Test manual drag and varied-DPI behaviour on actual devices.


## Optional Explorer previews

ExplorerPreview.cs owns the opt-in Space hook. It accepts only Explorer file-list focus without a text caret or keyboard modifiers; COM selection work runs on a background STA thread and verifies the foreground window again before opening. Disabling/unloading releases the hook. QuickFilePreview.cs runs only through the existing Jago-Preview.exe helper with --quick. No extra launcher/installer component is created. WebView2 is pinned under Dependencies/WebView2 and copied by Build.ps1; document controls start only when a PDF or Markdown file is opened. Native settings/notes do not depend on that engine.

Run Source/Tests/run-preview-checks.ps1 -Output <fresh test directory> after Build.ps1. Optional synthetic sample.pdf and seek-fixture.mp4 enable document/video checks. Set JAGO_PREVIEW_EXPLORER_TEST=1 to additionally open, select within and close a test-only Explorer folder. The callback test uses that folder's real focused view without injecting keyboard input into user applications. JAGO_PREVIEW_CACHE isolates document browser storage.

References: Windows ShellFolderView SelectedItems and LowLevelKeyboardProc documentation; QuickLook and PowerToys Peek informed the interaction. No code was imported from their repositories. Office engines, Library/Board actions, custom shortcuts, Desktop and alternative file-manager integration remain future extensions. Current matching targets classic Explorer file-list HWND classes; Windows 11 tab/focus variations need additional real-PC testing.
