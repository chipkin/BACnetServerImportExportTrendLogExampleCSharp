/**
 * BACnet Server Trend Log Example CSharp
 * ----------------------------------------------------------------------------
 * Program.cs
 * 
 * In this CAS BACnet Stack example, we create a BACnet IP server with a Trend Log Object
 * and a Trend Log Multiple Object that will be pre-loaded with data from a backed up file.
 * 
 * There are some additional user input options that can trigger logic that will read 
 * the current values in the Trend Logs and back them up in another file.
 *
 * More information https://github.com/chipkin/BACnetServerTrendLogExampleCSharp
 * 
 * Created by: Alex Fontaine
 * Created on: May 29, 2020 
 * Last updated: May 29, 2020
*/

using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using CASBACnetStack;

namespace BACnetServerTrendLogExampleCSharp
{
    class Program
    {
        // Main function
        static void Main(string[] args)
        {
            BACnetServer bacnetServer = new BACnetServer();
            bacnetServer.Run();
        }

        // BACnet Server Object
        unsafe class BACnetServer
        {
            // UDP
            UdpClient udpServer;
            IPEndPoint remoteIpEndPoint;

            string _trendLogBackupPath;
            string _trendLogMultipleBackupPath;

            // Set up the BACnet port 
            const UInt16 SETTING_BACNET_PORT = 47808;

            // A Simple Example Database to store the values used in this example
            private ExampleDatabase database = new ExampleDatabase();

            // Version
            const string APPLICATION_VERSION = "0.0.1";

