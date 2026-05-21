using CommunityToolkit.Mvvm.ComponentModel;
using Core.Models;

namespace WpfApp.ViewModels;

public partial class DeviceViewModel : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private ProtocolType _protocol;

    [ObservableProperty]
    private string _ipAddress = string.Empty;

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private bool _isScanning;

    [ObservableProperty]
    private DateTime _lastUpdate = DateTime.Now;

    public DeviceConfig Config { get; }

    public DeviceViewModel(DeviceConfig config)
    {
        Config = config;
        Name = config.Name;
        Protocol = config.Protocol;
        IpAddress = config.IpAddress;
    }
}
