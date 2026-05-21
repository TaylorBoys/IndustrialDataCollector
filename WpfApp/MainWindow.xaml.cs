using System.Windows;
using WpfApp.ViewModels;

namespace WpfApp;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new DeviceManagementViewModel();
    }
}
