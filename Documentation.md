For documentation see the API reference. Here are some quick getting started snippets:

[Library reference online](http://winusbnet.googlecode.com/svn/trunk/docs/index.html)
See also: [INF file template](INFTemplate.md)

## Opening devices ##

Opening a single device (first device matching GUID):
```
// GUID is an example, specify your own unique GUID in the .inf file
USBDevice device = USBDevice.GetSingleDevice("{BB9176E8-924F-4a7e-963A-6DC6A4E87FC2}");
// device will be null if no device could be found
```

Listing all connected devices, then opening one:
```
// GUID is an example, specify your own unique GUID in the .inf file
USBDeviceInfo[] details = USBDevice.GetDevices("{BB9176E8-924F-4a7e-963A-6DC6A4E87FC2}");
// Find your device in the array
USBDevice device = new USBDevice(details[index]);
```

Or use LINQ/lambda expressions for advanced filtering:
```
USBDeviceInfo match = details.First(info => info.VID == 0x8888 && info.PID == 0x9999);
USBDevice device = new USBDevice(match); 
```

## Control transfers ##

OUT transfers
```
// Without data:
device.ControlOut(requestType, request, value, index);

// With data buffer:
device.ControlOut(requestType, request, value, index, dataBuffer);
```

IN transfers
```
// Read data into buffer:
byte[] data = new byte[dataLength];
device.ControlIn(requestType, request, value, index, data);

// Automatically create buffer:
byte[] data =  device.ControlIn(requestType, request, value, index, dataLength);
```

## Finding interfaces ##

Looping through interfaces
```
foreach (USBInterface iface in device.Interfaces)
{
    // Use iface.BaseClass, iface.SubClass etc.
}
```

Finding by base class:
```
USBInterface iface = device.Interfaces.Find(USBBaseClass.VendorSpecific);
```

Finding with lambda expression:
```
USBInterface iface = device.Interfaces.First(
                        usbIf => 
                               usbIf.BaseClass == USBBaseClass.VendorSpecific &&
                               usbIf.Protocol == 2
                    );
```

## Writing to interface pipes ##
Reading from first IN endpoint on interface
```
byte[] buffer = new byte[length];
iface.InPipe.Read(buffer);
```

Writing to first OUT endpoint on interface
```
byte[] buffer = new byte[length];
// fill buffer
iface.OutPipe.Write(buffer);
```

Reading from specific pipe
```
iface.Pipes[pipeAddress].Read(buffer);
```