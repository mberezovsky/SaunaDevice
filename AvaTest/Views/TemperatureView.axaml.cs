using System;
using System.ComponentModel;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AvaTest.ViewModels;
using OxyPlot.Avalonia;

// using OxyPlot.Avalonia;

namespace AvaTest.Views
{
    public partial class TemperatureView : UserControl
    {
        public TemperatureView()
        {
            InitializeComponent();
            if (Plotter == null)
                Plotter = this.FindControl<Plot>("Plotter");
            DataContextChanged += OnDataContextChanged;

            string locale = Thread.CurrentThread.CurrentUICulture.Name;
        }

        private void OnDataContextChanged(object? sender, EventArgs e)
        {
            var viewModel = DataContext as GpioViewModel;
            if (viewModel != null)
            {
                viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            }
        }

        private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Plotter != null)
            {
                Dispatcher.UIThread.InvokeAsync(() => Plotter.InvalidatePlot(true));
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}