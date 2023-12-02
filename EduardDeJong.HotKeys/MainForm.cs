using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;

namespace EduardDeJong.HotKeys;

public unsafe partial class MainForm : Form
{
    private volatile bool _handlingHotKey = false;

    private readonly HotKey[] _hotKeys = [
        new(0x00000000, Keys.CapsLock,
            () => SendKeys.SendWait("{CapsLock}{Esc}")),

        new(HOT_KEY_MODIFIERS.MOD_SHIFT, Keys.CapsLock,
            () => { /* No action needed */}),

        new(HOT_KEY_MODIFIERS.MOD_WIN, Keys.Oemtilde,
            () => SendKeyboardInputs([
                new(wVk: VIRTUAL_KEY.VK_LWIN),
                new(wVk: VIRTUAL_KEY.VK_1)])),

        new(HOT_KEY_MODIFIERS.MOD_WIN | HOT_KEY_MODIFIERS.MOD_SHIFT, Keys.B,
            () => ExecuteCommand(
                fileName: "firefox",
                arguments: Array.Empty<string>())),

        new(HOT_KEY_MODIFIERS.MOD_WIN, Keys.C,
            () => ExecuteCommand(
                fileName: "code",
                arguments: Array.Empty<string>()))
    ];

    public MainForm()
    {
        InitializeComponent();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        foreach ((HotKey hotKey, int index) in _hotKeys.Select((hotKey, index) => (hotKey, index)))
        {
            PInvoke.UnregisterHotKey((HWND)Handle, index);
            PInvoke.RegisterHotKey((HWND)Handle, index, hotKey.FsModifiers, (uint)hotKey.Vk);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        for (int index = 0; index < _hotKeys.Length; index++)
        {
            PInvoke.UnregisterHotKey((HWND)Handle, index);
        }

        base.OnFormClosing(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (!_handlingHotKey && (uint)m.Msg is PInvoke.WM_HOTKEY)
        {
            _handlingHotKey = true;
            int hotKeyId = m.WParam.ToInt32();
            HotKey hotKey = _hotKeys[hotKeyId];
            hotKey.Action();
#if DEBUG
            Debug.WriteLine($"""
                {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {nameof(HotKey)} #{hotKeyId}
                ({nameof(hotKey.FsModifiers)}: {string.Join(" | ",
                    Enum.GetValues<HOT_KEY_MODIFIERS>()
                    .Where(x => hotKey.FsModifiers.HasFlag(x))
                    .Select(x => Enum.GetName(x))
                    .DefaultIfEmpty($"0x{hotKey.FsModifiers:x}"))},
                {nameof(hotKey.Vk)}: {hotKey.Vk}) pressed.
                """.ReplaceLineEndings(" "));
#endif
            _handlingHotKey = false;
        }
        else
        {
            base.WndProc(ref m);
        }
    }

    private static void SendKeyboardInputs(
        ReadOnlySpan<KeyboardInput> keyboardInputs,
        bool autoKeyUp = true)
    {
        Span<INPUT> inputs = stackalloc INPUT[(autoKeyUp ? 2 : 1) * keyboardInputs.Length];
        int inputIndex = 0;
        for (int keyboardInputIndex = 0;
            keyboardInputIndex < keyboardInputs.Length;
            keyboardInputIndex++, inputIndex++)
        {
            inputs[inputIndex] = new()
            {
                type = INPUT_TYPE.INPUT_KEYBOARD,
                Anonymous = new()
                {
                    ki = keyboardInputs[keyboardInputIndex].Ki
                }
            };
        }
        if (autoKeyUp)
        {
            for (int keyboardInputIndex = keyboardInputs.Length - 1;
                keyboardInputIndex >= 0;
                keyboardInputIndex--, inputIndex++)
            {
                KEYBDINPUT keybdInput = keyboardInputs[keyboardInputIndex].Ki;
                keybdInput.dwFlags |= KEYBD_EVENT_FLAGS.KEYEVENTF_KEYUP;
                inputs[inputIndex] = new()
                {
                    type = INPUT_TYPE.INPUT_KEYBOARD,
                    Anonymous = new() { ki = keybdInput }
                };
            }
        }
        PInvoke.SendInput(inputs, sizeof(INPUT));
    }

    private static void ExecuteCommand(
        string fileName, IEnumerable<string> arguments,
        bool useShellExecute = true)
    {
        using Process process = new()
        {
            StartInfo = new(fileName, arguments)
            {
                UseShellExecute = useShellExecute,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }
        };
        process.Start();
    }

    private sealed record class HotKey(
        HOT_KEY_MODIFIERS FsModifiers,
        Keys Vk,
        Action Action);

    private readonly struct KeyboardInput
    {
        public KeyboardInput(
            VIRTUAL_KEY wVk,
            ushort wScan = 0,
            KEYBD_EVENT_FLAGS dwFlags = 0x00000000,
            uint time = 0,
            nuint dwExtraInfo = 0)
        {
            Ki = new()
            {
                wVk = wVk,
                wScan = wScan,
                dwFlags = dwFlags,
                time = time,
                dwExtraInfo = dwExtraInfo
            };
        }

        public KEYBDINPUT Ki { get; }
    }
}
