using System;

namespace WinApiWrappers
{
    public class DisplayController
    {
        private static DisplayController instance = null;

        public static DisplayController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new DisplayController();

                }
                return instance;
            }
        }

        public void EnableDisplayCapture(IntPtr handle)
        {
            WinApiMethods.SetWindowDisplayAffinity(handle, 0);
        }

        public void DisableDisplayCapture(IntPtr handle)
        {
            WinApiMethods.SetWindowDisplayAffinity(handle, 1);
        }
    }
}
