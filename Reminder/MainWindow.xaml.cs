using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ReminderApp
{
    public partial class MainWindow : Window
    {
        private List<Reminder> reminders = new List<Reminder>();
        private DispatcherTimer timer;
        private string dataFile = "reminders.json";

        public MainWindow()
        {
            InitializeComponent();
            DateInput.SelectedDate = DateTime.Today;
            LoadReminders();
            UpdateList();
            StartTimer();
        }

        private void RepeatCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (RepeatCheckBox.IsChecked == true)
            {
                RepeatPanel.Visibility = Visibility.Visible;
            }
            else
            {
                RepeatPanel.Visibility = Visibility.Collapsed;
            }
        }

        private void RepeatType_Changed(object sender, RoutedEventArgs e)
        {
            if (IntervalLabel == null) return;

            if (MinutesRadio.IsChecked == true)
            {
                IntervalLabel.Text = "мин.";
            }
            else if (HoursRadio.IsChecked == true)
            {
                IntervalLabel.Text = "ч.";
            }
            else if (DaysRadio.IsChecked == true)
            {
                IntervalLabel.Text = "дн.";
            }
        }

        private void StartTimer()
        {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            CheckReminders();
        }

        private void CheckReminders()
        {
            var now = DateTime.Now;
            var toRemove = new List<Reminder>();

            foreach (var reminder in reminders.ToList())
            {
                if (now >= reminder.NextTime)
                {
                    ShowNotification(reminder.Text);

                    if (reminder.RepeatType == "once")
                    {
                        toRemove.Add(reminder);
                    }
                    else if (reminder.RepeatType == "minutes")
                    {
                        reminder.NextTime = reminder.NextTime.AddMinutes(reminder.Interval);
                    }
                    else if (reminder.RepeatType == "hours")
                    {
                        reminder.NextTime = reminder.NextTime.AddHours(reminder.Interval);
                    }
                    else if (reminder.RepeatType == "days")
                    {
                        reminder.NextTime = reminder.NextTime.AddDays(reminder.Interval);
                    }
                }
            }

            foreach (var r in toRemove)
            {
                reminders.Remove(r);
            }

            if (toRemove.Count > 0)
            {
                SaveReminders();
                UpdateList();
            }
        }

        private void ShowNotification(string text)
        {
            SystemSounds.Exclamation.Play();

            var notificationWindow = new NotificationWindow(text);
            notificationWindow.Show();
        }

        private void AddReminder_Click(object sender, RoutedEventArgs e)
        {
            var text = TextInput.Text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                MessageBox.Show("Введите текст напоминания", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!DateInput.SelectedDate.HasValue)
            {
                MessageBox.Show("Выберите дату", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            TimeSpan time;
            if (!TimeSpan.TryParse(TimeInput.Text, out time))
            {
                MessageBox.Show("Неверный формат времени (используйте ЧЧ:ММ)", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var selectedDate = DateInput.SelectedDate.Value;
            var nextTime = selectedDate.Date.Add(time);

            if (nextTime <= DateTime.Now)
            {
                MessageBox.Show("Время напоминания должно быть в будущем", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string repeatType = "once";
            int interval = 0;

            if (RepeatCheckBox.IsChecked == true)
            {
                if (MinutesRadio.IsChecked == true)
                    repeatType = "minutes";
                else if (HoursRadio.IsChecked == true)
                    repeatType = "hours";
                else if (DaysRadio.IsChecked == true)
                    repeatType = "days";

                if (!int.TryParse(IntervalInput.Text, out interval) || interval <= 0)
                {
                    MessageBox.Show("Введите корректный интервал (число больше 0)", "Ошибка",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }

            var reminder = new Reminder();
            reminder.Text = text;
            reminder.NextTime = nextTime;
            reminder.RepeatType = repeatType;
            reminder.Interval = interval;

            reminders.Add(reminder);
            SaveReminders();
            UpdateList();

            TextInput.Clear();
            DateInput.SelectedDate = DateTime.Today;
            TimeInput.Text = "12:00";
            RepeatCheckBox.IsChecked = false;
            IntervalInput.Text = "1";

            MessageBox.Show("Напоминание успешно создано!", "Успех",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void DeleteReminder_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var reminder = button.Tag as Reminder;
            if (reminder != null)
            {
                var result = MessageBox.Show("Удалить это напоминание?", "Подтверждение",
                    MessageBoxButton.YesNo, MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    reminders.Remove(reminder);
                    SaveReminders();
                    UpdateList();
                }
            }
        }

        private void UpdateList()
        {
            var displayItems = new List<ReminderDisplay>();
            foreach (var r in reminders)
            {
                var display = new ReminderDisplay();
                display.Text = r.Text;
                display.TimeInfo = GetTimeInfo(r);
                display.Original = r;
                displayItems.Add(display);
            }

            RemindersList.ItemsSource = displayItems;
        }

        private string GetTimeInfo(Reminder r)
        {
            var timeStr = r.NextTime.ToString("dd.MM.yyyy в HH:mm");
            string repeatInfo = "";

            if (r.RepeatType == "minutes")
                repeatInfo = " • Повтор каждые " + r.Interval + " мин.";
            else if (r.RepeatType == "hours")
                repeatInfo = " • Повтор каждые " + r.Interval + " ч.";
            else if (r.RepeatType == "days")
                repeatInfo = " • Повтор каждые " + r.Interval + " дн.";

            return timeStr + repeatInfo;
        }

        private void SaveReminders()
        {
            try
            {
                var options = new JsonSerializerOptions();
                options.WriteIndented = true;
                var json = JsonSerializer.Serialize(reminders, options);
                File.WriteAllText(dataFile, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadReminders()
        {
            if (File.Exists(dataFile))
            {
                try
                {
                    var json = File.ReadAllText(dataFile);
                    var loaded = JsonSerializer.Deserialize<List<Reminder>>(json);
                    reminders = loaded != null ? loaded : new List<Reminder>();
                }
                catch
                {
                    reminders = new List<Reminder>();
                }
            }
        }
    }

    public class Reminder
    {
        public string Text { get; set; }
        public DateTime NextTime { get; set; }
        public string RepeatType { get; set; }
        public int Interval { get; set; }
    }

    public class ReminderDisplay
    {
        public string Text { get; set; }
        public string TimeInfo { get; set; }
        public Reminder Original { get; set; }
    }
}