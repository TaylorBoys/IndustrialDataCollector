using Core.Models;
using System.Windows;

namespace WpfApp;

public partial class AddDeviceWindow : Window
{
    public DeviceConfig? NewDevice { get; private set; }

    public AddDeviceWindow()
    {
        InitializeComponent();
    }

    public AddDeviceWindow(DeviceConfig existing)
    {
        InitializeComponent();
        DeviceNameTextBox.Text = existing.Name;
        
        ProtocolComboBox.SelectedIndex = existing.Protocol switch
        {
            ProtocolType.ModbusTcp => 0,
            ProtocolType.ModbusRtu => 1,
            ProtocolType.OpcUa => 2,
            _ => 0
        };
        
        IpAddressTextBox.Text = existing.IpAddress;
        PortTextBox.Text = existing.Port.ToString();
        SlaveIdTextBox.Text = existing.SlaveId.ToString();
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(DeviceNameTextBox.Text))
        {
            MessageBox.Show("请输入设备名称", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!int.TryParse(PortTextBox.Text, out int port))
        {
            MessageBox.Show("请输入有效的端口号", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        if (!int.TryParse(SlaveIdTextBox.Text, out int slaveId))
        {
            MessageBox.Show("请输入有效的设备地址", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        var protocol = ProtocolComboBox.SelectedIndex switch
        {
            0 => ProtocolType.ModbusTcp,
            1 => ProtocolType.ModbusRtu,
            2 => ProtocolType.OpcUa,
            _ => ProtocolType.ModbusTcp
        };

        NewDevice = new DeviceConfig
        {
            Name = DeviceNameTextBox.Text,
            Protocol = protocol,
            IpAddress = IpAddressTextBox.Text,
            Port = port,
            OpcUaEndpoint = protocol == ProtocolType.OpcUa ? IpAddressTextBox.Text : null,
            ScanIntervalMs = 1000,
            SlaveId = slaveId
        };

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
