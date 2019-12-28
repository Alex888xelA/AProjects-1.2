using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;

namespace AProjects
{
    class Model
    {
        //private List<Record> records = new List<Record>();
        private List<Record> modelRecords;
        //public SpecialObservableCollection<ViewRecord> viewRecords;
        private Stack<UndoAction> undoActions;
        private Stack<UndoAction> redoActions;
        private ViewRecord lastRowAdded = null; //Последняя добавленная в модель запись (для использования в ViewModel)
        private const String ApplicationVersion = "AProject"; //Тип приложения, для проверки совместимости формата файла данных
        private const String FileFormatVersion = "v1"; //Версия формата файла данных
        private Queue<Record> clipboard; //Стэк для хранения скопированных или вырезаных записей

        public Model()
        {
            modelRecords = new List<Record>(); //Модель
            //viewRecords = new SpecialObservableCollection<ViewRecord>(); //Выборка из модели для отображения
            undoActions = new Stack<UndoAction>(); //Список действий для команды Undo
            redoActions = new Stack<UndoAction>(); //Список действий для отмены Undo
            lastRowAdded = new ViewRecord();
            clipboard = new Queue<Record>();
        }

        public void GenerateModelTemplatet()
        {
            modelRecords.Add(new Record()
            {
                ProjNumber = (Int32)1,
                JobNumber = (Int32)0,
                ActNumber = (Int32)0,
                RecordDate = DateTime.Today.ToString("dd.MM.yyyy"),
                Content = (String)"Первый проект",
                AlarmTime = (String)"",
                AlarmRepit = AlarmRepit.no,
                Note = (String)"",
                RecordType = RecordType.project,
                RecordStatus = RecordStatus.active,
                Fill = (Int32)0,
                Hide = (Boolean)false
            });

            modelRecords.Add(new Record()
            {
                ProjNumber = (Int32)1,
                JobNumber = (Int32)1,
                ActNumber = (Int32)0,
                RecordDate = DateTime.Today.ToString("dd.MM.yyyy"),
                Content = (String)"Первая работа",
                AlarmTime = (String)"",
                AlarmRepit = AlarmRepit.no,
                Note = (String)"",
                RecordType = RecordType.job,
                RecordStatus = RecordStatus.active,
                Fill = (Int32)0,
                Hide = (Boolean)false
            });

            modelRecords.Add(new Record()
            {

                ProjNumber = (Int32)1,
                JobNumber = (Int32)1,
                ActNumber = (Int32)1,
                RecordDate = DateTime.Today.ToString("dd.MM.yyyy"),
                Content = (String)"Первое дело",
                AlarmTime = (String)"",
                AlarmRepit = AlarmRepit.year,
                Note = (String)"",
                RecordType = RecordType.act,
                RecordStatus = RecordStatus.active,
                Fill = (Int32)0,
                Hide = (Boolean)false
            });
            redoActions.Clear();
        }
        /*
         * private Int32 _projNumber; //Номер проекта
         * private Int32 _jobNumber; //Номер работы
         * private Int32 _actNumber; //Номер дела
         * private String _recordDate; //Дата записи
         * private String _content; //Запись
         * private String _alarmTime; //Дата и время срабатывания будильника
         * private AlarmRepit _alarmRepit; //Признак повтора будильника
         * private String _note; //Примечание
         * private RecordType _recordType; //Тип записи - проект/работа/дело
         * private RecordStatus _recordStatus; //Статус записи - активный/завершен/архив
         * private Int32 _fill; //Индекс заливки
         * private Boolean _hide; //Признак: False - показать, True - скрыть
         */

        public void GetModelData(ref List<Record> viewModelRecords)
        {
            viewModelRecords.Clear();
            foreach (Record element in modelRecords)
            {
                viewModelRecords.Add(element);
            }
            return;
        }

        /// <summary>
        /// Изменение полей в существующей записи Record
        /// </summary>
        /// <param name="viewRecord">ViewRecord передается из ViewModel</param>
        public void UpdateModelRecord(ViewRecord viewRecord)
        {
            Int32 index;
            Record updatedRecord;
            //Поиск строки по номеру Проект/Работа/Дело
            String[] number = viewRecord.Number.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);
            Int32 job = Int32.Parse(number[1]);
            Int32 act = Int32.Parse(number[2]);

            index = IndexOfNumber(proj, job, act);
            if (index < 0)
            {
                MessageBox.Show("Не найдена строка для обновления в modelRecords");
                return;
            }

