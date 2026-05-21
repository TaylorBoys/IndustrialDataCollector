using Core.Models;
using System.Windows;

namespace WpfApp;

public partial class DataPointWindow : Window
{
    public List<DataPointConfig> DataPoints { get; set; } = new();

    public DataPointWindow(List<DataPointConfig> existingPoints)
    {
        InitializeComponent();
        DataPoints = new List<DataPointConfig>(existingPoints);
        DataPointListView.ItemsSource = DataPoints;
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new DataPointEditDialog();
        if (dialog.ShowDialog() == true && dialog.DataPoint != null)
        {
            DataPoints.Add(dialog.DataPoint);
            DataPointListView.Items.Refresh();
        }
    }

    private void EditButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = DataPointListView.SelectedItem as DataPointConfig;
        if (selected != null)
        {
            var dialog = new DataPointEditDialog(selected);
            if (dialog.ShowDialog() == true)
            {
                DataPointListView.Items.Refresh();
            }
        }
        else
        {
            MessageBox.Show("请选择要编辑的数据点");
        }
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = DataPointListView.SelectedItem as DataPointConfig;
        if (selected != null)
        {
            if (MessageBox.Show($"确定要删除数据点 '{selected.Name}' 吗?", "确认删除", 
                MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK)
            {
                DataPoints.Remove(selected);
                DataPointListView.Items.Refresh();
            }
        }
        else
        {
            MessageBox.Show("请选择要删除的数据点");
        }
    }

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
