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
    /// UsbPipe represents a single pipe on a WinUSB device. A pipe is connected
    /// to a certain endpoint on the device and has a fixed direction (IN or OUT)
    /// </summary>
    public class USBPipe
    {
        private API.WINUSB_PIPE_INFORMATION _pipeInfo;
        private USBInterface _interface = null;
        private USBDevice _device;
        private USBPipePolicy _policy;

        /// <summary>
        /// Endpoint address including the direction in the most significant bit
        /// </summary>
        public byte Address 
        {
            get
            {
                return _pipeInfo.PipeId;
            }
        }
        
        /// <summary>
        /// The USBDevice this pipe is associated with
        /// </summary>
        public USBDevice Device
        {
            get
            {
                return _device;
            }
        }
        
        /// <summary>
        /// Maximum packet size for transfers on this endpoint
        /// </summary>
        public int MaximumPacketSize
        {
            get
            {
                return _pipeInfo.MaximumPacketSize;
            }
        }

        /// <summary>
        /// The interface associated with this pipe
        /// </summary>
        public USBInterface Interface
        {
            get
            {
                return _interface;
            }
        }

        /// <summary>
        /// The pipe policy settings for this pipe
        /// </summary>
        public USBPipePolicy Policy
        {
            get
            {
                return _policy;
            }
        }

        /// <summary>
        /// True if the pipe has direction OUT (host to device), false otherwise.
        /// </summary>
        public bool IsOut
        {
            get
            {
                return (_pipeInfo.PipeId & 0x80) == 0;
            }
        }

        /// <summary>
        /// True if the pipe has direction IN (device to host), false otherwise.
        /// </summary>
        public bool IsIn
        {
            get
            {
                return (_pipeInfo.PipeId & 0x80) != 0;
            }
        }

        /// <summary>
        /// Reads data from the pipe into a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read data into. The maximum number of bytes that will be read is specified by the length of the buffer.</param>
        /// <returns>The number of bytes read from the pipe.</returns>
        public int Read(byte[] buffer)
        {
            return Read(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Reads data from the pipe into a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to read data into.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> from which to begin writing data read from the pipe.</param>
        /// <param name="length">The maximum number of bytes to read, starting at offset</param>
        /// <returns>The number of bytes read from the pipe.</returns>
        public int Read(byte[] buffer, int offset, int length)
        {
            CheckReadParams(buffer, offset, length);

            try
            {
                uint bytesRead;
                
                _device.InternalDevice.ReadPipe(Interface.InterfaceIndex, _pipeInfo.PipeId, buffer, offset, length, out bytesRead);
                
                return (int)bytesRead;
            }
            catch (API.APIException e)
            {
                throw new USBException("Failed to read from pipe.", e);
            }
        }

        private void CheckReadParams(byte[] buffer, int offset, int length)
        {
            if (!IsIn)
                throw new USBException("Cannot read from a pipe with OUT direction.");

            int bufferLength = buffer.Length;
            if (offset < 0 || offset >= bufferLength)
                throw new ArgumentOutOfRangeException("Offset of data to read is outside the buffer boundaries.");
            if (length < 0 || (offset + length) > bufferLength)
                throw new ArgumentOutOfRangeException("Length of data to read is outside the buffer boundaries.");
        }
        private void CheckWriteParams(byte[] buffer, int offset, int length)
        {
            if (!IsOut)
                throw new USBException("Cannot write to a pipe with IN direction.");

            int bufferLength = buffer.Length;
            if (offset < 0 || offset >= bufferLength)
                throw new ArgumentOutOfRangeException("Offset of data to write is outside the buffer boundaries.");
            if (length < 0 || (offset + length) > bufferLength)
                throw new ArgumentOutOfRangeException("Length of data to write is outside the buffer boundaries.");

        }

        public IAsyncResult BeginRead(byte[] buffer, int offset, int length, AsyncCallback userCallback, object stateObject)
        {
            CheckReadParams(buffer, offset, length);

            USBAsyncResult result = new USBAsyncResult(userCallback, stateObject);
            try
            {
                _device.InternalDevice.ReadPipeOverlapped(Interface.InterfaceIndex, _pipeInfo.PipeId, buffer, offset, length, result);
            }
            catch (API.APIException e)
            {
                if (result != null)
                    result.Dispose();
                throw new USBException("Failed to read from pipe.", e);
            }
            catch (Exception)
            {
                if (result != null)
                    result.Dispose();
                throw;
            }
            return result;
        }

        public int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new NullReferenceException("asyncResult cannot be null");
            if (!(asyncResult is USBAsyncResult))
                throw new ArgumentException("AsyncResult object was not created by calling BeginRead on this class.");

            // todo: check duplicate end reads?
            USBAsyncResult result = (USBAsyncResult)asyncResult;
            try
            {
                if (!result.IsCompleted)
                    result.AsyncWaitHandle.WaitOne();

                if (result.Error != null)
                    throw new USBException("Asynchronous read from pipe has failed.", result.Error);

                return result.BytesTransfered;
            }
            finally
            {
                result.Dispose();
            }

        }

        /// <summary>
        /// Writes data from a buffer to the pipe.
        /// </summary>
        /// <param name="buffer">The buffer to write data from. The complete buffer will be written to the device.</param>
        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }
        
        /// <summary>
        /// Writes data from a buffer to the pipe.
        /// </summary>
        /// <param name="buffer">The buffer to write data from.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> from which to begin writing.</param>
        /// <param name="length">The number of bytes to write, starting at offset</param>
        public void Write(byte[] buffer, int offset, int length)
        {
            CheckWriteParams(buffer, offset, length);

            if (offset != 0)
            {
                // Not the complete buffer, slice buffer
                byte[] newBuffer = new byte[length];
                Array.Copy(buffer, offset, newBuffer, 0, length);
                buffer = newBuffer;
            }

            try
            {
                _device.InternalDevice.WritePipe(Interface.InterfaceIndex, _pipeInfo.PipeId, buffer, offset, length);
            }
            catch (API.APIException e)
            {
                throw new USBException("Failed to write to pipe.", e);
            }
        }

        public IAsyncResult BeginWrite(byte[] buffer, int offset, int length, AsyncCallback userCallback, object stateObject)
        {
            CheckWriteParams(buffer, offset, length);

            USBAsyncResult result = new USBAsyncResult(userCallback, stateObject);
            try
            {
                _device.InternalDevice.WriteOverlapped(Interface.InterfaceIndex, _pipeInfo.PipeId, buffer, offset, length, result);
            }
            catch (API.APIException e)
            {
                if (result != null)
                    result.Dispose();
                throw new USBException("Failed to write to pipe.", e);
            }
            catch (Exception)
            {
                if (result != null)
                    result.Dispose();
                throw;
            }
            return result;
        }

        public void EndWrite(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new NullReferenceException("asyncResult cannot be null");
            if (!(asyncResult is USBAsyncResult))
                throw new ArgumentException("AsyncResult object was not created by calling BeginWrite on this class.");

            USBAsyncResult result = (USBAsyncResult)asyncResult;
            try
            {
                // todo: check duplicate end writes?

                if (!result.IsCompleted)
                    result.AsyncWaitHandle.WaitOne();

                if (result.Error != null)
                    throw new USBException("Asynchronous write from pipe has failed.", result.Error);
            }
            finally
            {
                result.Dispose();
            }
        }

        /// <summary>
        /// Aborts all pending transfers for this pipe.
        /// </summary>
        public void Abort()
        {
            try
            {
                _device.InternalDevice.AbortPipe(Interface.InterfaceIndex, _pipeInfo.PipeId);
            }
            catch (API.APIException e)
            {
                throw new USBException("Failed to abort pipe.", e);
            }
        }
        
        /// <summary>
        /// Flushes the pipe, discarding any data that is cached. Only available on IN direction pipes.
        /// </summary>
        public void Flush()
        {
            if (!IsIn)
                throw new USBException("Flush is only supported on IN direction pipes");
            try
            {
                _device.InternalDevice.FlushPipe(Interface.InterfaceIndex, _pipeInfo.PipeId);
            }
            catch (API.APIException e)
            {
                throw new USBException("Failed to flush pipe.", e);
            }
        }

        internal USBPipe(USBDevice device, API.WINUSB_PIPE_INFORMATION pipeInfo)
        {
            _pipeInfo = pipeInfo;
            _device = device;

            // Policy is not set until interface is attached
            _policy = null;
        }

        internal void AttachInterface(USBInterface usbInterface)
        {
            _interface = usbInterface;

            // Initialize policy now that interface is set (policy requires interface)
            _policy = new USBPipePolicy(_device, _interface.InterfaceIndex, _pipeInfo.PipeId);
        }
        
    }

}