            updatedRecord = modelRecords[index];
            Record undoRecord = updatedRecord.Clone();
            UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
            undoActions.Push(undoAction);
            redoActions.Clear();

            updatedRecord.RecordDate = viewRecord.RecordDate;
            updatedRecord.Content = viewRecord.Content;
            updatedRecord.AlarmTime = viewRecord.AlarmTime;
            updatedRecord.Note = viewRecord.Note;
            //MessageBox.Show("Model Updated");
        }

        /// <summary>
        /// Добавление новой записи типа Проект
        /// </summary>
        public void CreateProject()
        {
            Record r = new Record();

            r.ProjNumber = MaxNumber() + 1;
            r.JobNumber = 0;
            r.ActNumber = 0;
            r.RecordDate = DateTime.Today.ToString("dd.MM.yyyy");
            r.Content = "Новый проект";
            r.AlarmTime = "";
            r.AlarmRepit = AlarmRepit.no;
            r.Note = "";
            r.RecordType = RecordType.project;
            r.RecordStatus = RecordStatus.active;
            r.Fill = 0;
            r.Hide = false;
            modelRecords.Add(r);
            LastRowUpdate(r); //Запись созданной записи в свойство (для установки выделения)
            //Запись действия для отката
            UndoAction undoAction = new UndoAction(ActionType.addRecord, r);
            undoActions.Push(undoAction);
            redoActions.Clear();
            modelRecords.Sort();
        }

        public void CreateJob(String selectedRowNumber)
        {
            //Поиск строки по номеру Проект/Работа/Дело
            String[] number = selectedRowNumber.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);

