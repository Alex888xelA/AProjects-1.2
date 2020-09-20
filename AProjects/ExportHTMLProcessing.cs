using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace AProjects
{
    class ExportHTMLProcessing
    {
        private Dictionary<String, Boolean> exportSettings;
        private Model model;
        private readonly String fileName;
        private List<Record> recordList = new List<Record>(); //Коллекция строк до фильтрации по опциям экспорта

        public ExportHTMLProcessing(Dictionary<String, Boolean> exportSettings, Model model, String fileName)
        {
            this.exportSettings = exportSettings;
            this.model = model;
            this.fileName = fileName;
            //MessageBox.Show("ExportHTMLProcessing");
        }

        /* Экспортировать выделенные записи
         * Экспортировать активные Проекты/Работы/Дела (*) = _recordStatus
         * Экспортировать свернутые записи (*) = _hide
         * Экспортировать завершенные Проекты/Работы/Дела (*) = _recordStatus
         * Экспортировать архивные проекты (*) = _recordStatus
         * 
         * Сохранить цветовую разметку записей
         * Экспортировать примечания
         * Экспортировать сигналы
         */

        /// <summary>
        /// Экспорт выделенных строк
        /// </summary>
        /// <param name="vrNumbers">Список номеров выделенных строк</param>
        public void SelectionExport(List<String> vrNumbers)
        {
            foreach (String recordNumber in vrNumbers)
            {
                recordList.Add(model.GetSingleRecord(recordNumber));
            }
            FormatRecords();
        }

        /// <summary>
        /// Экспорт записей (за исключением случая экспорта выделенных строк)
        /// </summary>
        public void Export()
        {
            List<Record> modelRecords = new List<Record>();
            model.GetModelData(ref modelRecords);
            foreach (Record record in modelRecords)
            {
                if (exportSettings["ExportCollapsedRecords"] == false && record.Hide == true) //В экспорт не включаются свернутые записи
                    continue;
                if (exportSettings["ExportActiveRecords"] == true && record.RecordStatus == RecordStatus.active) //В экспорт включили записи со статусом Активные
                {
                    recordList.Add(record);
                    continue;
                }
                if (exportSettings["ExportFinishedRecords"] == true && record.RecordStatus == RecordStatus.finished)
                {
                    recordList.Add(record);
                    continue;
                }
                if (exportSettings["ExportArhRecords"] == true && record.RecordStatus == RecordStatus.arx)
                {
                    recordList.Add(record);
                }
            }
            FormatRecords();
        }

        /// <summary>
        /// Формирование списка (List) строк и запись их в файл
        /// </summary>
        private void FormatRecords()
        {
            List<String> output = new List<string>();
            output.Add("<!DOCTYPE html>");
            output.Add("<html>");
            output.Add("<head>");
            output.Add("<meta http-equiv=\"Content-Type\" content=\"text/html; charset =\"windows-1251\">");
            output.Add("<title>AProjects</title>");
            output.Add("</head>");
            output.Add("<body>");

            output.Add("<style type=\"text/css\">");
            output.Add("TABLE {border-collapse: collapse; border: 2px solid white;}");
            output.Add("TD, TH {padding: 3px; border: 1px solid black;}");
            output.Add("</style>");

            output.Add("<table>");
            output.Add("<tr>");
            output.Add("<th>Номер</th>");
            output.Add("<th>Дата</th>");
            output.Add("<th>Содержание</th>");
            output.Add("</tr>");

            foreach (Record record in recordList)
            {
                String s = "";
                switch (record.RecordType)
                {
                    case RecordType.project:
                        s = "<tr style=\"font-size: 18px; font-weight: bold; color:white; background-color:DimGray;\">";
                        break;
                    case RecordType.job:
                        s = "<tr style=\"font-size: 16px; font-weight: bold; color:black;";
                        break;
                    case RecordType.act:
                        s = "<tr style=\"font-size: 14px; font-weight: normal; color:black;";
                        break;
                }

                if (exportSettings["ExportColors"] && record.RecordType != RecordType.project && record.RecordStatus != RecordStatus.finished)
                {
                    switch (record.Fill)
                    {
                        case 0:
                            s += " background-color:white;\">";
                            break;
                        case 1:
                            s += " background-color:red;\">";
                            break;
                        case 2:
                            s += " background-color:yellow;\">";
                            break;
                        case 3:
                            s += " background-color:green;\">";
                            break;
                        case 4:
                            s += " background-color:DeepSkyBlue;\">";
                            break;
                        case 5:
                            s += " background-color:Magenta;\">";
                            break;
                    }
                } else if (!exportSettings["ExportColors"] && record.RecordType != RecordType.project && record.RecordStatus != RecordStatus.finished)
                {
                    s += " background-color:white;\">";
                }
                if (record.RecordStatus == RecordStatus.finished && record.RecordType != RecordType.project)
                {
                    s += " background-color:LightGray;\">";
                }

                output.Add(s);
                output.Add("<td>" + record.ProjNumber.ToString() + "." + record.JobNumber.ToString() + "." + record.ActNumber.ToString() + "</td>");
                output.Add("<td>" + record.RecordDate + "</td>");
                output.Add("<td>" + record.Content + "</td></tr>");

                if ((exportSettings["ExportAlarms"] && record.AlarmTime != "") || (exportSettings["ExportNotes"] && record.Note != "")) //Добавляем вторую строку для выода будильников и примечаний
                {
                    output.Add("<tr>");
                    output.Add("<td colspan=\"2\">");
                    if (exportSettings["ExportAlarms"])
                    {
                        output.Add(record.AlarmTime);
                    }
                    output.Add("</td><td>");
                    if (exportSettings["ExportNotes"])
                    {
                        output.Add(record.Note);
                    }
                    output.Add("</td></tr>");
                }
            }
            output.Add("</table></body></html>");
            try
            {
                StreamWriter sw = new StreamWriter(@fileName, false);
                foreach (String s in output)
                {
                    sw.WriteLine(s);
                }
                sw.Close();
            }
            finally
            {
                output = null;
            }
        }
    }
}
