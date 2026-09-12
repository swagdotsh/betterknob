# BetterKnob

a script that adds skipping and previous-track actions to your keyboard knobs on windows. i built this because i was sick of opening spotify all the time playing black ops 3

## usage
1. launch **betterknob.exe**.
2. play music on your media player of choice (spotify, apple music, hell even vlc or windows media player)
3. press the knob once to play/pause, twice to skip, or three times for previous track (or restart the current song)

this should normally be universal across keyboards that have a play/pause button/knob as they all map to the same media key.

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
