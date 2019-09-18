# Transferrer
A small tool for transferring files between devices within the same LAN.

## User Guide
Currently this project is still in development, but you may have a taste.
**We don't have excutables exported temporily**, so you need `Visual Studio 2019` and `dotnet core sdk 2.2` to complete compilation, and you can compile it to any platform supported by .Net Core.

### Core
> #### For RECEIVER
>> `-a` active mode, will respond to Send's scanning.

>>`--start` start working, will receive files or text sent by sender.

> #### For SENDER
>> `-s` send scan signal, then save a receiver.

>> `-l` list all recorded device.

>> `-t <device name> <text>` send text to specified device.

>> `-f <device name> <file name>` send file to specified device.


## TODO: 
|Steps|Status|
|-|:-:|
|1. Multi-platform core library.|✔|
|2. Interface on Android.|![](https://camo.githubusercontent.com/8367389469b3bc13ceaa808e9b85991abbf57b7c/687474703a2f2f696d672e6c616e72656e74756b752e636f6d2f696d672f616c6c696d672f313231322f352d31323132303431393352302e676966)|
|3. Interface on Windows.|❌|
