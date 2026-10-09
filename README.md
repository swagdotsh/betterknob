# betterknob

a script that adds skipping and previous-track actions to your keyboard knobs on windows. i built this because i was sick of opening spotify constantly to change the song while playing black ops 3

## demonstration

<img width="428" height="240" alt="202609122347" src="https://github.com/user-attachments/assets/df250a77-8935-41bd-b435-7a4a31b8cb9f" />
<br>
<img width="546" height="272" alt="image" src="https://github.com/user-attachments/assets/b3fc4f7c-88f1-4a31-a420-ad6be428f100" />

look at the tool running in the taskbar


## usage
1. launch betterknob.exe
2. play music on your media player of choice (spotify, apple music, hell even vlc or windows media player)
3. press the knob once to play/pause, twice to skip, or three times for previous track (or restart the current song)

this should normally be universal across keyboards that have a play/pause button/knob as they all map to the same media key.

## open on startup

right-click the betterknob tray icon (check the hidden icons arrow) and tick **Open on startup**. it will launch when you sign into Windows. untick it to stop launching automatically; exiting the app does not disable startup.

this setting is saved for your Windows account and needs no admin rights. it starts disabled until you enable it. keep the exe in its current location; if you move it, launch it from the new location and enable the option again. if you disable betterknob in Windows Startup Apps, re-enable it there too.

## build instructions

1. before starting ensure it's not already running. no extra frameworks or dependencies are needed, it uses the .NET Framework C# compiler already pre-installed on windows (normally)
2. open PowerShell and run:

```powershell
cd C:\Users\[yourname]\Documents\betterknob
.\build.cmd
```

this script compiles the source, runs gesture and native struct-layout tests, and creates `betterknob.exe`. look for a `Build and self-tests passed`. 

then start it:

```powershell
.\betterknob.exe
```
(or you could just double-click it in your file explorer)

## credits and references

implementation references: [Windows keyboard hooks](https://learn.microsoft.com/en-us/windows/win32/winmsg/lowlevelkeyboardproc), [media key codes](https://learn.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes), and [Microsoft's media input example](https://devblogs.microsoft.com/oldnewthing/20140929-00/?p=43963).
