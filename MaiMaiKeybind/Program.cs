using System;
using System.Diagnostics;
using System.Threading;
using System.Timers;
using SharpDX.DirectInput;
using Timer = System.Timers.Timer;

namespace MaiMaiKeybind
{
    class Program
    {
        private MaiMaiConnection MaiMai { get; }

        private string JOYSTICK_NAME = "Touch Button Panel";
        private Joystick InputDevice { get; }
        private Timer PollTimer { get; }

        public Program()
        {
            MaiMai = new MaiMaiConnection();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var thread = new Thread(MaiMai.RunThread);
            thread.Start();

            var directInput = new DirectInput();
            var inputDeviceGuid = Guid.Empty;
            foreach (var device in directInput.GetDevices(DeviceClass.All, DeviceEnumerationFlags.AllDevices))
                if (device.InstanceName.Contains(JOYSTICK_NAME))
                    inputDeviceGuid = device.InstanceGuid;

            if (inputDeviceGuid == Guid.Empty)
            {
                Console.WriteLine($"Couldn't find Device called: {JOYSTICK_NAME}\nExiting Now!");
                Thread.Sleep(5000);
                Environment.Exit(1);
            }

            InputDevice = new Joystick(directInput, inputDeviceGuid);
            InputDevice.Acquire();

            //PollTimer = new Timer { AutoReset = true, Interval = 1, Enabled = true };
            //PollTimer.Elapsed += PollTimerOnElapsed;
            while (true) PollTimerOnElapsed();
        }


        public static void Main()
        {
            new Program();
        }


        //Update Loop
        private void PollTimerOnElapsed()
        {
            InputDevice.Poll();
            var state = InputDevice.GetCurrentState();

            uint maiMaiTouchState = 0;
            byte maiMaiHardwareButtonState = 0;


            for (var i = 0; i < 8; i++)
                if (state.Buttons[i])
                    maiMaiHardwareButtonState |= (byte) MaiMaiConnection.OrderedHardwareKeys[i];


            for (var i = 8; i < 25; i++)
            {
                if (state.Buttons[i])
                    maiMaiTouchState |= (uint) MaiMaiConnection.OrderedTouchKeys[i - 8];
            }

            MaiMai.HardwareKeyStatus = maiMaiHardwareButtonState;
            MaiMai.TouchKeyStatus = maiMaiTouchState;
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!(e.ExceptionObject is Exception ex)) return;

            // Log this error. Logging the exception doesn't correct the problem but at least now
            // you may have more insight as to why the exception is being thrown.
            Debug.WriteLine("Unhandled Exception: " + ex.Message);
            Debug.WriteLine("Unhandled Exception: " + ex);
        }
    }
}
