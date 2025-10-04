using System;
using System.Windows;
using System.Windows.Threading;

namespace ReminderApp
{
    public partial class NotificationWindow : Window
    {
        private DispatcherTimer autoCloseTimer;

        public NotificationWindow(string message)
        {
            InitializeComponent();
            MessageText.Text = message;

            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - Width - 20;
            Top = workArea.Bottom - Height - 20;

            autoCloseTimer = new DispatcherTimer();
            autoCloseTimer.Interval = TimeSpan.FromSeconds(10);
            autoCloseTimer.Tick += AutoClose_Tick;
            autoCloseTimer.Start();
        }

        private void AutoClose_Tick(object sender, EventArgs e)
        {
            autoCloseTimer.Stop();
            Close();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            autoCloseTimer.Stop();
            Close();
        }
    }
}