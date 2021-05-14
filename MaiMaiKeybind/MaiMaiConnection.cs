using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;

namespace MaiMaiKeybind
{
    public class MaiMaiConnection
    {
        /// <summary>
        /// Location of the Input Struct in the Memory
        /// </summary>
        /// <remarks>
        /// 0x8DF9C0 is Reaver's magic address in MaiMai Green
        /// 0xF40D28 is Address in MaiMai Finale
        /// </remarks>
        private const int TouchStructMemoryLocation = 0xF40D28;

        private const int OffsetActive = 52;

        private const int OffsetInactive = 60;

        public MaiMaiState CurrentState { get; private set; }
        // these are the Key positions...
        /* Touch Location
         *            O8        O1
         *             A8      A1
         *              B8    B1
         * O7  A7  B7              B2  A2  O2
         *                 C
         * O6  A8  B6              B3  A3  O3
         *              B5    B4
         *             A5      A4
         *            O5        O4
         */

        //
        private static readonly Keyboard.DirectXKeyStrokes[] HardwareKeys =
        {
            Keyboard.DirectXKeyStrokes.DIK_W, Keyboard.DirectXKeyStrokes.DIK_E, Keyboard.DirectXKeyStrokes.DIK_D,
            Keyboard.DirectXKeyStrokes.DIK_C, Keyboard.DirectXKeyStrokes.DIK_X,
            Keyboard.DirectXKeyStrokes.DIK_Z, Keyboard.DirectXKeyStrokes.DIK_A,
            Keyboard.DirectXKeyStrokes.DIK_Q, 
        };

        //Ordered List of the Sensor Pressed on the hardware Ring
        public static readonly HardwareKeyPress[] OrderedHardwareKeys =
        {
            HardwareKeyPress.O1, HardwareKeyPress.O2, HardwareKeyPress.O3, HardwareKeyPress.O4, 
            HardwareKeyPress.O5, HardwareKeyPress.O6, HardwareKeyPress.O7, HardwareKeyPress.O8,
        };

        public enum HardwareKeyPress
        {
            O1 = 1 << 0,
            O2 = 1 << 1,
            O3 = 1 << 2,
            O4 = 1 << 3,
            O5 = 1 << 4,
            O6 = 1 << 5,
            O7 = 1 << 6,
            O8 = 1 << 7,
        }

        //Ordered List of the Sensor Pressed from outer Ring to center
        public static readonly TouchSensorPress[] OrderedTouchKeys =
        {
            TouchSensorPress.A1, TouchSensorPress.A2, TouchSensorPress.A3, TouchSensorPress.A4, TouchSensorPress.A5,
            TouchSensorPress.A6, TouchSensorPress.A7, TouchSensorPress.A8, TouchSensorPress.B1, TouchSensorPress.B2,
            TouchSensorPress.B3, TouchSensorPress.B4, TouchSensorPress.B5, TouchSensorPress.B6, TouchSensorPress.B7,
            TouchSensorPress.B8, TouchSensorPress.C
        };

        public enum TouchSensorPress
        {
            A1 = 1 << 0,
            B1 = 1 << 1,
            A2 = 1 << 2,
            B2 = 1 << 3,

            // gap 4
            A3 = 1 << 5,
            B3 = 1 << 6,
            A4 = 1 << 7,
            B4 = 1 << 8,

            // gap 9
            A5 = 1 << 10,
            B5 = 1 << 11,
            A6 = 1 << 12,
            B6 = 1 << 13,

            // gap 14
            A7 = 1 << 15,
            B7 = 1 << 16,
            A8 = 1 << 17,
            B8 = 1 << 18,
            C = 1 << 19,
        }

        /// <summary>
        /// This is for setting the TouchKeys
        /// </summary>
        public uint TouchKeyStatus
        {
            get
            {
                lock (TouchKeyLock)
                    return _touchKeyStatus;
            }
            set
            {
                lock (TouchKeyLock)
                    _touchKeyStatus = value;
            }
        }

        private uint _touchKeyStatus;

        public object TouchKeyLock = new object();

        /// <summary>
        /// This is for setting the HardwareKeys
        /// </summary>
        public byte HardwareKeyStatus
        {
            get
            {
                lock (HardwareKeyLock)
                    return _hardwareKeyStatus;
            }
            set
            {
                lock (HardwareKeyLock)
                    _hardwareKeyStatus = value;
            }
        }

        private byte _hardwareKeyStatus;

        public object HardwareKeyLock = new object();

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize,
            ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize,
            ref int lpNumberOfBytesWritten);

        public enum MaiMaiState
        {
            Waiting,
            Connecting,
            Setup,
            Running,
            Error
        }

        public void RunThread()
        {
            CurrentState = MaiMaiState.Waiting;

            while (true)
            {
                
                byte prevHardwareStatus = 0;
                uint prevTouchStatus = 0;

                Thread.Sleep(500);

                var allMais = Process.GetProcessesByName("maimai_dump_");

                if (allMais.Length > 0 && allMais[0] != null)
                {
                    CurrentState = MaiMaiState.Connecting;
                    Thread.Sleep(10000);
                    var process = allMais[0];
                    var processHandle = process.Handle; // OpenProcess(PROCESS_WM_READ, false, process.Id);

                    var bytesRead = 0;
                    var buffer = new byte[4]; // ptr 4 bytes

                    ReadProcessMemory((int) processHandle, TouchStructMemoryLocation, buffer, buffer.Length,
                        ref bytesRead);

                    // get the pointer
                    var val = BitConverter.ToInt32(buffer, 0);
                    Console.WriteLine($@"{val:X}");

                    CurrentState = MaiMaiState.Setup;

                    while (CurrentState != MaiMaiState.Error)
                    {
                        CurrentState = MaiMaiState.Running;

                        var touchKeyStatus = TouchKeyStatus;
                        var toWriteActive = BitConverter.GetBytes(touchKeyStatus);
                        var toWriteInactive = BitConverter.GetBytes(~touchKeyStatus);
                        var hardwareStatus = HardwareKeyStatus;

                        if (!process.HasExited)
                        {
                            try
                            {
                                if (prevHardwareStatus != hardwareStatus)
                                    for (var i = 0; i < 8; i++)
                                        Keyboard.SendKey(HardwareKeys[i], (hardwareStatus & (1 << i)) == 0,
                                            Keyboard.InputType.Keyboard);

                                if (prevTouchStatus != touchKeyStatus)
                                {
                                    WriteProcessMemory((int) processHandle, val + OffsetActive, toWriteActive,
                                        4,
                                        ref bytesRead);
                                    WriteProcessMemory((int) processHandle, val + OffsetInactive, toWriteInactive,
                                        4,
                                        ref bytesRead);
                                }
                            }
                            catch
                            {
                                CurrentState = MaiMaiState.Error;
                                break;
                            }
                        }
                        else
                        {
                            CurrentState = MaiMaiState.Error;
                            break;
                        }

                        prevHardwareStatus = hardwareStatus;
                        prevTouchStatus = touchKeyStatus;
                        Thread.Sleep(1);
                    }
                }

                CurrentState = MaiMaiState.Error;
                Thread.Sleep(2000);
            }
        }
    }
}