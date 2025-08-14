using System.Runtime.InteropServices;
using System.Text;

namespace SecurePaste.Core
{
    /// <summary>
    /// Service for reading and writing clipboard data
    /// </summary>
    public class ClipboardService
    {
        private static readonly object _lock = new object();
        private static volatile bool _isInternalPaste = false;

        /// <summary>
        /// Gets text from the clipboard
        /// </summary>
        /// <returns>Clipboard text or null if not available</returns>
        public static string? GetText()
        {
            lock (_lock)
            {
                if (!WindowsApi.OpenClipboard(IntPtr.Zero))
                    return null;

                try
                {
                    // Try Unicode first
                    IntPtr handle = WindowsApi.GetClipboardData(WindowsApi.CF_UNICODETEXT);
                    if (handle == IntPtr.Zero)
                    {
                        // Fall back to ANSI
                        handle = WindowsApi.GetClipboardData(WindowsApi.CF_TEXT);
                        if (handle == IntPtr.Zero)
                            return null;
                    }

                    IntPtr pointer = WindowsApi.GlobalLock(handle);
                    if (pointer == IntPtr.Zero)
                        return null;

                    try
                    {
                        string text = Marshal.PtrToStringUni(pointer) ?? Marshal.PtrToStringAnsi(pointer) ?? string.Empty;
                        return text;
                    }
                    finally
                    {
                        WindowsApi.GlobalUnlock(handle);
                    }
                }
                finally
                {
                    WindowsApi.CloseClipboard();
                }
            }
        }

        /// <summary>
        /// Sets text to the clipboard
        /// </summary>
        /// <param name="text">Text to set</param>
        /// <returns>True if successful</returns>
        public static bool SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            lock (_lock)
            {
                if (!WindowsApi.OpenClipboard(IntPtr.Zero))
                    return false;

                try
                {
                    if (!WindowsApi.EmptyClipboard())
                        return false;

                    // Convert to Unicode
                    byte[] bytes = Encoding.Unicode.GetBytes(text + '\0');
                    IntPtr hGlobal = WindowsApi.GlobalAlloc(WindowsApi.GMEM_MOVEABLE, (UIntPtr)bytes.Length);

                    if (hGlobal == IntPtr.Zero)
                        return false;

                    IntPtr lpMem = WindowsApi.GlobalLock(hGlobal);
                    if (lpMem == IntPtr.Zero)
                    {
                        WindowsApi.GlobalFree(hGlobal);
                        return false;
                    }

                    try
                    {
                        Marshal.Copy(bytes, 0, lpMem, bytes.Length);
                        WindowsApi.GlobalUnlock(hGlobal);

                        if (WindowsApi.SetClipboardData(WindowsApi.CF_UNICODETEXT, hGlobal) == IntPtr.Zero)
                        {
                            WindowsApi.GlobalFree(hGlobal);
                            return false;
                        }

                        return true;
                    }
                    catch
                    {
                        WindowsApi.GlobalUnlock(hGlobal);
                        WindowsApi.GlobalFree(hGlobal);
                        return false;
                    }
                }
                finally
                {
                    WindowsApi.CloseClipboard();
                }
            }
        }
    }
}