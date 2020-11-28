using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WinApiWrappers
{
    public class ClipboardEventController
    {
        private static ClipboardEventController instance = null;

        public static ClipboardEventController Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new ClipboardEventController();

                }
                return instance;
            }
        }

        public List<ClipboardEventListenerWindow> RegisteredClipboardEventListeners { get; set; } = new List<ClipboardEventListenerWindow>();


        public void RegisterClipboardEvent(string Purpose, Action OnClipboardDraw)
        {
            ClipboardEventListenerWindow clipboardEventListenerWindow = RegisteredClipboardEventListeners.SingleOrDefault(o => o.ClipboardEvent.Purpose == Purpose);

            if (clipboardEventListenerWindow == null)
            {
                ClipboardEvent clipboardEvent = new ClipboardEvent() { Purpose = Purpose, OnClipboardDraw = OnClipboardDraw };
                ClipboardEventListenerWindow window = new ClipboardEventListenerWindow(clipboardEvent);
                IntPtr windowHandle = window.Handle;
                bool issuccess = WinApiMethods.AddClipboardFormatListener(windowHandle);
                if (!issuccess)
                {
                    Win32Exception ex = new Win32Exception(Marshal.GetLastWin32Error());

                    throw ex;

                }
                else
                {
                    RegisteredClipboardEventListeners.Add(clipboardEventListenerWindow);
                }
            }
            else
            {
                throw new AlreadyMappedException(clipboardEventListenerWindow.ClipboardEvent);
            }
        }

        public void UnRegisterClipboardEvent(string Purpose)
        {
            ClipboardEventListenerWindow clipboardEventListenerWindow = RegisteredClipboardEventListeners.SingleOrDefault(o => o.ClipboardEvent.Purpose == Purpose);

            if (clipboardEventListenerWindow != null)
            {
                bool issuccess = WinApiMethods.RemoveClipboardFormatListener(clipboardEventListenerWindow.Handle);
                if (!issuccess)
                {
                    Win32Exception ex = new Win32Exception(Marshal.GetLastWin32Error());

                    throw ex;

                }
                else
                {
                    RegisteredClipboardEventListeners.Remove(clipboardEventListenerWindow);
                }
            }
            else
            {
                throw new ClipboardEventNotFoundException(Purpose);
            }
        }
    }

    public class ClipboardEventListenerWindow : Form
    {
        const int clipboardDraw = 0x031D;
        public ClipboardEvent ClipboardEvent { get; private set; }

        private bool isOnCooldown;

        public ClipboardEventListenerWindow(ClipboardEvent clipboardEvent)
        {
            ClipboardEvent = clipboardEvent;
            isOnCooldown = false;
        }
        protected override void WndProc(ref Message m)
        {
            if (!isOnCooldown && m.Msg == clipboardDraw)
            {
                isOnCooldown = true;
                ClipboardEvent.OnClipboardDraw();
                Task.Delay(10).ContinueWith(_ => isOnCooldown = false);
            }

            base.WndProc(ref m);
        }
    }
}
