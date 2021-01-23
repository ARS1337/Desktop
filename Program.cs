/////////////////////////////////////////////////////////////////////////////////////////////////////////
//
// This project demonstrates how to write a simple vJoy feeder in C#
//
// You can compile it with either #define ROBUST OR #define EFFICIENT
// The fuctionality is similar - 
// The ROBUST section demonstrate the usage of functions that are easy and safe to use but are less efficient
// The EFFICIENT ection demonstrate the usage of functions that are more efficient
//
// Functionality:
//	The program starts with creating one joystick object. 
//	Then it petches the device id from the command-line and makes sure that it is within range
//	After testing that the driver is enabled it gets information about the driver
//	Gets information about the specified virtual device
//	This feeder uses only a few axes. It checks their existence and 
//	checks the number of buttons and POV Hat switches.
//	Then the feeder acquires the virtual device
//	Here starts and endless loop that feedes data into the virtual device
//
/////////////////////////////////////////////////////////////////////////////////////////////////////////
#define ROBUST
//#define EFFICIENT

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Don't forget to add this
using vJoyInterfaceWrap;

namespace FeederDemoCS
{
    class Program
    {
        // Declaring one joystick (Device id 1) and a position structure. 
        static public vJoy joystick;
        static public vJoy.JoystickState iReport;
        static public uint id = 1;

        static UdpClient client;
        static IPEndPoint endpoint;
        static IPAddress ip;
        static byte[] data;
            static void Main(string[] args)
        {
            //udp starts;
            client = new UdpClient(49000);
            ip = new IPAddress(new byte[] { 192, 168, 1, 3 });

            endpoint = new IPEndPoint(ip, 49000);
            //udp ends

            // Create one joystick object and a position structure.
            joystick = new vJoy();
            iReport = new vJoy.JoystickState();

            
            // Device ID can only be in the range 1-16
            if (args.Length>0 && !String.IsNullOrEmpty(args[0]))
                id = Convert.ToUInt32(args[0]);
            if (id <= 0 || id > 16)
            {
                Console.WriteLine("Illegal device ID {0}\nExit!",id); 
                return;
            }

            // Get the driver attributes (Vendor ID, Product ID, Version Number)
            if (!joystick.vJoyEnabled())
            {
                Console.WriteLine("vJoy driver not enabled: Failed Getting vJoy attributes.\n");
                return;
            }
            else
                Console.WriteLine("Vendor: {0}\nProduct :{1}\nVersion Number:{2}\n", joystick.GetvJoyManufacturerString(), joystick.GetvJoyProductString(), joystick.GetvJoySerialNumberString());

            // Get the state of the requested device
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status)
            {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
                    return;
            };

            // Check which axes are supported
            bool AxisX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_X);
            bool AxisY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Y);
            bool AxisZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_Z);
            bool AxisRX = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RX);
            bool AxisRZ = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RZ);
            bool AxisRY = joystick.GetVJDAxisExist(id, HID_USAGES.HID_USAGE_RY);
            // Get the number of buttons and POV Hat switchessupported by this vJoy device
            int nButtons = joystick.GetVJDButtonNumber(id);
            int ContPovNumber = joystick.GetVJDContPovNumber(id);
            int DiscPovNumber = joystick.GetVJDDiscPovNumber(id);

            // Print results
            Console.WriteLine("\nvJoy Device {0} capabilities:\n", id);
            Console.WriteLine("Numner of buttons\t\t{0}\n", nButtons);
            Console.WriteLine("Numner of Continuous POVs\t{0}\n", ContPovNumber);
            Console.WriteLine("Numner of Descrete POVs\t\t{0}\n", DiscPovNumber);
            Console.WriteLine("Axis X\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Y\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Z\t\t{0}\n", AxisX ? "Yes" : "No");
            Console.WriteLine("Axis Rx\t\t{0}\n", AxisRX ? "Yes" : "No");
            Console.WriteLine("Axis Rz\t\t{0}\n", AxisRZ ? "Yes" : "No");

            // Test if DLL matches the driver
            UInt32 DllVer = 0, DrvVer = 0;
            bool match = joystick.DriverMatch(ref DllVer, ref DrvVer);
            if (match)
                Console.WriteLine("Version of Driver Matches DLL Version ({0:X})\n", DllVer);
            else
                Console.WriteLine("Version of Driver ({0:X}) does NOT match DLL Version ({1:X})\n", DrvVer, DllVer);


            // Acquire the target
            if ((status == VjdStat.VJD_STAT_OWN) || ((status == VjdStat.VJD_STAT_FREE) && (!joystick.AcquireVJD(id))))
            {
                Console.WriteLine("Failed to acquire vJoy device number {0}.\n", id);
                return ;
            }
            else
                Console.WriteLine("Acquired: vJoy device number {0}.\n", id);

            //Console.WriteLine("\npress enter to stat feeding");
            //Console.ReadKey(true);

            int X, Y, Z, ZR, XR, YR;
            uint count = 0;
            long maxval = 0;

            X = 00;
            Y = 00;
            Z = 00;
            XR = 0;
            ZR = 0;
            YR = 0;
            Console.WriteLine("dsds");
            joystick.GetVJDAxisMax(id, HID_USAGES.HID_USAGE_X, ref maxval);

