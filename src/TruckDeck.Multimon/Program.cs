using System;
using System.Windows.Forms;
using TruckDeck.Multimon.Helpers;

namespace TruckDeck.Multimon
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Services.GameWindowSpanService.EnsureDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (_, args) =>
                MessageBox.Show(args.Exception.ToString(), "TruckDeck Multimon", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.Run(new MainForm());
        }
    }
}
