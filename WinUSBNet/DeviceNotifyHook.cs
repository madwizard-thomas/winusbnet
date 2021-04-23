/*  WinUSBNet library
 *  (C) 2010 Thomas Bleeker (www.madwizard.org)
 *
 *  Licensed under the MIT license, see license.txt or:
 *  http://www.opensource.org/licenses/mit-license.php
 */

using MadWizard.WinUSBNet.API;
using System;
using System.Runtime.InteropServices;

namespace MadWizard.WinUSBNet
{
    
    internal class DeviceNotifyHook : IDisposable
    {
        // http://msdn.microsoft.com/en-us/library/system.windows.forms.nativewindow.aspx

        // TODO: disposed exception when disposed

        private USBNotifier _notifier;
        private Guid _guid;
        private IntPtr _notifyHandle;
        private Win32Window win32Window;
        private const int WM_NCDESTROY = 0x0082;

#if false
        public DeviceNotifyHook(USBNotifier notifier,Guid guid)
        {
            _guid = guid;
            //parent.HandleCreated += new EventHandler(this.OnHandleCreated);
            //parent.HandleDestroyed += new EventHandler(this.OnHandleDestroyed);
            _notifier = notifier;
        }
#endif
        public DeviceNotifyHook(USBNotifier notifier, Guid guid)
        {
            _guid = guid;
            win32Window = new Win32Window(WndProc);
            if (win32Window.Create() == false)
            {
                throw new Exception("Create Win32 window failed!");
            }
            RegisterNotify();   // TODO handle StopNotify on handle destroy
            _notifier = notifier;
        }

        ~DeviceNotifyHook()
        {
            Dispose(false);
        }

        // Listen for the control's window creation and then hook into it.
        /*private void OnHandleCreated(object sender, EventArgs e)
        {
            try
            {
                // Window is now created, assign handle to NativeWindow.
                IntPtr handle = ((Control)sender).Handle;
                RegisterNotify(handle);
            }
            catch (API.APIException ex)
            {
                throw new USBException("Failed to register new window handle for device notification.", ex);
            }
        }*/

        /*private void OnHandleDestroyed(object sender, EventArgs e)
        {
            try
            {
                // Window was destroyed, release hook.
                StopNotify();
            }
            catch (API.APIException ex)
            {
                throw new USBException("Failed to unregister destroyed window handle for device notification.", ex);
            }
        }*/

        private void RegisterNotify()
        {
            //AssignHandle(handle);

            if (_notifyHandle != IntPtr.Zero)
            {
                API.DeviceManagement.StopDeviceDeviceNotifications(_notifyHandle);
                _notifyHandle = IntPtr.Zero;
            }
            API.DeviceManagement.RegisterForDeviceNotifications(win32Window.WinFromHwnd, _guid, ref _notifyHandle);
        }

        private void StopNotify()
        {
            //ReleaseHandle();
            if (_notifyHandle != IntPtr.Zero)
            {
                API.DeviceManagement.StopDeviceDeviceNotifications(_notifyHandle);
                _notifyHandle = IntPtr.Zero;
            }
        }

        private IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            // Listen for operating system messages

            switch (msg)
            {
                case API.DeviceManagement.WM_DEVICECHANGE:
                    _notifier.HandleDeviceChange( wParam, lParam);
                    break;
                case WM_NCDESTROY:
                    // Note: when a control is used, OnHandleDestroyed will be called and the
                    // handle is already released from NativeWindow. In that case, this
                    // WM_NCDESTROY message will not be caught here. This is no problem since
                    // StopNotify is already called. Even if it does, calling it twice does not cause
                    // problems.
                    // When a window handle is used instead of a Control the OnHandle events will not
                    // fire and this handler is necessary to release the handle and stop notifications
                    // when the window is destroyed.
                    StopNotify();
                    break;
            }
            return DeviceManagement.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // clean managed resources

                // do not clean the notifier here. the notifier owns and will dispose this object.
            }
            if (_notifyHandle != IntPtr.Zero)
            {
                API.DeviceManagement.StopDeviceDeviceNotifications(_notifyHandle);
                _notifyHandle = IntPtr.Zero;
            }
        }

    }
}
