using System.Windows;

using Sentinel.Core.ViewModels;

namespace Sentinel
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainWindowViewModel();
        }
    }
}