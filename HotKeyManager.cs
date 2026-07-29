using ShimizuToolkit.HotkeyWinUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using static ShimizuToolkit.HotkeyWinUI.Win32;


namespace ShimizuToolkit.HotkeyWinUI
{
    public class HotKeyManager : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public IntPtr Hwnd;
        private const uint WM_HOTKEY = 0x0312;

        private Microsoft.UI.Xaml.Window _windowWinUI;
        private Win32.WndProc _originalWndProc;
        private Win32.WndProc _wndProcHook;

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        public Dictionary<string, HotKeyInfo> HotKeys
        {
            get => _hotKeys;
            private set
            {
                if (_hotKeys != value)
                {
                    _hotKeys = value;
                    OnPropertyChanged();
                }
            }
        }
        private Dictionary<string, HotKeyInfo> _hotKeys = [];
        private readonly HashSet<int> _idPool = [];

        private int _lIDCounter = 1;

        public HotKeyManager()
        {
            _windowWinUI = new();
            IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(_windowWinUI);
            Hwnd = handle;
            HWND hWND = new(handle);
            if (Environment.Is64BitProcess)
            {
                _originalWndProc = Marshal.GetDelegateForFunctionPointer<Win32.WndProc>(
                    Win32.GetWindowLongPtr(hWND, (int)Win32.WINDOW_LONG_PTR_INDEX.GWL_WNDPROC));
                _wndProcHook = WndProcHook;
                    Win32.SetWindowLongPtr(hWND, (int)Win32.WINDOW_LONG_PTR_INDEX.GWL_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(_wndProcHook));
            }
            else
            {
                _originalWndProc = Marshal.GetDelegateForFunctionPointer<Win32.WndProc>(
                    Win32.x86.GetWindowLongPtrW(hWND, (int)Win32.WINDOW_LONG_PTR_INDEX.GWL_WNDPROC));
                _wndProcHook = WndProcHook;
                    Win32.x86.SetWindowLongPtrW(hWND, (int)Win32.WINDOW_LONG_PTR_INDEX.GWL_WNDPROC,
                    Marshal.GetFunctionPointerForDelegate(_wndProcHook));
            }
        }

        public static HotKeyManager Current { get { return LazyInitializer.Instance; } }

        private static class LazyInitializer
        {
            static LazyInitializer() { }
            public static readonly HotKeyManager Instance = new();
        }

        private IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            HotKeyInfo? hotkey;
            bool handled = false;
            var result = HandleHotkeyMessage(hWnd, (int)msg, wParam, lParam, ref handled, out hotkey);
            if (handled)
                return result;