#if ROBUST
            Console.WriteLine("maxval: " + maxval);
            bool resX, resY, resZ;
	// Reset this device to default values
	joystick.ResetVJD(id);
            short rudder=0, pitch=0, roll=0;int val = 0;
            bool engine8 = false, flap7 = false, gear6 = false, mgs5 = false, cannon4 = false,bomb = false,trim=false,
                prevEngine = false, prevFlap = false, prevGear = false,prevMgs=false,prevCannon=false,prevBomb=false,prevTrim=false,
                gearSet = false,engineSet=false,flapSet=false;
            int throttle=0;

            // Feed the device in endless loop
            while (true)
            {


                // Press/Release Buttons
                //res = joystick.SetBtn(true, id, count / 50);
                //res = joystick.SetBtn(false, id, 1 + count / 50);



                //System.Threading.Thread.Sleep(20);
                //Console.WriteLine("sdsdfs");

                data = client.Receive(ref endpoint);
                //Console.WriteLine("sdsdfs2222222222222222");

                ///X = BitConverter.ToInt16(new byte[] { data[0],data[1]},0);
                ////Y=BitConverter.ToInt16(new byte[] { data[2], data[3] }, 0);
                //Console.WriteLine("sdsdfs333333333333");
                rudder = BitConverter.ToInt16(new byte[] { data[0], data[1] }, 0);
                pitch = BitConverter.ToInt16(new byte[] { data[2], data[3] }, 0);
                roll = BitConverter.ToInt16(new byte[] { data[4], data[5] }, 0);
                if ((data[6] & 0b10000000) == 128) engine8 = true;
                else engine8 = false;
                if ((data[6] & 0b01000000) == 64) flap7 = true;
                else flap7 = false;
                if ((data[6] & 0b00100000) == 32) gear6 = true;
                else gear6 = false;
                if ((data[6] & 0b00010000) == 16) mgs5 = true;
                else mgs5 = false;
                if ((data[6] & 0b00001000) == 8) cannon4 = true;
                else cannon4 = false;
                if ((data[6] & 0b00000100) == 4) bomb = true;
                else bomb = false;
                if (data[7] > 100) trim = true;
                else trim = false;
                X = (pitch) ;
                Y = (roll);  
                Z= ((rudder)+128)*125;
                if (data[7] <= 100) throttle = data[7];
                throttle = (int)(throttle)-127;
                //X = Y = (-1)* val++;
                //if (val >= 32768) val = 0; 
                //Console.WriteLine("X: " + X);
                //byte[] data = client.Receive(ref endpoint);
                //Console.WriteLine(data+" "+data.Length);
                //X = BitConverter.ToUInt16(new byte[] { data[1], data[0] }, 0);

                // Set position of 4 axes
                resX = joystick.SetAxis(X, id, HID_USAGES.HID_USAGE_X);
                resY = joystick.SetAxis(Y, id, HID_USAGES.HID_USAGE_Y);
                resZ = joystick.SetAxis(Z, id, HID_USAGES.HID_USAGE_Z);
                  resX = joystick.SetAxis(throttle,id,HID_USAGES.HID_USAGE_SL0);
                if (gearSet)
                {
                    joystick.SetBtn(false, id, 29);
                    gearSet = false;
                }
                if (flapSet)
                {
                    joystick.SetBtn(false, id, 28);
                    flapSet = false;
                }
                if (engineSet)
                {
                    joystick.SetBtn(false, id, 32);
                    engineSet = false;
                }

                if (trim == true & prevTrim == false)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    joystick.SetBtn(true, id, 26);
                    //Thread.Sleep(10);
                    //joystick.SetBtn(false, id, 31);
                }
                if (trim == false & prevTrim == true)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    //joystick.SetBtn(true, id, 31);
                    //Thread.Sleep(10);
                    joystick.SetBtn(false, id, 26);
                }

                if (bomb == true & prevBomb == false)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    joystick.SetBtn(true, id, 27);
                    //Thread.Sleep(10);
                    //joystick.SetBtn(false, id, 31);
                }
                if (bomb == false & prevBomb == true)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    //joystick.SetBtn(true, id, 31);
                    //Thread.Sleep(10);
                    joystick.SetBtn(false, id, 27);
                }
                if (cannon4== true & prevCannon == false)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    joystick.SetBtn(true, id, 31);
                    //Thread.Sleep(10);
                    //joystick.SetBtn(false, id, 31);
                }
                if (cannon4 == false & prevCannon == true)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    //joystick.SetBtn(true, id, 31);
                    //Thread.Sleep(10);
                    joystick.SetBtn(false, id, 31);
                }
                if (mgs5 == true & prevMgs == false)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    joystick.SetBtn(true, id, 30);
                    //Thread.Sleep(10);
                    //joystick.SetBtn(false, id, 30);
                }
                if (mgs5 == false & prevMgs== true)
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    //joystick.SetBtn(true, id, 30);
                    //Thread.Sleep(10);
                    joystick.SetBtn(false, id, 30);
                }
                
                if ((gear6 == true & prevGear == false)|(gear6 == false & prevGear == true))
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    joystick.SetBtn(true, id, 29);
                    gearSet = true;
                    //Thread.Sleep(10);
                    //joystick.SetBtn(false, id, 29);
                }
                //if (gear6 == false & prevGear == true)
                //{
                //    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                //    joystick.SetBtn(true, id, 29);
                //    Thread.Sleep(10);
                //    joystick.SetBtn(false, id, 29);
                //}
                if ((flap7 == true & prevFlap == false)|(flap7 == false & prevFlap == true))
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    joystick.SetBtn(true, id, 28);
                    flapSet = true;
                    //Thread.Sleep(10);
                    //joystick.SetBtn(false, id, 28);
                }
                //if (flap7 == false & prevFlap == true)
                //{
                //    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                //    joystick.SetBtn(true, id, 28);
                //    Thread.Sleep(10);
                //    joystick.SetBtn(false, id, 28);
                //}
                if ((engine8 == true & prevEngine == false)|(engine8 == false & prevEngine == true))
                {
                    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                    joystick.SetBtn(true, id, 32);
                    engineSet = true;
                    //Thread.Sleep(10);
                    //joystick.SetBtn(false, id, 32);
                }
                //if (engine8 == false & prevEngine == true)
                //{
                //    Console.WriteLine("ENGINE TOGGLED " + engine8 + " " + prevEngine + " " + nButtons);
                //    joystick.SetBtn(true, id, 32);
                //    Thread.Sleep(10);
                //    joystick.SetBtn(false, id, 32);
                //}

                prevEngine = engine8;
                prevCannon = cannon4;
                prevMgs = mgs5;
                prevGear = gear6;
                prevFlap = flap7;
                prevBomb = bomb;
                prevTrim = trim;
                //joystick.SetAxis(XR, id, HID_USAGES.HID_USAGE_X);
                //joystick.SetAxis(YR, id, HID_USAGES.HID_USAGE_Y);

                Console.WriteLine(" RUDDER : " + Z + " " + resZ + "  ROLL : " + Y + " " + resY + " PITCH : " + X + " " + resX + " ");
                //Console.WriteLine(throttle+" "+data[7] +" ENGINE "+engine8.ToString()+"   FLAP "+ flap7.ToString() + "   GEAR " + gear6.ToString() + "   M-GUN " +mgs5.ToString() + "   CANNON " + cannon4.ToString() + "  Bomb " + bomb+" "+trim);
                System.Threading.Thread.Sleep(20);
                Console.WriteLine("data was " + data[7] + " throttle set to: " + throttle);

                //Console.WriteLine(data[6]);



            } // While (Robust)

#endif // ROBUST
        } // Main
    } // class Program
} // namespace FeederDemoCS
