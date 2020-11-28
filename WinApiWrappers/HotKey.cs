using System;
using System.Windows.Forms;

namespace WinApiWrappers
{
    public class HotKey
    {
        public int id { get; set; }
        public KeyModifiers fsModifiers { get; set; }
        public Keys vk { get; set; }
        public Action<HotKey> OnPressed { get; set; }
        public string Purpose { get; set; }
    }
}