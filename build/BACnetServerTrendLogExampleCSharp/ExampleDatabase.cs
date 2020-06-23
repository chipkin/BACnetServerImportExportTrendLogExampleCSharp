/**
 * BACnet Server Trend Log Example CSharp
 * ----------------------------------------------------------------------------
 * ExampleDatabase.cs
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
using System.Collections.Generic;
using System.Text;

namespace BACnetServerTrendLogExampleCSharp
{
    class ExampleDatabase
    {
        public class ExampleDatabaseBase
        {
            public String Name;
            public UInt32 Instance;
        }

        public class ExampleDatabaseDevice : ExampleDatabaseBase
        {
            public String ModelName;
            public String Description;
            public UInt32 VendorIdentifier;
            public String VendorName;
        }

        public class ExampleDatabaseAnalogInput : ExampleDatabaseBase
        {
            public float PresentValue;
        }

        public class ExampleDatabaseBinaryInput : ExampleDatabaseBase
        {
            public bool PresentValue;
        }

        public class ExampleDatabaseMultiStateInput : ExampleDatabaseBase
        {
            public UInt32 PresentValue;
            public string[] StateText;

            public static string[] _colorText = { "Red, Green, Blue" };
        }

        public class ExampleDatabaseTrendLog : ExampleDatabaseBase
        {
        }

        public class ExampleDatabaseTrendLogMultiple : ExampleDatabaseBase
        {
        }

        public ExampleDatabaseDevice Device;
        public ExampleDatabaseAnalogInput AnalogInputAutoIncrement;
        public ExampleDatabaseAnalogInput AnalogInputManualIncrement;
        public ExampleDatabaseBinaryInput BinaryInput;
        public ExampleDatabaseMultiStateInput MultiStateInput;
        public ExampleDatabaseTrendLog TrendLog;
        public ExampleDatabaseTrendLogMultiple TrendLogMultiple;

        public void Setup()
        {
            this.Device = new ExampleDatabaseDevice()
            {
                Name = "Example Trending Device",
                Instance = 389000,
                Description = "Example device to demonstrate Import and Export Trend Log functionality of the CAS BACnet Stack",
                ModelName = "BACnetServerTrendLogExampleCSharp",
                VendorIdentifier = 389,
                VendorName = "Chipkin Automation Systems"
            };

            this.AnalogInputAutoIncrement = new ExampleDatabaseAnalogInput()
            {
                Name = "AI - Auto Increment",
                Instance = 1,
                PresentValue = 0.0f
            };

            this.AnalogInputManualIncrement = new ExampleDatabaseAnalogInput()
            {
                Name = "AI - Manual Increment",
                Instance = 2,
                PresentValue = 0.0f
            };

            this.BinaryInput = new ExampleDatabaseBinaryInput()
            {
                Name = "BI - Binary Input",
                Instance = 3,
                PresentValue = false
            };

            this.MultiStateInput = new ExampleDatabaseMultiStateInput()
            {
                Name = "MSI - Multi-State Input",
                Instance = 4,
                PresentValue = 1,
                StateText = ExampleDatabaseMultiStateInput._colorText
            };

            this.TrendLog = new ExampleDatabaseTrendLog()
            {
                Name = "TL - Trend Log (AI1)",
                Instance = 10,
            };

            this.TrendLogMultiple = new ExampleDatabaseTrendLogMultiple()
            {
                Name = "TLM - Trend Log Multiple (AI1, AI2, BI3, MSI4)",
                Instance = 20,
            };
        }

        // Loop to update database values
        public void Loop()
        {
            DateTime current = DateTime.Now;
            this.AnalogInputAutoIncrement.PresentValue = (float)current.Second;
            this.BinaryInput.PresentValue = current.Minute % 2 == 0 ? true : false;
            this.MultiStateInput.PresentValue = (uint)(current.Hour % 3 == 0 ? 3 : current.Hour % 2 == 0 ? 2 : 1);
        }
    }
}
