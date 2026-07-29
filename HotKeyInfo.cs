using ShimizuToolkit.HotkeyWinUI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ShimizuToolkit.HotkeyWinUI
{
    public class HotKeyInfo : INotifyPropertyChanged
    {
        public EventHandler<HotkeyEventArgs>? Handler { get; private set; }
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(null, new PropertyChangedEventArgs(propertyName));
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged();
                }
            }
        }
        private bool _isEnabled = true;

        /// <summary>
        /// Indicates whether the left and right modifier keys matter for this hotkey.
        /// </summary>
        public bool IgnoreModifierLR
        {
            get => _ignoreModifierLR;
            set
            {
                if (_ignoreModifierLR != value)
                {
                    _ignoreModifierLR = value;
                    OnPropertyChanged();
                }
                ;
            }
        }
        private bool _ignoreModifierLR = false;

        /// <summary>
        /// App-range unique identifier, use it to manage your hotkeys.
        /// </summary>
        public string Identifier
        {
            get => _identifier;
            set
            {
                if (_identifier != value)
                {
                    _identifier = value;
                    OnPropertyChanged();
                }
            }
        }
        private string _identifier = "";

        /// <summary>
        /// An unique ID for lib hotkey management.
        /// </summary>
        internal int HotKeyID
        {
            get => _hotKeyID;
            set
            {
                if (_hotKeyID != value)
                {
                    _hotKeyID = value;
                    OnPropertyChanged();
                }
            }
        }
        private int _hotKeyID = 0;

        /// <summary>
        /// All keys the hotkey contains.
        /// </summary>
        public HashSet<VirtualKey> Keys { get; private set; } = [];

        /// <summary>
        /// Modifier key(s) of the hotkey.
        /// </summary>
        public Win32HotKeyModifier ModifierFlag { get; private set; } = Win32HotKeyModifier.None;

        /// <summary>
        /// The modifier key of the hotkey (optional).
        /// </summary>
        public VirtualKey ActionKey { get; private set; } = 0x00;
        public HashSet<VirtualKey> ModifierKeys { get; private set; } = [];

        public HotKeyInfo(){ }

        /// <summary>
        /// This constructor is used to check hotkey conflict.
        /// it could not be used to register hotkey because it has no name.
        /// </summary>
        /// <param name="keys"></param>
        /// <param name="ignoreModifierLR"></param>
        internal HotKeyInfo(List<VirtualKey> keys, bool ignoreModifierLR = true)
        {
            SetHotKey(keys, ignoreModifierLR);
        }
        public HotKeyInfo(string hotKeyIdentifier, EventHandler<HotkeyEventArgs> eventHandler, List<VirtualKey> keys, bool ignoreModifierLR = true)
        {
            Handler = eventHandler;
            Identifier = hotKeyIdentifier;
            SetHotKey(keys, ignoreModifierLR);
        }

        public void SetHandler(EventHandler<HotkeyEventArgs> eventHandler)
        {
            Handler = eventHandler;
        }

        public void SetHotKey(List<VirtualKey> keys, bool ignoreModifierLR = true)
        {
            Keys.Clear();
            foreach (VirtualKey key in keys)    
            {
                Keys.Add(key);
            }
            IgnoreModifierLR = ignoreModifierLR;
            if (DetectIfLRModifierBothExists(keys))
            {
                IgnoreModifierLR = false;
            }

            UpdateModifierAndActionKey();
        }

        private static bool DetectIfLRModifierBothExists(List<VirtualKey> keys)
        {
            if (keys.Contains(VirtualKey.VK_LCONTROL) && keys.Contains(VirtualKey.VK_RCONTROL))
            {
                return true;
            }
            if (keys.Contains(VirtualKey.VK_LSHIFT) && keys.Contains(VirtualKey.VK_RSHIFT))
            {
                return true;
            }
            if (keys.Contains(VirtualKey.VK_LMENU) && keys.Contains(VirtualKey.VK_RMENU))
            {
                return true;
            }
            if (keys.Contains(VirtualKey.VK_LWIN) && keys.Contains(VirtualKey.VK_RWIN))
            {
                return true;
            }
            return false;
        }

        private void UpdateModifierAndActionKey()   
        {
            ModifierFlag = Win32HotKeyModifier.None;
            ActionKey = 0x00;
            foreach (var key in Keys)
            {
                switch (key)
                {
                    case VirtualKey.VK_LCONTROL:
                        ModifierFlag |= Win32HotKeyModifier.Control;
                        ModifierKeys.Add(VirtualKey.VK_LCONTROL);
                        break;
                    case VirtualKey.VK_RCONTROL:
                        ModifierFlag |= Win32HotKeyModifier.Control;
                        ModifierKeys.Add(VirtualKey.VK_RCONTROL);
                        break;
                    case VirtualKey.VK_LSHIFT:
                        ModifierFlag |= Win32HotKeyModifier.Shift;
                        ModifierKeys.Add(VirtualKey.VK_LSHIFT);
                        break;
                    case VirtualKey.VK_RSHIFT:
                        ModifierFlag |= Win32HotKeyModifier.Shift;
                        ModifierKeys.Add(VirtualKey.VK_RSHIFT);
                        break;
                    case VirtualKey.VK_LMENU:
                        ModifierFlag |= Win32HotKeyModifier.Alt;
                        ModifierKeys.Add(VirtualKey.VK_LMENU);
                        break;
                    case VirtualKey.VK_RMENU:
                        ModifierFlag |= Win32HotKeyModifier.Alt;
                        ModifierKeys.Add(VirtualKey.VK_RMENU);
                        break;
                    case VirtualKey.VK_LWIN:
                        ModifierFlag |= Win32HotKeyModifier.Windows;
                        ModifierKeys.Add(VirtualKey.VK_LWIN);
                        break;
                    case VirtualKey.VK_RWIN:
                        ModifierFlag |= Win32HotKeyModifier.Windows;
                        ModifierKeys.Add(VirtualKey.VK_RWIN);
                        break;
                    default:
                        ActionKey = key;
                        break;
                }
            }
        }

        public bool IsValidHotKey()
        {
            if (ActionKey == 0x00)
            {
                return false; // Hotkey must contain an action key.
            }
            if (ModifierFlag == Win32HotKeyModifier.None)
            {
                return false; // Hotkey must contain a modifier key.
            }
            return true;
        }
    }
}
