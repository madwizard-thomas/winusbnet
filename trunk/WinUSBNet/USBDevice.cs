/*  WinUSBNet library
 *  (C) 2010 Thomas Bleeker (www.madwizard.org)
 *  
 *  Licensed under the MIT license, see license.txt or:
 *  http://www.opensource.org/licenses/mit-license.php
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace MadWizard.WinUSBNet
{
    /// <summary>
    /// The UsbDevice class represents a single WinUSB device.
    /// </summary>
    public class USBDevice : IDisposable
    {
        private API.WinUSBDevice _wuDevice = null;
        private bool _disposed = false;

        /// <summary>
        /// Collection of all pipes available on the USB device
        /// </summary>
        public USBPipeCollection Pipes
        {
            get;
            private set;
        }



        
        /// <summary>
        /// Collection of all interfaces available on the USB device
        /// </summary>
        public USBInterfaceCollection Interfaces
        {
            get;
            private set;
        }

        /// <summary>
        /// Device descriptor with information about the device
        /// </summary>
        public USBDeviceDescriptor Descriptor
        {
            get;
            private set;
        }

        /// <summary>
        /// Constructs a new USB device
        /// </summary>
        /// <param name="deviceInfo">USB device info of the device to create</param>
        public USBDevice(USBDeviceInfo deviceInfo)
            : this(deviceInfo.DevicePath)
        {
            // Handled in other constructor
        }

        /// <summary>
        /// Disposes the UsbDevice including all unmanaged WinUSB handles. This function
        /// should be called when the UsbDevice object is no longer in use, otherwise
        /// unmanaged handles will remain open until the garbage collector finalizes the
        /// object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Finalizer for the UsbDevice. Disposes all unmanaged handles.
        /// </summary>
        ~USBDevice()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes the object
        /// </summary>
        /// <param name="disposing">Indicates wether Dispose was called manually (true) or by
        /// the garbage collector (false) via the destructor.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                if (_wuDevice != null)
                    _wuDevice.Dispose();
            }

            // Clean unmanaged resources here.
            // (none currently)

            _disposed = true;
        }

        /// <summary>
        /// Constructs a new USB device
        /// </summary>
        /// <param name="devicePathName">Device path name of the USB device to create</param>
        public USBDevice(string devicePathName)
        {
            Descriptor = GetDeviceDescriptor(devicePathName);
            _wuDevice = new API.WinUSBDevice();
            try
            {
                _wuDevice.OpenDevice(devicePathName);
                InitializeInterfaces();
            }
            catch (API.APIException e)
            {
                _wuDevice.Dispose();
                throw new USBException("Failed to open device.", e);
            }
        }

        internal API.WinUSBDevice InternalDevice
        {
            get
            {
                return _wuDevice;
            }
        }

        private void InitializeInterfaces()
        {
            int numInterfaces = _wuDevice.InterfaceCount;

            List<USBPipe> allPipes = new List<USBPipe>();

            USBInterface[] interfaces = new USBInterface[numInterfaces];
            // UsbEndpoint
            for (int i = 0; i < numInterfaces; i++)
            {
                API.USB_INTERFACE_DESCRIPTOR descriptor;
                API.WINUSB_PIPE_INFORMATION[] pipesInfo;
                _wuDevice.GetInterfaceInfo(i, out descriptor, out pipesInfo);
                USBPipe[] interfacePipes = new USBPipe[pipesInfo.Length];
                for(int k=0;k<pipesInfo.Length;k++)
                {
                    USBPipe pipe = new USBPipe(this, pipesInfo[k]);
                    interfacePipes[k] = pipe;
                    allPipes.Add(pipe);
                }
                // TODO:
                //if (descriptor.iInterface != 0)
                //    _wuDevice.GetStringDescriptor(descriptor.iInterface);
                USBPipeCollection pipeCollection = new USBPipeCollection(interfacePipes);
                interfaces[i] = new USBInterface(this, i, descriptor, pipeCollection); 
            }
            Pipes = new USBPipeCollection(allPipes.ToArray());
            Interfaces = new USBInterfaceCollection(interfaces);
        }

        private void CheckControlParams(int value, int index, byte[] data, int length)
        {
            if (value < ushort.MinValue || value > ushort.MaxValue)
                throw new USBException("Value parameter out of range.");
            if (index < ushort.MinValue || index > ushort.MaxValue)
                throw new USBException("Index parameter out of range.");
            if (length > data.Length)
                throw new USBException("Length parameter is larger than the size of the data buffer.");
            if (length > ushort.MaxValue)
                throw new USBException("Length too large");
        }

        public void ControlTransfer(byte requestType, byte request, int value, int index, byte[] data, int length)
        {
            // Parameters are int and not ushort because ushort is not CLS compliant.
            CheckNotDisposed();
            CheckControlParams(value, index, data, length);
            
            try
            {
                _wuDevice.ControlTransfer(requestType, request, (ushort)value, (ushort)index, (ushort)length, data);
            }
            catch (API.APIException e)
            {
                throw new USBException("Control transfer failed", e);
            }
        }

        public IAsyncResult BeginControlTransfer(byte requestType, byte request, int value, int index, byte[] data, int length, AsyncCallback userCallback, object stateObject)
        {
            // Parameters are int and not ushort because ushort is not CLS compliant.
            CheckNotDisposed();
            CheckControlParams(value, index, data, length);

            USBAsyncResult result = new USBAsyncResult(userCallback, stateObject);
            
            try
            {
                _wuDevice.ControlTransferOverlapped(requestType, request, (ushort)value, (ushort)index, (ushort)length, data, result);
            }
            catch (API.APIException e)
            {
                if (result != null)
                    result.Dispose();
                 throw new USBException("Asynchronous control transfer failed", e);
            }
            catch (Exception)
            {
                if (result != null)
                    result.Dispose();
                throw;
            }
            return result;
        }

        public int EndControlTransfer(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new NullReferenceException("asyncResult cannot be null");
            if (!(asyncResult is USBAsyncResult))
                throw new ArgumentException("AsyncResult object was not created by calling one of the BeginControl* methods on this class.");

            // todo: check duplicate end control
            USBAsyncResult result = (USBAsyncResult)asyncResult;
            try
            {
                if (!result.IsCompleted)
                    result.AsyncWaitHandle.WaitOne();

                if (result.Error != null)
                    throw new USBException("Asynchronous control transfer from pipe has failed.", result.Error);

                return result.BytesTransfered;
            }
            finally
            {
                result.Dispose();
            }

        }


        public void ControlTransfer(byte requestType, byte request, int value, int index, byte[] data)
        {
            ControlTransfer(requestType, request, value, index, data, data.Length);
        }

        public void ControlTransfer(byte requestType, byte request, int value, int index)
        {
            // TODO: null instead of empty buffer. But overlapped code would have to be fixed for this (no buffer to pin)
            ControlTransfer(requestType, request, value, index, new byte[0], 0);
        }

        private void CheckIn(byte requestType)
        {
            if ((requestType & 0x80) == 0) // Host to device?
                throw new USBException("Request type is not IN.");
        }

        private void CheckOut(byte requestType)
        {
            if ((requestType & 0x80) == 0x80) // Device to host?
                throw new USBException("Request type is not OUT.");
        }

        public byte[] ControlIn(byte requestType, byte request, int value, int index, int length)
        {
            CheckIn(requestType);
            byte[] data = new byte[length];
            ControlTransfer(requestType, request, value, index, data, data.Length);
            return data;
        }


        public byte[] ControlIn(byte requestType, byte request, int value, int index, byte[] data, int length)
        {
            CheckIn(requestType);
            ControlTransfer(requestType, request, value, index, data, length);
            return data;
        }

        public byte[] ControlIn(byte requestType, byte request, int value, int index, byte[] data)
        {
            CheckIn(requestType);
            ControlTransfer(requestType, request, value, index, data);
            return data;
        }

        public void ControlOut(byte requestType, byte request, int value, int index, byte[] data)
        {
            CheckOut(requestType);
            ControlTransfer(requestType, request, value, index, data);
        }

        public void ControlOut(byte requestType, byte request, int value, int index)
        {
            CheckOut(requestType);
            // TODO: null instead of empty buffer. But overlapped code would have to be fixed for this (no buffer to pin)
            ControlTransfer(requestType, request, value, index, new byte[0]); 
        }
        
        public void ControlOut(byte requestType, byte request, int value, int index, byte[] data, int length)
        {
            CheckOut(requestType);
            ControlTransfer(requestType, request, value, index, data, length);
        }

        public IAsyncResult BeginControlTransfer(byte requestType, byte request, int value, int index, byte[] data, AsyncCallback userCallback, object stateObject)
        {
            return BeginControlTransfer(requestType, request, value, index, data, data.Length, userCallback, stateObject);
        }

        public IAsyncResult BeginControlTransfer(byte requestType, byte request, int value, int index, AsyncCallback userCallback, object stateObject)
        {
            // TODO: null instead of empty buffer. But overlapped code would have to be fixed for this (no buffer to pin)
            return BeginControlTransfer(requestType, request, value, index, new byte[0], 0, userCallback, stateObject);
        }

        public IAsyncResult BeginControlIn(byte requestType, byte request, int value, int index, byte[] data, int length, AsyncCallback userCallback, object stateObject)
        {
            CheckIn(requestType);
            return BeginControlTransfer(requestType, request, value, index, data, length, userCallback, stateObject);
        }

        public IAsyncResult BeginControlIn(byte requestType, byte request, int value, int index, byte[] data, AsyncCallback userCallback, object stateObject)
        {
            CheckIn(requestType);
            return BeginControlTransfer(requestType, request, value, index, data, userCallback, stateObject);
        }

        public IAsyncResult BeginControlOut(byte requestType, byte request, int value, int index, byte[] data, AsyncCallback userCallback, object stateObject)
        {
            CheckOut(requestType);
            return BeginControlTransfer(requestType, request, value, index, data, userCallback, stateObject);
        }

        public IAsyncResult BeginControlOut(byte requestType, byte request, int value, int index, AsyncCallback userCallback, object stateObject)
        {
            CheckOut(requestType);
            // TODO: null instead of empty buffer. But overlapped code would have to be fixed for this (no buffer to pin)
            return BeginControlTransfer(requestType, request, value, index, new byte[0], userCallback, stateObject);
        }

        public IAsyncResult BeginControlOut(byte requestType, byte request, int value, int index, byte[] data, int length, AsyncCallback userCallback, object stateObject)
        {
            CheckOut(requestType);
            return BeginControlTransfer(requestType, request, value, index, data, length, userCallback, stateObject);
        } 
        


        private void CheckNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException("USB device object has been disposed.");
        }

        /// <summary>
        /// Finds WinUSB devices with a GUID matching the parameter guidString
        /// </summary>
        /// <param name="guidString">The GUID string that the device should match.
        /// The format of this string may be any format accepted by the constructor 
        /// of the System.Guid class</param>
        /// <returns>An array of USBDeviceInfo objects representing the 
        /// devices found. When no devices are found an empty array is 
        /// returned.</returns>
        public static USBDeviceInfo[] GetDevices(string guidString)
        {
            return GetDevices(new Guid(guidString));
        }

        /// <summary>
        /// Finds WinUSB devices with a GUID matching the parameter guid
        /// </summary>
        /// <param name="guid">The GUID that the device should match.</param>
        /// <returns>An array of USBDeviceInfo objects representing the 
        /// devices found. When no devices are found an empty array is 
        /// returned.</returns>
        public static USBDeviceInfo[] GetDevices(Guid guid)
        {
            API.DeviceDetails[] detailList = API.DeviceManagement.FindDevicesFromGuid(guid);

            USBDeviceInfo[] devices = new USBDeviceInfo[detailList.Length];

            for (int i = 0; i < detailList.Length; i++)
            {
                devices[i] = new USBDeviceInfo(detailList[i]);
            }
            return devices;
        }

        /// <summary>
        /// Finds the first WinUSB device with a GUID matching the parameter guid.
        /// If multiple WinUSB devices match the GUID only the first one is returned.
        /// </summary>
        /// <param name="guid">The GUID that the device should match.</param>
        /// <returns>An UsbDevice object representing the device if found. If
        /// no device with the given GUID could be found null is returned.</returns>
        public static USBDevice GetSingleDevice(Guid guid)
        {
            API.DeviceDetails[] detailList = API.DeviceManagement.FindDevicesFromGuid(guid);
            if (detailList.Length == 0)
                return null;

      
            return new USBDevice(detailList[0].DevicePath);
        }

        /// <summary>
        /// Finds the first WinUSB device with a GUID matching the parameter guidString.
        /// If multiple WinUSB devices match the GUID only the first one is returned.
        /// </summary>
        /// <param name="guidString">The GUID string that the device should match.</param>
        /// <returns>An UsbDevice object representing the device if found. If
        /// no device with the given GUID could be found null is returned.</returns>
        public static USBDevice GetSingleDevice(string guidString)
        {
            
            return USBDevice.GetSingleDevice(new Guid(guidString));         
        }

        private static USBDeviceDescriptor GetDeviceDescriptor(string devicePath)
        {
            try
            {
                USBDeviceDescriptor descriptor;
                using (API.WinUSBDevice wuDevice = new API.WinUSBDevice())
                {
                    wuDevice.OpenDevice(devicePath);
                    API.USB_DEVICE_DESCRIPTOR deviceDesc = wuDevice.GetDeviceDescriptor();
                    // string q = wuDevice.GetStringDescriptor(0);
                    // TODO: use language id properly
                    string manufacturer = null, product = null, serialNumber = null;
                    byte idx = 0;
                    idx = deviceDesc.iManufacturer;
                    if (idx > 0)
                        manufacturer = wuDevice.GetStringDescriptor(idx);

                    idx = deviceDesc.iProduct;
                    if (idx > 0)
                        product = wuDevice.GetStringDescriptor(idx);

                    idx = deviceDesc.iSerialNumber;
                    if (idx > 0)
                        serialNumber = wuDevice.GetStringDescriptor(idx);
                    descriptor = new USBDeviceDescriptor(devicePath, deviceDesc, manufacturer, product, serialNumber);
                }
                return descriptor;

            }
            catch (API.APIException e)
            {
                throw new USBException("Failed to retrieve device descriptor.", e);
            }
        }
        
    }
}
