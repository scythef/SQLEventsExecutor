using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;

namespace SQLEventsExecutor
{
    //thanks to https://stackoverflow.com/questions/2422212/how-to-create-csv-excel-file-c
    public class CsvExport<T> where T : class
    {
        public List<T> Objects;
        public Simulator Sim;
        public DependencyObject UIElement;

        public CsvExport(List<T> objects, Simulator aSimInstance, DependencyObject aExecUIElementToRefresh)
        {
            Objects = objects;
            Sim = aSimInstance;
            UIElement = aExecUIElementToRefresh;
        }

        public string Export()
        {
            return Export(true);
        }

        public string Export(bool includeHeaderLine)
        {

            StringBuilder sb = new StringBuilder();
            //Get properties using reflection.
            IList<PropertyInfo> propertyInfos = typeof(T).GetProperties();

            if (includeHeaderLine)
            {
                //add header line.
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    if ((propertyInfo.Name != "Execution_Output") && (propertyInfo.Name != "Execution_Dataset")) //skip due to overall size of the file to later on import to the Excel
                    sb.Append(propertyInfo.Name).Append(",");
                }
                sb.Remove(sb.Length - 1, 1).AppendLine();
            }

            //add value for each property.
            for (int i = 0; i < Objects.Count; i++)
            {
                T obj = Objects[i];
                foreach (PropertyInfo propertyInfo in propertyInfos)
                {
                    if ((propertyInfo.Name != "Execution_Output") && (propertyInfo.Name != "Execution_Dataset")) //skip due to overall size of the file to later on import to the Excel
                    sb.Append(MakeValueCsvFriendly(propertyInfo.GetValue(obj, null))).Append(",");
                }
                sb.Remove(sb.Length - 1, 1).AppendLine();
                Sim.ExportProgressValue = i + 1;
                Sim.ExportProgressText = (i + 1) + " exported.";
                if (i == Math.Floor(Convert.ToDouble(i / 100)) * 100)
                {
                    Sim.Refresh(UIElement);
                }
                if (!Sim.ExportRunning) break;
            }

            return sb.ToString();
        }

        //export to a file.
        public string ExportToFile(string path)
        {
            string lExport = Export();
            string lExportFile = String.Format(path, Sim.ExportRunning ? "" : "_incomplete_export_interruped_by_user");
            File.WriteAllText(lExportFile, lExport);
            return lExportFile;
        }

        //export as binary data.
        public byte[] ExportToBytes()
        {
            return Encoding.UTF8.GetBytes(Export());
        }

        //get the csv value for field.
        private string MakeValueCsvFriendly(object value)
        {
            if (value == null) return "";
            if (value is Nullable && ((INullable)value).IsNull) return "";

            if (value is DateTime)
            {
                if (((DateTime)value).TimeOfDay.TotalSeconds == 0)
                    return ((DateTime)value).ToString("yyyy-MM-dd");
                return ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");
            }
            string output = value.ToString();

            if (output.Contains(Environment.NewLine))
                output = output.Replace(Environment.NewLine, "; ");

            if (output.Contains(",") || output.Contains("\""))
                output = '"' + output.Replace("\"", "\"\"") + '"';

            return output;

        }
    }
}
