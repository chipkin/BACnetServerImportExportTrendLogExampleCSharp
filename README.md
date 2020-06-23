# BACnet Server TrendLog Example C#
A simple BACnet Server example in C# to demonstrate functionality to Import and Export data from Trend Log Objects using the [CAS BACnet Stack](https://store.chipkin.com/services/stacks/bacnet-stack). 

## Releases

Build versions of this example can be downloaded from the [Releases](https://github.com/chipkin/BACnetServerTrendLogExampleCSharp) page.

## Installation

Download the latest release zip file on the [Releases](https://github.com/chipkin/BACnetServerTrendLogExampleCSharp) page.

## Usage
Trendlog object is tied to Analog Input 1 and is updated every 30 seconds. Trendlog Multiple is tied to Analog Input 1, Analog Input 2, Binary Input 3, and Multi-State Input 4.

Pre-configured with the following example BACnet device and objects:
- Device: 389000  (Example Trending Device)
  - analog_input: 1  (AI - Auto Increment)
  - analog_input: 2  (AI - Manual Increment)
  - binary_input: 3  (BI - Binary Input)
  - multi-state_input: 4  (MSI - Multi-State Input)
  - trend_log: 10 (TL - Trend Log (AI1))
  - trend_log_multiple: 20 (TLM - Trend Log Multiple (AI1, AI2, BI3, MSI4))

The following keyboard commands can be issued in the server window:
* **H**: Display help menu
* **F1**: Display Version Info
* **Up Arrow**: Increment Analog Input 2 Present Value
* **Down Arrow**: Decrement Analog Input 2 Present Value
* **B**: Backup TrendLog to File
* **S**: Backup TrendLogMultiple to file
* **Q**: Quit the program

## Build

A [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/) project is included with this project. This project also auto built using [Gitlab CI](https://docs.gitlab.com/ee/ci/) on every commit.

1. Copy *CASBACnetStack_x64_Debug.dll*, *CASBACnetStack_x64_Debug.lib*, *CASBACnetStack_x64_Release.dll*, and *CASBACnetStack_x64_Release.lib* from the [CAS BACnet Stack](https://store.chipkin.com/services/stacks/bacnet-stack) project into the /bin/netcoreapp3.1/ folder.
2. Use [Visual Studio 2019](https://visualstudio.microsoft.com/vs/) to build the project. The solution can be found in the */build/BACnetServerTrendLogExampleCSharp/* folder.

## Example Output
```
Starting Windows BACnetServer TrendLog Example CSharp version: 0.0.1.0
https://github.com/chipkin/BACnetServerTrendLogExampleCSharp
FYI: BACnet Stack version: 3.16.0.1107
FYI: CAS BACnet Stack Setup, successfully
FYI: Loading initial data points to Trend Log and Trend Log Multiple objects
FYI: Initial data load complete
FYI: Starting main loop
FYI: Request for CallbackGetPropertyReal. objectType=0, objectInstance=1, propertyIdentifier=85, propertyArrayIndex=0
FYI: AnalogInput[1].value got [0]
FYI: Request for CallbackGetPropertyReal. objectType=0, objectInstance=1, propertyIdentifier=85, propertyArrayIndex=0
FYI: AnalogInput[1].value got [0]
FYI: Request for CallbackGetPropertyReal. objectType=0, objectInstance=2, propertyIdentifier=85, propertyArrayIndex=0
FYI: AnalogInput[2].value got [0]
FYI: Request for CallbackGetEnumerated. objectType=3, objectInstance=3, propertyIdentifier=85
FYI: BinaryInput[3].value got [False]
FYI: Request for CallbackGetUnsignedInteger. objectType=13, objectInstance=4, propertyIdentifier=85, propertyArrayIndex=0
        F1:      Version Info
        Up:      Increase ManualIncrement Analog Input Present Value by 0.01f
        Down:    Decrease ManualIncrement Analog Input Present Value by 0.01f
        B:       Backup TrendLog to file
        S:       Backup TrendLogMultiple to file
FYI: Recving 19 bytes from 192.168.1.18:58533
FYI: Request for CallbackGetUnsignedInteger. objectType=8, objectInstance=389000, propertyIdentifier=11, propertyArrayIndex=0
```