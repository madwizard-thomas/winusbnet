/*  WinUSBNet library
 *  (C) 2010 Thomas Bleeker (www.madwizard.org)
 *  
 *  Licensed under the MIT license, see license.txt or:
 *  http://www.opensource.org/licenses/mit-license.php
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MadWizard.WinUSBNet
{

    /// <summary>
    /// Describes the policy for a specific USB pipe
    /// </summary>
    public class USBPipePolicy
    {

        private byte _pipeID;
        private int _interfaceIndex;
        private USBDevice _device;

        internal USBPipePolicy(USBDevice device, int interfaceIndex, byte pipeID)
        {
            _pipeID = pipeID;
            _interfaceIndex = interfaceIndex;
            _device = device;
        }

 

        private void RequireDirection(bool inDir)
        {
            // Some policy types only apply specifically to IN or OUT direction pipes
            // This function checks for this.
            if (inDir && (_pipeID & 0x80) == 0)
                throw new USBException("This policy type is only allowed on IN direction pipes.");
            if (!inDir && (_pipeID & 0x80) != 0)
                throw new USBException("This policy type is only allowed on OUT direction pipes.");
        }

        /// <summary>
        /// When false, read requests fail when the device returns more data than requested. When true, extra data is 
        /// saved and returned on the next read. Default value is true.
        /// </summary>
        public bool AllowPartialReads
        {
            get
            {
                RequireDirection(true);
                return _device.InternalDevice.GetPipePolicyBool(_interfaceIndex, _pipeID, API.POLICY_TYPE.ALLOW_PARTIAL_READS);
            }
            set
            {
                RequireDirection(true);
                _device.InternalDevice.SetPipePolicy(_interfaceIndex, _pipeID, API.POLICY_TYPE.ALLOW_PARTIAL_READS, value);
            }
        }


        public bool AutoClearStall
        {
            get
            {
                return _device.InternalDevice.GetPipePolicyBool(_interfaceIndex, _pipeID, API.POLICY_TYPE.AUTO_CLEAR_STALL);
            }
            set
            {
                _device.InternalDevice.SetPipePolicy(_interfaceIndex, _pipeID, API.POLICY_TYPE.AUTO_CLEAR_STALL, value);
            }
        }
        
        public bool AutoFlush
        {
            get
            {
                RequireDirection(true);
                return _device.InternalDevice.GetPipePolicyBool(_interfaceIndex, _pipeID, API.POLICY_TYPE.AUTO_FLUSH); ;
            }
            set
            {
                RequireDirection(true);
                _device.InternalDevice.SetPipePolicy(_interfaceIndex, _pipeID, API.POLICY_TYPE.AUTO_FLUSH, value);
            }
        }
        
        public bool IgnoreShortPackets
        {
            get
            {
                RequireDirection(true);
                return _device.InternalDevice.GetPipePolicyBool(_interfaceIndex, _pipeID, API.POLICY_TYPE.IGNORE_SHORT_PACKETS); ;
            }
            set
            {
                RequireDirection(true);
                _device.InternalDevice.SetPipePolicy(_interfaceIndex, _pipeID, API.POLICY_TYPE.IGNORE_SHORT_PACKETS, value);
            }
        }


        public int PipeTransferTimeout
        {
            get
            {
                return (int)_device.InternalDevice.GetPipePolicyUInt(_interfaceIndex, _pipeID, API.POLICY_TYPE.PIPE_TRANSFER_TIMEOUT);
            }
            set
            {
                if (value < 0)
                    throw new ArgumentException("Pipe transfer timeout cannot be negative.");
                _device.InternalDevice.SetPipePolicy(_interfaceIndex, _pipeID, API.POLICY_TYPE.PIPE_TRANSFER_TIMEOUT, (uint)value);
            }
        }


        public bool RawIO
        {
            get
            {
                return _device.InternalDevice.GetPipePolicyBool(_interfaceIndex, _pipeID, API.POLICY_TYPE.RAW_IO); ;
            }
            set
            {
                _device.InternalDevice.SetPipePolicy(_interfaceIndex, _pipeID, API.POLICY_TYPE.RAW_IO, value);
            }
        }

      
        public bool ShortPacketTerminate
        {
            get
            {
                RequireDirection(false);
                return _device.InternalDevice.GetPipePolicyBool(_interfaceIndex, _pipeID, API.POLICY_TYPE.SHORT_PACKET_TERMINATE); ;
            }
            set
            {
                RequireDirection(false);
                _device.InternalDevice.SetPipePolicy(_interfaceIndex, _pipeID, API.POLICY_TYPE.SHORT_PACKET_TERMINATE, value);
            }
        }

    }
}
