using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Keyboard
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
        
        private const int ProcessWmRead = 0x0010;

        public MaiMaiState CurrentState { get; private set; }

        private bool HasProcess { get; set; } = false;

        // these are the bits to set on the touch sensor presses...
        /* Touch Location
         *
         *          A8      A1
         *           B8    B1
         *  A7  B7              B2  A2
         *              C
         *  A8  B6              B3  A3
         *           B5    B4
         *          A5      A4  
         */
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

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

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
                Thread.Sleep(500);

                var allMais = Process.GetProcessesByName("maimai_dump_");

                if (allMais.Length > 0 && allMais[0] != null)
                {
                    CurrentState = MaiMaiState.Connecting;
                    Thread.Sleep(10000);
                    var process = allMais[0];
                    var processHandle = process.Handle;// OpenProcess(PROCESS_WM_READ, false, process.Id);

                    var bytesRead = 0;
                    var buffer = new byte[4]; // ptr 4 bytes

                    ReadProcessMemory((int)processHandle, TouchStructMemoryLocation, buffer, buffer.Length, ref bytesRead);

                    // get the pointer
                    var val = BitConverter.ToInt32(buffer, 0);
                    Console.WriteLine($"{val:X}");

                    CurrentState = MaiMaiState.Setup;

                    var structBuf = new byte[8];

                    try
                    {
                        ReadProcessMemory((int)processHandle, val + 52, structBuf, structBuf.Length, ref bytesRead);

                        for (var i = 0; i < 8; ++i)
                        {
                            Console.Write($"{structBuf[i]:X}");
                        }
                        Console.WriteLine();
                    }
                    catch
                    {
                        CurrentState = MaiMaiState.Error;
                    }

                    while (CurrentState != MaiMaiState.Error)
                    {
                        CurrentState = MaiMaiState.Running;

                        //TODO Fill with information from Keyboard or other input means
                        const int positions = 0;
                        var toWrite = BitConverter.GetBytes(positions);

                        if (!process.HasExited)
                        {
                            try
                            {
                                WriteProcessMemory((int)processHandle, val + 52, toWrite, structBuf.Length, ref bytesRead);
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

                        Thread.Sleep(16);
                    }
                }
                CurrentState = MaiMaiState.Error;
                Thread.Sleep(2000);
            }
        }

    }
}