            return _originalWndProc(hWnd, msg, wParam, lParam);
        }

        private HashSet<VirtualKey> GetModifierDownKeys()
        {
            bool isLShift = (GetAsyncKeyState((int)VirtualKey.VK_LSHIFT) & 0x8000) != 0;
            bool isRShift = (GetAsyncKeyState((int)VirtualKey.VK_RSHIFT) & 0x8000) != 0;
            bool isLAlt = (GetAsyncKeyState((int)VirtualKey.VK_LMENU) & 0x8000) != 0;
            bool isRAlt = (GetAsyncKeyState((int)VirtualKey.VK_RMENU) & 0x8000) != 0;
            bool isLControl = (GetAsyncKeyState((int)VirtualKey.VK_LCONTROL) & 0x8000) != 0;
            bool isRControl = (GetAsyncKeyState((int)VirtualKey.VK_RCONTROL) & 0x8000) != 0;
            bool isLWin = (GetAsyncKeyState((int)VirtualKey.VK_LWIN) & 0x8000) != 0;
            bool isRWin = (GetAsyncKeyState((int)VirtualKey.VK_RWIN) & 0x8000) != 0;
            HashSet<VirtualKey> modifiers = [];
            if (isLShift)
                modifiers.Add(VirtualKey.VK_LSHIFT);
            if (isRShift)
                modifiers.Add(VirtualKey.VK_RSHIFT);
            if (isLAlt)
                modifiers.Add(VirtualKey.VK_LMENU);
            if (isRAlt)
                modifiers.Add(VirtualKey.VK_RMENU);
            if (isLControl)
                modifiers.Add(VirtualKey.VK_LCONTROL);
            if (isRControl)
                modifiers.Add(VirtualKey.VK_RCONTROL);
            if (isLWin)
                modifiers.Add(VirtualKey.VK_LWIN);
            if (isRWin)
                modifiers.Add(VirtualKey.VK_RWIN);
            return modifiers;
        }

        internal IntPtr HandleHotkeyMessage(
            IntPtr hwnd,
            int msg,
            IntPtr wParam,
            IntPtr lParam,
            ref bool handled,
            out HotKeyInfo? hotkey)
        {
            hotkey = null;
            if (/*IsEnabled &&*/ msg == WM_HOTKEY)
            {
                int id = wParam.ToInt32();
                hotkey = HotKeys.Values.Where(x => x.HotKeyID == id).FirstOrDefault();
                if (hotkey != null)
                {
                    handled = ManageHotKeyEvent(id, GetModifierDownKeys());
                }
            }
            return IntPtr.Zero;
        }

        private bool ManageHotKeyEvent(int id, HashSet<VirtualKey> modifiers)
        {
            bool handled = false;
            HotKeyInfo? info = HotKeys.Values.Where(x => x.HotKeyID == id).FirstOrDefault();
            if (info == null)
            {
                // Hotkey reference lost, unregister to free hotkey.
                Win32UnregisterHotKey(id);
                return false;
            }
            List<HotKeyInfo> keys = [];

            // Get active hotkeys having same modifier flags and action keys.
#pragma warning disable IDE0305
            keys = HotKeys.Values.Where(x => x.ModifierKeys == info.ModifierKeys && x.ActionKey == info.ActionKey && x.IsEnabled).ToList();
#pragma warning restore IDE0305
            foreach (HotKeyInfo key in keys)
            {
                bool modLRBothPressed = false;
                if (modifiers.Contains(VirtualKey.VK_LSHIFT) && modifiers.Contains(VirtualKey.VK_RSHIFT)
                || modifiers.Contains(VirtualKey.VK_LMENU) && modifiers.Contains(VirtualKey.VK_RMENU)
                || modifiers.Contains(VirtualKey.VK_LCONTROL) && modifiers.Contains(VirtualKey.VK_RCONTROL)
                || modifiers.Contains(VirtualKey.VK_LWIN) && modifiers.Contains(VirtualKey.VK_RWIN))
                {
                    modLRBothPressed = true;
                }
                if (key.IgnoreModifierLR && modLRBothPressed)// If a hotkey has both LR type of a same modifier, it shouldn't ignore modifier LR.
                    continue;
                if (key.IgnoreModifierLR && !modLRBothPressed)
                {
                    //HotKeyTriggered?.Invoke(key.Identifier);
                    if (key.Handler != null)
                    {
                        key.Handler(this, new HotkeyEventArgs(key.Identifier));
                    }
                    handled = true;
                }
                else// check if all modifiers matches.
                {
                    bool matchAllKeys = true;
                    foreach (VirtualKey vk in key.ModifierKeys)
                    {
                        if (!modifiers.Contains(vk))
                        {
                            matchAllKeys = false;
                            break;
                        }
                    }
                    if (!matchAllKeys) continue;
                    foreach (VirtualKey vk in modifiers)
                    {
                        if (!key.ModifierKeys.Contains(vk))
                        {
                            matchAllKeys = false;
                            break;
                        }
                    }
                    if (!matchAllKeys) continue;
                    // All modifiers matches.
                    //HotKeyTriggered?.Invoke(key.Identifier);
                    if (key.Handler != null)
                    {
                        key.Handler(this, new HotkeyEventArgs(key.Identifier));
                    }
                    handled = true;
                }
            }
            return handled;
        }

        private int GenerateID()
        {
            unsafe
            {
                int id = _lIDCounter;
                while (id > 0)
                {   
                    if (!_idPool.Contains(id))
                    {
                        _lIDCounter = id;
                        _idPool.Add(id);
                        return id;
                    }
                    id++;
                }
                if (id <= 0)
                {
                    throw new IndexOutOfRangeException("ID pool was full.");
                }
                return id;
            }
        }

        public bool AddOrOverwriteHotKey(string identifier, HotKeyInfo hotKey)
        {
            bool registerSucceed;
            hotKey.Identifier = identifier;
            if (HotKeys.TryGetValue(identifier, out HotKeyInfo? info))
            {
                Win32UnregisterHotKey(info.HotKeyID);
                hotKey.HotKeyID = info.HotKeyID;
                registerSucceed = Win32RegisterHotKey(hotKey);
            }
            else
            {
                hotKey.HotKeyID = GenerateID();
                registerSucceed = Win32RegisterHotKey(hotKey);
            }
            if (registerSucceed)
            {
                HotKeys[identifier] = hotKey;
            }
            return registerSucceed;
        }

        public bool AddOrOverwriteHotKey(HotKeyInfo hotKey)
        {
            bool registerSucceed;
            if (HotKeys.TryGetValue(hotKey.Identifier, out HotKeyInfo? info))
            {
                Win32UnregisterHotKey(info.HotKeyID);
                hotKey.HotKeyID = info.HotKeyID;
                registerSucceed = Win32RegisterHotKey(hotKey);
            }
            else
            {
                hotKey.HotKeyID = GenerateID();
                registerSucceed = Win32RegisterHotKey(hotKey);
            }
            if (registerSucceed)
            {
                HotKeys[hotKey.Identifier] = hotKey;
            }
            return registerSucceed;
        }

        public void RemoveHotKey(string identifier)
        {
            HotKeyInfo info = HotKeys[identifier];
            if (info != null)
            {
                if (info.HotKeyID < _lIDCounter)
                {
                    _lIDCounter = info.HotKeyID;
                }
                Win32UnregisterHotKey(info.HotKeyID);
                _idPool.Remove(info.HotKeyID);
                HotKeys.Remove(identifier);
            }
        }

        internal void RemoveHotKey(long hotKeyID)
        {
            HotKeyInfo? info = HotKeys.Where(x => x.Value.HotKeyID == hotKeyID).Select(x => x.Value).FirstOrDefault();
            if (info != null)
            {
                if (info.HotKeyID < _lIDCounter)
                {
                    _lIDCounter = info.HotKeyID;
                }
                Win32UnregisterHotKey(info.HotKeyID);
                _idPool.Remove(info.HotKeyID);
                HotKeys.Remove(info.Identifier);
            }
        }

        public void EnableHotKey(string identifier)
        {
            if (HotKeys.TryGetValue(identifier, out HotKeyInfo? value))
            {
                value.IsEnabled = true;
            }
        }

        internal void EnableHotKey(long hotKeyID)
        {
            HotKeyInfo? info = HotKeys.Where(x => x.Value.HotKeyID == hotKeyID).Select(x => x.Value).FirstOrDefault();
            if (info != null)
            {
                info.IsEnabled = true;
            }
        }

        private bool Win32RegisterHotKey(HotKeyInfo info)
        {
            return Win32.RegisterHotKey(Hwnd, info.HotKeyID, (uint)info.ModifierFlag, (uint)info.ActionKey);
        }

        private void Win32UnregisterHotKey(int id)
        {
            Win32.UnregisterHotKey(Hwnd, id);
        }

        public Dictionary<string, HotKeyInfo> GetHotKeys()
        {
            return HotKeys;
        }

        public void DisableHotKey(string identifier)
        {
            if (HotKeys.TryGetValue(identifier, out HotKeyInfo? value))
            {
                value.IsEnabled = false;
            }
        }

        internal void DisableHotKey(long hotKeyID)
        {
            HotKeyInfo? info = HotKeys.Where(x => x.Value.HotKeyID == hotKeyID).Select(x => x.Value).FirstOrDefault();
            if (info != null)
            {
                info.IsEnabled = false;
            }
        }

        public void Clear()
        {
            foreach (HotKeyInfo info in HotKeys.Values)
            {
                Win32UnregisterHotKey(info.HotKeyID);
            }
            HotKeys.Clear();
            _idPool.Clear();
            _lIDCounter = 1;
        }

        public bool IdentifierExists(string identifier)
        {
            return HotKeys.ContainsKey(identifier);
        }

        public bool HotKeyIDExists(long identifier)
        {
            return HotKeys.Where(x => x.Value.HotKeyID == identifier).Any();
        }

        public List<HotKeyInfo> GetHotKeyConflictList(List<VirtualKey> keys, bool ignoreModifierLR = true, bool checkEnabledHotKeysOnly = false)
        {
#pragma warning disable IDE0305
            List<HotKeyInfo> hotKeyCheckList = checkEnabledHotKeysOnly ? HotKeys.Where(x => x.Value.IsEnabled).Select(x => x.Value).ToList() : HotKeys.Select(x => x.Value).ToList();
#pragma warning restore IDE0305
            List<HotKeyInfo> conflictList = [];

            HotKeyInfo checkTargetHotKey = new(keys, ignoreModifierLR);

            // Find hotkeys having same action key and modifier
            foreach (HotKeyInfo info in hotKeyCheckList)
            {
                if (info.ActionKey == checkTargetHotKey.ActionKey
                && info.ModifierFlag == checkTargetHotKey.ModifierFlag)
                {
                    // check modifier conflict
                    if (ignoreModifierLR || info.IgnoreModifierLR)
                    {
                        // conflict
                        conflictList.Add(info);
                    }
                    else
                    {
                        if (checkTargetHotKey.ModifierKeys.Count != info.ModifierKeys.Count)
                        {
                            continue;
                        }
                        // Ensure they have same modifiers
                        bool allConflictA = true;
                        foreach (VirtualKey mod in checkTargetHotKey.ModifierKeys)
                        {
                            if (!info.ModifierKeys.Contains(mod))
                            {
                                allConflictA = false;
                                break;
                            }

                        }
                        bool allConflictB = true;
                        foreach (VirtualKey mod in info.ModifierKeys)
                        {
                            if (!checkTargetHotKey.ModifierKeys.Contains(mod))
                            {
                                allConflictB = false;
                                break;
                            }

                        }
                        if (allConflictA && allConflictB)
                        {
                            conflictList.Add(info);
                        }
                    }
                }
            }

            return conflictList;
        }

        public bool IsHotKeyHasConflict(List<VirtualKey> keys, bool ignoreModifierLR = true, bool checkEnabledHotKeysOnly = false)
        {
#pragma warning disable IDE0305
            List<HotKeyInfo> hotKeyCheckList = checkEnabledHotKeysOnly ? HotKeys.Where(x => x.Value.IsEnabled).Select(x => x.Value).ToList() : HotKeys.Select(x => x.Value).ToList();
#pragma warning restore IDE0305

            HotKeyInfo checkTargetHotKey = new(keys);

            // Find hotkeys having same action key and modifier
            foreach (HotKeyInfo info in hotKeyCheckList)
            {
                if (info.ActionKey == checkTargetHotKey.ActionKey
                && info.ModifierFlag == checkTargetHotKey.ModifierFlag)
                {
                    // check modifier conflict
                    if (ignoreModifierLR || info.IgnoreModifierLR)
                    {
                        // conflict
                        return true;
                    }
                    else
                    {
                        if (checkTargetHotKey.ModifierKeys.Count != info.ModifierKeys.Count)
                        {
                            continue;
                        }
                        // Ensure they have same modifiers
                        bool allConflictA = true;
                        foreach (VirtualKey mod in checkTargetHotKey.ModifierKeys)
                        {
                            if (!info.ModifierKeys.Contains(mod))
                            {
                                allConflictA = false;
                                break;
                            }

                        }
                        bool allConflictB = true;
                        foreach (VirtualKey mod in info.ModifierKeys)
                        {
                            if (!checkTargetHotKey.ModifierKeys.Contains(mod))
                            {
                                allConflictB = false;
                                break;
                            }

                        }
                        if (allConflictA && allConflictB)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    }
}
