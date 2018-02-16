using LinqToExcel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SQLEventsExecutor
{
    public class Simulator : NotificationBase
    {
        private delegate void NoArgDelegate();
        private int LoadErrorCount;

        public ObservableCollection<SQLEvent> SQLEvents;
        public ObservableCollection<SQLEvent> SQLEventsLoadOK;
        public ObservableCollection<SQLEvent> SQLEventsLoadFailed;
        public ObservableCollection<SQLEvent> SQLExecuted;
        public ObservableCollection<SQLEvent> SQLExecutionOK;
        public ObservableCollection<SQLEvent> SQLExecutionFailed;

        private Dictionary<int, ObservableCollection<SQLEvent>> SQLCollections = new Dictionary<int, ObservableCollection<SQLEvent>>();
        private Dictionary<int, string> SQLCollectionsNames = new Dictionary<int, string> {
            { 0, "All loaded events" },
            { 1, "  Loaded correctly" },
            { 2, "  Loaded with error" },
            { 3, "All executed events" },
            { 4, "  Executed correctly" },
            { 5, "  Execution with error" }
        };
        public List<string> GetCollectionNamesList()
        {
            List<string> lResult = new List<string>();
            for (int i = 0; i < SQLCollectionsNames.Count; i++)
            {
                lResult.Add(SQLCollectionsNames[i]);
            }
            return lResult;
        }
        public ObservableCollection<SQLEvent> GetSelectedCollection
        {
            get
            {
                return SQLCollections[SQLCollectionSelectedIndex];
            }
        }
        public string GetSelectedCollectionName
        {
            get
            {
                return SQLCollectionsNames[SQLCollectionSelectedIndex];
            }
        }
        private int _SQLCollectionSelectedIndex;
        public int SQLCollectionSelectedIndex
        {
            get
            {
                return _SQLCollectionSelectedIndex;
            }
            set
            {
                SetProperty(ref _SQLCollectionSelectedIndex, value);
                RaisePropertyChanged("GetSelectedCollection");
                RaisePropertyChanged("SQLCollectionsNames");
                RaisePropertyChanged("ExportCommand");
                RaisePropertyChanged("ExportProgressMax");
            }
        }

        #region Load
        private string[] _FilePaths;
        public string[] FilePaths
        {
            get
            {
                return _FilePaths;
            }
            set
            {
                SetProperty(ref _FilePaths, value);
                RaisePropertyChanged("LoadCommand");
                RaisePropertyChanged("LoadProgressMax");
            }
        }
        public string LoadCommand
        {
            get
            {
                if (LoadRunning)
                {
                    return "Stop loading!";
                }
                else
                {
                    return "Load "+ FilePaths.Count() +" files.";
                }
            }
        }
        private bool _LoadRunning;
        public bool LoadRunning
        {
            get
            {
                return _LoadRunning;
            }
            set
            {
                SetProperty(ref _LoadRunning, value);
                RaisePropertyChanged("LoadCommand");
            }
        }
        private string _LoadProgressText;
        public string LoadProgressText
        {
            get
            {
                return _LoadProgressText;
            }
            set
            {
                SetProperty(ref _LoadProgressText, value);
            }
        }
        private string _LoadErrorText;
        public string LoadErrorText
        {
            get
            {
                return _LoadErrorText;
            }
            set
            {
                SetProperty(ref _LoadErrorText, value);
            }
        }
        private int _LoadProgressValue;
        public int LoadProgressValue
        {
            get
            {
                return _LoadProgressValue;
            }
            set
            {
                SetProperty(ref _LoadProgressValue, value);
            }
        }

        public int LoadProgressMax
        {
            get
            {
                return FilePaths.Count()*100;
            }
        }
        public string _LoadProgressColor;
        public string LoadProgressColor
        {
            get
            {
                return _LoadProgressColor;
            }
            set
            {
                SetProperty(ref _LoadProgressColor, value);
            }
        }
        public bool Loaded
        {
            get
            {
                return (SQLEvents.Count > 0);
            }
        }
        public bool CSVFound
        {
            get
            {
                return (FilePaths.Count() > 0);
            }
        }
        public void ReadDirectory(string aPath)
        {
            try
            {
                FilePaths = Directory.GetFiles(aPath, "*.csv");
            }
            catch
            {
                FilePaths = new string[] { };
                //todo some message
            }
            RaisePropertyChanged("CSVFound");
        }
        public void LoadFiles(DependencyObject aExecUIElementToRefresh)
        {
            if (!LoadRunning)
            {
                SQLEvents.Clear();
                SQLEventsLoadOK.Clear();
                SQLEventsLoadFailed.Clear();
                SQLExecuted.Clear();
                SQLExecutionOK.Clear();
                SQLExecutionFailed.Clear();
                LoadErrorCount = 0;
                FirstExecutedIndex = -1;
                LastExecutedIndex = -1;
                try
                {
                    LoadRunning = true;
                    LoadProgressValue = 0;
                    LoadProgressText = " ";
                    LoadErrorText = " ";
                    try
                    {
                        LoadProgressValue = 0;
                        for (int i = 0; i < FilePaths.Count(); i++)
                        {
                            LoadSQLEvents(i, aExecUIElementToRefresh);
                            if (!LoadRunning) break;
                        }
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show(e.Message);
                    }
                }
                finally
                {
                    try
                    {
                        LoadProgressColor = "Yellow";
                        LoadProgressText = "Sorting...";
                        Refresh(aExecUIElementToRefresh);
                        ChronologyCheck(aExecUIElementToRefresh);
                    }
                    finally
                    {
                        FirstExecutedIndex = 0;
                        LastExecutedIndex = SQLEvents.Count - 1;
                        LoadProgressColor = "Green";
                        LoadProgressText = SQLEvents.Count() + " loaded.";
                        Refresh(aExecUIElementToRefresh);
                        System.Windows.Forms.MessageBox.Show("Loaded " + SQLEvents.Count + " SQL events." + Environment.NewLine + LoadErrorCount + " errors.");
                        LoadRunning = false;
                        RaisePropertyChanged("Loaded");
                    }
                }
            }
        }
        private void ChronologyCheck(DependencyObject aExecUIElementToRefresh)
        {
            var lSorted = SQLEvents.OrderBy(x => x.Timestamp);
            ObservableCollection<SQLEvent> lTemp = new ObservableCollection<SQLEvent>(lSorted);
            SQLEvents.Clear();
            for (int i = 0; i < lTemp.Count(); i++)
            {
                lTemp[i].Index = i;
                SQLEvents.Add(lTemp[i]);
                if (lTemp[i].Loading_Result == "OK")
                {
                    SQLEventsLoadOK.Add(lTemp[i]);
                }
                else
                {
                    SQLExecutionFailed.Add(lTemp[i]);
                }
            }
        }
        private void LoadSQLEvents(int aFileIndex, DependencyObject aExecUIElementToRefresh)
        {
            var csv = new ExcelQueryFactory(FilePaths[aFileIndex]);
            var lRows = from c in csv.Worksheet<Row>() select c;
            List<Row> lListOfRows = lRows.ToList();
            if (lListOfRows.Count() > 0)
            {
                for (int i = 0; i < lListOfRows.Count(); i++)
                {
                    Row xRow = lListOfRows[i];
                    try
                    {
                        SQLEvent lSQLE = new SQLEvent(this)
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
                    }
                    catch (Exception le)
                    {
                        SQLEvent lSQLE = new SQLEvent(this)
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
                        LoadErrorCount++;
                        LoadErrorText = LoadErrorCount + " errors.";
                    }
                    if (i == Math.Floor(Convert.ToDouble(i / 100)) * 100)
                    {
                        LoadProgressValue = aFileIndex * 100 + (int)Math.Round((double)((i + 1) / lListOfRows.Count) * 100);
                        LoadProgressText = SQLEvents.Count() + " loaded.";
                        Refresh(aExecUIElementToRefresh);
                    }
                    if (!LoadRunning) break;
                }
                if (LoadRunning)
                {
                    LoadProgressValue = (aFileIndex + 1) * 100;
                    LoadProgressText = SQLEvents.Count() + " loaded.";
                    Refresh(aExecUIElementToRefresh);
                }
            }
            Refresh(aExecUIElementToRefresh);
        }



        #endregion Load

        #region Execution
        private int _FirstExecutedIndex;
        public int FirstExecutedIndex
        {
            get
            {
                return _FirstExecutedIndex;
            }
            set
            {
                SetProperty(ref _FirstExecutedIndex, value);
                RaisePropertyChanged("ExecProgressMax");
                RaisePropertyChanged("ExecCommand");
                if (SQLEvents.Count > 0)
                {
                    foreach(SQLEvent x in SQLEvents)
                    {
                        x.RefreshExecution_ExecColor();
                    }
                }
            }
        }
        private int _LastExecutedIndex;
        public int LastExecutedIndex
        {
            get
            {
                return _LastExecutedIndex;
            }
            set
            {
                SetProperty(ref _LastExecutedIndex, value);
                RaisePropertyChanged("ExecProgressMax");
                RaisePropertyChanged("ExecCommand");
                if (SQLEvents.Count > 0)
                {
                    foreach (SQLEvent x in SQLEvents)
                    {
                        x.RefreshExecution_ExecColor();
                    }
                }
            }
        }
        public string ExecCommand
        {
            get
            {
                if (ExecRunning)
                {
                    return "Stop execution!";
                }
                else
                {
                    if (Loaded)
                    {
                        return "Execute " + SQLEvents[FirstExecutedIndex].Timestamp.ToString("dd MMM HH:mm:ss") + " - " + SQLEvents[LastExecutedIndex].Timestamp.ToString("dd MMM HH:mm:ss") + ".";
                    }
                    else
                    {
                        return "Execute!";
                    }
                }
            }
        }
        private bool _ExecRunning;
        public bool ExecRunning
        {
            get
            {
                return _ExecRunning;
            }
            set
            {
                SetProperty(ref _ExecRunning, value);
                RaisePropertyChanged("ExecCommand");
            }
        }
        private string _ExecProgressText;
        public string ExecProgressText
        {
            get
            {
                return _ExecProgressText;
            }
            set
            {
                SetProperty(ref _ExecProgressText, value);
            }
        }
        private string _ExecErrorText;
        public string ExecErrorText
        {
            get
            {
                return _ExecErrorText;
            }
            set
            {
                SetProperty(ref _ExecErrorText, value);
            }
        }
        private int _ExecProgressValue;
        public int ExecProgressValue
        {
            get
            {
                return _ExecProgressValue;
            }
            set
            {
                SetProperty(ref _ExecProgressValue, value);
            }
        }
        public int ExecProgressMax
        {
            get
            {
                return _LastExecutedIndex - _FirstExecutedIndex + 1;
            }
        }
        public long ExecTimeDiffTicks { get; set; }
        public string _ExecProgressColor;
        public string ExecProgressColor
        {
            get
            {
                return _ExecProgressColor;
            }
            set
            {
                SetProperty(ref _ExecProgressColor, value);
            }
        }
        public void ExecuteSQLEvents(string aSQLConnectionString, DependencyObject aExecUIElementToRefresh)
        {
            if (!ExecRunning)
            {
                SQLExecuted.Clear();
                SQLExecutionOK.Clear();
                SQLExecutionFailed.Clear();
                try
                {
                    ExecRunning = true;
                    ExecProgressValue = 0;
                    ExecProgressColor = "Green";
                    ExecErrorText = " ";
                    ExecProgressText = " ";
                    try
                    {
                        SqlConnection lConnection = new SqlConnection(aSQLConnectionString);
                        lConnection.Open();
                        try
                        {
                            ExecTimeDiffTicks = DateTime.Now.Ticks - SQLEvents[FirstExecutedIndex].Timestamp.Ticks;
                            for (int i = FirstExecutedIndex; i <= LastExecutedIndex; i++)
                            {
                                while (SQLEvents[i].Timestamp.Ticks + ExecTimeDiffTicks > DateTime.Now.Ticks)
                                {
                                    //waiting for a right time :)
                                    ExecProgressColor = "Yellow";
                                    ExecProgressText = "Waiting till " + SQLEvents[i].Timestamp.AddTicks(ExecTimeDiffTicks).ToString() + ". " + ExecProgressValue + " executed.";
                                    Refresh(aExecUIElementToRefresh);
                                    if (!ExecRunning) break;
                                }
                                if (!ExecRunning) break;
                                ExecProgressColor = "Green";
                                if (SQLEvents[i].Batch_text != "")
                                {
                                    try
                                    {
                                        try
                                        {
                                            SqlDataReader lReader = null;
                                            try
                                            {
                                                SqlCommand lCommand = new SqlCommand(SQLEvents[i].Batch_text, lConnection);
                                                SQLEvents[i].ExecutionTimestamp = DateTime.Now;
                                                lReader = lCommand.ExecuteReader();
                                                SQLEvents[i].ExecutionTime = (int)Math.Round((double)((DateTime.Now.Ticks - SQLEvents[i].ExecutionTimestamp.Ticks) * 1000 / TimeSpan.TicksPerMillisecond), 3);
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
                                        }
                                        catch (SqlException e)
                                        {
                                            SQLEvents[i].Execution_Result = "Error";
                                            SQLEvents[i].Execution_Error = e.Message;
                                            SQLExecutionFailed.Add(SQLEvents[i]);
                                            ExecErrorText = SQLExecutionFailed.Count + " errors.";
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        if (SQLEvents[i].Execution_Result == "")
                                        {
                                            SQLEvents[i].Execution_Result = "Error";
                                            SQLEvents[i].Execution_Error = e.Message;
                                            SQLExecutionFailed.Add(SQLEvents[i]);
                                            ExecErrorText = SQLExecutionFailed.Count + " errors.";
                                        }
                                    }
                                    SQLExecuted.Add(SQLEvents[i]);
                                }
                                ExecProgressValue = i - FirstExecutedIndex + 1;
                                ExecProgressText = ExecProgressValue + " processed.";
                                Refresh(aExecUIElementToRefresh);
                                if (!ExecRunning) break;
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
                    System.Windows.Forms.MessageBox.Show("Executed " + ExecProgressValue + " SQL batches." + Environment.NewLine + SQLExecutionFailed.Count + " errors.");
                    ExecRunning = false;
                    ExecProgressColor = "Green";
                }
            }
        }
        #endregion Execution

        #region Export
        public string ExportCommand
        {
            get
            {
                if (ExportRunning)
                {
                    return "Stop exporting!";
                }
                else
                {
                    return "Export " + GetSelectedCollectionName + " (" + GetSelectedCollection.Count + " records).";
                }
            }
        }
        private bool _ExportRunning;
        public bool ExportRunning
        {
            get
            {
                return _ExportRunning;
            }
            set
            {
                SetProperty(ref _ExportRunning, value);
                RaisePropertyChanged("ExportCommand");
            }
        }
        private string _ExportProgressText;
        public string ExportProgressText
        {
            get
            {
                return _ExportProgressText;
            }
            set
            {
                SetProperty(ref _ExportProgressText, value);
            }
        }
        private string _ExportErrorText;
        public string ExportErrorText
        {
            get
            {
                return _ExportErrorText;
            }
            set
            {
                SetProperty(ref _ExportErrorText, value);
            }
        }
        private int _ExportProgressValue;
        public int ExportProgressValue
        {
            get
            {
                return _ExportProgressValue;
            }
            set
            {
                SetProperty(ref _ExportProgressValue, value);
            }
        }
        public int ExportProgressMax
        {
            get
            {
                if (GetSelectedCollection.Count == 0)
                {
                    return 1;
                }
                else
                {
                    return GetSelectedCollection.Count;
                }
            }
        }

        public void ExportEvents(string aFileExportPath, DependencyObject aUIElementToRefresh)
        {
            if (!ExportRunning)
            {
                try
                {
                    ExportRunning = true;
                    ExportProgressValue = 0;
                    ExportErrorText = " ";
                    ExportProgressText = " ";
                    Refresh(aUIElementToRefresh);

                    CsvExport<SQLEvent> csv = new CsvExport<SQLEvent>(GetSelectedCollection.ToList<SQLEvent>(), this, aUIElementToRefresh);
                    try
                    {
                        string lFileName = String.Format(@"{0}\Export\SQLEventsExeExport_{1}_{2}{{0}}.csv",
                            aFileExportPath,
                            GetSelectedCollectionName.Trim(),
                            DateTime.Now.ToString("yyyy_MM_dd_HH_mm_ss")
                            );
                        lFileName = csv.ExportToFile(lFileName);
                        Refresh(aUIElementToRefresh);
                        System.Windows.Forms.MessageBox.Show("Exported " + ExportProgressValue + " records to " + lFileName);
                    }
                    catch (Exception e)
                    {
                        System.Windows.Forms.MessageBox.Show(e.Message);
                    }
                }
                finally
                {
                    ExportRunning = false;
                }
            }
        }
        #endregion Export

        public Simulator()
        {
            ExportRunning = false;
            ExportProgressText = " ";
            ExportErrorText = " ";
            ExportProgressValue = 0;
            ExecRunning = false;
            ExecProgressText = " ";
            ExecErrorText = " ";
            ExecProgressValue = 0;
            LoadRunning = false;
            LoadProgressText = " ";
            LoadErrorText = " ";
            LoadProgressValue = 0;
            SQLEvents = new ObservableCollection<SQLEvent>();
            SQLEventsLoadOK = new ObservableCollection<SQLEvent>();
            SQLEventsLoadFailed = new ObservableCollection<SQLEvent>();
            SQLExecuted = new ObservableCollection<SQLEvent>();
            SQLExecutionOK = new ObservableCollection<SQLEvent>();
            SQLExecutionFailed = new ObservableCollection<SQLEvent>();
            SQLCollections.Add(0, SQLEvents);
            SQLCollections.Add(1, SQLEventsLoadOK);
            SQLCollections.Add(2, SQLEventsLoadFailed);
            SQLCollections.Add(3, SQLExecuted);
            SQLCollections.Add(4, SQLExecutionOK);
            SQLCollections.Add(5, SQLExecutionFailed);
            FirstExecutedIndex = -1;
            LastExecutedIndex = -1;
        }




        public void Refresh(DependencyObject obj)
        {
            obj.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
                (NoArgDelegate)delegate { });
        }
    }
}
