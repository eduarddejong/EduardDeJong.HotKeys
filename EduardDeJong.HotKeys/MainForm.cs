using EduardDeJong.HotKeys.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.Input.KeyboardAndMouse;
using Windows.Win32.UI.WindowsAndMessaging;

namespace EduardDeJong.HotKeys;

public unsafe partial class MainForm : Form
{
    private static readonly HookKey[] _hookKeys = [
        new(ModifiersState: [(key: Keys.Shift, state: new(Down: false))],
            Vk: Keys.CapsLock,
            Action: (out bool intercept) =>
            {
                intercept = true;
                SendKeys.SendWait("{Esc}");
            }),

        new(ModifiersState: [(key: Keys.Shift, state: new(Down: true))],
            Vk: Keys.CapsLock,
            // Do nothing
            Action: (out bool intercept) => intercept = false),
    ];

    private static readonly HotKey[] _hotKeys = [
        new(FsModifiers: HOT_KEY_MODIFIERS.MOD_WIN,
            Vk: Keys.Oemtilde,
            Action: () => SendKeyboardInputs([
                new(wVk: (VIRTUAL_KEY)Keys.LWin),
                new(wVk: (VIRTUAL_KEY)Keys.D1)])),

        new(FsModifiers: HOT_KEY_MODIFIERS.MOD_WIN | HOT_KEY_MODIFIERS.MOD_SHIFT,
            Vk: Keys.B,
            Action: () => ExecuteCommand(
                fileName: "firefox",
                arguments: Array.Empty<string>())),

        new(FsModifiers: HOT_KEY_MODIFIERS.MOD_WIN,
            Vk: Keys.C,
            Action: () => ExecuteCommand(
                fileName: "code",
                arguments: Array.Empty<string>()))
    ];

    private HHOOK _windowsHook;

    private volatile bool _handlingHotKey = false;

    public MainForm()
    {
        InitializeComponent();

        Icon = Resources.HotKeysIcon;
        NotifyIcon.Icon = Resources.HotKeysIcon;
        WindowState = FormWindowState.Minimized;
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        using ProcessModule? module = Process.GetCurrentProcess().MainModule;
        _windowsHook = PInvoke.SetWindowsHookEx(
            idHook: WINDOWS_HOOK_ID.WH_KEYBOARD_LL,
            lpfn: HookProc,
            hmod: (HINSTANCE)(module?.BaseAddress ?? nint.Zero),
            dwThreadId: 0);

        foreach ((HotKey hotKey, int index) in _hotKeys.Select((hotKey, index) => (hotKey, index)))
        {
            PInvoke.RegisterHotKey((HWND)Handle, index, hotKey.FsModifiers, (uint)hotKey.Vk);
        }

        BeginInvoke(Hide);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        for (int index = 0; index < _hotKeys.Length; index++)
        {
            PInvoke.UnregisterHotKey((HWND)Handle, index);
        }

        PInvoke.UnhookWindowsHookEx(_windowsHook);

        base.OnFormClosing(e);
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);