            // Server setup and main loop
            public void Run()
            {
                Console.WriteLine("Starting Windows BACnetServer TrendLog Example CSharp version: {0}.{1}", APPLICATION_VERSION, CIBuildVersion.CIBUILDNUMBER);
                Console.WriteLine("https://github.com/chipkin/BACnetServerTrendLogExampleCSharp");
                Console.WriteLine("FYI: BACnet Stack version: {0}.{1}.{2}.{3}",
                    CASBACnetStackAdapter.GetAPIMajorVersion(),
                    CASBACnetStackAdapter.GetAPIMinorVersion(),
                    CASBACnetStackAdapter.GetAPIPatchVersion(),
                    CASBACnetStackAdapter.GetAPIBuildVersion());

                // 1. Setup the callbacks
                // ---------------------------------------------------------------------------

                // Send/Recv Callbacks
                CASBACnetStackAdapter.RegisterCallbackSendMessage(SendMessage);
                CASBACnetStackAdapter.RegisterCallbackReceiveMessage(RecvMessage);

                // System Callbacks
                CASBACnetStackAdapter.RegisterCallbackGetSystemTime(CallbackGetSystemTime);

                // Get Property Callbacks
                CASBACnetStackAdapter.RegisterCallbackGetPropertyCharacterString(CallbackGetPropertyCharString);
                CASBACnetStackAdapter.RegisterCallbackGetPropertyEnumerated(CallbackGetEnumerated);
                CASBACnetStackAdapter.RegisterCallbackGetPropertyReal(CallbackGetPropertyReal);
                CASBACnetStackAdapter.RegisterCallbackGetPropertyUnsignedInteger(CallbackGetUnsignedInteger);


                // 2. Setup the BACnet device
                // ---------------------------------------------------------------------------

                // Setup database
                database.Setup();

                // Add Objects
                // ---------------------------------------------------------------------------

                // Add the device
                CASBACnetStackAdapter.AddDevice(database.Device.Instance);
                CASBACnetStackAdapter.SetPropertyEnabled(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_DEVICE, database.Device.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_DESCRIPTION, true);

                // Add analog inputs
                CASBACnetStackAdapter.AddObject(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT, database.AnalogInputAutoIncrement.Instance);
                CASBACnetStackAdapter.AddObject(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT, database.AnalogInputManualIncrement.Instance);

                // Add binary input
                CASBACnetStackAdapter.AddObject(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_BINARY_INPUT, database.BinaryInput.Instance);

                // Add multi-state input
                CASBACnetStackAdapter.AddObject(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_MULTI_STATE_INPUT, database.MultiStateInput.Instance);

                // Add Trend Log
                CASBACnetStackAdapter.AddTrendLogObject(database.Device.Instance, database.TrendLog.Instance, CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT, database.AnalogInputAutoIncrement.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE, 100, false, 0);
                CASBACnetStackAdapter.SetTrendLogTypeToPolled(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_TREND_LOG, database.TrendLog.Instance, true, false, 3000);

                // Add Trend Log Multiple
                CASBACnetStackAdapter.AddTrendLogMultipleObject(database.Device.Instance, database.TrendLogMultiple.Instance, 100);
                CASBACnetStackAdapter.AddLoggedObjectToTrendLogMultiple(database.Device.Instance, database.TrendLogMultiple.Instance, CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT, database.AnalogInputAutoIncrement.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE, false, 0, false, 0);
                CASBACnetStackAdapter.AddLoggedObjectToTrendLogMultiple(database.Device.Instance, database.TrendLogMultiple.Instance, CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT, database.AnalogInputManualIncrement.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE, false, 0, false, 0);
                CASBACnetStackAdapter.AddLoggedObjectToTrendLogMultiple(database.Device.Instance, database.TrendLogMultiple.Instance, CASBACnetStackAdapter.OBJECT_TYPE_BINARY_INPUT, database.BinaryInput.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE, false, 0, false, 0);
                CASBACnetStackAdapter.AddLoggedObjectToTrendLogMultiple(database.Device.Instance, database.TrendLogMultiple.Instance, CASBACnetStackAdapter.OBJECT_TYPE_MULTI_STATE_INPUT, database.MultiStateInput.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE, false, 0, false, 0);
                CASBACnetStackAdapter.SetTrendLogTypeToPolled(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_TREND_LOG_MULTIPLE, database.TrendLogMultiple.Instance, true, false, 3000);


                // 3. Enable Services
                // ---------------------------------------------------------------------------
                // Enable Optional Properties
                CASBACnetStackAdapter.SetServiceEnabled(database.Device.Instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_READ_PROPERTY_MULTIPLE, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.Device.Instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_WRITE_PROPERTY, true);
                CASBACnetStackAdapter.SetServiceEnabled(database.Device.Instance, CASBACnetStackAdapter.SERVICES_SUPPORTED_READ_RANGE, true);

                // All done with the BACnet Setup
                Console.WriteLine("FYI: CAS BACnet Stack Setup, successfully");


                // 4. Setup Trend Log objects
                // ---------------------------------------------------------------------------

                Console.WriteLine("FYI: Loading initial data points to Trend Log and Trend Log Multiple objects");
                
                // Preload Trend Log and Trend Log Multiple from file
                string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                _trendLogBackupPath = Path.Combine(currentDirectory, "trendLogBackupExample.txt");
                _trendLogMultipleBackupPath = Path.Combine(currentDirectory, "trendLogMultipleBackupExample.txt");

                if (!LoadTrendLogFromFile(_trendLogBackupPath))
                {
                    Console.WriteLine("Failed to load trendLog from {0}", _trendLogBackupPath);
                    return;
                }
                if (!LoadTrendLogMultipleFromFile(_trendLogMultipleBackupPath))
                {
                    Console.WriteLine("Failed to load trendLogMultiple from {0}", _trendLogMultipleBackupPath);
                    return;
                }
                Console.WriteLine("FYI: Initial data load complete");

                // 5. Open the BACnet port to receive messages
                // ---------------------------------------------------------------------------
                udpServer = new UdpClient(SETTING_BACNET_PORT);
                remoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);


                // 6. Main loop
                // ---------------------------------------------------------------------------
                Console.WriteLine("FYI: Starting main loop");
                for (; ; )
                {
                    CASBACnetStackAdapter.Loop(); // BACnet Stack adapter update loop

                    database.Loop(); // Update values in the example database

                    DoUserInput(); // Handle user input
                }
            }

            // Load TrendLog initial data points
            private bool LoadTrendLogFromFile(string filename)
            {
                string line;
                StreamReader file = new StreamReader(filename);
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("#"))
                    {
                        // Ignore comments, lines that start with #
                        continue;
                    }

                    // Split on ,
                    string[] elements = line.Split(',');
                    if (elements.Length != 4)
                    {
                        Console.WriteLine("Error - invalid data line: {0}", line);
                        continue;
                    }

