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
            List<string> lItems = Sim.GetCollectionNamesList();
            for (int i = 0; i < lItems.Count; i++)
            {
                cbFiltr.Items.Add(lItems[i]);
            }
            cbFiltr.SelectedIndex = -1;
            btnExport.DataContext = Sim;
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
                Sim.SQLCollectionSelectedIndex = cbFiltr.SelectedIndex;
                lvSQLEvents.ItemsSource = Sim.GetSelectedCollection;
            }
        }

        private void Button_Click_Export(object sender, RoutedEventArgs e)
        {
            if (Sim.ExportRunning)
            {
                Sim.ExportRunning = false;
            }
            else
            {
                Sim.ExportEvents(tbPath.Text, spButtonExport);
            }
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

        private void TbPath_TextChanged(object sender, TextChangedEventArgs e)
        {
            Sim.ReadDirectory(tbPath.Text);
        }
    }

}
