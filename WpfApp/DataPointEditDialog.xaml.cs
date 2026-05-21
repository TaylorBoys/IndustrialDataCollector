using Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp;

public partial class DataPointEditDialog : Window
{
    public DataPointConfig? DataPoint { get; private set; }
    private bool _isEditMode = false;
    private Dictionary<string, DataType> _dataTypeMap = new();

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
        
        // 根据数据类型选择对应的中文显示项
        var typeNames = new Dictionary<DataType, string>
        {
            { DataType.Bool, "布尔" },
            { DataType.Int16, "短整数" },
            { DataType.UInt16, "无符号短整数" },
            { DataType.Int32, "整数" },
            { DataType.UInt32, "无符号整数" },
            { DataType.Float, "浮点数" },
            { DataType.Double, "双精度" },
            { DataType.String, "字符串" }
        };
        
        if (typeNames.TryGetValue(existing.DataType, out var typeName))
        {
            DataTypeComboBox.SelectedItem = typeName;
        }
        
        UnitTextBox.Text = existing.Unit ?? string.Empty;
        ScaleFactorTextBox.Text = existing.ScaleFactor?.ToString() ?? "1.0";
        OffsetTextBox.Text = existing.Offset?.ToString() ?? "0.0";
    }

    private void InitializeComboBox()
    {
        _dataTypeMap = new Dictionary<string, DataType>
        {
            { "布尔", DataType.Bool },
            { "短整数", DataType.Int16 },
            { "无符号短整数", DataType.UInt16 },
            { "整数", DataType.Int32 },
            { "无符号整数", DataType.UInt32 },
            { "浮点数", DataType.Float },
            { "双精度", DataType.Double },
            { "字符串", DataType.String }
        };
        
        foreach (var typeName in _dataTypeMap.Keys)
        {
            DataTypeComboBox.Items.Add(typeName);
        }
        DataTypeComboBox.SelectedIndex = 1; // 默认短整数
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

        if (!double.TryParse(ScaleFactorTextBox.Text, out double scaleFactor))
        {
            MessageBox.Show("请输入有效的缩放因子");
            return;
        }

        if (!double.TryParse(OffsetTextBox.Text, out double offset))
        {
            MessageBox.Show("请输入有效的偏移量");
            return;
        }

        DataType dataType = DataType.Int16;
        if (DataTypeComboBox.SelectedItem is string selectedTypeName && 
            _dataTypeMap.TryGetValue(selectedTypeName, out var type))
        {
            dataType = type;
        }

        DataPoint = new DataPointConfig
        {
            Name = NameTextBox.Text,
            Address = AddressTextBox.Text,
            DataType = dataType,
            Unit = string.IsNullOrWhiteSpace(UnitTextBox.Text) ? null : UnitTextBox.Text,
            ScaleFactor = scaleFactor,
            Offset = offset
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
