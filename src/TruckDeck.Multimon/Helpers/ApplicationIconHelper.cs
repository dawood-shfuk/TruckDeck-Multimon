using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TruckDeck.Multimon.Helpers
{
    public static class ApplicationIconHelper
    {
        public static Icon Load()
        {
            try
            {
                var exeIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (exeIcon != null)
                    return (Icon)exeIcon.Clone();

                var icoPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Resources", "app.ico");
                if (File.Exists(icoPath))
                    return new Icon(icoPath);
            }
            catch
            {
                // fall through
            }

            return SystemIcons.Application;
        }

        public static void Apply(Form form)
        {
            if (form == null)
                return;
            using (var icon = Load())
                form.Icon = (Icon)icon.Clone();
        }
    }
}
