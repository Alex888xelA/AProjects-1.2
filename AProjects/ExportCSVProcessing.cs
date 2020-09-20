using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AProjects
{
    class ExportCSVProcessing
    {
        private Dictionary<String, Boolean> exportSettings;
        private Model model;
        private readonly String fileName;
        private List<Record> recordList = new List<Record>(); //Коллекция строк до фильтрации по опциям экспорта

        public ExportCSVProcessing(Dictionary<String, Boolean> exportSettings, Model model, String fileName)
        {
            this.exportSettings = exportSettings;
            this.model = model;
            this.fileName = fileName;
        }

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
        /// Формирование списка (List) строк и запись их в файл
        /// </summary>
        private void FormatRecords()
        {
            List<StringBuilder> output = new List<StringBuilder>();
            Boolean flagFinished = exportSettings["ExportFinishedRecords"];
            Boolean flagArx = exportSettings["ExportActiveRecords"] && exportSettings["ExportArhRecords"];
            Boolean flagColor = exportSettings["ExportColors"];
            Boolean flagNotes = exportSettings["ExportNotes"];
            Boolean flagSignal = exportSettings["ExportAlarms"];
            String tmpString;
            StringBuilder s = new StringBuilder();
            //TODO: Добавить заголовки столбцов в первой строке

            foreach (Record record in recordList)
            {
                s.Clear();
                s.Append(record.ProjNumber.ToString() + "." + record.JobNumber.ToString() + "." + record.ActNumber + ";");
                s.Append(record.RecordDate + ";");
                tmpString = record.Content;
                tmpString.Replace(";", "\\;");
                s.Append(tmpString + ";");
                if (flagFinished) //Выводим признак завершения записи
                {
                    if (record.RecordStatus == RecordStatus.finished)
                        s.Append("+;");
                    else
                        s.Append(";");
                }
                if (flagArx) //Выводим признак "Архивный"
                {
                    if (record.RecordStatus == RecordStatus.arx)
                    {
                        s.Append("+;");
                    }
                    else
                    {
                        s.Append(";");
                    }
                }
                if (flagColor) //Выводим индекс цвета заливки фона
                {
                    s.Append(record.Fill.ToString() + ";");
                }
                if (flagNotes) //Выводим примечания
                {
                    tmpString = record.Note;
                    tmpString.Replace(";", "\\;");
                    s.Append(tmpString + ";");
                }
                if(flagSignal)
                {
                    tmpString = record.AlarmTime;
                    tmpString.Replace(";", "\\;");
                    s.Append(tmpString + ";");
                }
                output.Add(s);
            }
            try
            {
                StreamWriter sw = new StreamWriter(@fileName, false);
                foreach (StringBuilder sOut in output)
                {
                    sw.WriteLine(sOut);
                }
                sw.Close();
            }
            finally
            {
                output = null;
            }
        }
        //TODO: Реализовать методы класса ExportCSVProcessing, экранирование точки с запятой
    }
}
