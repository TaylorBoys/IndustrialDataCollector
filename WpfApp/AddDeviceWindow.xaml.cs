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
        ProtocolComboBox.SelectedIndex = existing.Protocol == ProtocolType.OpcUa ? 1 : 0;
        IpAddressTextBox.Text = existing.IpAddress;
        PortTextBox.Text = existing.Port.ToString();
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

        var protocol = ProtocolComboBox.SelectedIndex == 0 ? ProtocolType.ModbusTcp : ProtocolType.OpcUa;

        NewDevice = new DeviceConfig
        {
            Name = DeviceNameTextBox.Text,
            Protocol = protocol,
            IpAddress = IpAddressTextBox.Text,
            Port = port,
            OpcUaEndpoint = protocol == ProtocolType.OpcUa ? IpAddressTextBox.Text : null,
            ScanIntervalMs = 1000,
            SlaveId = 1
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
