# WinUSBNet

WinUSBNet is a .NET class library that provides easy access to the WinUSB API from C#, VB.NET and other .NET languages. WinUSB is a user mode API available for Windows XP, Vista and 7 (XP will require an update), allowing low level access to USB devices such as control transfers and reading from and writing to endpoints.

Please note that there is at least one different project with the same name (at codeplex), this libary is not related.

## Download

[Download latest release](https://github.com/madwizard-thomas/winusbnet/releases/latest)

## Status

The library is *stable*. If you find any bugs or problems please report them using the issue tracker or add a pull request if you have fixed something yourself.

## Features

  * MIT licensed with C# source code available (free for both personal and commercial use)
  * CLS compliant library (usable from all .NET languages such as C#, VB.NET and C++.NET)
  * Synchronous and asynchronous data transfers
  * Support for 32-bit and 64-bit Windows versions
  * Notification events for device attachment and removal
  * Support for multiple interfaces and endpoints
  * Intellisense documentation

## Related documentation
  * [Library reference online](http://madwizard-thomas.github.io/winusbnet/docs/)
  * [Online wiki with short howto](https://github.com/madwizard-thomas/winusbnet/wiki)
  * [Changelog](Changelog.md)
  * [WinUSB overview](https://docs.microsoft.com/en-us/windows-hardware/drivers/usbcon/winusb)
  * [How to Access a USB Device by Using WinUSB Functions](https://docs.microsoft.com/en-us/windows-hardware/drivers/usbcon/using-winusb-api-to-communicate-with-a-usb-device)
  * [Jan Axelson's page on WinUSB](http://janaxelson.com/winusb.htm)