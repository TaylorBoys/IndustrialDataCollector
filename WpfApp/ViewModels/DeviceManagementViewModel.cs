using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Core.Models;
using Serilog;
using System.Collections.ObjectModel;
using System.Windows;

namespace WpfApp.ViewModels;

public partial class DeviceManagementViewModel : ObservableObject
{
    [ObservableProperty]
    private DeviceViewModel? _selectedDevice;

    public ObservableCollection<DeviceViewModel> Devices { get; } = new();

    public DeviceManagementViewModel()
    {
        LoadDemoData();
    }

    private void LoadDemoData()
    {
        var demoConfig = new DeviceConfig
        {
            Name = "Modbus设备1",
            Protocol = ProtocolType.ModbusTcp,
            IpAddress = "192.168.1.100",
            Port = 502,
            ScanIntervalMs = 1000,
            DataPoints = new List<DataPointConfig>
            {
                new DataPointConfig { Name = "温度传感器", Address = "40001", DataType = DataType.Float, Unit = "°C" },
                new DataPointConfig { Name = "压力传感器", Address = "40003", DataType = DataType.Float, Unit = "kPa" }
            }
        };
        
        Devices.Add(new DeviceViewModel(demoConfig)
        {
            IsConnected = true,
            IsScanning = true
        });
    }

    [RelayCommand]
    private void AddDevice()
    {
        var window = new AddDeviceWindow
        {
            Owner = Application.Current.MainWindow
        };
        
        if (window.ShowDialog() == true && window.NewDevice != null)
        {
            Devices.Add(new DeviceViewModel(window.NewDevice));
            Log.Information("Added new device: {Name}", window.NewDevice.Name);
        }
    }

    [RelayCommand]
    private void EditDevice()
    {
        if (SelectedDevice != null)
        {
            var window = new AddDeviceWindow(SelectedDevice.Config)
            {
                Owner = Application.Current.MainWindow
            };
            
            if (window.ShowDialog() == true && window.NewDevice != null)
            {
                SelectedDevice.Name = window.NewDevice.Name;
                SelectedDevice.Protocol = window.NewDevice.Protocol;
                SelectedDevice.IpAddress = window.NewDevice.IpAddress;
                SelectedDevice.Config.Port = window.NewDevice.Port;
                SelectedDevice.Config.ScanIntervalMs = window.NewDevice.ScanIntervalMs;
                SelectedDevice.Config.SlaveId = window.NewDevice.SlaveId;
                SelectedDevice.Config.OpcUaEndpoint = window.NewDevice.OpcUaEndpoint;
                
                Log.Information("Edited device: {Name}", window.NewDevice.Name);
            }
        }
        else
        {
            MessageBox.Show("请选择要编辑的设备");
        }
    }

    [RelayCommand]
    private void DeleteDevice()
    {
        if (SelectedDevice != null)
        {
            if (MessageBox.Show($"确定要删除设备 '{SelectedDevice.Name}' 吗?", "确认删除", 
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                Devices.Remove(SelectedDevice);
                Log.Information("Deleted device: {Name}", SelectedDevice.Name);
            }
        }
        else
        {
            MessageBox.Show("请选择要删除的设备");
        }
    }

    [RelayCommand]
    private void ManageDataPoints()
    {
        if (SelectedDevice != null)
        {
            var window = new DataPointWindow(SelectedDevice.Config.DataPoints)
            {
                Owner = Application.Current.MainWindow
            };
            
            if (window.ShowDialog() == true)
            {
                SelectedDevice.Config.DataPoints = window.DataPoints;
                Log.Information("Updated data points for device: {Name}", SelectedDevice.Name);
            }
        }
        else
        {
            MessageBox.Show("请选择要管理数据点的设备");
        }
    }

    [RelayCommand]
    private void StartAll()
    {
        Log.Information("Start all devices");
        foreach (var device in Devices)
        {
            device.IsScanning = true;
        }
    }

    [RelayCommand]
    private void StopAll()
    {
        Log.Information("Stop all devices");
        foreach (var device in Devices)
        {
            device.IsScanning = false;
        }
    }
}
