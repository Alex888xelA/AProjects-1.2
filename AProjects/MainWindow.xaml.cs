using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace AProjects
{
    //public delegate void PropertyChangedEventHandler(Object sender, PropertyChangedEventArgs e);
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ViewModel viewModel;

        public MainWindow()
        {
            InitializeComponent();
            viewModel = new ViewModel();
            this.DataContext = viewModel;
            viewModel.mainDataGrid = mainDataGrid;
            mainDataGrid.ItemsSource = viewModel.viewRecords;
            this.Top = Properties.Settings.Default.WindowTop;
            this.Left = Properties.Settings.Default.WindowLeft;
            Application.Current.MainWindow.Width = Properties.Settings.Default.WindowWidth;
            Application.Current.MainWindow.Height = Properties.Settings.Default.WindowHeight;
        }

        //Этот метод срабатывает при редактировании ячеек (до принятия изменений)
        //Определяет измененную ячейку GataGrid и передает ее адрес и содержимое во ViewModel
        private void MainDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                DataGridRow row = e.Row;
                ViewRecord vr = (ViewRecord)row.Item;
                //Int32 columnIndex = e.Column.DisplayIndex; //Номер ячейки в строке
                DataGridCell dataGridCell = (DataGridCell)e.EditingElement.Parent;
                TextBox textBox = (TextBox)dataGridCell.Content;
                String content = textBox.Text;
                String columnName = e.Column.SortMemberPath;
                viewModel.DataGridChanged(vr, columnName, content);
            }
        }

        //Срабатывает при изменении выделения
        private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            viewModel.SelectedViewRecords.Clear();
            foreach (DataGridCellInfo cell in mainDataGrid.SelectedCells)
            {
                viewModel.SelectedViewRecords.Add((ViewRecord)cell.Item);
            }
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            viewModel.SaveBeforExit();
            //Double top = this.Top;
            Properties.Settings.Default.WindowTop = this.Top;
            Properties.Settings.Default.WindowLeft = this.Left;
            Properties.Settings.Default.WindowWidth = this.ActualWidth;
            Properties.Settings.Default.WindowHeight = this.ActualHeight;
        }

        //Щелчек по кнопке "Развернуть"
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            String s = sender.ToString();
            MessageBox.Show(s);
            viewModel.UnHideRecordsCommand(null);
        }
    }
}