                    // Insert TrendLog entry
                    ulong timestamp = Convert.ToUInt64(elements[1]);
                    byte datumType = Convert.ToByte(elements[2]);
                    byte[] datumAsString = Encoding.ASCII.GetBytes(elements[3]);

                    fixed (byte* ptr = datumAsString)
                    {
                        if (!CASBACnetStackAdapter.InsertTrendLogRecord(database.Device.Instance, database.TrendLog.Instance, timestamp, datumType, ptr, (uint)datumAsString.Length, null, 0))
                        {
                            Console.WriteLine("Error - failed to insert record in TrendLog for line: {0}", line);
                            continue;
                        }
                    }
                }

                return true;
            }

            // Load TrendLogMultiple initial data points
            private bool LoadTrendLogMultipleFromFile(string filename)
            {
                string line;
                StreamReader file = new StreamReader(filename);
                while ((line = file.ReadLine()) != null)
                {
                    if (line.StartsWith("#"))
                    {
                        // Ignore comments, lines that start with #
                        continue;
                    }

                    int start = 0;
                    int index = line.IndexOf(',');
                    string number = line.Substring(start, index);
                    start = index + 1;
                    index = line.IndexOf(',', start);
                    string timestampStr = line.Substring(start, index - start);
                    start = index + 1;
                    index = line.IndexOf(',', start);
                    string dataTypeStr = line.Substring(start, index - start);
                    string data = line.Substring(index + 1);

                    // Insert TrendLog entry
                    ulong timestamp = Convert.ToUInt64(timestampStr);
                    byte dataType = Convert.ToByte(dataTypeStr);
                    byte[] dataAsString = Encoding.ASCII.GetBytes(data);

                    fixed (byte* ptr = dataAsString)
                    {
                        if (!CASBACnetStackAdapter.InsertTrendLogMultipleRecord(database.Device.Instance, database.TrendLogMultiple.Instance, timestamp, dataType, ptr, (uint)dataAsString.Length))
                        {
                            Console.WriteLine("Error - failed to insert record in TrendLogMultiple for line: {0}", line);
                            continue;
                        }
                    }
                }

                return true;
            }

            // Backup trendlog data points to txt file
            private bool BackupTrendLogToFile()
            {
                string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string format = "yyyyMMMdHHmm";
                string suffix = DateTime.Now.ToString(format);
                string output = Path.Combine(currentDirectory, "trendLogBackupExample_" + suffix + ".txt");

                StreamWriter file = new StreamWriter(output);
                uint offset = 0;
                bool more = true;

                file.WriteLine("# Backup generated on {0}", DateTime.Now.ToString("o"));
                file.WriteLine("#");
                file.WriteLine("# Number, Timestamp (Epoch), DatumType, DatumAsString");

                while (more)
                {
                    ulong timestamp = 0;
                    byte datumType = 0;
                    byte[] datumAsString = new byte[256];
                    uint datumLength = 0;
                    fixed (byte* ptr = datumAsString)
                    {
                        if (!CASBACnetStackAdapter.ReadTrendLogRecord(database.Device.Instance, database.TrendLog.Instance, offset, &timestamp, &datumType, ptr, &datumLength, 256, null, null, 0, &more))
                        {
                            Console.WriteLine("Error - failed to read record from TrendLog");
                            continue;
                        }
                        offset++;

                        string datum = Encoding.ASCII.GetString(datumAsString);

                        file.WriteLine("{0},{1},{2},{3}", offset, timestamp, datumType, datum);
                    }
                }

                file.Close();

                return true;
            }

            // Backup trendlog multiple data points to txt file
            private bool BackupTrendLogMultipleToFile()
            {
                string currentDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string format = "yyyyMMMdHHmm";
                string suffix = DateTime.Now.ToString(format);
                string output = Path.Combine(currentDirectory, "trendLogMultipleBackupExample_" + suffix + ".txt");

                StreamWriter file = new StreamWriter(output);
                uint offset = 0;
                bool more = true;

                file.WriteLine("# Backup generated on {0}", DateTime.Now.ToString("o"));
                file.WriteLine("#");
                file.WriteLine("# Number, Timestamp (Epoch), DataType, DataAsString");

                while (more)
                {
                    ulong timestamp = 0;
                    byte dataType = 0;
                    byte[] dataAsString = new byte[256];
                    uint dataLength = 0;
                    fixed (byte* ptr = dataAsString)
                    {
                        if (!CASBACnetStackAdapter.ReadTrendLogMultipleRecord(database.Device.Instance, database.TrendLogMultiple.Instance, offset, &timestamp, &dataType, ptr, &dataLength, 256, &more))
                        {
                            Console.WriteLine("Error - failed to read record from TrendLogMultiple");
                            return false;
                        }
                        offset++;

                        string data = Encoding.ASCII.GetString(dataAsString);

                        file.WriteLine("{0},{1},{2},{3}", offset, timestamp, dataType, data);
                    }
                }

                file.Close();

                return true;
            }

            // Handle user input
            private void DoUserInput()
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.F1:
                            Console.WriteLine("FYI: BACnet Stack version: {0}.{1}.{2}.{3}",
                                CASBACnetStackAdapter.GetAPIMajorVersion(),
                                CASBACnetStackAdapter.GetAPIMinorVersion(),
                                CASBACnetStackAdapter.GetAPIPatchVersion(),
                                CASBACnetStackAdapter.GetAPIBuildVersion());
                            break;
                        case ConsoleKey.UpArrow:
                            database.AnalogInputManualIncrement.PresentValue += 0.01f;
                            Console.WriteLine("FYI: Increment Analog input {0} present value to {1:0.00}", 0, database.AnalogInputManualIncrement.PresentValue);

                            // Notify the CAS BACnet stack that this value has been updated. 
                            // If there are any subscribers to this value, they will be sent be sent the updated value. 
                            CASBACnetStackAdapter.ValueUpdated(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT, database.AnalogInputManualIncrement.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE);
                            break;
                        case ConsoleKey.DownArrow:
                            database.AnalogInputManualIncrement.PresentValue -= 0.01f;
                            Console.WriteLine("FYI: Decrement Analog input {0} present value to {1:0.00}", 0, database.AnalogInputManualIncrement.PresentValue);
                            CASBACnetStackAdapter.ValueUpdated(database.Device.Instance, CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT, database.AnalogInputManualIncrement.Instance, CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE);
                            break;
                        case ConsoleKey.B:
                            // BackUp Trend Log
                            Console.WriteLine("Backing up the Trend Log contents...");
                            BackupTrendLogToFile();
                            Console.WriteLine("Back up complete");
                            break;
                        case ConsoleKey.S:
                            // Save Trend Log Multiple
                            Console.WriteLine("Backing up the Trend Log Multiple contents...");
                            BackupTrendLogMultipleToFile();
                            Console.WriteLine("Back up complete");
                            break;
                        case ConsoleKey.Q:
                            Console.WriteLine("Exiting program");
                            System.Environment.Exit(1);
                            break;
                        case ConsoleKey.H:
                        default:
                            PrintHelp();
                            break;
                    }
                }
            }

            public void PrintHelp()
            {
                Console.WriteLine("\tF1:\t Version Info");
                Console.WriteLine("\tUp:\t Increase ManualIncrement Analog Input Present Value by 0.01f");
                Console.WriteLine("\tDown:\t Decrease ManualIncrement Analog Input Present Value by 0.01f");
                Console.WriteLine("\tB:\t Backup TrendLog to file");
                Console.WriteLine("\tS:\t Backup TrendLogMultiple to file");
            }

            // Callback used by the BACnet Stack to get the current time
            public ulong CallbackGetSystemTime()
            {
                // https://stackoverflow.com/questions/9453101/how-do-i-get-epoch-time-in-c
                return (ulong)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
            }

            // Callback used by the BACnet Stack to send a BACnet message
            public UInt16 SendMessage(System.Byte* message, UInt16 messageLength, System.Byte* connectionString, System.Byte connectionStringLength, System.Byte networkType, Boolean broadcast)
            {
                if (connectionStringLength < 6 || messageLength <= 0)
                {
                    return 0;
                }
                // Extract the connection string into a IP address and port. 
                IPAddress ipAddress = new IPAddress(new byte[] { connectionString[0], connectionString[1], connectionString[2], connectionString[3] });
                IPEndPoint ipEndPoint = new IPEndPoint(ipAddress, (connectionString[4] + connectionString[5] * 256));

                // Debug 
                Console.WriteLine("FYI: Sending {0} bytes to {1}", messageLength, ipEndPoint.ToString());

                // Copy from the unsafe pointer to a Byte array. 
                byte[] sendBytes = new byte[messageLength];
                Marshal.Copy((IntPtr)message, sendBytes, 0, messageLength);

                try
                {
                    udpServer.Send(sendBytes, sendBytes.Length, ipEndPoint);
                    return (UInt16)sendBytes.Length;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }

                return 0;
            }

            // Callback used by the BACnet Stack to check if there is a message to process
            public UInt16 RecvMessage(System.Byte* message, UInt16 maxMessageLength, System.Byte* receivedConnectionString, System.Byte maxConnectionStringLength, System.Byte* receivedConnectionStringLength, System.Byte* networkType)
            {
                try
                {
                    if (udpServer.Available > 0)
                    {
                        // Data buffer for incoming data.  
                        byte[] receiveBytes = udpServer.Receive(ref remoteIpEndPoint);
                        byte[] ipAddress = remoteIpEndPoint.Address.GetAddressBytes();
                        byte[] port = BitConverter.GetBytes(UInt16.Parse(remoteIpEndPoint.Port.ToString()));

                        // Copy from the unsafe pointer to a Byte array. 
                        Marshal.Copy(receiveBytes, 0, (IntPtr)message, receiveBytes.Length);

                        // Copy the Connection string 
                        Marshal.Copy(ipAddress, 0, (IntPtr)receivedConnectionString, 4);
                        Marshal.Copy(port, 0, (IntPtr)receivedConnectionString + 4, 2);
                        *receivedConnectionStringLength = 6;

                        // Debug 
                        Console.WriteLine("FYI: Recving {0} bytes from {1}", receiveBytes.Length, remoteIpEndPoint.ToString());

                        // Return length. 
                        return (ushort)receiveBytes.Length;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
                return 0;
            }

            // Callback used by the BACnet Stack to set Charstring property values to the user
            public bool CallbackGetPropertyCharString(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, System.Byte* value, UInt32* valueElementCount, UInt32 maxElementCount, System.Byte encodingType, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyCharString. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);

                switch (objectType)
                {
                    case CASBACnetStackAdapter.OBJECT_TYPE_DEVICE:
                        if (deviceInstance == database.Device.Instance && objectInstance == database.Device.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.Device.Name);
                                return true;
                            }
                            else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_MODEL_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.Device.ModelName);
                                return true;
                            }
                            else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_VENDOR_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.Device.VendorName);
                                return true;
                            }
                            else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_DESCRIPTION)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.Device.Description);
                                return true;
                            }
                            else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_APPLICATIONSOFTWAREVERSION)
                            {
                                string version = APPLICATION_VERSION + "." + CIBuildVersion.CIBUILDNUMBER;
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, version);
                                return true;
                            }
                        }
                        break;
                    case CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT:
                        if (objectInstance == database.AnalogInputAutoIncrement.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.AnalogInputAutoIncrement.Name);
                                return true;
                            }
                        }
                        else if (objectInstance == database.AnalogInputManualIncrement.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.AnalogInputManualIncrement.Name);
                                return true;
                            }
                        }
                        break;
                    case CASBACnetStackAdapter.OBJECT_TYPE_BINARY_INPUT:
                        if (objectInstance == database.BinaryInput.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.BinaryInput.Name);
                                return true;
                            }
                        }
                        break;
                    case CASBACnetStackAdapter.OBJECT_TYPE_MULTI_STATE_INPUT:
                        if (objectInstance == database.MultiStateInput.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.MultiStateInput.Name);
                                return true;
                            }
                            else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_STATETEXT && useArrayIndex)
                            {
                                if (propertyArrayIndex <= database.MultiStateInput.StateText.Length)
                                {
                                    *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.MultiStateInput.StateText[propertyArrayIndex - 1]);
                                    return true;
                                }
                            }
                        }
                        break;
                    case CASBACnetStackAdapter.OBJECT_TYPE_TREND_LOG:
                        if (objectInstance == database.TrendLog.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.TrendLog.Name);
                                return true;
                            }
                        }
                        break;
                    case CASBACnetStackAdapter.OBJECT_TYPE_TREND_LOG_MULTIPLE:
                        if (objectInstance == database.TrendLogMultiple.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_OBJECT_NAME)
                            {
                                *valueElementCount = CASBACnetStackAdapter.UpdateStringAndReturnSize(value, maxElementCount, database.TrendLogMultiple.Name);
                                return true;
                            }
                        }
                        break;
                    default:
                        break;
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false; // Could not handle this request. 
            }

            // Callback used by the BACnet Stack to get Enumerated property values from the user
            public bool CallbackGetEnumerated(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetEnumerated. objectType={0}, objectInstance={1}, propertyIdentifier={2}", objectType, objectInstance, propertyIdentifier);

                switch (objectType)
                {
                    case CASBACnetStackAdapter.OBJECT_TYPE_BINARY_INPUT:
                        if (objectInstance == database.BinaryInput.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE)
                            {
                                *value = (UInt32)(database.BinaryInput.PresentValue ? 1 : 0);
                                Console.WriteLine("FYI: BinaryInput[{0}].value got [{1}]", objectInstance, database.BinaryInput.PresentValue);
                                return true;
                            }
                        }
                        break;
                    default:
                        break;
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            // Callback used by the BACnet Stack to get Real property values from the user
            public bool CallbackGetPropertyReal(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, float* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetPropertyReal. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);

                switch (objectType)
                {
                    case CASBACnetStackAdapter.OBJECT_TYPE_ANALOG_INPUT:
                        if (objectInstance == database.AnalogInputAutoIncrement.Instance)
                        {
                            *value = database.AnalogInputAutoIncrement.PresentValue;
                            Console.WriteLine("FYI: AnalogInput[{0}].value got [{1}]", objectInstance, database.AnalogInputAutoIncrement.PresentValue);
                            return true;
                        }
                        else if (objectInstance == database.AnalogInputManualIncrement.Instance)
                        {
                            *value = database.AnalogInputManualIncrement.PresentValue;
                            Console.WriteLine("FYI: AnalogInput[{0}].value got [{1}]", objectInstance, database.AnalogInputManualIncrement.PresentValue);
                            return true;
                        }
                        break;
                    default:
                        break;
                }

                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }

            // Callback used by the BACnet Stack to get Unsigned Integer property values from the user
            public bool CallbackGetUnsignedInteger(UInt32 deviceInstance, UInt16 objectType, UInt32 objectInstance, UInt32 propertyIdentifier, UInt32* value, bool useArrayIndex, UInt32 propertyArrayIndex)
            {
                Console.WriteLine("FYI: Request for CallbackGetUnsignedInteger. objectType={0}, objectInstance={1}, propertyIdentifier={2}, propertyArrayIndex={3}", objectType, objectInstance, propertyIdentifier, propertyArrayIndex);
                switch (objectType)
                {
                    case CASBACnetStackAdapter.OBJECT_TYPE_DEVICE:
                        if (deviceInstance == database.Device.Instance && objectInstance == database.Device.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_VENDOR_IDENTIFIER)
                            {
                                *value = database.Device.VendorIdentifier;
                                return true;
                            }
                        }
                        break;

                    case CASBACnetStackAdapter.OBJECT_TYPE_MULTI_STATE_INPUT:
                        if (objectInstance == database.MultiStateInput.Instance)
                        {
                            if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_PRESENT_VALUE)
                            {
                                *value = database.MultiStateInput.PresentValue;
                                return true;
                            }
                            else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_STATETEXT && useArrayIndex && propertyArrayIndex == 0)
                            {
                                *value = Convert.ToUInt32(database.MultiStateInput.StateText.Length);
                                return true;
                            }
                            else if (propertyIdentifier == CASBACnetStackAdapter.PROPERTY_IDENTIFIER_NUMBEROFSTATES)
                            {
                                *value = Convert.ToUInt32(database.MultiStateInput.StateText.Length);
                                return true;
                            }
                        }
                        break;
                    default:
                        break;
                }
                Console.WriteLine("   FYI: Not implmented. propertyIdentifier={0}", propertyIdentifier);
                return false;
            }
        }
    }
}
