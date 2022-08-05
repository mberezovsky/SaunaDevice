using System.Reactive;
using ReactiveUI;

namespace AvaTest.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _greeting;

        public MainWindowViewModel()
        {
            _greeting = "Greeting to Avalonia";

            IncreaseCounterCommand = ReactiveCommand.Create(DoIncreaseCounter);
        }

        private void DoIncreaseCounter()
        {
            m_counter++;
            Greeting = $"Greeting to Avalonia (counter: {m_counter})";
        }

        public string Greeting
        {
            get => _greeting;
            set => this.RaiseAndSetIfChanged(ref _greeting, value);
        }

        private int m_counter = 0;

        private ReactiveCommand<Unit, Unit> IncreaseCounterCommand { get; }

        public CalendarViewModel Calendar { get; } = new CalendarViewModel();
        public GpioViewModel GpioViewModel { get; } = new GpioViewModel();
    }
}