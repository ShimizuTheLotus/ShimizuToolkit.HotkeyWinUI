using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShimizuToolkit.HotkeyWinUI
{
    [Flags]
    public enum Win32HotKeyModifier : uint
    {
        None = 0x0,
        Alt = 0x1,
        Control = 0x2,
        Shift = 0x4,
        Windows = 0x8,
        NoRepeat = 0x4000
    }
}
