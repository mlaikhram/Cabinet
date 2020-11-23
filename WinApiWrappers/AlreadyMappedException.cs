using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinApiWrappers
{
    public class AlreadyMappedException : Exception
    {

        public AlreadyMappedException(HotKey key)
            : base(string.Format("Key is already Defined for this {0} purpose", key.Purpose))
        {

        }

        public AlreadyMappedException(ClipboardEvent clipboardEvent)
            : base(string.Format("Clipboard Event is already Defined for this {0} purpose", clipboardEvent.Purpose))
        {

        }
    }

    public class KeyNotFoundException : Exception
    {

        public KeyNotFoundException(string purpose)
            : base(string.Format("Key not found for this {0} purpose. cannot perform this action", purpose))
        {

        }

    }

    public class ClipboardEventNotFoundException : Exception
    {
        public ClipboardEventNotFoundException(string purpose)
            : base(string.Format("Clipboard Event not found for this {0} purpose. cannot perform this action", purpose))
        {

        }
    }
}