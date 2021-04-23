/*  WinUSBNet library
 *  (C) 2010 Thomas Bleeker (www.madwizard.org)
 *
 *  Licensed under the MIT license, see license.txt or:
 *  http://www.opensource.org/licenses/mit-license.php
 */

/* NOTE: Parts of the code in this file are based on the work of Jan Axelson
 * See http://www.lvr.com/winusb.htm for more information
 */

using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace MadWizard.WinUSBNet.API
{
    
    internal class Win32Window
    {

        //typedef LRESULT(CALLBACK* WNDPROC)(HWND, UINT, WPARAM, LPARAM);
        private DeviceManagement.WndProc delegWndProc = null;// = myWndProc;
        private Thread WndProcThread;
        internal IntPtr WinFromHwnd;
        //[DllImport("user32.dll")]
        //static extern bool TranslateMessage([In] ref MSG lpMsg);

        //[DllImport("user32.dll")]
        //static extern IntPtr DispatchMessage([In] ref MSG lpmsg);
        public Win32Window(DeviceManagement.WndProc wndProc)
        {
            delegWndProc = wndProc;
           
        }
        private bool CreateWindowOperationCompleteFlag = false;
        private void MessagePool()
        {
            DeviceManagement.WNDCLASS wind_class = new DeviceManagement.WNDCLASS();
            //wind_class.cbSize = Marshal.SizeOf(typeof(DeviceManagement.WNDCLASS));
            //wind_class.style = (int)(DeviceManagement.CS_HREDRAW | DeviceManagement.CS_VREDRAW | DeviceManagement.CS_DBLCLKS); //Doubleclicks are active
            //wind_class.hbrBackground = (IntPtr)COLOR_BACKGROUND + 1; //Black background, +1 is necessary
            wind_class.cbClsExtra = 0;
            wind_class.cbWndExtra = 0;
            wind_class.hInstance = Marshal.GetHINSTANCE(this.GetType().Module); ;// alternative: Process.GetCurrentProcess().Handle;
            wind_class.hIcon = IntPtr.Zero;
            wind_class.hCursor = DeviceManagement.LoadCursor(IntPtr.Zero, (int)DeviceManagement.IDC_CROSS);// Crosshair cursor;
            wind_class.lpszMenuName = null;
            wind_class.lpszClassName = "myClass";
            wind_class.lpfnWndProc = Marshal.GetFunctionPointerForDelegate(delegWndProc);
            //wind_class.hIconSm = IntPtr.Zero;
            ushort regResult = DeviceManagement.RegisterClass(ref wind_class);

            if (regResult == 0)
            {
                uint error = DeviceManagement.GetLastError();

                goto exi;
            }
            string wndClass = wind_class.lpszClassName;

            //The next line did NOT work with me! When searching the web, the reason seems to be unclear! 
            //It resulted in a zero hWnd, but GetLastError resulted in zero (i.e. no error) as well !!??)
            //IntPtr hWnd = CreateWindowEx(0, wind_class.lpszClassName, "MyWnd", WS_OVERLAPPEDWINDOW | WS_VISIBLE, 0, 0, 30, 40, IntPtr.Zero, IntPtr.Zero, wind_class.hInstance, IntPtr.Zero);

            //This version worked and resulted in a non-zero hWnd
            IntPtr hWnd = DeviceManagement.CreateWindowEx(0,
                "myClass",
                "Hello Win32",
                DeviceManagement.WS_OVERLAPPEDWINDOW,
                unchecked((int)0x80000000), unchecked((int)0x80000000), unchecked((int)0x80000000), unchecked((int)0x80000000),
                IntPtr.Zero,
                IntPtr.Zero,
                wind_class.hInstance,
                IntPtr.Zero);

            if (hWnd == ((IntPtr)0))
            {
                uint error = DeviceManagement.GetLastError();
                goto exi;
            }
            WinFromHwnd = hWnd;
            CreateWindowOperationCompleteFlag = true;
            //DeviceManagement.ShowWindow(hWnd, 1);
            //DeviceManagement.UpdateWindow(hWnd);
            DeviceManagement.MSG msg;
            while (true)
            {
                while (DeviceManagement.GetMessage(out msg, IntPtr.Zero, DeviceManagement.WM_DEVICECHANGE, DeviceManagement.WM_DEVICECHANGE)!=0)
                {
                    DeviceManagement.TranslateMessage(out msg);
                    DeviceManagement.DispatchMessage(out msg);
                }
            }
        exi:
            CreateWindowOperationCompleteFlag = true;
        }
        internal bool Create()
        {
            if(WinFromHwnd!=IntPtr.Zero && CreateWindowOperationCompleteFlag==false)
            {
                return false;
            }
            WndProcThread = new Thread(MessagePool);
            WndProcThread.IsBackground = true;
            WndProcThread.Name = "Win32Window Message Pool Thread";
            WndProcThread.Start();
            while (CreateWindowOperationCompleteFlag == false) ;//don't worry,so fast!
            return true;

            //The explicit message pump is not necessary, messages are obviously dispatched by the framework.
            //However, if the while loop is implemented, the functions are called... Windows mysteries...
            //MSG msg;
            //while (GetMessage(out msg, IntPtr.Zero, 0, 0) != 0)
            //{
            //    TranslateMessage(ref msg);
            //    DispatchMessage(ref msg);
            //}
        }

        private static IntPtr myWndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {

                case API.DeviceManagement.WM_DESTROY:
                    DeviceManagement.DestroyWindow(hWnd);

                    //If you want to shutdown the application, call the next function instead of DestroyWindow
                    //PostQuitMessage(0);
                    break;

                default:
                    break;
            }
            return DeviceManagement.DefWindowProc(hWnd, msg, wParam, lParam);
        }
    }
    /// <summary>
    /// API declarations relating to device management (SetupDixxx and
    /// RegisterDeviceNotification functions).
    /// </summary>
    internal static partial class DeviceManagement
    {
        // from dbt.h

        internal const Int32 DBT_DEVICEARRIVAL = 0X8000;
        internal const Int32 DBT_DEVICEREMOVECOMPLETE = 0X8004;
        private const Int32 DBT_DEVTYP_DEVICEINTERFACE = 5;
        private const Int32 DBT_DEVTYP_HANDLE = 6;
        private const Int32 DEVICE_NOTIFY_ALL_INTERFACE_CLASSES = 4;
        private const Int32 DEVICE_NOTIFY_SERVICE_HANDLE = 1;
        private const Int32 DEVICE_NOTIFY_WINDOW_HANDLE = 0;
        internal const Int32 WM_DEVICECHANGE = 0X219;

        // from setupapi.h

        private const Int32 DIGCF_PRESENT = 2;
        private const Int32 DIGCF_DEVICEINTERFACE = 0X10;


        internal const UInt32 WS_OVERLAPPEDWINDOW = 0xcf0000;
        internal const UInt32 WS_VISIBLE = 0x10000000;
        internal const UInt32 CS_USEDEFAULT = 0x80000000;
        internal const UInt32 CS_DBLCLKS = 8;
        internal const UInt32 CS_VREDRAW = 1;
        internal const UInt32 CS_HREDRAW = 2;
        internal const UInt32 COLOR_WINDOW = 5;
        internal const UInt32 COLOR_BACKGROUND = 1;
        internal const UInt32 IDC_CROSS = 32515;
        internal const UInt32 WM_DESTROY = 2;
        internal const UInt32 WM_PAINT = 0x0f;
        internal const UInt32 WM_LBUTTONUP = 0x0202;
        internal const UInt32 WM_LBUTTONDBLCLK = 0x0203;
        

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        internal struct WNDCLASS
        {
            //[MarshalAs(UnmanagedType.U4)]
            public uint style;
            public IntPtr lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszMenuName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string lpszClassName;
        }

        // Two declarations for the DEV_BROADCAST_DEVICEINTERFACE structure.

        // Use this one in the call to RegisterDeviceNotification() and
        // in checking dbch_devicetype in a DEV_BROADCAST_HDR structure:

        [StructLayout(LayoutKind.Sequential)]
        private class DEV_BROADCAST_DEVICEINTERFACE
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            internal Guid dbcc_classguid;
            internal Int16 dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DEV_BROADCAST_DEVICEINTERFACE_1
        {
            internal Int32 dbcc_size;
            internal Int32 dbcc_devicetype;
            internal Int32 dbcc_reserved;
            internal Guid dbcc_classguid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 255)]
            internal Char[] dbcc_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        private class DEV_BROADCAST_HDR
        {
            internal Int32 dbch_size;
            internal Int32 dbch_devicetype;
            internal Int32 dbch_reserved;
        }

        private struct SP_DEVICE_INTERFACE_DATA
        {
            internal Int32 cbSize;
            internal Guid InterfaceClassGuid;
            internal Int32 Flags;
            internal IntPtr Reserved;
        }
        private struct SP_DEVINFO_DATA
        {
            internal Int32 cbSize;
            internal Guid ClassGuid;
            internal Int32 DevInst;
            internal IntPtr Reserved;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            long x;
            long y;
        }
        [StructLayout(LayoutKind.Sequential)]
        internal struct MSG
        {
            IntPtr hwnd;
            uint message;
            UIntPtr wParam;
            IntPtr lParam;
            int time;
            POINT pt;
        }
        // from pinvoke.net
        private enum SPDRP : uint
        {
            SPDRP_DEVICEDESC = 0x00000000,
            SPDRP_HARDWAREID = 0x00000001,
            SPDRP_COMPATIBLEIDS = 0x00000002,
            SPDRP_NTDEVICEPATHS = 0x00000003,
            SPDRP_SERVICE = 0x00000004,
            SPDRP_CONFIGURATION = 0x00000005,
            SPDRP_CONFIGURATIONVECTOR = 0x00000006,
            SPDRP_CLASS = 0x00000007,
            SPDRP_CLASSGUID = 0x00000008,
            SPDRP_DRIVER = 0x00000009,
            SPDRP_CONFIGFLAGS = 0x0000000A,
            SPDRP_MFG = 0x0000000B,
            SPDRP_FRIENDLYNAME = 0x0000000C,
            SPDRP_LOCATION_INFORMATION = 0x0000000D,
            SPDRP_PHYSICAL_DEVICE_OBJECT_NAME = 0x0000000E,
            SPDRP_CAPABILITIES = 0x0000000F,
            SPDRP_UI_NUMBER = 0x00000010,
            SPDRP_UPPERFILTERS = 0x00000011,
            SPDRP_LOWERFILTERS = 0x00000012,
            SPDRP_MAXIMUM_PROPERTY = 0x00000013,

            SPDRP_ENUMERATOR_NAME = 0x16,
        }

        private enum RegTypes : int
        {
            // incomplete list, these are just the ones used.
            REG_SZ = 1,
            REG_MULTI_SZ = 7
        }

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr RegisterDeviceNotification(IntPtr hRecipient, IntPtr NotificationFilter, Int32 Flags);

        //[DllImport("setupapi.dll", SetLastError = true)]
        //internal static extern Int32 SetupDiCreateDeviceInfoList(ref System.Guid ClassGuid, Int32 hwndParent);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern Int32 SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInterfaces(IntPtr DeviceInfoSet, IntPtr DeviceInfoData, ref System.Guid InterfaceClassGuid, Int32 MemberIndex, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, SPDRP Property, out int PropertyRegDataType, byte[] PropertyBuffer, uint PropertyBufferSize, out UInt32 RequiredSize);

        [DllImport("setupapi.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetupDiGetDeviceRegistryProperty(IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData, SPDRP Property, IntPtr PropertyRegDataType, IntPtr PropertyBuffer, uint PropertyBufferSize, out UInt32 RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SetupDiGetClassDevs(ref System.Guid ClassGuid, IntPtr Enumerator, IntPtr hwndParent, Int32 Flags);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceInterfaceDetail(IntPtr DeviceInfoSet, ref SP_DEVICE_INTERFACE_DATA DeviceInterfaceData, IntPtr DeviceInterfaceDetailData, Int32 DeviceInterfaceDetailDataSize, ref Int32 RequiredSize, IntPtr DeviceInfoData);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterDeviceNotification(IntPtr Handle);


        [DllImport("user32.dll", SetLastError = true, EntryPoint = "RegisterClass", CharSet = CharSet.Unicode)]
        internal static extern System.UInt16 RegisterClass([In] ref WNDCLASS lpWndClass);

        [DllImport("kernel32.dll")]
        internal static extern uint GetLastError();

        [DllImport("user32.dll")]
        internal static extern IntPtr DefWindowProc(IntPtr hWnd, uint uMsg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        internal static extern void PostQuitMessage(int nExitCode);

        [DllImport("user32.dll")]
        internal static extern sbyte GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin,
           uint wMsgFilterMax);

        [DllImport("user32.dll")]
        internal static extern sbyte TranslateMessage(out MSG lpMsg);

        [DllImport("user32.dll")]
        internal static extern IntPtr DispatchMessage(out MSG lpMsg);

        [DllImport("user32.dll")]
        internal static extern IntPtr LoadCursor(IntPtr hInstance, int lpCursorName);

        [UnmanagedFunctionPointer(CallingConvention.Winapi, SetLastError = true)]
        internal delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        internal static extern bool UpdateWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        internal static extern bool DestroyWindow(IntPtr hWnd);


        [DllImport("user32.dll", SetLastError = true, EntryPoint = "CreateWindowEx", CharSet = CharSet.Unicode)]
        internal static extern IntPtr CreateWindowEx(
           int dwExStyle,
          //UInt16 regResult,
          [MarshalAs(UnmanagedType.LPWStr)]
          string lpClassName,
        [MarshalAs(UnmanagedType.LPWStr)]
        string lpWindowName,
           UInt32 dwStyle,
           int x,
           int y,
           int nWidth,
           int nHeight,
           IntPtr hWndParent,
           IntPtr hMenu,
           IntPtr hInstance,
           IntPtr lpParam);

        private const int ERROR_NO_MORE_ITEMS = 259;
        private const int ERROR_INSUFFICIENT_BUFFER = 122;
    }
}
