using Core.Models;
using System.Windows;

namespace WpfApp;

public partial class DataPointEditDialog : Window
{
    public DataPointConfig? DataPoint { get; private set; }
    private bool _isEditMode = false;

    public DataPointEditDialog()
    {
        InitializeComponent();
        InitializeComboBox();
    }

    public DataPointEditDialog(DataPointConfig existing)
    {
        InitializeComponent();
        InitializeComboBox();
        _isEditMode = true;
        
        NameTextBox.Text = existing.Name;
        AddressTextBox.Text = existing.Address;
        DataTypeComboBox.SelectedItem = existing.DataType;
        UnitTextBox.Text = existing.Unit ?? string.Empty;
    }

    private void InitializeComboBox()
    {
        foreach (var type in Enum.GetValues(typeof(DataType)))
        {
            DataTypeComboBox.Items.Add(type);
        }
        DataTypeComboBox.SelectedIndex = 0;
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            MessageBox.Show("请输入数据点名称");
            return;
        }

        if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
        {
            MessageBox.Show("请输入数据点地址");
            return;
        }

        DataPoint = new DataPointConfig
        {
            Name = NameTextBox.Text,
            Address = AddressTextBox.Text,
            DataType = (DataType)(DataTypeComboBox.SelectedItem ?? DataType.Int16),
            Unit = string.IsNullOrWhiteSpace(UnitTextBox.Text) ? null : UnitTextBox.Text,
            ScaleFactor = 1.0,
            Offset = 0.0
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