        if (WindowState == FormWindowState.Minimized)
        {
            Hide();
        }
    }

    private void NotifyIcon_Click(object sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }
        else
        {
            WindowState = FormWindowState.Minimized;
            Hide();
        }
    }

    private LRESULT HookProc(int code, WPARAM wParam, LPARAM lParam)
    {
        if (code >= 0)
        {
            KEYBDINPUT keybdInput = *(KEYBDINPUT*)(nint)lParam;
            switch ((uint)wParam)
            {
                case PInvoke.WM_KEYDOWN or PInvoke.WM_SYSKEYDOWN:
                    foreach ((HookKey hookKey, int hookKeyIndex) in _hookKeys.Select((hookKey, index) => (hookKey, index)))
                    {
                        if ((Keys)(int)keybdInput.wVk == hookKey.Vk)
                        {
                            if (!hookKey.ModifiersState.Any(x =>
                            {
                                (Keys key, HookKeyModifierState modifierState) = x;
                                int keyState = key switch
                                {
                                    Keys.Control =>
                                        PInvoke.GetKeyState((int)Keys.LControlKey)
                                        | PInvoke.GetKeyState((int)Keys.RControlKey),

                                    Keys.Shift =>
                                        PInvoke.GetKeyState((int)Keys.LShiftKey)
                                        | PInvoke.GetKeyState((int)Keys.RShiftKey),

                                    Keys.Alt =>
                                        PInvoke.GetKeyState((int)Keys.LMenu)
                                        | PInvoke.GetKeyState((int)Keys.RMenu),

                                    _ => PInvoke.GetKeyState((int)key)
                                };
                                if (modifierState.Down is bool modifierStateDown)
                                {
                                    bool keyStateDown = (keyState & 0x8000) == 0x8000;
                                    if (modifierStateDown != keyStateDown)
                                    {
                                        return true;
                                    }
                                }
                                if (modifierState.Toggled is bool modifierStateToggled)
                                {
                                    bool keyStateToggled = (keyState & 0x0001) == 0x0001;
                                    if (modifierStateToggled != keyStateToggled)
                                    {
                                        return true;
                                    }
                                }
                                return false;
                            }))
                            {
#if DEBUG
                                Debug.WriteLine($"""
                                    {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {nameof(HotKey)} #{hookKeyIndex}
                                    ({nameof(hookKey.ModifiersState)}: [{string.Join(" | ",
                                        hookKey.ModifiersState
                                        .Select(modifiersState =>
                                        {
                                            (Keys key, HookKeyModifierState state) = modifiersState;
                                            string modifierStateDescription = string.Join(", ",
                                                (state.Down is bool stateDown ? $"{nameof(state.Down)}: {stateDown}" : string.Empty) +
                                                (state.Toggled is bool stateToggled ? $"{nameof(state.Toggled)}: {stateToggled}" : string.Empty));
                                            return $"{nameof(key)}: {key}, {nameof(state)}: ({modifierStateDescription})";
                                        })
                                        .DefaultIfEmpty(""))}],
                                    {nameof(hookKey.Vk)}: {hookKey.Vk}) pressed.
                                    """.ReplaceLineEndings(" "));
#endif
                                hookKey.Action(out bool intercept);
                                if (intercept)
                                {
                                    return (LRESULT)(-1);
                                }
                            }
                        }
                    }
                    break;
            }
        }

        return PInvoke.CallNextHookEx(_windowsHook, code, wParam, lParam);
    }

    protected override void WndProc(ref Message m)
    {
        if (!_handlingHotKey && (uint)m.Msg is PInvoke.WM_HOTKEY)
        {
            _handlingHotKey = true;
            int hotKeyId = m.WParam.ToInt32();
            HotKey hotKey = _hotKeys[hotKeyId];
#if DEBUG
            Debug.WriteLine($"""
                {DateTime.Now:yyyy-MM-dd HH:mm:ss} - {nameof(HotKey)} #{hotKeyId}
                ({nameof(hotKey.FsModifiers)}: {string.Join(" | ",
                    Enum.GetValues<HOT_KEY_MODIFIERS>()
                    .Where(value => hotKey.FsModifiers.HasFlag(value))
                    .Select(value => Enum.GetName(value))
                    .DefaultIfEmpty($"0x{hotKey.FsModifiers:x}"))},
                {nameof(hotKey.Vk)}: {hotKey.Vk}) pressed.
                """.ReplaceLineEndings(" "));
#endif
            hotKey.Action();
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

    private sealed record class HookKey(
        (Keys key, HookKeyModifierState state)[] ModifiersState,
        Keys Vk,
        HookKeyAction Action);

    private record struct HookKeyModifierState(
        bool? Down = null,
        bool? Toggled = null);

    private delegate void HookKeyAction(out bool intercept);

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
