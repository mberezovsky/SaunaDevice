using System;
using System.Globalization;
using Avalonia.Threading;
using ReactiveUI;

namespace AvaTest.ViewModels
{
    public class CalendarViewModel : ViewModelBase
    {
        private DispatcherTimer m_dispatcherTimer;
        private int _year;
        private int _month;
        private int _day;
        private DayOfWeek _dayOfWeek;
        private int _hour;
        private int _minute;
        private int _second;
        private string _dayOfWeekString;

        public CalendarViewModel()
        {
            m_dispatcherTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, TimerTick);
            m_dispatcherTimer.Start();
        }

        private void TimerTick(object? sender, EventArgs e)
        {
            var currentDateTime = DateTime.Now;

            Year = currentDateTime.Year;
            Month = currentDateTime.Month;
            Day = currentDateTime.Day;
            DayOfWeek = currentDateTime.DayOfWeek;
            Hour = currentDateTime.Hour;
            Minute = currentDateTime.Minute;
            Second = currentDateTime.Second;
        }

        public int Year
        {
            get => _year;
            set => this.RaiseAndSetIfChanged(ref _year, value);
        }

        public int Month
        {
            get => _month;
            set => this.RaiseAndSetIfChanged(ref _month, value);
        }

        public int Day
        {
            get => _day;
            set => this.RaiseAndSetIfChanged(ref _day, value);
        }

        public DayOfWeek DayOfWeek
        {
            get => _dayOfWeek;
            set
            {
                if (value == _dayOfWeek)
                    return;

                var cultureInfo = CultureInfo.CurrentCulture;
                DayOfWeekString = System.Globalization.DateTimeFormatInfo.CurrentInfo.DayNames[(int)value];// value.ToString("dddd");
                this.RaiseAndSetIfChanged(ref _dayOfWeek, value);
            }
        }

        public string DayOfWeekString
        {
            get => _dayOfWeekString;
            set => this.RaiseAndSetIfChanged(ref _dayOfWeekString, value);
        }
        
        public int Hour
        {
            get => _hour;
            set => this.RaiseAndSetIfChanged(ref _hour, value);
        }

        public int Minute
        {
            get => _minute;
            set => this.RaiseAndSetIfChanged(ref _minute, value);
        }

        public int Second
        {
            get => _second;
            set => this.RaiseAndSetIfChanged(ref _second, value);
        }
    }
}