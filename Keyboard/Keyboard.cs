using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;

namespace Keyboard
{
    public partial class Keyboard : Form
    {
        private MaiMaiConnection MaiMai { get; } = new MaiMaiConnection();

        const bool CaptureOnlyInForeground = false;
        // Todo: add checkbox to form when checked/uncheck create method to call that does the same as Keyboard ctor

        public Keyboard()
        {
            InitializeComponent();
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            var thread = new Thread(MaiMai.RunThread);
            thread.Start();
            
            timer1.Tick += Timer1_Tick;
        }

        private void Timer1_Tick(object sender, EventArgs e) =>
            label1.Text = MaiMai.CurrentState.ToString();

        private void Keyboard_FormClosing(object sender, FormClosingEventArgs e)
        {
            //TODO unsubscribe everything
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (!(e.ExceptionObject is Exception ex)) return;

            // Log this error. Logging the exception doesn't correct the problem but at least now
            // you may have more insight as to why the exception is being thrown.
            Debug.WriteLine("Unhandled Exception: " + ex.Message);
            Debug.WriteLine("Unhandled Exception: " + ex);
            MessageBox.Show(ex.Message);
        }


    }
}
