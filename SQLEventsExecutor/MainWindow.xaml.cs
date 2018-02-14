using LinqToExcel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace SQLEventsExecutor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Simulator Sim;

        bool InitDone = false;
        private delegate void NoArgDelegate();

        public MainWindow()
        {
            Sim = new Simulator();
            DataContext = Sim.SQLEvents;
            InitializeComponent();
            cbFiltr.SelectedIndex = -1;
            btnExecute.DataContext = Sim;
            btnLoad.DataContext = Sim;
            try
            {
                tbPath.Text = Connections.ConnectionDict["CSVDefault"];
                tbConnectionString.Text = Connections.ConnectionDict["SQLDefault"];
            }
            catch
            {
                //I do not care - strings can be copied into textboxes directly from a clipboard later on
            }
            InitDone = true;
        }

        private void Button_Click_Execute(object sender, RoutedEventArgs e)
        {
            if (Sim.ExecRunning)
            {
                Sim.ExecRunning = false;
            }
            else
            {
                Sim.ExecuteSQLEvents(tbConnectionString.Text, spButtonExecute);
            }
        }

        private void ExportAllSQLEvents()
        {
            ObservableCollection<SQLEvent> lToExport;
            switch (cbFiltr.SelectedIndex)
            {
                case 0: lToExport = Sim.SQLEvents; break;
                case 1: lToExport = Sim.SQLEventsLoadOK; break;
                case 2: lToExport = Sim.SQLEventsLoadFailed; break;
                case 3: lToExport = Sim.SQLExecuted; break;
                case 4: lToExport = Sim.SQLExecutionOK; break;
                case 5: lToExport = Sim.SQLExecutionFailed; break;
                default: lToExport = Sim.SQLEvents; break;
            }

            CsvExport<SQLEvent> csv = new CsvExport<SQLEvent>(lToExport.ToList<SQLEvent>());
            try
            {
                string lFileName = String.Format(@"{0}\Export\SQLEventsExeExport_{1}.csv",
                    tbPath.Text,
                    DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss"));
                csv.ExportToFile(lFileName);

                System.Windows.Forms.MessageBox.Show("Exported "+lToExport.Count+" records to "+ lFileName);
            }
            catch (Exception e)
            {
                System.Windows.Forms.MessageBox.Show(e.Message);
            }
        }

        public static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                (NoArgDelegate)delegate { });
        }

        private void Button_Click_Load(object sender, RoutedEventArgs e)
        {
            if (Sim.LoadRunning)
            {
                Sim.LoadRunning = false;
            }
            else
            {
                Sim.LoadFiles(spButtonLoad);
            }
            cbFiltr.SelectedIndex = 0;
        }

        private void ListViewSQLEvents_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            spDetail.DataContext = ((sender as System.Windows.Controls.ListView).SelectedItem as SQLEvent);
        }

        private void Button_Click_Open(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog fbd = new FolderBrowserDialog
            {
                Description = "Choose folder containing CSV (Comma Separated Values) Files (*.csv)"
            };
            DialogResult result = fbd.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                tbPath.Text = fbd.SelectedPath;
            }
        }

        private void CbFiltr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (InitDone)
            {
                switch (cbFiltr.SelectedIndex)
                {
                    case 0: lvSQLEvents.ItemsSource = Sim.SQLEvents; break;
                    case 1: lvSQLEvents.ItemsSource = Sim.SQLEventsLoadOK; break;
                    case 2: lvSQLEvents.ItemsSource = Sim.SQLEventsLoadFailed; break;
                    case 3: lvSQLEvents.ItemsSource = Sim.SQLExecuted; break;
                    case 4: lvSQLEvents.ItemsSource = Sim.SQLExecutionOK; break;
                    case 5: lvSQLEvents.ItemsSource = Sim.SQLExecutionFailed; break;
                    default: lvSQLEvents.ItemsSource = null; break;
                }
            }
        }

        private void Button_Click_Export(object sender, RoutedEventArgs e)
        {
            ExportAllSQLEvents();
        }

        private void Button_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button x = (sender as System.Windows.Controls.Button);
            x.Background = Brushes.Green;
        }

        private void Button_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            System.Windows.Controls.Button x = (sender as System.Windows.Controls.Button);
            x.Background = Brushes.LightBlue;
        }

        private void Grid_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            foreach (UIElement x in (sender as Grid).Children)
            {
                if (x.GetType() == typeof(System.Windows.Controls.Button))
                {
                    switch ((x as System.Windows.Controls.Button).Name)
                    {
                        case "btnFirst":
                            if (((int)(x as System.Windows.Controls.Button).Tag < Sim.LastExecutedIndex) &&
                                ((int)(x as System.Windows.Controls.Button).Tag != Sim.FirstExecutedIndex))
                            {
                                (x as System.Windows.Controls.Button).Visibility = Visibility.Visible;
                            }
                            break;
                        case "btnLast":
                            if (((int)(x as System.Windows.Controls.Button).Tag > Sim.FirstExecutedIndex) &&
                                ((int)(x as System.Windows.Controls.Button).Tag != Sim.LastExecutedIndex))
                            {
                                (x as System.Windows.Controls.Button).Visibility = Visibility.Visible;
                            }
                            break;
                    }
                }
            }
        }

        private void Grid_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            foreach (UIElement x in (sender as Grid).Children)
            {
                if (x.GetType() == typeof(System.Windows.Controls.Button))
                {
                    (x as System.Windows.Controls.Button).Visibility = Visibility.Collapsed;
                }
            }

        }

        private void ButtonGreen_Click(object sender, RoutedEventArgs e)
        {
            Sim.FirstExecutedIndex = (int)(sender as System.Windows.Controls.Button).Tag;
        }

        private void ButtonRed_Click(object sender, RoutedEventArgs e)
        {
            Sim.LastExecutedIndex = (int)(sender as System.Windows.Controls.Button).Tag;
        }

        private void tbPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Sim.ReadDirectory(tbPath.Text);
        }
    }

}