            Int32 maxJobNumber = MaxNumber(proj);
            Record r = new Record
            {
                ProjNumber = proj,
                JobNumber = maxJobNumber + 1,
                ActNumber = 0,
                RecordDate = DateTime.Today.ToString("dd.MM.yyyy"),
                Content = "Новая работа",
                AlarmTime = "",
                AlarmRepit = AlarmRepit.no,
                Note = "",
                RecordType = RecordType.job,
                RecordStatus = RecordStatus.active,
                Fill = 0,
                Hide = false
            };
            modelRecords.Add(r);
            LastRowUpdate(r); //Запись номера созданной записи в свойство (для установки выделения)
            //Запись действия для отката
            UndoAction undoAction = new UndoAction(ActionType.addRecord, r);
            undoActions.Push(undoAction);
            redoActions.Clear();
            modelRecords.Sort();
        }

        public void CreateAct(String selectedRowNumber)
        {
            //Поиск строки по номеру Проект/Работа/Дело
            String[] number = selectedRowNumber.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);
            Int32 job = Int32.Parse(number[1]);

            Int32 maxActNumber = MaxNumber(proj, job);
            Record r = new Record
            {
                ProjNumber = proj,
                JobNumber = job,
                ActNumber = maxActNumber + 1,
                RecordDate = DateTime.Today.ToString("dd.MM.yyyy"),
                Content = "Новое дело",
                AlarmTime = "",
                AlarmRepit = AlarmRepit.no,
                Note = "",
                RecordType = RecordType.act,
                RecordStatus = RecordStatus.active,
                Fill = 0,
                Hide = false
            };
            modelRecords.Add(r);
            LastRowUpdate(r); //Запись номера созданной записи в свойство (для установки выделения)
            //Запись действия для отката
            UndoAction undoAction = new UndoAction(ActionType.addRecord, r);
            undoActions.Push(undoAction);
            redoActions.Clear();
            modelRecords.Sort();
        }

        //Завершить проект/Дело/Работу
        public void Finalize(String selectedRowNumber)
        {
            String[] number = selectedRowNumber.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);
            Int32 job = Int32.Parse(number[1]);
            Int32 act = Int32.Parse(number[2]);
            Int32 index;

            if (act > 0) //Завершить дело
            {
                index = IndexOfNumber(proj, job, act);

                Record undoRecord = modelRecords[index].Clone();
                UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
                undoActions.Push(undoAction);

                modelRecords[index].RecordStatus = RecordStatus.finished;
            }
            else if (job > 0) //Завершить работу
            {
                List<Int32> indexes = IndexOfNumberList(proj, job);
                if (indexes.Count > 0)
                {
                    foreach (Int32 j in indexes)
                    {
                        Record undoRecord = modelRecords[j].Clone();
                        UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
                        undoActions.Push(undoAction);

                        modelRecords[j].RecordStatus = RecordStatus.finished;
                    }
                }
            }
            else //Завершить проект
            {
                List<Int32> indexes = IndexOfNumberList(proj);
                if (indexes.Count > 0)
                {
                    foreach (Int32 j in indexes)
                    {
                        Record undoRecord = modelRecords[j].Clone();
                        UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
                        undoActions.Push(undoAction);
                        modelRecords[j].RecordStatus = RecordStatus.finished;
                    }
                }
            }
            redoActions.Clear();
        }

        //Удалить запись или группу записей (все записи Дела/Проекта)
        public void DelRecord(String selectedRowNumber)
        {
            List<Int32> indexList = new List<int>();
            List<Record> deletedRecords = new List<Record>();
            String[] number = selectedRowNumber.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);
            Int32 job = Int32.Parse(number[1]);
            Int32 act = Int32.Parse(number[2]);
            Int32 index;
            index = IndexOfNumber(proj, job, act);
            Record delatedRecord = modelRecords[index];
            //
            switch (delatedRecord.RecordType)
            {
                case RecordType.act:
                    indexList.Add(index);
                    break;
                case RecordType.job:
                    indexList = IndexOfNumberList(proj, job);
                    break;
                case RecordType.project:
                    indexList = IndexOfNumberList(proj);
                    break;
            }
            foreach (Int32 j in indexList)
            {
                deletedRecords.Add(modelRecords[j]);
            }
            foreach (Record r in deletedRecords)
            {
                Record undoRecord = r.Clone();
                UndoAction undoAction = new UndoAction(ActionType.delRecord, undoRecord);
                undoActions.Push(undoAction);
                redoActions.Clear();
                modelRecords.Remove(r);
            }
        }

        //Снять признак завершения проекта/Дела/Работы
        public void UnFinalize(String selectedRowNumber)
        {
            Int32 index;
            Int32[] numbers = GetNumbers(selectedRowNumber);
            index = IndexOfNumber(numbers[0], numbers[1], numbers[2]);
            Record undoRecord = modelRecords[index].Clone();
            UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
            undoActions.Push(undoAction);
            redoActions.Clear();
            modelRecords[index].RecordStatus = RecordStatus.active;
        }

        //Установить цвет записи (Установка поля Fill)
        public void SetColor(String selectedRowNumber, Int32 colorIndex)
        {
            Int32 index;
            Int32[] numbers = GetNumbers(selectedRowNumber);
            index = IndexOfNumber(numbers[0], numbers[1], numbers[2]);
            Record undoRecord = modelRecords[index].Clone();
            UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
            undoActions.Push(undoAction);
            redoActions.Clear();
            modelRecords[index].Fill = colorIndex;
        }

        //Отмена предыдущей операции
        public void Undo()
        {
            Int32 i;
            UndoAction undoAction = undoActions.Pop();
            UndoAction redoAction = new UndoAction();
            switch (undoAction.UndoActionType)
            {
                case ActionType.addRecord: //Отмена добавления записи
                    i = IndexOfNumber(undoAction.RecordData.ProjNumber, undoAction.RecordData.JobNumber, undoAction.RecordData.ActNumber);
                    redoAction.UndoActionType = ActionType.delRecord;
                    redoAction.RecordData = modelRecords[i];
                    redoActions.Push(redoAction);
                    modelRecords.RemoveAt(i);

                    break;
                case ActionType.delRecord: //Отмена удаления записи
                    redoAction.UndoActionType = ActionType.addRecord;
                    redoAction.RecordData = undoAction.RecordData;
                    redoActions.Push(redoAction);
                    modelRecords.Add(undoAction.RecordData);
                    modelRecords.Sort();
                    break;
                case ActionType.editRecord: //Отмена редактировании записи
                    i = IndexOfNumber(undoAction.RecordData.ProjNumber, undoAction.RecordData.JobNumber, undoAction.RecordData.ActNumber);
                    Debug.Assert(i > 0);
                    //
                    redoAction.UndoActionType = ActionType.editRecord;
                    redoAction.RecordData = modelRecords[i];
                    redoActions.Push(redoAction);
                    //
                    modelRecords[i] = undoAction.RecordData;

                    break;
                default:
                    Debug.Assert(false, "Ошибка ActionType в операции Undo");
                    break;
            }
        }

        public Boolean UndoCanExecute()
        {
            Boolean result = false;
            if (undoActions.Count > 0)
                result = true;
            return result;
        }

        //Отмена отмены предыдущей операции
        public void Redo()
        {
            Int32 i;
            UndoAction redoAction = redoActions.Pop();
            UndoAction undoAction = new UndoAction();
            switch (redoAction.UndoActionType)
            {
                case ActionType.addRecord: //Отмена добавления записи
                    undoAction.UndoActionType = ActionType.delRecord;
                    i = IndexOfNumber(redoAction.RecordData.ProjNumber, redoAction.RecordData.JobNumber, redoAction.RecordData.ActNumber);
                    undoAction.RecordData = modelRecords[i];
                    undoActions.Push(undoAction);
                    modelRecords.RemoveAt(i);
                    break;

                case ActionType.delRecord: //Отмена удаления записи
                    undoAction.UndoActionType = ActionType.addRecord;
                    undoAction.RecordData = redoAction.RecordData;
                    modelRecords.Add(redoAction.RecordData);
                    undoActions.Push(undoAction);
                    modelRecords.Sort();
                    break;

                case ActionType.editRecord: //Отмена редактировании записи

                    i = IndexOfNumber(redoAction.RecordData.ProjNumber, redoAction.RecordData.JobNumber, redoAction.RecordData.ActNumber);
                    Debug.Assert(i > 0);
                    undoAction.UndoActionType = ActionType.editRecord;
                    undoAction.RecordData = modelRecords[i];
                    undoActions.Push(undoAction);
                    modelRecords[i] = redoAction.RecordData;
                    break;
                default:
                    Debug.Assert(false, "Ошибка ActionType в операции Redo");
                    break;
            }
        }

        public Boolean RedoCanExecute()
        {
            Boolean result = false;
            if (redoActions.Count > 0)
                result = true;
            return result;
        }

        public void SaveToFile(String fileName)
        {
            IOData forSerial = new IOData();
            foreach (Record element in modelRecords)
            {
                forSerial.data.Add(element);
            }
            String parsed = JsonConvert.SerializeObject(forSerial); //+++++++++++++++++++++++++++++++++++++++++++++
            try
            {
                StreamWriter sw = new StreamWriter(@fileName, false);
                sw.WriteLine(ApplicationVersion);
                sw.WriteLine(FileFormatVersion);
                sw.WriteLine(parsed);
                sw.Close();
            }
            catch
            {
                throw;
            }

            //System.IO.FileInfo fileInfo = new System.IO.FileInfo(@"C:\file.txt");
        }

        public void ReadFromFile(String fileName)
        {
            String line;
            IOData readingData = new IOData();
            modelRecords.Clear();
            try
            {
                StreamReader sr = new StreamReader(@fileName);
                line = sr.ReadLine();
                if (line != ApplicationVersion)
                    throw new Exception("Файл создан другим приложением! Выберите файл данных приложения AProject.");
                line = sr.ReadLine();
                if (line != FileFormatVersion)
                    throw new Exception("Файл создан другим приложением! Выберите файл данных приложения AProject.");
                line = sr.ReadLine();
            }
            catch
            {
                throw;
            }
            readingData = (IOData)JsonConvert.DeserializeObject< IOData>(line);
            modelRecords = readingData.data;
        }

        public void Clear()
        {
            modelRecords.Clear();
        }

        public void Arhivate(String selectedRowNumber)
        {
            String[] number = selectedRowNumber.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);

            List<Int32> indexes = IndexOfNumberList(proj);
            if (indexes.Count > 0)
            {
                foreach (Int32 j in indexes)
                {
                    Record undoRecord = modelRecords[j].Clone();
                    UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
                    undoActions.Push(undoAction);
                    redoActions.Clear();
                    modelRecords[j].RecordStatus = RecordStatus.arx;
                }
            }
        }

        public void UnArhivate(String selectedRowNumber)
        {
            String[] number = selectedRowNumber.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);

            List<Int32> indexes = IndexOfNumberList(proj);
            if (indexes.Count > 0)
            {
                foreach (Int32 j in indexes)
                {
                    Record undoRecord = modelRecords[j].Clone();
                    UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
                    undoActions.Push(undoAction);
                    redoActions.Clear();
                    modelRecords[j].RecordStatus = RecordStatus.active;
                }
            }
        }

        public void HideRecord(String selectedRowNumber)
        {
            Int32 index;
            List<Int32> indexes = new List<int>(3);
            Int32[] numbers = GetNumbers(selectedRowNumber);
            index = IndexOfNumber(numbers[0], numbers[1], numbers[2]);
            switch (modelRecords[index].RecordType)
            {
                case RecordType.project:
                    indexes = IndexOfNumberList(numbers[0]);
                    break;
                case RecordType.job:
                    indexes = IndexOfNumberList(numbers[0], numbers[1]);
                    break;
                case RecordType.act:
                    indexes.Add(index);
                    break;
            }
            foreach (Int32 i in indexes)
            {
                if (modelRecords[index].RecordType == RecordType.project && modelRecords[i].RecordType == RecordType.project)
                    continue;
                else if (modelRecords[index].RecordType == RecordType.job && modelRecords[i].RecordType == RecordType.job)
                    continue;

                Record undoRecord = modelRecords[i].Clone();
                UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
                undoActions.Push(undoAction);
                redoActions.Clear();
                modelRecords[i].Hide = true;
            }
        }

        public void UnHideRecord(String selectedRowNumber)
        {
            Int32 index;
            List<Int32> indexes = new List<int>(3);
            Int32[] numbers = GetNumbers(selectedRowNumber);
            index = IndexOfNumber(numbers[0], numbers[1], numbers[2]);

            switch(modelRecords[index].RecordType)
            {
                case RecordType.project:
                    indexes = IndexOfNumberList(numbers[0]);
                    break;
                case RecordType.job:
                    indexes = IndexOfNumberList(numbers[0], numbers[1]);
                    break;
                case RecordType.act:
                    //Взять следующую строку и запустить цикл по номерам от следующего за текущим и до второй строки
                    indexes.Add(index);
                    for (int i = index + 1; i < modelRecords.Count; i++)
                    {
                        if (modelRecords[i].Hide == true && modelRecords[i].RecordType == RecordType.act)
                        {
                            indexes.Add(i);
                        }
                        else
                            break;
                    }
                    break;
            }
            foreach (Int32 i in indexes)
            {
                Record undoRecord = modelRecords[index].Clone();
                UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
                undoActions.Push(undoAction);
                redoActions.Clear();
                modelRecords[i].Hide = false;
            }
        }

        public void Hide2Projects()
        {
            foreach (Record rec in modelRecords)
            {
                if (rec.RecordType != RecordType.project)
                {
                    rec.Hide = true;
                }
            }
        }

        public void Hide2Jobs()
        {
            foreach (Record rec in modelRecords)
            {
                if (rec.RecordType == RecordType.act)
                {
                    rec.Hide = true;
                }
            }
        }

        public void SetNote(String rowNumber, String noteContent)
        {
            Int32[] number = GetNumbers(rowNumber);
            Int32 rowIndex = IndexOfNumber(number[0], number[1], number[2]);
            Record undoRecord = modelRecords[rowIndex].Clone();
            UndoAction undoAction = new UndoAction(ActionType.editRecord, undoRecord);
            undoActions.Push(undoAction);
            redoActions.Clear();

            modelRecords[rowIndex].Note = noteContent;
        }

        /// <summary>
        /// Возвращает единственную запись по ее номеру
        /// Используется в NoteViewModel, ExportHTMLProcessing
        /// </summary>
        /// <param name="selectedRowNumber">Значение номера записи</param>
        /// <returns></returns>
        public Record GetSingleRecord(String selectedRowNumber)
        {
            Int32[] number = GetNumbers(selectedRowNumber);
            Int32 rowIndex = IndexOfNumber(number[0], number[1], number[2]);
            return modelRecords[rowIndex];
        }

        public Boolean MoveRecordUp(String selectedRowNumber)
        {
            Boolean res = false;
            Int32[] number = GetNumbers(selectedRowNumber);
            Int32 rowIndex = IndexOfNumber(number[0], number[1], number[2]);
            switch (modelRecords[rowIndex].RecordType)
            {
                case RecordType.act:
                    if (rowIndex > 0)
                    {
                        if (modelRecords[rowIndex - 1].RecordType == RecordType.act)
                        {
                            Int32 n = modelRecords[rowIndex - 1].ActNumber;
                            modelRecords[rowIndex - 1].ActNumber = modelRecords[rowIndex].ActNumber;
                            modelRecords[rowIndex].ActNumber = n;
                            res = true;
                        }
                    }
                    break;
                case RecordType.job:
                    if (modelRecords[rowIndex].JobNumber > 0)
                    {
                        List<Record> tempRecordList1 = new List<Record>();
                        List<Record> tempRecordList2 = new List<Record>();
                        foreach (Record r in modelRecords)
                        {
                            if (modelRecords[rowIndex].ProjNumber == r.ProjNumber && modelRecords[rowIndex].JobNumber == r.JobNumber)
                            {
                                tempRecordList1.Add(r.Clone());
                            }
                            if (modelRecords[rowIndex].ProjNumber == r.ProjNumber && (modelRecords[rowIndex].JobNumber - 1) == r.JobNumber)
                            {
                                tempRecordList2.Add(r.Clone());
                            }
                        }
                        foreach(Record r in tempRecordList1)
                        {
                            modelRecords.Remove(r);
                            --r.JobNumber;
                        }
                        foreach (Record r in tempRecordList2)
                        {
                            modelRecords.Remove(r);
                            ++r.JobNumber;
                        }
                        foreach (Record r in tempRecordList1)
                        {
                            modelRecords.Add(r);
                        }
                        foreach (Record r in tempRecordList2)
                        {
                            modelRecords.Add(r);
                        }
                        res = true;
                    }
                    break;
            }
            modelRecords.Sort();
            return res;
        }

        public Boolean MoveRecordDown(String selectedRowNumber)
        {
            Boolean res = false;
            Int32 maxNumber = 0;
            Int32[] number = GetNumbers(selectedRowNumber);
            Int32 rowIndex = IndexOfNumber(number[0], number[1], number[2]);

            switch (modelRecords[rowIndex].RecordType)
            {
                case RecordType.act:
                    maxNumber = MaxNumber(modelRecords[rowIndex].ProjNumber, modelRecords[rowIndex].JobNumber);
                    if (number[2] < maxNumber)
                    {
                        Int32 n = modelRecords[rowIndex + 1].ActNumber;
                        modelRecords[rowIndex + 1].ActNumber = modelRecords[rowIndex].ActNumber;
                        modelRecords[rowIndex].ActNumber = n;
                        res = true;
                    }
                    break;
                case RecordType.job:
                    maxNumber = MaxNumber(modelRecords[rowIndex].ProjNumber);
                    if (modelRecords[rowIndex].JobNumber < maxNumber)
                    {
                        List<Record> tempRecordList1 = new List<Record>();
                        List<Record> tempRecordList2 = new List<Record>();
                        foreach (Record r in modelRecords)
                        {
                            if (modelRecords[rowIndex].ProjNumber == r.ProjNumber && modelRecords[rowIndex].JobNumber == r.JobNumber)
                            {
                                tempRecordList1.Add(r.Clone());
                            }
                            if (modelRecords[rowIndex].ProjNumber == r.ProjNumber && (modelRecords[rowIndex].JobNumber + 1) == r.JobNumber)
                            {
                                tempRecordList2.Add(r.Clone());
                            }
                        }
                        foreach (Record r in tempRecordList1)
                        {
                            modelRecords.Remove(r);
                            ++r.JobNumber;
                        }
                        foreach (Record r in tempRecordList2)
                        {
                            modelRecords.Remove(r);
                            --r.JobNumber;
                        }
                        foreach (Record r in tempRecordList1)
                        {
                            modelRecords.Add(r);
                        }
                        foreach (Record r in tempRecordList2)
                        {
                            modelRecords.Add(r);
                        }
                        res = true;
                    }
                    break;
            }
            modelRecords.Sort();
            return res;
        }

        //Возвращает количество имеющихся проектов
        public Int32 ProjCount()
        {
            Int32 res = MaxNumber();
            return res;
        }

        //Возвращает количество имеющихся записей
        public Int32 RecordsCount()
        {
            return modelRecords.Count();
        }

        //Вырезает группу записей типа "Дело" во внутренний клипборд
        public void CutActRecords(List<ViewRecord> SelectedViewRecords)
        {
            Int32[] number;
            Int32 index;
            Record r;
            clipboard.Clear();

            foreach (ViewRecord vr in SelectedViewRecords)
            {
                number = GetNumbers(vr.Number);
                index = IndexOfNumber(number[0], number[1], number[2]);
                r = modelRecords[index].Clone();
                Record undoRecord = modelRecords[index].Clone();
                UndoAction undoAction = new UndoAction(ActionType.delRecord, undoRecord);
                undoActions.Push(undoAction);
                clipboard.Enqueue(r);
                modelRecords.Remove(r);
            }
            NumerationRepair();
            //TODO: model.CutActRecords криво работает отмена, добавить перенумерацию записей при выполнении Undo
        }

        //Вырезает запись типа "Работа" и ее "дела" во внутренний клипборд
        public void CutJobRecords(List<ViewRecord> SelectedViewRecords)
        {
            Int32[] number;
            Int32 index;
            Record r;
            clipboard.Clear();
            ViewRecord vr = SelectedViewRecords[0];
            number = GetNumbers(vr.Number);
            foreach (Record rec in modelRecords)
            {
                if (rec.ProjNumber == number[0] && rec.JobNumber == number[1])
                {
                    index = IndexOfNumber(number[0], number[1], number[2]);
                    r = rec.Clone();
                    Record undoRecord = rec.Clone();
                    UndoAction undoAction = new UndoAction(ActionType.delRecord, undoRecord);
                    undoActions.Push(undoAction);
                    clipboard.Enqueue(r);
                }
            }
            foreach (Record rc in clipboard)
            {
                modelRecords.Remove(rc);
            }
            NumerationRepair();
        }

        //Вставляет записи из внутреннего клипборда
        public void PasteRecords(List<ViewRecord> SelectedViewRecords)
        {
            if (clipboard.Count == 0)
            {
                return;
            }
            Int32[] number;
            Int32 index;
            Boolean lastRecordFlag = true;
            ViewRecord vr = SelectedViewRecords[0];
            number = GetNumbers(vr.Number);
            index = IndexOfNumber(number[0], number[1], number[2]);
            RecordType type = clipboard.Peek().RecordType;
            if (type == RecordType.act) //Вставляем группу записей Act
            {
                foreach (Record r in clipboard)
                {
                    Record undoRecord = r.Clone();
                    UndoAction undoAction = new UndoAction(ActionType.addRecord, undoRecord);
                    undoActions.Push(undoAction);

                    modelRecords.Insert(++index, r);
                }
            }
            else //Вставляем Работу и ее Дела
            {
                for (Int32 i = ++index; i < modelRecords.Count(); i++) //Ищем следующую запись типа Проект или Работа
                {
                    if (modelRecords[i].RecordType == RecordType.job || modelRecords[i].RecordType == RecordType.project)
                    {
                        index = i - 1;
                        lastRecordFlag = false;
                        break;
                    }
                }
                if (lastRecordFlag) //Если дошли до конца modelRecords
                    index = modelRecords.Count() - 1;
                foreach (Record r in clipboard)
                {
                    Record undoRecord = r.Clone();
                    UndoAction undoAction = new UndoAction(ActionType.addRecord, undoRecord);
                    undoActions.Push(undoAction);

                    modelRecords.Insert(++index, r);
                }
            }
            NumerationRepair();
        }

        //Изменяет значение сигнала записи
        public void SetSignal(String rowNumber, String dateTime, AlarmRepit alarmRepit)
        {
            Int32[] number = GetNumbers(rowNumber);
            Int32 rowIndex = IndexOfNumber(number[0], number[1], number[2]);
            modelRecords[rowIndex].AlarmTime = dateTime;
            modelRecords[rowIndex].AlarmRepit = alarmRepit;
        }

        public List<Alarm> GetSignals()
        {
            List<Alarm> alarms = new List<Alarm>();
            foreach (Record record in modelRecords)
            {
                if (record.AlarmTime != "")
                {
                    Alarm alarm = new Alarm();
                    alarm.dateTime = record.AlarmTime;
                    alarm.recordNumber = record.ProjNumber.ToString() + "." + record.JobNumber.ToString() + "." + record.ActNumber.ToString();
                    alarms.Add(alarm);
                }
            }
            return alarms;
        }

        #region Секция свойств
        public ViewRecord LastRowAdded
        {
            get { return lastRowAdded; }
        }
        #endregion

        #region Служебные функции
        /// <summary>
        /// Поиск индекса записи (Record) по значению ее номера
        /// </summary>
        /// <param name="proj">Int32 Номер проекта</param>
        /// <param name="job">Int32 Номер работы</param>
        /// <param name="act">Int32 Номер дела</param>
        /// <returns>Индекс записи в modelRecords или -1, если запись не найдена</returns>
        private Int32 IndexOfNumber(Int32 proj, Int32 job, Int32 act)
        {
            Record r;
            for (Int32 i = 0; i < modelRecords.Count; i++)
            {
                r = modelRecords[i];
                if (proj == r.ProjNumber && job == r.JobNumber && act == r.ActNumber)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Поиск индексов записей (Record) по значениям ее номера
        /// </summary>
        /// <param name="proj">Int32 Номер проекта</param>
        /// <param name="job">Int32 Номер работы (опция)</param>
        /// <returns>Коллекцию индексов записей в modelRecords или пустую коллекцию, если записи не найдены</returns>
        private List<Int32> IndexOfNumberList(Int32 proj, Int32 job = -1)
        {
            Record r;
            List<Int32> res = new List<int>();
            if (job == -1) //Поиск только по номеру Проекта
            {
                for (Int32 i = 0; i < modelRecords.Count; i++)
                {
                    r = modelRecords[i];
                    if (proj == r.ProjNumber)
                    {
                        res.Add(i);
                    }
                }
            }
            else //Поиск по номеру проекта и номеру работы
            {
                for (Int32 i = 0; i < modelRecords.Count; i++)
                {
                    r = modelRecords[i];
                    if (proj == r.ProjNumber && job == r.JobNumber)
                    {
                        res.Add(i);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Поиск максимального номера записи - максимальный номер проекта/работы/дела
        /// </summary>
        /// <param name="proj">Номер проекта. Если не задан, метод возвращает максимальный номер проекта</param>
        /// <param name="job">Номер работы. Если задан номер проекта и не задан номер работы, то возвращает максимальный номер работы в проекте. Иначе возвращает максимальный номер дела.</param>
        /// <returns>Возвращает максимальный существующий номер проекта, работы, дела</returns>
        private Int32 MaxNumber(Int32 proj = -1, Int32 job = -1)
        {
            Int32 max = 0;
            Record r;
            if (proj == -1) //Ищем максимальный номер проекта
            {
                Debug.Assert(job == -1, "Ошибка аргументов метода MaxNumber: proj = -1, job = -1");
                for (Int32 i = 0; i < modelRecords.Count; i++)
                {
                    r = modelRecords[i];
                    if (max < r.ProjNumber)
                        max = r.ProjNumber;
                }
                return max;
            }
            if (proj > -1 && job == -1) //Ищем максимальный номер работы в проекте
            {
                for (Int32 i = 0; i < modelRecords.Count; i++)
                {
                    r = modelRecords[i];
                    if (r.ProjNumber == proj && max < r.JobNumber)
                        max = r.JobNumber;
                }
                return max;
            }

            if (proj > -1 && job > -1) //Ищем максимальный номер дела
            {
                for (Int32 i = 0; i < modelRecords.Count; i++)
                {
                    r = modelRecords[i];
                    if (r.ProjNumber == proj && r.JobNumber == job && max < r.ActNumber)
                        max = r.ActNumber;
                }
            }
            return max;
        }

        /// <summary>
        /// Обновляет поле ViewRecord lastRowAdded после добавления новой записи в модель
        /// </summary>
        /// <param name="r">Добавленная в модель запись</param>
        private void LastRowUpdate(Record r)
        {
            lastRowAdded.Number = (String)(r.ProjNumber + "." + r.JobNumber + "." + r.ActNumber);
            lastRowAdded.RecordDate = (String)r.RecordDate;
            lastRowAdded.Content = (String)r.Content;
            lastRowAdded.AlarmTime = (String)r.AlarmTime;
            lastRowAdded.Note = (String)r.Note;
        }

        /// <summary>
        /// Преобразовывает номер записи из формата String в массив Int32
        /// </summary>
        /// <param name="number">Строка номера записи в формате ViewModel</param>
        /// <returns></returns>
        private Int32[] GetNumbers(String number)
        {
            Int32[] res = new Int32[3];
            String[] numberStrings = number.Split(new char[] { '.' });
            res[0] = Int32.Parse(numberStrings[0]);
            res[1] = Int32.Parse(numberStrings[1]);
            res[2] = Int32.Parse(numberStrings[2]);
            return res;
        }

        /// <summary>
        /// Восстанавливает нумерацию записей (поля ProjNumber, JobNumber, ActNumber) в зависимости от положения записи в modelRecords
        /// </summary>
        private void NumerationRepair()
        {
            Int32 proj = 0;
            Int32 job = 0;
            Int32 act = 0;

            foreach (Record r in modelRecords)
            {
                switch (r.RecordType)
                {
                    case RecordType.project:
                        r.ProjNumber = ++proj;
                        job = 0;
                        r.JobNumber = job;
                        act = 0;
                        r.ActNumber = act;
                        break;
                    case RecordType.job:
                        r.ProjNumber = proj;
                        r.JobNumber = ++job;
                        act = 0;
                        r.ActNumber = act;
                        break;
                    case RecordType.act:
                        r.ProjNumber = proj;
                        r.JobNumber = job;
                        r.ActNumber = ++act;
                        break;
                }
            }
        }
        #endregion
    }
}

//TODO: При отмене архивирования проекта (Undo) восстановление производится по одной строке
//TODO: При архивировании проекта статус "Завершенные" теряется
