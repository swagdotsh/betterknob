using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("betterknob")]
[assembly: System.Reflection.AssemblyProduct("betterknob")]
[assembly: System.Reflection.AssemblyDescription("Multi-press controls for keyboard media knobs")]

public sealed class Gesture
{
    public int Window = 350;
    public bool Held;
    int count;
    long deadline;
    public readonly Queue<int> Ready = new Queue<int>();
    public void Down(long now) { if (Held) return; Poll(now); Held = true; count++; }
    public void Up(long now) {
        if (!Held) return;
        Held = false;
        deadline = now + Window;
        if (count == 3) Finish();
    }
    public void Poll(long now) { if (!Held && count > 0 && now >= deadline) Finish(); }
    void Finish() { Ready.Enqueue(count); count = 0; }
}

static class Program
{
    [STAThread] static int Main(string[] args)
    {
        if (Array.IndexOf(args, "--self-test") >= 0) return SelfTest();
        bool created;
        using (var mutex = new Mutex(true, "Local\\KnobCrownUtility", out created)) {
            if (!created) { MessageBox.Show("betterknob is already running. Look in the system tray."); return 0; }
            Application.EnableVisualStyles();
            try { using (var app = new BetterKnobContext()) Application.Run(app); }
            catch (Exception e) { MessageBox.Show(e.Message, "betterknob", MessageBoxButtons.OK, MessageBoxIcon.Error); return 1; }
        }
        return 0;
    }
    static void Expect(bool value) { if (!value) throw new Exception("Gesture test failed"); }
    static int SelfTest()
    {
        try {
            var g = new Gesture();
            g.Down(0); g.Up(30); g.Poll(379); Expect(g.Ready.Count == 0);
            g.Poll(380); Expect(g.Ready.Dequeue() == 1);
            g.Down(500); g.Up(520); g.Down(650); g.Up(670); g.Poll(1020); Expect(g.Ready.Dequeue() == 2);
            g.Down(1100); g.Up(1120); g.Down(1200); g.Up(1220); g.Down(1300); g.Up(1320); Expect(g.Ready.Dequeue() == 3);
            g.Down(1400); g.Down(1500); g.Poll(2000); Expect(g.Ready.Count == 0);
            g.Up(2100); g.Poll(2450); Expect(g.Ready.Dequeue() == 1);
            g.Down(2600); g.Up(2620); g.Down(3100); Expect(g.Ready.Dequeue() == 1);
            g.Up(3120); g.Poll(3470); Expect(g.Ready.Dequeue() == 1);
            g.Up(3500); g.Poll(3900); Expect(g.Ready.Count == 0);
            Expect(Marshal.SizeOf(typeof(Native.Input)) == (IntPtr.Size == 8 ? 40 : 28));
            return 0;
        } catch { return 1; }
    }
}

