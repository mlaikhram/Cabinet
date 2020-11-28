using System;

namespace WinApiWrappers
{
    public class ClipboardEvent
    { 
        public Action OnClipboardDraw { get; set; }
        public string Purpose { get; set; }
    }
}
