using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace AvaTest.Views
{
    public partial class CalendarView : UserControl
    {
        public CalendarView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}