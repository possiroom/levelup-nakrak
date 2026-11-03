using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

// Editor-only physical keyboard input for manual-path regression playback.
// Refuses to inject into a foreground application other than this Unity Editor.
public static class TutorialKeyboardInput
{
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint process);
    [DllImport("user32.dll")] private static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
    [DllImport("user32.dll")] private static extern uint MapVirtualKey(uint key, uint mode);

    public static string Tap(byte key, double seconds = .12)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Enter Play mode first.");
        var process = Process.GetCurrentProcess();
        uint foreground;
        GetWindowThreadProcessId(GetForegroundWindow(), out foreground);
        if (foreground != process.Id) throw new InvalidOperationException("Unity is not the foreground application; no keyboard input sent.");
        byte scan = (byte)MapVirtualKey(key, 0);
        uint extended = key >= 0x21 && key <= 0x2e ? 1u : 0u;
        keybd_event(key, scan, extended, UIntPtr.Zero);
        double releaseAt = EditorApplication.timeSinceStartup + seconds;
        EditorApplication.CallbackFunction release = null;
        release = () =>
        {
            if (EditorApplication.timeSinceStartup < releaseAt && EditorApplication.isPlaying) return;
            keybd_event(key, scan, extended | 2u, UIntPtr.Zero);
            EditorApplication.update -= release;
        };
        EditorApplication.update += release;
        return "Sent physical key " + key;
    }
}
