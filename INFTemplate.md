**NOTE: please don't just copy this INF file but read the instructions below!**

```
[Version]
Signature="$Windows NT$"
Class=CustomClassName
ClassGuid={FDE184DF-2C3E-4af3-9D6D-CEDE19D02714}
Provider=%ProviderName%
DriverVer=01/16/2010,1.0.0
; CatalogFile=catalog.cat

[ClassInstall32]
AddReg=CustomClassAddReg

[CustomClassAddReg]
HKR,,,,%DisplayClassName%
HKR,,Icon,,-20

[Manufacturer]
%ProviderName% = MyWinUSBDevice,NTx86,NTamd64

[MyWinUSBDevice.NTx86]
%DeviceName%=USB_Install, USB\VID_XXXX&PID_YYYY

[MyWinUSBDevice.NTamd64]
%DeviceName%=USB_Install, USB\VID_XXXX&PID_YYYY

[USB_Install]
Include=winusb.inf
Needs=WINUSB.NT

[USB_Install.Services]
Include=winusb.inf
AddService=WinUSB,0x00000002,WinUSB_ServiceInstall

[WinUSB_ServiceInstall]
DisplayName     = %WinUSB_SvcDesc%
ServiceType     = 1
StartType       = 3
ErrorControl    = 1
ServiceBinary   = %12%\WinUSB.sys

[USB_Install.Wdf]
KmdfService=WINUSB, WinUsb_Install

[WinUSB_Install]
KmdfLibraryVersion=1.9

[USB_Install.HW]
AddReg=Dev_AddReg

[Dev_AddReg]
HKR,,DeviceInterfaceGUIDs,0x10000,"{BB9176E8-924F-4a7e-963A-6DC6A4E87FC2}"

[USB_Install.CoInstallers]
AddReg=CoInstallers_AddReg
CopyFiles=CoInstallers_CopyFiles

[CoInstallers_AddReg]
HKR,,CoInstallers32,0x00010000,"WinUSBCoInstaller2.dll","WdfCoInstaller01009.dll,WdfCoInstaller"

[CoInstallers_CopyFiles]
WinUSBCoInstaller2.dll
WdfCoInstaller01009.dll

[DestinationDirs]
CoInstallers_CopyFiles=11

[SourceDisksNames]
1 = %InstallDisk%,,,\x86
2 = %InstallDisk%,,,\amd64

[SourceDisksFiles.x86]
WinUSBCoInstaller2.dll=1
WdfCoInstaller01009.dll=1

[SourceDisksFiles.amd64]
WinUSBCoInstaller2.dll=2
WdfCoInstaller01009.dll=2

[Strings]
ProviderName="Provider of this INF file"
Manufacturer="Device manufacturer"
DeviceName="TestDevice"
DisplayClassName="Test class"
InstallDisk="Installation disk or directory"
WinUSB_SvcDesc="WinUSBService"
```

You can use this INF file as a template, but do change the following settings:

  * Class and ClassGuid: specify the category which your device falls under in the device manager. Your device can either belong to a [system-supplied device class](http://msdn.microsoft.com/en-us/library/ms791134.aspx) or you can create your own. If you are using a system-supplied device class (you should if there is one(, specify its **Class** and **ClassGuid** value in the Version section and please remove the **ClassInstall32** and **CustomClassAddReg** sections. If no device class matches your device, you can add your own by generating a unique GUID (don't use the one from the template!) for **ClassGuid** and select a unique name for the **Class** property. Microsoft has a 'GUID generator' utility available. The **%DisplayClassName%** string in the Strings section can be modified to a user-friendly class name.
  * **DriverVer** should be the date you created the INF file in english date format (mm/dd/yyyy). A driver version can also be added.
  * In the USB\VID\_XXXX&PID\_YYYY strings (both of them) XXXX and YYY need to be changed to your device's VID and PID numbers in hexadecimal. If WinUSB needs to be installed for a specific interface (in case of a composite device), you can use the form USB\VID\_XXXX&PID\_YYYY&MI\_ZZ, where ZZ is the interface number.
  * In the strings section the name of the device, manufacturer, provider etc. can be specified.
  * In the **Dev\_AddReg** section you should place another unique GUID (do not use the same one as the ClassGuid). You will use this GUID with the WinUSB API to list your devices.
  * WinUSB requires two coinstallers: **WinUSBCoInstaller2.dll** and **WdfCoInstaller01009.dll**. These files will install WinUSB if it is not yet installed or outdated. There are different coinstallers for 32-bit and 64-bit systems. The coinstallers are available from the [Windows DDK](http://www.microsoft.com/whdc/devtools/ddk/default.mspx) in the \redist\winusb\x86 (amd64) and \redist\wdf\x86 (amd64) directories. You should copy these files into two directories, x86 and amd64 next to your inf file. Note that newer versions of these DLLs may have different names which you need to replace in the inf file as well. Also the line KmdfLibraryVersion=1.9 should reflect the version of the WdfCoInstaller.

