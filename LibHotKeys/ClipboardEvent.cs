using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinApiWrappers
{
    public class ClipboardEvent
    { 
        public Action OnClipboardDraw { get; set; }
        public string Purpose { get; set; }
    }
}