sealed class BetterKnobContext : ApplicationContext
{
    readonly Gesture gesture = new Gesture();
    readonly Stopwatch watch = Stopwatch.StartNew();
    readonly Native.HookProc callback;
    readonly System.Windows.Forms.Timer timer;
    readonly NotifyIcon tray;
    readonly ContextMenuStrip menu;
    readonly ToolStripMenuItem status;
    readonly NativeWindow commandWindow;
    IntPtr hook;
    public BetterKnobContext()
    {
        callback = OnKey;
        hook = Native.SetWindowsHookEx(13, callback, Native.GetModuleHandle(null), 0);
        if (hook == IntPtr.Zero) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
        commandWindow = new NativeWindow();
        commandWindow.CreateHandle(new CreateParams { Caption = "betterknob media commands" });
        menu = new ContextMenuStrip();
        menu.Items.Add("betterknob | 1 play/pause / 2 next / 3 previous").Enabled = false;
        status = new ToolStripMenuItem("Waiting for a knob press...") { Enabled = false };
        menu.Items.Add(status);
        var speed = new ToolStripMenuItem("Click window");
        foreach (int ms in new int[] { 250, 350, 450, 550 }) {
            int chosen = ms;
            var item = new ToolStripMenuItem(ms + " ms") { Checked = ms == 350 };
            item.Click += delegate {
                gesture.Window = chosen;
                foreach (ToolStripMenuItem other in speed.DropDownItems) other.Checked = other == item;
            };
            speed.DropDownItems.Add(item);
        }
        menu.Items.Add(speed);
        menu.Items.Add("Exit", null, delegate { ExitThread(); });
        tray = new NotifyIcon { Icon = SystemIcons.Application, Text = "betterknob: 1 play / 2 next / 3 previous", ContextMenuStrip = menu, Visible = true };
        timer = new System.Windows.Forms.Timer { Interval = 15 };
        timer.Tick += delegate {
            gesture.Poll(watch.ElapsedMilliseconds);
            while (gesture.Ready.Count > 0) Send(gesture.Ready.Dequeue());
        };
        timer.Start();
        tray.ShowBalloonTip(3000, "betterknob is running", "1 press: play/pause. 2: next. 3: previous. Right-click this tray icon to adjust timing or exit.", ToolTipIcon.Info);
    }
    IntPtr OnKey(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0) {
            var key = (Native.KeyboardHook)Marshal.PtrToStructure(data, typeof(Native.KeyboardHook));
            if (key.Key == 0xB3 && key.Extra != Native.Marker) {
                int msg = message.ToInt32();
                if (msg == 0x100 || msg == 0x104) { gesture.Down(watch.ElapsedMilliseconds); return (IntPtr)1; }
                if (msg == 0x101 || msg == 0x105) { gesture.Up(watch.ElapsedMilliseconds); return (IntPtr)1; }
            }
        }
        return Native.CallNextHookEx(hook, code, message, data);
    }
    void Send(int presses)
    {
        if (presses == 1) {
            Native.DefWindowProc(commandWindow.Handle, 0x319, commandWindow.Handle, (IntPtr)(14 << 16));
            status.Text = "Last command: Play / pause";
            return;
        }
        ushort key = presses == 2 ? (ushort)0xB0 : presses == 3 ? (ushort)0xB1 : (ushort)0xB3;
        var inputs = new Native.Input[2];
        for (int i = 0; i < 2; i++) {
            inputs[i].Type = 1;
            inputs[i].Data.Keyboard.Key = key;
            inputs[i].Data.Keyboard.Flags = i == 0 ? 0u : 2u;
            inputs[i].Data.Keyboard.Extra = Native.Marker;
        }
        uint sent = Native.SendInput(2, inputs, Marshal.SizeOf(typeof(Native.Input)));
        if (sent == 1) Native.SendInput(1, new Native.Input[] { inputs[1] }, Marshal.SizeOf(typeof(Native.Input)));
        status.Text = sent == 2 ? "Last action: " + (presses == 2 ? "Next track" : presses == 3 ? "Previous track" : "Play / pause") : "Windows blocked the media command";
    }
    protected override void Dispose(bool disposing)
    {
        if (hook != IntPtr.Zero) { Native.UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
        if (disposing) { if (timer != null) timer.Dispose(); if (commandWindow != null) commandWindow.DestroyHandle(); if (tray != null) { tray.Visible = false; tray.Dispose(); } if (menu != null) menu.Dispose(); }
        base.Dispose(disposing);
    }
}

static class Native
{
    public static readonly UIntPtr Marker = new UIntPtr(0x4B43524E);
    public delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] public struct KeyboardHook { public uint Key, Scan, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] public struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] public struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Explicit)] public struct InputUnion { [FieldOffset(0)] public KeyboardInput Keyboard; [FieldOffset(0)] public MouseInput Mouse; }
    [StructLayout(LayoutKind.Sequential)] public struct Input { public uint Type; public InputUnion Data; }
    [DllImport("user32.dll", SetLastError = true)] public static extern IntPtr SetWindowsHookEx(int id, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll")] public static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] public static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Auto)] public static extern IntPtr GetModuleHandle(string name);
    [DllImport("user32.dll", SetLastError = true)] public static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll", CharSet = CharSet.Auto)] public static extern IntPtr DefWindowProc(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
