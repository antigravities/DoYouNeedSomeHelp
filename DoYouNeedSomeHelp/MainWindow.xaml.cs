using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Management;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace DoYouNeedSomeHelp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private String file;

        private String[] blacklist =
        {
            "svchost.exe",
            "OpenWith.exe",
            "dllhost.exe",
            "explorer.exe",
            "WerFault.exe"
        };

        // POGGERS!
        // https://stackoverflow.com/questions/972039/is-there-a-system-event-when-processes-are-created
        void WaitForProcess()
        {
            ManagementEventWatcher startWatch = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
            startWatch.EventArrived += new EventArrivedEventHandler(OnProcessStarted);
            startWatch.Start();
        }

        void OnProcessStarted(object sender, EventArrivedEventArgs e)
        {
            if (Visibility == Visibility.Visible || Process.GetCurrentProcess().Id == int.Parse(e.NewEvent.Properties["ParentProcessID"].Value.ToString()) || blacklist.Contains(e.NewEvent.Properties["ProcessName"].Value))
            {
                return;
            }

            var itemName = (string)e.NewEvent.Properties["ProcessName"].Value;

            foreach (PropertyData w in e.NewEvent.Properties)
            {
                Console.WriteLine(w.Name + " = " + w.Value);
            }

            Process mProc = null;
            try
            {
                mProc = Process.GetProcessById(int.Parse(e.NewEvent.Properties["ProcessID"].Value.ToString()));

                ManagementObjectCollection j = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + mProc.Id).Get();

                // https://stackoverflow.com/questions/2633628/can-i-get-command-line-arguments-of-other-processes-from-net-c
                file = j.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                

                mProc.Kill();

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (WpfAnimatedGif.ImageBehavior.GetIsAnimationLoaded(Clippy))
                    {
                        var controller = WpfAnimatedGif.ImageBehavior.GetAnimationController(Clippy);
                        controller.GotoFrame(0);
                        controller.Play();
                    }

                    Visibility = Visibility.Visible;

                    HelpText.Text = "I noticed that you tried to run " + itemName + ". Would you like some help with that?";
                }));
            }
            catch (Exception)
            {
                Console.WriteLine("...");
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            Visibility = Visibility.Hidden;
            WaitForProcess();
        }

        private void ButtonYes_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Visibility = Visibility.Hidden;

                GroupCollection matches = new Regex("^\"(.*)\" (.*)$").Match(file).Groups;

                try
                {
                    Process.Start(matches[1].ToString(), matches[2].ToString());
                }
                catch (Exception)
                {
                    Console.WriteLine(matches[1]);
                }
            }));
        }

        private void ButtonNo_Click(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                Visibility = Visibility.Hidden;
            }));
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
            window.Activate();
        }
    }
}
