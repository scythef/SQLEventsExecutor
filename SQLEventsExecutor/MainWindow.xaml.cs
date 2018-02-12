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

namespace SQLEventsExecutor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<SQLEvent> SQLEvents = new ObservableCollection<SQLEvent>();
        ObservableCollection<SQLEvent> SQLEventsLoadOK = new ObservableCollection<SQLEvent>();
        ObservableCollection<SQLEvent> SQLEventsLoadFailed = new ObservableCollection<SQLEvent>();
        ObservableCollection<SQLEvent> SQLExecutionOK = new ObservableCollection<SQLEvent>();
        ObservableCollection<SQLEvent> SQLExecutionFailed = new ObservableCollection<SQLEvent>();
        bool InitDone = false;
        bool SQLExecutionRunning = false;
        private delegate void NoArgDelegate();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = SQLEvents;
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
            if (SQLExecutionRunning)
            {
                SQLExecutionRunning = false;
            }
            else
            {
                ExecuteSQLEvents();
            }
        }
        private void ExecuteSQLEvents()
        {
            int lFirstItem = 0;
            int lCount = SQLEvents.Count();
            if (!SQLExecutionRunning)
            {
                if (lvSQLEvents.SelectedItems.Count > 1)
                {
                    // MUST be selected TOP DOWN in a grid !!!
                    lFirstItem = lvSQLEvents.SelectedIndex;
                    lCount = lvSQLEvents.SelectedItems.Count;
                }

                int lOKCount = 0;
                int lFailedCount = 0;
                SQLExecutionOK.Clear();
                SQLExecutionFailed.Clear();

                try
                {
                    SQLExecutionRunning = true;
                    btnExecute.Content = "Stop SQL execution!";

                    gMain.Visibility = Visibility.Collapsed;
                    cbFiltr.SelectedIndex = -1;
                    spDetail.Visibility = Visibility.Collapsed;

                    pbProgress.Minimum = 0;
                    pbProgress.Maximum = lCount;
                    pbProgress.Value = 0;
                    spProgress.Visibility = Visibility.Visible;

                    try
                    {
                        SqlConnection lConnection = new SqlConnection(tbConnectionString.Text);
                        lConnection.Open();
                        try
                        {
                            for (int i = lFirstItem; i < lFirstItem+lCount; i++)
                            {
                                try
                                {
                                    try
                                    {
                                        SqlDataReader lReader = null;
                                        try
                                        {
                                            SqlCommand lCommand = new SqlCommand(SQLEvents[i].Batch_text, lConnection);
                                            lReader = lCommand.ExecuteReader();
                                            List<string> lResult = new List<string>
                                            {
                                                "Affected: " + lReader.FieldCount
                                            };
                                            int x = 0;
                                            while (lReader.Read())
                                            {
                                                x++;
                                                List<string> lRow = new List<string>();
                                                for (int j = 0; j < lReader.FieldCount; j++)
                                                {
                                                    lRow.Add(lReader[j].ToString());
                                                }
                                                lResult.Add(string.Join(", ", lRow));
                                            }
                                            lResult.Insert(1, "Read: " + x);
                                            SQLEvents[i].Execution_Dataset = string.Join(Environment.NewLine, lResult.ToArray());
                                        }
                                        finally
                                        {
                                            if (lReader != null)
                                            {
                                                lReader.Close();
                                            }
                                        }
                                        SQLEvents[i].Execution_Result = "OK";
                                        SQLExecutionOK.Add(SQLEvents[i]);
                                        lOKCount++;
                                    }
                                    catch (SqlException e)
                                    {
                                        SQLEvents[i].Execution_Result = "Error";
                                        SQLEvents[i].Execution_Error = e.Message;
                                        SQLExecutionFailed.Add(SQLEvents[i]);
                                        lFailedCount++;
                                    }
                                }
                                catch (Exception e)
                                {
                                    if (SQLEvents[i].Execution_Result == "")
                                    {
                                        SQLEvents[i].Execution_Result = "Error";
                                        SQLEvents[i].Execution_Error = e.Message;
                                        SQLExecutionFailed.Add(SQLEvents[i]);
                                        lFailedCount++;
                                    }
                                }
                                pbProgress.Value = (i + 1);
                                tbOK.Text = lOKCount.ToString();
                                tbFailed.Text = lFailedCount.ToString();
                                Refresh(spProgress);

                                if (!SQLExecutionRunning) break;
                            }
                        }
                        finally
                        {
                            if (lConnection != null)
                            {
                                lConnection.Close();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show(e.Message);
                    }
                }
                finally
                {
                    cbFiltr.SelectedIndex = 0;
                    gMain.Visibility = Visibility.Visible;
                    spDetail.Visibility = Visibility.Visible;
                    spProgress.Visibility = Visibility.Collapsed;
                    System.Windows.Forms.MessageBox.Show("Executed " + (lOKCount+lFailedCount).ToString() + " SQL batches." + Environment.NewLine + lFailedCount + " errors.");
                    SQLExecutionRunning = false;
                    btnExecute.Content = "Execute";
                }
            }
        }

        private void LoadFiles()
        {
            string[] filePaths = Directory.GetFiles(tbPath.Text, "*.csv");
            gMain.Visibility = Visibility.Collapsed;
            cbFiltr.SelectedIndex = -1;
            spDetail.Visibility = Visibility.Collapsed;
            pbFileProgress.Minimum = 0;
            pbFileProgress.Maximum = filePaths.Count();
            pbFileProgress.Value = 0;
            tbFiles.Text = "Files loaded: 0 / " + filePaths.Count();
            spProgress.Visibility = Visibility.Visible;
            SQLEvents.Clear();
            SQLEventsLoadOK.Clear();
            SQLEventsLoadFailed.Clear();
            try
            {
                for (int i = 0; i < filePaths.Count(); i++)
                {
                    LoadSQLEvents(filePaths[i]);
                    tbFiles.Text = "Files loaded: " + (i+1) + " / " + filePaths.Count();
                    pbFileProgress.Value = (i + 1);
                    Refresh(spProgress);
                }
                System.Windows.Forms.MessageBox.Show("Loaded " + SQLEvents.Count + " records." + Environment.NewLine + SQLEventsLoadFailed.Count + " errors.");
            }
            finally
            {
                cbFiltr.SelectedIndex = 0;
                gMain.Visibility = Visibility.Visible;
                spDetail.Visibility = Visibility.Visible;
                spProgress.Visibility = Visibility.Collapsed;
            }
        }

        private void ExportAllSQLEvents()
        {
            ObservableCollection<SQLEvent> lToExport;
            switch (cbFiltr.SelectedIndex)
            {
                case 0: lToExport = SQLEvents; break;
                case 1: lToExport = SQLEventsLoadOK; break;
                case 2: lToExport = SQLEventsLoadFailed; break;
                case 3: lToExport = SQLExecutionOK; break;
                case 4: lToExport = SQLExecutionFailed; break;
                default: lToExport = SQLEvents; break;
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

        private void LoadSQLEvents(string aFile)
        {
            var csv = new ExcelQueryFactory(aFile);
            var lRows = from c in csv.Worksheet<Row>() select c;
            List<Row> lListOfRows = lRows.ToList();
            if (lListOfRows.Count() > 0)
            {
                int lOKCount = 0;
                int lFailedCount = 0;

                pbProgress.Minimum = 0;
                pbProgress.Maximum = lListOfRows.Count();
                pbProgress.Value = 0;
                for (int i = 0; i < lListOfRows.Count(); i++)
                {
                    Row xRow = lListOfRows[i];
                    try
                    {
                        SQLEvent lSQLE = new SQLEvent
                        {
                            Name = xRow[0].ToString(),
                            Timestamp = DateTime.ParseExact(xRow[1], "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                            Timestamputc = DateTime.ParseExact(xRow[2], "yyyy-MM-dd HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
                            Cpu_time = int.Parse(xRow[3]),
                            Duration = int.Parse(xRow[4]),
                            Physical_reads = int.Parse(xRow[5]),
                            Logical_reads = int.Parse(xRow[6]),
                            Writes = int.Parse(xRow[7]),
                            Row_count = int.Parse(xRow[8]),
                            Event_Result = xRow[9],
                            Batch_text = xRow[10],
                            Database_name = xRow[11],
                            Loading_Error = "",
                            Loading_Result = "OK",
                            Execution_Result = "",
                            Execution_Error = "",
                            Execution_Dataset = ""
                        };
                        SQLEvents.Add(lSQLE);
                        SQLEventsLoadOK.Add(lSQLE);
                        lOKCount++;
                    }
                    catch (Exception le)
                    {
                        SQLEvent lSQLE = new SQLEvent
                        {
                            Name = xRow[0],
                            Event_Result = xRow[9],
                            Batch_text = xRow[10],
                            Database_name = xRow[11],
                            Loading_Error = le.Message,
                            Loading_Result = "Error",
                            Execution_Result = "",
                            Execution_Error = "",
                            Execution_Dataset = ""
                        };
                        SQLEvents.Add(lSQLE);
                        SQLEventsLoadFailed.Add(lSQLE);
                        lFailedCount++;
                    }

                    if (i == Math.Floor(Convert.ToDouble(i / 100)) * 100)
                    {
                        pbProgress.Value = (i + 1);
                        tbOK.Text = lOKCount.ToString();
                        tbFailed.Text = lFailedCount.ToString();
                        Refresh(spProgress);
                    }
                }
            }
            ExeLoadInfo.Content = String.Format("Loaded {0} SQL events.", SQLEvents.Count().ToString());
            Refresh(ExeLoadInfo);
        }

        public static void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                (NoArgDelegate)delegate { });
        }

        private void Button_Click_Load(object sender, RoutedEventArgs e)
        {
            LoadFiles();
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
                    case 0: lvSQLEvents.ItemsSource = SQLEvents; break;
                    case 1: lvSQLEvents.ItemsSource = SQLEventsLoadOK; break;
                    case 2: lvSQLEvents.ItemsSource = SQLEventsLoadFailed; break;
                    case 3: lvSQLEvents.ItemsSource = SQLExecutionOK; break;
                    case 4: lvSQLEvents.ItemsSource = SQLExecutionFailed; break;
                    default: lvSQLEvents.ItemsSource = null; break;
                }
            }
        }

        private void Button_Click_Export(object sender, RoutedEventArgs e)
        {
            ExportAllSQLEvents();
        }
    }

    public class SQLEvent
    {
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime Timestamputc { get; set; }
        public int Cpu_time { get; set; }
        public int Duration { get; set; }
        public int Physical_reads { get; set; }
        public int Logical_reads { get; set; }
        public int Writes { get; set; }
        public int Row_count { get; set; }
        public string Batch_text { get; set; }
//        public string Batch_Result { get; set; }
        public string Database_name { get; set; }
        public string Event_Result { get; set; }
        public string Loading_Result { get; set; }
        public string Loading_Error { get; set; }
//        public bool IsVisible { get; set; }
        public string Execution_Result { get; set; }
        public string Execution_Error { get; set; }
        public string Execution_Dataset { get; set; }
        public string Execution_Output
        {
            get
            {
                if (Execution_Error == "") //Execution OK
                {
                    return Execution_Dataset;
                }
                else //Execution error
                {
                    return Execution_Error;
                }
            }
        }
        public string Execution_OutputColor
        {
            get
            {
                if (Execution_Error == "") //Execution OK
                {
                    return "DarkGreen";
                }
                else //Execution error
                {
                    return "Red";
                }
            }
        }
        public string Background
        {
            get
            {
                if (Execution_Result == "") //not executed yet
                {
                    if (Loading_Result == "OK") //loading OK
                    {
                        return "White";
                    }
                    else //loading error
                    {
                        return "LightYellow";
                    }
                }
                else //already executed
                {
                    if (Execution_Result == "OK") //execution OK
                    {
                        return "LightGreen";
                    }
                    else //execution error
                    {
                        return "Yellow";
                    }
                }
            }
        }
    }
}
