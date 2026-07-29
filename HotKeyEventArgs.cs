using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShimizuToolkit.HotkeyWinUI
{
    public class HotkeyEventArgs : EventArgs
    {
        public HotkeyEventArgs(string name)
        {
            Name = name;
        }

        public string Name{ get; }

        public bool Handled { get; set; }
    }
}
