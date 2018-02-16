using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SQLEventsExecutor
{
    public class SQLEvent : NotificationBase
    {
        private Simulator _SimInstance { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime Timestamputc { get; set; }
        public int Cpu_time { get; set; }
        public int Duration { get; set; } //microseconds - only SQL
        public int Physical_reads { get; set; }
        public int Logical_reads { get; set; }
        public int Writes { get; set; }
        public int Row_count { get; set; }
        public string Batch_text { get; set; }
        public string Database_name { get; set; }
        public string Event_Result { get; set; }
        public string Loading_Result { get; set; }
        public string Loading_Error { get; set; }
        private DateTime _ExecutionTimestamp;
        public DateTime ExecutionTimestamp
        {
            get
            {
                return _ExecutionTimestamp;
            }
            set
            {
                SetProperty(ref _ExecutionTimestamp, value);
                RaisePropertyChanged("ExecutionTimestampString");
            }
        }
        public string ExecutionTimestampString
        {
            get
            {
                if (DateTime.Compare(_ExecutionTimestamp, DateTime.MinValue) == 0)
                {
                    return "";
                }
                else
                {
                    return _ExecutionTimestamp.ToString();
                }
            }
        }
        private string _Execution_Result;
        public string Execution_Result
        {
            get
            {
                return _Execution_Result;
            }
            set
            {
                SetProperty(ref _Execution_Result, value);
                RaisePropertyChanged("Background");
            }
        }
        public string Execution_Error { get; set; }
        public string Execution_Dataset { get; set; }
        public SQLEvent(Simulator aSimulatorInstance)
        {
            _SimInstance = aSimulatorInstance;
        }
        public string Execution_Output
        {
            get
            {
                if (Execution_Result == "") //not executed yet
                {
                    if (Loading_Result == "OK") //loading OK
                    {
                        return "";
                    }
                    else //loading error
                    {
                        return Loading_Error;
                    }
                }
                else //already executed
                {
                    if (Execution_Result == "OK") //execution OK
                    {
                        return Execution_Dataset;
                    }
                    else //execution error
                    {
                        return Execution_Error;
                    }
                }
            }
        }
        public string Execution_OutputColor
        {
            get
            {
                if (Execution_Result == "") //not executed yet
                {
                    if (Loading_Result == "OK") //loading OK
                    {
                        return "DarkGreen";
                    }
                    else //loading error
                    {
                        return "Red";
                    }
                }
                else //already executed
                {
                    if (Execution_Result == "OK") //execution OK
                    {
                        return "DarkGreen";
                    }
                    else //execution error
                    {
                        return "Red";
                    }
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
        public string Execution_ExecColor
        {
            get
            {
                if ((Index < _SimInstance.FirstExecutedIndex) || (Index > _SimInstance.LastExecutedIndex))
                {
                    return "White";
                }
                else
                {
                    if (Index == _SimInstance.FirstExecutedIndex)
                    {
                        return "Green";
                    }
                    else
                    {
                        if (Index == _SimInstance.LastExecutedIndex)
                        {
                            return "Red";
                        }
                        else
                        {
                            return "LightBlue";
                        }
                    }
                }
            }
        }
        private int _ExecutionTime; //microseconds - Internet + SQL
        public int ExecutionTime
        {
            get
            {
                return _ExecutionTime;
            }
            set
            {
                SetProperty(ref _ExecutionTime, value);
                RaisePropertyChanged("ExecutionTimeString");
            }
        }
        public string ExecutionTimeString
        {
            get
            {
                if (_ExecutionTime == 0)
                {
                    return "";
                }
                else
                {
                    return _ExecutionTime.ToString();
                }
            }
        }

        public void RefreshExecution_ExecColor()
        {
            RaisePropertyChanged("Execution_ExecColor");
        }

    }
}
