using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace AProjects
{
    class ViewModel : INotifyPropertyChanged
    {
        private Model model;
        public DataGrid mainDataGrid;
        public SpecialObservableCollection<ViewRecord> viewRecords;
        private List<Record> viewModelRecords; //Для получения данных из модели
        public List<ViewRecord> SelectedViewRecords; //Список выделенных строк
        //public List<String> SelectedRowNumbers;
        private ViewRecord selectedRow; //Хранит строку для последующей установки выделения на ней
        private Boolean archiveView; //False - активные записи, True - архивные записи
        private Boolean viewWithFinished; //False - завершенные не показывать, True - завершенные показывать
        private String currentFile; //Полное имя текущего файла данных
        private DateTime currentDate; //Текущее значение даты для отображения в DatePicker
        private Dictionary<String, NoteWindow> noteWindows; //Список открытых окон редактирования примечаний (NoteWindow): <Номер строки, указатель на окно>
        private Int32 projectCount; //Количество проектов, для вывода в StatusBar
        private Int32 recordCount; //Количество записей, для вывода в StatusBar
        private Dictionary<String, SignalWindow> signalWindows; //Список открытых окон редактирования сигналов (SignalWindow): <Номер строки, указатель на окно>
        private List<Alarm> alarms; //Список будильников
        private SignalProcessing signalProcessing; //Класс работы с таймерами для будильников
        private Export exportVM; //Окно настроек экспорта в HTML
        private ExportViewModel exportViewModel; //viewModel для окна настроек экспорта в HTML
        private ExportMode exportMode; //Режим экспорта - HTML/CSV

        public ViewModel()
        {
            model = new Model();
            viewModelRecords = new List<Record>();
            //SelectedRowNumbers = new List<string>();
            SelectedViewRecords = new List<ViewRecord>();
            currentFile = Properties.Settings.Default.LastFileName;
            viewRecords = new SpecialObservableCollection<ViewRecord>();
            archiveView = false;
            viewWithFinished = Properties.Settings.Default.viewWithFinished;

            //viewRecords.CollectionChanged += viewRecords_CollectionChanged; //TODO: проверить необходимость обработчик событий ViewModel.ViewRecords.CollectionChanged
            //viewRecords.ListItemChanged += viewRecords_PropertyChanged;

            if (currentFile != null)
                LoadFileOnStart();
            ViewUpdate();
            noteWindows = new Dictionary<string, NoteWindow>();
            signalWindows = new Dictionary<string, SignalWindow>();
            ProjectCountUpdate();
            RecordCountUpdate();
            signalProcessing = new SignalProcessing();
            signalProcessing.RaiseAlarmTimerTickEvent += signalProcessing_AlarmTimerTick;
            alarms = model.GetSignals();
            signalProcessing.UpdateTimers(alarms);
        }

        #region Секция событий и обработчиков
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private void NoteWindowClosedEventHandler(object sender, NoteWindowClosedEventArgs eventArgs)
        {
            if (noteWindows.ContainsKey(eventArgs.Message))
            {

                NoteWindow noteWindow = noteWindows[eventArgs.Message];
                noteWindows.Remove(eventArgs.Message);
                NoteViewModel noteViewModel = (NoteViewModel)noteWindow.DataContext;
                noteViewModel.RaiseNoteWindowClosedEvent -= NoteWindowClosedEventHandler;
                noteWindow.Close();
                noteViewModel = null;
                ViewUpdate();
                Int32 rowIndex = GetIndexByNumber(eventArgs.Message);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                Debug.Assert(true, "NoteWindowClosedEventArgs, отсутствует указатель на окно в списке noteWindows");
        }

        private void SignalWindowClosedEventHandler(object sender, SignalWindowClosedEventArgs eventArgs)
        {
            if (signalWindows.ContainsKey(eventArgs.Message))
            {
                SignalWindow signalWindow = signalWindows[eventArgs.Message];
                signalWindows.Remove(eventArgs.Message);
                SignalViewModel signalViewModel = (SignalViewModel)signalWindow.DataContext;
                signalViewModel.RaiseSignalWindowClosedEvent -= SignalWindowClosedEventHandler;
                signalWindow.Close();
                signalViewModel = null;

                alarms = model.GetSignals();
                signalProcessing.UpdateTimers(alarms);

                ViewUpdate();
                Int32 rowIndex = GetIndexByNumber(eventArgs.Message);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                Debug.Assert(true, "SignalWindowClosedEventArgs, отсутствует указатель на окно в списке signalWindows");
            }

        private void signalProcessing_AlarmTimerTick(object sender, AlarmTimerTickEventArgs eventArgs)
        {
            //Сработал таймер
            String signalRowNumber = eventArgs.Message;
            if (signalWindows.ContainsKey(signalRowNumber)) //Окно для этой строки уже открыто
            {
                signalWindows[signalRowNumber].Topmost = true; //Выводим его на передний план
            }
            else //Окна для этой строки нет, создаем его
            {
                SignalViewModel signalViewModel = new SignalViewModel(model, signalRowNumber, true);
                signalViewModel.RaiseSignalWindowClosedEvent += SignalWindowClosedEventHandler;
                SignalWindow signalWindow = new SignalWindow();
                signalWindow.Owner = Application.Current.MainWindow;
                signalWindow.DataContext = signalViewModel;
                signalWindows.Add(signalRowNumber, signalWindow);
                ViewUpdate();
                Int32 rowIndex = GetIndexByNumber(signalRowNumber);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
                signalWindow.Show();
            }
            alarms = model.GetSignals();
            signalProcessing.UpdateTimers(alarms);
        }

        private void ExportWindowClosedEventHandler(object sender, ExportWindowEventArgs eventArgs)
        {
            exportVM.Close();
            exportViewModel.RaiseExportWindowClosedEvent -= ExportWindowClosedEventHandler;
            exportViewModel = null;
            Dictionary<String, Boolean> exportSettings = (Dictionary < String, Boolean > )eventArgs.Message;
            if (exportSettings.Count == 0) //Если уставки отсутствуют, то пользователь нажал кнопку "Отмена"
                return;
            if (exportMode == ExportMode.Html)
            {
                //Экспорт в HTML
                Export2HTML(exportSettings);
            }
            else if (exportMode == ExportMode.Csv)
            {
                //Экспорт в CSV
                Export2CSV(exportSettings);
            }
            else
                Debug.Assert(true, "ошибка режима экспорта");
        }
        #endregion

        #region Секция полей
        public String LoadedFile
        {
            get => "Файл:  " + currentFile;
            set
            {
                this.currentFile = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("LoadedFile"));
                }
            }
        }

        public Boolean ArchiveView
        {
            get => archiveView;
            set
            {
                this.archiveView = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ArchiveView"));
                }
            }
        }

        public Boolean ViewWithFinished
        {
            get => viewWithFinished;
            set
            {
                this.viewWithFinished = value;
                Properties.Settings.Default.viewWithFinished = value;
                Properties.Settings.Default.Save();
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ViewWithFinished"));
                }
            }
        }

        public DateTime CurrentDate
        {
            get => DateTime.Today;
            set
            {
                this.currentDate = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("CurrentDate"));
                }
            }
        }

        public Int32 ProjectCount
        {
            get => projectCount;
            set
            {
                this.projectCount = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ProjectCount"));
                }
            }
        }

        public Int32 RecordCount
        {
            get => recordCount;
            set
            {
                this.recordCount = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("RecordCount"));
                }
            }
        }
        #endregion

        #region Служебные функции
        /// <summary>
        /// Обновление записи ViewRecord после окончания редактирования ячейки и передача обновленного значения в модель
        /// </summary>
        /// <param name="viewRecord"> Редактируемая запись</param>
        /// <param name="columnName">Наименование колонки, в которой производилось редактирование</param>
        /// <param name="content">Новое значение отредактированной ячейки</param>
        public void DataGridChanged(ViewRecord viewRecord, String columnName, String content)
        {
            if (columnName == "Number")
            {
                viewRecord.Number = content;
            }
            else if (columnName == "RecordDate")
            {
                viewRecord.RecordDate = content;
            }
            else if (columnName == "Content")
            {
                viewRecord.Content = content;
            }
            else if (columnName == "AlarmTime")
            {
                viewRecord.AlarmTime = content;
            }
            else if (columnName == "Note")
            {
                viewRecord.Note = content;
            }
            else
                System.Windows.MessageBox.Show("VievModel.DataGridChanged: Ошибка аргумента \n columnName = " + columnName);

            model.UpdateModelRecord(viewRecord);
            ViewUpdate();
        }

        /// <summary>
        /// Получает тип записи (Проект/Работа/Дело) для записи типа ViewRecord
        /// </summary>
        /// <param name="viewRecord"></param>
        /// <returns></returns>
        private RecordType GetRecordType(ViewRecord viewRecord)
        {
            RecordType res;
            String[] number = viewRecord.Number.Split(new char[] { '.' });
            Int32 proj = Int32.Parse(number[0]);
            Int32 job = Int32.Parse(number[1]);
            Int32 act = Int32.Parse(number[2]);
            if (act > 0)
            {
                res = RecordType.act;
            }
            else if (job > 0)
            {
                res = RecordType.job;
            }
            else
                res = RecordType.project;
            return res;
        }

        //Автоматическое сохранение перед закрытием формы окна
        //Вызывается из MainWindow.xaml.cs по прерыванию, вызывает команду сохранения
        public void SaveBeforExit()
        {
            SaveFileCommand(null);
        }

        //Запускается из конструктора ViewModel для загрузки файла данных прошлого сеанса или обработки ошибки при открытии файла данных прошлого сеанса.
        private void LoadFileOnStart()
        {
            try
            {
                model.ReadFromFile(currentFile);
            }
            catch (Exception e)
            {
                MessageBoxResult res = MessageBox.Show("Файл открыть не удалось. \n\n Вы можете открыть другой файл (Да) \n или создать новый (Нет)", "Открытие файла данных", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
                if (res == MessageBoxResult.Yes)
                {
                    OpenFileCommand(null);
                }
                else if (res == MessageBoxResult.No)
                {
                    NewFileCommand(null);
                }
            }
        }

        //Добавляет запись в ViewRecords на основе Record. Исользуется в ViewUpdate()
        private void ViewRecordAdding(List<ViewRecord> preViewRecords, Record element)
        {
            preViewRecords.Add(new ViewRecord()
            {
                Number = (String)(element.ProjNumber + "." + element.JobNumber + "." + element.ActNumber),
                RecordDate = (String)element.RecordDate,
                Content = (String)element.Content,
                AlarmTime = (String)element.AlarmTime,
                Note = (String)element.Note,
                VisualStyle = StyleCalculate(element),
                ExpandIcon = (Boolean)element.Hide
            });
        }

        /// <summary>
        /// Поиск индекса строки в ViewRecords по значению поля Number
        /// </summary>
        /// <param name="number">String значение поля Number</param>
        /// <returns>Int32 Возвращает значение индекса в ViewRecords</returns>
        private Int32 GetIndexByNumber(String number)
        {
            ViewRecord record = null;
            Int32 res = 0;
            foreach (ViewRecord vr in viewRecords)
            {
                if (vr.Number == number)
                {
                    record = vr;
                    break;
                }
                else
                    Debug.Assert(true, "FindIndexByNumber(). Индекс записи не найден.");
            }
            res = viewRecords.IndexOf(record);
            return res;
        }

        /// <summary>
        /// Обновляет значение поля ProjectCount (количество проектов)
        /// </summary>
        private void ProjectCountUpdate()
        {
            ProjectCount = model.ProjCount();
        }

        /// <summary>
        /// Обновляет значение поля RecordCount (общее количество записей)
        /// </summary>
        private void RecordCountUpdate()
        {
            RecordCount = model.RecordsCount();
        }

        /// <summary>
        /// Экспорт данных в HTML-файл
        /// </summary>
        /// <param name="exportSettings">Словарь (Dictionary) параметров экспорта</param>
        private void Export2HTML(Dictionary<String, Boolean> exportSettings)
        {
            FileDialog dialog = new SaveFileDialog();
            dialog.FileName = "AProjects"; //Имя файла по умолчанию
            dialog.DefaultExt = ".html"; //Расширение по умолчанию
            dialog.Filter = "Веб-страница |*.html|Все файлы|*.*";
            if (dialog.ShowDialog() == true)
            {
                String fileName = dialog.FileName;
                ExportHTMLProcessing exportHTMLProcessing = new ExportHTMLProcessing(exportSettings, model, fileName);

                if (exportSettings["ExportSelectedRecords"] == true) //Экспорт выделенных записей
                {
                    //Выбираем выделенные записи
                    if (SelectedViewRecords.Count > 0)
                    {
                        List<String> vrNumbers = new List<string>();
                        foreach (ViewRecord vr in SelectedViewRecords)
                        {
                            vrNumbers.Add(vr.Number);
                        }
                        try //Перехват прерывание ошибки записи в файл
                        {
                            exportHTMLProcessing.SelectionExport(vrNumbers);
                        }
                        catch
                        {
                            MessageBox.Show("Ошибка записи файла HTML!");
                        }
                        finally
                        {
                            exportHTMLProcessing = null;
                            exportSettings = null;
                        }
                    }
                    else
                        Debug.Assert(true, "ExportWindowClosedEventHandler - SelectedViewRecords.Count = 0");
                }
                else //Экспорт во всех случаях, кроме экспорта выделенных строк
                {
                    try //Перехват прерывание ошибки записи в файл
                    {
                        exportHTMLProcessing.Export();
                    }
                    catch
                    {
                        MessageBox.Show("Ошибка записи файла HTML!");
                    }
                    finally
                    {
                        exportHTMLProcessing = null;
                        exportSettings = null;
                    }
                }
            }
        }

        /// <summary>
        /// Экспорт данных в CSV-файл
        /// </summary>
        /// <param name="exportSettings">Словарь (Dictionary) параметров экспорта</param>
        private void Export2CSV(Dictionary<String, Boolean> exportSettings)
        {
            FileDialog dialog = new SaveFileDialog();
            dialog.FileName = "AProjects"; //Имя файла по умолчанию
            dialog.DefaultExt = ".csv"; //Расширение по умолчанию
            dialog.Filter = "Файл CSV (разделитель точка с запятой) |*.csv|Все файлы|*.*";
            if (dialog.ShowDialog() == true)
            {
                String fileName = dialog.FileName;
                ExportCSVProcessing exportCSVProcessing = new ExportCSVProcessing(exportSettings, model, fileName);

                if (exportSettings["ExportSelectedRecords"] == true) //Экспорт выделенных записей
                {
                    //Выбираем выделенные записи
                    if (SelectedViewRecords.Count > 0)
                    {
                        List<String> vrNumbers = new List<string>();
                        foreach (ViewRecord vr in SelectedViewRecords)
                        {
                            vrNumbers.Add(vr.Number);
                        }
                        try //Перехват прерывание ошибки записи в файл
                        {
                            exportCSVProcessing.SelectionExport(vrNumbers);
                        }
                        catch
                        {
                            MessageBox.Show("Ошибка записи файла CSV!");
                        }
                        finally
                        {
                            exportCSVProcessing = null;
                            exportSettings = null;
                        }
                    }
                    else
                        Debug.Assert(true, "ExportWindowClosedEventHandler - SelectedViewRecords.Count = 0");
                }
                else //Экспорт во всех случаях, кроме экспорта выделенных строк
                {
                    try //Перехват прерывание ошибки записи в файл
                    {
                        exportCSVProcessing.Export();
                    }
                    catch
                    {
                        MessageBox.Show("Ошибка записи файла HTML!");
                    }
                    finally
                    {
                        exportCSVProcessing = null;
                        exportSettings = null;
                    }
                }
            }

        }
        #endregion

        /// <summary>
        /// Запрашивает данные из модели и формирует viewRecords с учетом признака стиля для отображения в View
        /// </summary>
        private void ViewUpdate()
        {
            model.GetModelData(ref viewModelRecords);
            viewRecords.Clear();
            List<ViewRecord> preViewRecords = new List<ViewRecord>();
            Boolean hideFlag = false;

            if (archiveView) //Архивные записи, завершенные и незавершенные
            {
                foreach (Record element in viewModelRecords)
                {
                    if (element.RecordStatus == RecordStatus.arx)
                    {
                        ViewRecordAdding(preViewRecords, element);
                    }
                }
            }
            else if (!archiveView && !viewWithFinished) //Активные записи, завершенные не показывать
            {
                foreach (Record element in viewModelRecords)
                {
                    if (element.RecordStatus == RecordStatus.active)
                    {
                        ViewRecordAdding(preViewRecords, element);
                    }
                }
            }
            else //Активные записи, завершенные показывать
            {
                foreach (Record element in viewModelRecords)
                {
                    if (element.RecordStatus == RecordStatus.active || element.RecordStatus == RecordStatus.finished)
                    {
                        ViewRecordAdding(preViewRecords, element);
                    }
                }
            }

            //Обработка признака Hide
            for (Int32 i = preViewRecords.Count - 1; i > -1; i--)
            {
                if (preViewRecords[i].ExpandIcon) //Hide = true
                {
                    hideFlag = true;
                }
                else //Hide = false
                {
                    if (hideFlag)
                        preViewRecords[i].ExpandIcon = true;
                    viewRecords.Insert(0, preViewRecords[i]);
                    hideFlag = false;
                }
            }
            viewRecords.Reverse();
        }

        /// <summary>
        /// Запрашивает данные из модели и формирует viewRecords с учетом признака стиля для отображения в View
        /// Вызывается по команде отображения строк с цветовой маркировкой
        /// </summary>
        private void ViewUpdateByColor(String colorParametr)
        {
            if (colorParametr == "White") //Если выбрана отмена цветового выделения или двойной клик на Белом - переходим к предыдущему режиму отбражения
            {
                ViewUpdate();
            }
            else //Если выбран режим отображения с цветным выделением
            {
                Int32 fillParameter = 0;
                switch (colorParametr)
                {
                    case "Red":
                        fillParameter = 1;
                        break;
                    case "Yellow":
                        fillParameter = 2;
                        break;
                    case "Green":
                        fillParameter = 3;
                        break;
                    case "Blue":
                        fillParameter = 4;
                        break;
                    case "Magenta":
                        fillParameter = 5;
                        break;
                    default:
                        Debug.Assert(true, "ViewUpdateByColor - неизвестный параметр цветового выделения");
                        break;
                }
                model.GetModelData(ref viewModelRecords);
                viewRecords.Clear();
                List<ViewRecord> preViewRecords = new List<ViewRecord>();

                if (archiveView) //Архивные записи, завершенные и незавершенные !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                {
                    foreach (Record element in viewModelRecords)
                    {
                        if (element.RecordStatus == RecordStatus.arx) //Неправильно, показать 
                        {
                            if (element.RecordType == RecordType.project || element.RecordType ==RecordType.job || element.Fill == fillParameter)
                            {
                                ViewRecordAdding(preViewRecords, element);
                            }
                        }
                    }
                }
                else if (!archiveView && !viewWithFinished) //Активные записи, завершенные не показывать
                {
                    foreach (Record element in viewModelRecords)
                    {
                        if (element.RecordStatus == RecordStatus.active)
                        {
                            if (element.RecordType == RecordType.project || element.RecordType == RecordType.job || element.Fill == fillParameter)
                            {
                                ViewRecordAdding(preViewRecords, element);
                            }
                        }
                    }
                }
                else //Активные записи, завершенные показывать
                {
                    foreach (Record element in viewModelRecords)
                    {
                        if (element.RecordStatus == RecordStatus.active || element.RecordStatus == RecordStatus.finished)
                        {
                            if (element.RecordType == RecordType.project || element.RecordType == RecordType.job || element.Fill == fillParameter)
                            {
                                ViewRecordAdding(preViewRecords, element);
                            }
                        }
                    }
                }
                preViewRecords.Reverse();
                foreach (ViewRecord element in preViewRecords)
                {
                    viewRecords.Insert(0, element);
                }
            }
        }

        #region Визуальное оформление View
        //Вычисление номера стиля для строки
        private Int32 StyleCalculate(Record record)
        {
            /* Номера стилей ========================
             * 0 - Дело, цвет фона основной
             * 1 - Дело, цвет фона красный
             * 2 - Дело, цвет фона желтый
             * 3 - Дело, цвет фона зеленый
             * 4 - Дело, цвет фона синий
             * 5 - Дело, цвет фона фиолетовый
             * 6 - Дело, завершенный
             * 
             * 20 - Работа, цвет фона основной
             * 21 - Работа, цвет фона красный
             * 22 - Работа, цвет фона желтый
             * 23 - Работа, цвет фона зеленый
             * 24 - Работа, цвет фона синий
             * 25 - Работа, цвет фона фиолетовый
             * 26 - Работа, завершенный
             * 
             * 40 - Проект, цвет фона основной
             * 41 - Проект, завершенный
             */

            Int32 res = 0;

            switch (record.RecordType)
            {
                case RecordType.act: //Обработка записей Дело
                    if (record.RecordStatus == RecordStatus.finished && archiveView == false)
                        res = 6;
                    else
                    {
                        switch (record.Fill)
                        {
                            case 0:
                                res = 0; //Белый
                                break;
                            case 1:
                                res = 1; //Красный
                                break;
                            case 2:
                                res = 2; //Желтый
                                break;
                            case 3:
                                res = 3; //Зеленый
                                break;
                            case 4:
                                res = 4; //Синий
                                break;
                            case 5:
                                res = 5; //Фиолетовый
                                break;
                            default:
                                throw new ArgumentException("Индекс поля Fill вне диапазона.");
                        }
                    }
                    break;
                case RecordType.job: //Обработка записей Работа
                    if (record.RecordStatus == RecordStatus.finished && archiveView == false)
                        res = 26;
                    else
                    {
                        switch (record.Fill)
                        {
                            case 0:
                                res = 20;
                                break;
                            case 1:
                                res = 21;
                                break;
                            case 2:
                                res = 22;
                                break;
                            case 3:
                                res = 23;
                                break;
                            case 4:
                                res = 24;
                                break;
                            case 5:
                                res = 25;
                                break;
                            default:
                                throw new ArgumentException("Индекс поля Fill вне диапазона.");
                        }
                    }
                    break;
                case RecordType.project: //Обработка записей Проект
                    if (record.RecordStatus == RecordStatus.finished && archiveView == false)
                        res = 41;
                    else
                        res = 40;
                    break;
                default:
                    MessageBox.Show("Ошибка типа записи RecordType в методе ViewModel.StyleCalculate()!");
                    break;
            }
            return res;
        }

        //Общая часть комманд SetColorXCommand
        //Передает значение поля Fill в модель, обновляет View и устанавливает выделение на перекрашеную строку
        private void SetRowColor(Int32 colorIndex)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                foreach (ViewRecord vr in SelectedViewRecords)
                {
                    model.SetColor(vr.Number, colorIndex);
                }
                ViewUpdate();
                Int32 rowIndex = viewRecords.IndexOf(selectedRow);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                MessageBox.Show("Выберите запись!");

        }
        #endregion

        #region Служебные функции для программной установки выделения ячейки

        private void SelectCellByIndex(DataGrid dataGrid, int rowIndex, int columnIndex)
        {
            if (!dataGrid.SelectionUnit.Equals(DataGridSelectionUnit.Cell))
                throw new ArgumentException("The SelectionUnit of the DataGrid must be set to Cell.");

            if (rowIndex < 0 || rowIndex > (dataGrid.Items.Count - 1))
                throw new ArgumentException(string.Format("{0} is an invalid row index.", rowIndex));

            if (columnIndex < 0 || columnIndex > (dataGrid.Columns.Count - 1))
                throw new ArgumentException(string.Format("{0} is an invalid column index.", columnIndex));

            dataGrid.SelectedCells.Clear();

            object item = dataGrid.Items[rowIndex];
            DataGridRow row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            if (row == null)
            {
                dataGrid.ScrollIntoView(item);
                row = dataGrid.ItemContainerGenerator.ContainerFromIndex(rowIndex) as DataGridRow;
            }
            if (row != null)
            {
                DataGridCell cell = GetCell(dataGrid, row, columnIndex);
                if (cell != null)
                {
                    DataGridCellInfo dataGridCellInfo = new DataGridCellInfo(cell);
                    dataGrid.SelectedCells.Add(dataGridCellInfo);
                    cell.Focus();
                }
            }
        }

        private DataGridCell GetCell(DataGrid dataGrid, DataGridRow rowContainer, Int32 column)
        {
            if (rowContainer != null)
            {
                DataGridCellsPresenter presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                if (presenter == null)
                {
                    /* if the row has been virtualized away, call its ApplyTemplate() method 
                     * to build its visual tree in order for the DataGridCellsPresenter
                     * and the DataGridCells to be created */
                    rowContainer.ApplyTemplate();
                    presenter = FindVisualChild<DataGridCellsPresenter>(rowContainer);
                }
                if (presenter != null)
                {
                    DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    if (cell == null)
                    {
                        /* bring the column into view
                         * in case it has been virtualized away */
                        dataGrid.ScrollIntoView(rowContainer, dataGrid.Columns[column]);
                        cell = presenter.ItemContainerGenerator.ContainerFromIndex(column) as DataGridCell;
                    }
                    return cell;
                }
            }
            return null;
        }

        private T FindVisualChild<T>(DependencyObject obj) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }
        #endregion

        #region Секция команд
        #region Файл
        //Файл.Новый
        private ICommand _newFile;
        public ICommand NewFile => _newFile ?? (_newFile = new RelayCommand(NewFileCommand));

        //Файл.Открыть
        private ICommand _openFile;
        public ICommand OpenFile => _openFile ?? (_openFile = new RelayCommand(OpenFileCommand));

        //Файл.Закрыть
        private ICommand _closeFile;
        public ICommand CloseFile => _closeFile ?? (_closeFile = new RelayCommand(CloseFileCommand));

        //Файл.Сохранить
        private ICommand _saveFile;
        public ICommand SaveFile => _saveFile ?? (_saveFile = new RelayCommand(SaveFileCommand));

        //Файл.Сохранить Как
        private ICommand _saveAsFile;
        public ICommand SaveAsFile => _saveAsFile ?? (_saveAsFile = new RelayCommand(SaveAsFileCommand));

        //Файл.Экспорт HTML
        private ICommand _exportHTML;
        public ICommand ExportHTML => _exportHTML ?? (_exportHTML = new RelayCommand(ExportHTMLCommand, IsCanExport));

        //Файл.Экспорт CSV
        private ICommand _exportCSV;
        public ICommand ExportCSV => _exportCSV ?? (_exportCSV = new RelayCommand(ExportCSVCommand, IsCanExport));


        //Файл.Выход
        private ICommand _exit;
        public ICommand AppExit => _exit ?? (_exit = new RelayCommand(ExitCommand));
        #endregion

        #region Правка
        //Undo
        private ICommand _undo;
        public ICommand Undo => _undo ?? (_undo = new RelayCommand(UndoCommand, IsCanUndo));

        //Redo
        private ICommand _redo;
        public ICommand Redo => _redo ?? (_redo = new RelayCommand(RedoCommand, IsCanRedo));

        //Cut Records
        private ICommand _cut;
        public ICommand Cut => _cut ?? (_cut = new RelayCommand(CutCommand));

        //Pust Records
        private ICommand _paste;
        public ICommand Paste => _paste ?? (_paste = new RelayCommand(PasteCommand));

        #endregion

        #region Вид
        //Вид.Завершенные.Показать
        private ICommand _finishedShow;
        public ICommand FinishedShow => _finishedShow ?? (_finishedShow = new RelayCommand(FinishedShowCommand));

        //Вид.Завершенные.Скрыть
        private ICommand _finishedHide;
        public ICommand FinishedHide => _finishedHide ?? (_finishedHide = new RelayCommand(FinishedHideCommand));

        //Вид.Завершенные.Скрыть/Показать
        private ICommand _finishedChangeMode;
        public ICommand FinishedChangeMode => _finishedChangeMode ?? (_finishedChangeMode = new RelayCommand(FinishedChangeModeCommand));

        //Вид.Архив.Архивные проекты
        private ICommand _arhShow;
        public ICommand ArhShow => _arhShow ?? (_arhShow = new RelayCommand(ArhShowCommand));

        //Вид.Архив.Активные проекты
        private ICommand _arhHide;
        public ICommand ArhHide => _arhHide ?? (_arhHide = new RelayCommand(ArhHideCommand));

        //Вид.Архив.Изменить режим показа
        private ICommand _arhChangeMode;
        public ICommand ArhChangeMode => _arhChangeMode ?? (_arhChangeMode = new RelayCommand(ArhChangeModeCommand));

        //Вид.Скрыть записи
        private ICommand _hideRecords;
        public ICommand HideRecords => _hideRecords ?? (_hideRecords = new RelayCommand(HideRecordsCommand));

        //Вид.Показать записи
        private ICommand _unHideRecords;
        public ICommand UnHideRecords => _unHideRecords ?? (_unHideRecords = new RelayCommand(UnHideRecordsCommand));

        //Вид.Скрыть/Показать записи (кнопки в DataGrid)
        //Вид.Скрыть записи
        private ICommand _changedHideMode;
        public ICommand ChangedHideMode => _changedHideMode ?? (_changedHideMode = new RelayCommand(ChangedHideModeCommand));

        //Вид.Свернуть проекты
        private ICommand _hideProjects;
        public ICommand HideProjects => _hideProjects ?? (_hideProjects = new RelayCommand(HideProjectsCommand));

        //Вид.Свернуть работы
        private ICommand _hideJobs;
        public ICommand HideJobs => _hideJobs ?? (_hideJobs = new RelayCommand(HideJobsCommand));

        //Вид.Показать цветовое выделение
        private ICommand _showColorSelection;
        public ICommand ShowColorSelection => _showColorSelection ?? (_showColorSelection = new RelayCommand(ShowColorSelectionCommand));
        #endregion

        #region Записи
        //Записи.Создать проект
        private ICommand _projCreate;
        public ICommand ProjCreate => _projCreate ?? (_projCreate = new RelayCommand(ProjCreateCommand));

        //Записи.Создать работу
        private ICommand _jobCreate;
        public ICommand JobCreate => _jobCreate ?? (_jobCreate = new RelayCommand(JobCreateCommand));

        //Записи.Создать дело
        private ICommand _actCreate;
        public ICommand ActCreate => _actCreate ?? (_actCreate = new RelayCommand(ActCreateCommand));

        //Записи.Завершить Проект/Работу/Дело
        private ICommand _finalize;
        public ICommand Finalize => _finalize ?? (_finalize = new RelayCommand(FinalizeCommand));

        //Записи.Удалить запись
        private ICommand _deleteRow;
        public ICommand DeleteRow => _deleteRow ?? (_deleteRow = new RelayCommand(DeleteRowCommand, IsCanDeleteRow));

        //Записи.Снять завершение с Проект/Работу/Дело
        private ICommand _unFinalize;
        public ICommand UnFinalize => _unFinalize ?? (_unFinalize = new RelayCommand(UnFinalizeCommand));

        //Записи.Перенести проект в архив
        private ICommand _proj2Arh;
        public ICommand Proj2Arh => _proj2Arh ?? (_proj2Arh = new RelayCommand(Proj2ArhCommand));

        //Записи.Восстановить проект из архива
        private ICommand _projFromArh;
        public ICommand ProjFromArh => _projFromArh ?? (_projFromArh = new RelayCommand(ProjFromArhCommand));
        #endregion

        #region Инструменты
        //Установить цвет записи 0 (Основной)
        private ICommand _setColor0;
        public ICommand SetColor0 => _setColor0 ?? (_setColor0 = new RelayCommand(SetColor0Command));

        //Установить цвет записи 1 (красный)
        private ICommand _setColor1;
        public ICommand SetColor1 => _setColor1 ?? (_setColor1 = new RelayCommand(SetColor1Command));

        //Установить цвет записи 2 (желтый)
        private ICommand _setColor2;
        public ICommand SetColor2 => _setColor2 ?? (_setColor2 = new RelayCommand(SetColor2Command));

        //Установить цвет записи 3 (Зеленый)
        private ICommand _setColor3;
        public ICommand SetColor3 => _setColor3 ?? (_setColor3 = new RelayCommand(SetColor3Command));

        //Установить цвет записи 4 (синий)
        private ICommand _setColor4;
        public ICommand SetColor4 => _setColor4 ?? (_setColor4 = new RelayCommand(SetColor4Command));

        //Установить цвет записи 5 (фиолетовый)
        private ICommand _setColor5;
        public ICommand SetColor5 => _setColor5 ?? (_setColor5 = new RelayCommand(SetColor5Command));

        //Добавить Примечание
        private ICommand _noteLook;
        public ICommand NoteLook => _noteLook ?? (_noteLook = new RelayCommand(NoteLookCommand));

        //Редактировать сигнал
        private ICommand _signalEdit;
        public ICommand SignalEdit => _signalEdit ?? (_signalEdit = new RelayCommand(SignalEditCommand));

        //Переместить запись вверх
        private ICommand _moveRecordUp;
        public ICommand MoveRecordUp => _moveRecordUp ?? (_moveRecordUp = new RelayCommand(MoveRecordUpCommand));

        //Переместить запись вниз
        private ICommand _moveRecordDown;
        public ICommand MoveRecordDown => _moveRecordDown ?? (_moveRecordDown = new RelayCommand(MoveRecordDownCommand));
        #endregion
        #endregion

        #region Секция методов
        #region Секция методов меню Файл
        //Команда Файл.Новый
        private void NewFileCommand(object parametr)
        {
            //MessageBoxResult res1;
            //MessageBoxResult res2;
            if (viewRecords.Count > 0)
            {
                MessageBoxResult res1 = MessageBox.Show("Создать новый файл?", "Новый файл", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res1 != MessageBoxResult.Yes)
                    return;
                //Предложить сохранить текущий файл
                MessageBoxResult res2 = MessageBox.Show("Сохранить текущий файл?", "Сохранение файла", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res2 == MessageBoxResult.Yes)
                {
                    model.SaveToFile(currentFile);
                }
            }
            LoadedFile = "";

            model.GenerateModelTemplatet();
            ViewUpdate();
            //Предложить новый файл сохранить
        }

        //Команда Файл.Открыть
        private void OpenFileCommand(object parametr)
        {
            MessageBoxResult res1;
            //Сохранить текущий файл
            if (viewRecords.Count > 0)
            {
                res1 = MessageBox.Show("Сохранить текущий файл?", "Сохранение файла", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res1 == MessageBoxResult.Yes)
                {
                    model.SaveToFile(currentFile);
                }
            }
            FileDialog dialog = new OpenFileDialog();
            dialog.FileName = "NewNotepad"; //Имя файла по умолчанию
            dialog.DefaultExt = ".apn"; //Расширение по умолчанию
            dialog.Filter = "Блокнот AProject (.apn)|*.apn|Все файлы|*.*";
            if (dialog.ShowDialog() == true)
            {
                LoadedFile = dialog.FileName;
            }

            try
            {
                model.ReadFromFile(currentFile);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ошибка чтения из файла \n" + e.Message);
            }
            ViewUpdate();
            Properties.Settings.Default.LastFileName = currentFile;
            Properties.Settings.Default.Save();
        }

        //Команда Файл.Закрыть
        private void CloseFileCommand(object parametr)
        {
            model.Clear();
            LoadedFile = "";
            Properties.Settings.Default.LastFileName = currentFile;
            Properties.Settings.Default.Save();
            ViewUpdate();
        }

        //Команда Файл.Сохранить
        private void SaveFileCommand(object parametr)
        {
            try
            {
                model.SaveToFile(currentFile);
            }
            catch (Exception e)
            {
                MessageBox.Show("Ошибка записи в файл \n" + e.Message);
            }
        }

        //Команда Файл.СохранитьКак
        private void SaveAsFileCommand(object parametr)
        {
            FileDialog dialog = new SaveFileDialog();
            dialog.FileName = "NewNotepad"; //Имя файла по умолчанию
            dialog.DefaultExt = ".apn"; //Расширение по умолчанию
            dialog.Filter = "Блокнот AProject (.apn)|*.apn|Все файлы|*.*";
            if (dialog.ShowDialog() == true)
            {
                LoadedFile = dialog.FileName;
                model.SaveToFile(currentFile);
                Properties.Settings.Default.LastFileName = currentFile;
                Properties.Settings.Default.Save();
            }
            ViewUpdate();
        }

        //Файл.Экспорт HTML
        private void ExportHTMLCommand(object parameter)
        {
            exportViewModel = new ExportViewModel();
            exportViewModel.RaiseExportWindowClosedEvent += ExportWindowClosedEventHandler;
            exportVM = new Export();
            exportVM.DataContext = exportViewModel;
            exportMode = ExportMode.Html;
            exportVM.Show();
        }

        private Boolean IsCanExport(object parameter)
        {
            Boolean res;
            if (null == exportViewModel)
                res = true;
            else
                res = false;
            return res;
        }

        //Файл.Экспорт CSV
        private void ExportCSVCommand(object parameter)
        {
            exportViewModel = new ExportViewModel();
            exportViewModel.RaiseExportWindowClosedEvent += ExportWindowClosedEventHandler;
            exportVM = new Export();
            exportVM.DataContext = exportViewModel;
            exportMode = ExportMode.Csv;
            exportVM.Show();
        }

        //Файл.Выход
        private void ExitCommand(object parametr)
        {
            Application.Current.Shutdown();
        }
        #endregion

        #region Секция методов меню Правка
        private void UndoCommand(object parameter)
        {
            model.Undo();
            ViewUpdate();
            ProjectCountUpdate();
            RecordCountUpdate();
        }

        private Boolean IsCanUndo(object parameter)
        {
            Boolean res = model.UndoCanExecute();
            return res;
        }

        private void RedoCommand(object parameter)
        {
            model.Redo();
            ViewUpdate();
            ProjectCountUpdate();
            RecordCountUpdate();
        }

        private Boolean IsCanRedo(object parameter)
        {
            Boolean res = model.RedoCanExecute();
            return res;
        }

        private void CutCommand(object parameter)
        {
            Boolean projFlaf = false;
            Boolean jobFlag = false;
            Boolean actFlag = false;
            Int32 rowIndex;

            if (SelectedViewRecords.Count > 0)
            {
                foreach(ViewRecord r in SelectedViewRecords)
                {
                    switch (GetRecordType(r))
                    {
                        case RecordType.act:
                            actFlag = true;
                            break;
                        case RecordType.job:
                            jobFlag = true;
                            break;
                        case RecordType.project:
                            projFlaf = true;
                            break;
                        default:
                            Debug.Assert(true, "CutCommand(). Ошибка типа записи RecordType");
                            break;
                    }
                }
                if (projFlaf) //В выделенных строках имеется запись типа "Проект"
                {
                    MessageBox.Show("Вырезать запись проекта нельзя.\n Выберите записи типа \"Работа\" или \"Дело\"");
                }
                else if (jobFlag && actFlag) //Выделены только записи типа "Работа" и "Дело"
                {
                    MessageBox.Show("Нельзя одновременно вырезать записи типа \"Работа\" и \"Дело\".\n Выберите записи типа \"Работа\" или \"Дело\"");
                }
                else if (actFlag && !jobFlag) //Выделены только записи типа "Дело"
                {
                    rowIndex = viewRecords.IndexOf(SelectedViewRecords[0]);
                    model.CutActRecords(SelectedViewRecords);
                    ProjectCountUpdate();
                    RecordCountUpdate();
                    ViewUpdate();
                    SelectCellByIndex(mainDataGrid, rowIndex - 1, 3);
                }
                else //Выделены только записи типа "Работа"
                {
                    MessageBoxResult res1 = MessageBox.Show("Вырезать работу и все ее дела?", "Вырезать", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res1 == MessageBoxResult.Yes)
                    {
                        rowIndex = viewRecords.IndexOf(SelectedViewRecords[0]);
                        model.CutJobRecords(SelectedViewRecords);
                        ProjectCountUpdate();
                        RecordCountUpdate();
                        ViewUpdate();
                        SelectCellByIndex(mainDataGrid, rowIndex - 1, 3);
                    }
                }
            }
            else
                MessageBox.Show("Выберите записи!");
        }
        //TODO: Обеспечить правильную установку курсора при закрытии вспомогательного окна крестиком
        private void PasteCommand(object parameter)
        {
            Int32 rowIndex = viewRecords.IndexOf(SelectedViewRecords[0]) + 1;
            model.PasteRecords(SelectedViewRecords);
            ProjectCountUpdate();
            RecordCountUpdate();
            ViewUpdate();
            SelectCellByIndex(mainDataGrid, rowIndex, 3);
        }
        #endregion

        #region Секция методов меню Вид
        //Вид.Завершенные.Показать
        private void FinishedShowCommand(object parameter)
        {
            ViewWithFinished = true;
            ViewUpdate();
        }

        //Вид.Завершенные.Скрыть
        private void FinishedHideCommand(object parameter)
        {
            ViewWithFinished = false;
            ViewUpdate();
        }

        //Вид (кнопка).Завершенные Скрыть/Показать
        private void FinishedChangeModeCommand(object parameter)
        {
            ViewWithFinished = !viewWithFinished;
            ViewUpdate();
        }

        //Вид.Архивные проекты
        private void ArhShowCommand(object parameter)
        {
            ArchiveView = true;
            ViewUpdate();
        }

        //Вид.Активные проекты
        private void ArhHideCommand(object parameter)
        {
            ArchiveView = false;
            ViewUpdate();
        }

        //Вид (кнопка).Архивные переключение режима
        private void ArhChangeModeCommand(object parameter)
        {
            ArchiveView = !archiveView;
            ViewUpdate();
        }

        //Вид.Свернуть записи
        private void HideRecordsCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                model.HideRecord(SelectedViewRecords[0].Number);
                ViewUpdate();
            }
            else
                MessageBox.Show("Выберите запись!");
        }

        //Вид.Развернуть записи
        public void UnHideRecordsCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                model.UnHideRecord(SelectedViewRecords[0].Number);
                ViewUpdate();
            }
            else
                MessageBox.Show("Выберите запись!");
        }

        //Вид.Свернуть или развернуть строки (вызов кнопками в DataGrid)
        private void ChangedHideModeCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                if ("Hide" == parameter as String)
                {
                    model.HideRecord(SelectedViewRecords[0].Number);
                }
                else if ("Unhide" == parameter as String)
                {
                    model.UnHideRecord(SelectedViewRecords[0].Number);
                }
                else
                    Debug.Assert(true, "ChangedHideModeCommand. Ошибка параметра команды.");
                ViewUpdate();
            }
            else
                MessageBox.Show("Выберите запись!");
        }

        //Вид.Свернуть проекты
        private void HideProjectsCommand(object parameter)
        {
            model.Hide2Projects();
            ViewUpdate();
        }

        //Вид.Свернуть работы
        private void HideJobsCommand(object parameter)
        {
            model.Hide2Jobs();
            ViewUpdate();
        }

        //Вид.Показать цветовое выделение
        private void ShowColorSelectionCommand(object parameter)
        {
            ViewUpdateByColor(parameter as String);
        }
        #endregion

        #region Секция методов меню Записи
        //Создать проект
        private void ProjCreateCommand(object parameter)
        {
            model.CreateProject();
            ViewUpdate();
            ProjectCountUpdate();
            Int32 rowIndex = viewRecords.IndexOf(model.LastRowAdded);
            SelectCellByIndex(mainDataGrid, rowIndex, 3); //Установка выделения на ячейке DataGrid
        }

        //Создать работу
        private void JobCreateCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                model.CreateJob(SelectedViewRecords[0].Number);
                ViewUpdate();
                RecordCountUpdate();
                Int32 rowIndex = viewRecords.IndexOf(model.LastRowAdded);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                MessageBox.Show("Выберите проект для добавления работы!");
        }

        //Создать дело
        private void ActCreateCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                model.CreateAct(SelectedViewRecords[0].Number);
                ViewUpdate();
                RecordCountUpdate();
                Int32 rowIndex = viewRecords.IndexOf(model.LastRowAdded);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                MessageBox.Show("Выберите проект или работу для добавления дела!");
        }

        //Завершить Проект/Работу/Дело
        private void FinalizeCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                String message = "";
                Int32 rowIndex = viewRecords.IndexOf(selectedRow);
                RecordType recordType = GetRecordType(SelectedViewRecords[0]);
                switch (recordType)
                {
                    case RecordType.act:
                        message = "Завершить дело?";
                        break;
                    case RecordType.job:
                        message = "Завершить работу и все ее дела?";
                        break;
                    case RecordType.project:
                        message = "Завершить проект и все его работы?";
                        break;
                }
                MessageBoxResult mbResult = MessageBox.Show(message, "Завершение", MessageBoxButton.YesNo);
                if (mbResult == MessageBoxResult.No)
                {
                    return;
                }
                model.Finalize(SelectedViewRecords[0].Number);
                ViewUpdate();
                if (rowIndex >= viewRecords.Count - 1)
                {
                    rowIndex = viewRecords.Count - 1;
                }
                Int32 rowIndex2 = viewRecords.IndexOf(selectedRow);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                MessageBox.Show("Выберите для завершения \n  Проект / Работу / Дело!");
        }

        //Записи.Удалить запись
        private void DeleteRowCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];

                String message = "";
                RecordType recordType = GetRecordType(selectedRow);
                switch (recordType)
                {
                    case RecordType.act:
                        message = "Удалить дело?";
                        break;
                    case RecordType.job:
                        message = "Удалить работу и все ее дела?";
                        break;
                    case RecordType.project:
                        message = "Удалить проект и все его работы?";
                        break;
                }
                MessageBoxResult mbResult = MessageBox.Show(message, "Удаление", MessageBoxButton.YesNo);
                if (mbResult == MessageBoxResult.No)
                {
                    return;
                }

                Int32 rowIndex = viewRecords.IndexOf(selectedRow);
                model.DelRecord(selectedRow.Number);
                ViewUpdate();
                if (rowIndex < 0)
                {
                    rowIndex = 0;
                }
                else if (rowIndex > (viewRecords.Count - 1))
                {
                    rowIndex = viewRecords.Count - 1;
                }
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                MessageBox.Show("Выберите запись!");
        }

        private Boolean IsCanDeleteRow(object parameter)
        {
            return false;
        }

        //Записи.Снять завершение с Проект/Работу/Дело
        private void UnFinalizeCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                MessageBoxResult mbResult = MessageBox.Show("Снять завершение?", "Завершение", MessageBoxButton.YesNo);
                if (mbResult == MessageBoxResult.No)
                {
                    return;
                }
                model.UnFinalize(selectedRow.Number);
                ViewUpdate();
                Int32 rowIndex = viewRecords.IndexOf(selectedRow);
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
            else
                MessageBox.Show("Выберите запись!");
        }

        //Записи.Перенести проект в архив
        private void Proj2ArhCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                if (GetRecordType(selectedRow) != RecordType.project)
                {
                    MessageBox.Show("Выберите проект для архивирования!");
                    return;
                }
                MessageBoxResult mbResult = MessageBox.Show("Перенести проект в архив?", "Архивирование проекта", MessageBoxButton.YesNo);
                if (mbResult == MessageBoxResult.Yes)
                {
                    Int32 rowIndex = viewRecords.IndexOf(selectedRow) - 1;
                    model.Arhivate(SelectedViewRecords[0].Number);
                    ViewUpdate();
                    if (rowIndex < 0)
                        rowIndex = 0;
                    SelectCellByIndex(mainDataGrid, rowIndex, 3);
                }
            }
            else
                MessageBox.Show("Выберите проект!");
        }

        //Записи.Восстановить проект из архива
        private void ProjFromArhCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                if (GetRecordType(selectedRow) != RecordType.project)
                {
                    MessageBox.Show("Выберите проект для восстановления из архива!");
                    return;
                }
                MessageBoxResult mbResult = MessageBox.Show("Восстановить проект из архива?", "Восстановление из архива", MessageBoxButton.YesNo);
                if (mbResult == MessageBoxResult.Yes)
                {
                    model.UnArhivate(SelectedViewRecords[0].Number);
                    ArchiveView = false;
                    ViewUpdate();
                    Int32 rowIndex = viewRecords.IndexOf(selectedRow);
                    SelectCellByIndex(mainDataGrid, rowIndex, 3);
                }
            }
            else
                MessageBox.Show("Выберите проект!");

        }
        #endregion

        #region Секция методов меню Инструменты (Команды установки цвета фона)
        //Установить цвет записи 0 (основной)
        private void SetColor0Command(object parameter)
        {
            SetRowColor(0);
        }

        //Установить цвет записи 1 (красный)
        private void SetColor1Command(object parameter)
        {
            SetRowColor(1);
        }

        //Установить цвет записи 2 (желтый)
        private void SetColor2Command(object parameter)
        {
            SetRowColor(2);
        }

        //Установить цвет записи 3 (зеленый)
        private void SetColor3Command(object parameter)
        {
            SetRowColor(3);
        }

        //Установить цвет записи 4 (синий)
        private void SetColor4Command(object parameter)
        {
            SetRowColor(4);
        }

        //Установить цвет записи 5 (фиолетовый)
        private void SetColor5Command(object parameter)
        {
            SetRowColor(5);
        }

        //Добавить Примечание
        private void NoteLookCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                if (noteWindows.ContainsKey(selectedRow.Number)) //Окно для этой строки уже открыто
                {
                    noteWindows[selectedRow.Number].Topmost = true; //Выводим его на передний план
                }
                else //Окна для этой строки нет, создаем его
                {
                    NoteViewModel noteViewModel = new NoteViewModel(model, selectedRow.Number);
                    noteViewModel.RaiseNoteWindowClosedEvent += NoteWindowClosedEventHandler;
                    NoteWindow noteWindow = new NoteWindow();
                    noteWindow.Owner = Application.Current.MainWindow;
                    noteWindow.DataContext = noteViewModel;
                    noteWindows.Add(selectedRow.Number, noteWindow);
                    ViewUpdate();
                    Int32 rowIndex = GetIndexByNumber(selectedRow.Number);
                    SelectCellByIndex(mainDataGrid, rowIndex, 3);
                    noteWindow.Show();
                }
            }
            else
                MessageBox.Show("Выберите запись!");
        }

        //Редактировать сигнал
        private void SignalEditCommand(object parameter)
        {
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                if (signalWindows.ContainsKey(selectedRow.Number)) //Окно для этой строки уже открыто
                {
                    signalWindows[selectedRow.Number].Topmost = true; //Выводим его на передний план
                }
                else //Окна для этой строки нет, создаем его
                {
                    SignalViewModel signalViewModel = new SignalViewModel(model, selectedRow.Number);
                    signalViewModel.RaiseSignalWindowClosedEvent += SignalWindowClosedEventHandler;
                    SignalWindow signalWindow = new SignalWindow();
                    signalWindow.Owner = Application.Current.MainWindow;
                    signalWindow.DataContext = signalViewModel;
                    signalWindows.Add(selectedRow.Number, signalWindow);
                    ViewUpdate();
                    Int32 rowIndex = GetIndexByNumber(selectedRow.Number);
                    SelectCellByIndex(mainDataGrid, rowIndex, 3);
                    signalWindow.Show();
                }
            }
        }

        //Переместить запись вверх
        private void MoveRecordUpCommand(object parameter)
        {
            Boolean res = false;
            String number = "";
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                number = selectedRow.Number;
                res = model.MoveRecordUp(number);
            }
            else
                MessageBox.Show("Выберите запись!");
            if (res)
            {
                ViewUpdate();
                Int32 rowIndex = GetIndexByNumber(number) - 1;
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
        }

        //Переместить запись вниз
        private void MoveRecordDownCommand(object parameter)
        {
            Boolean res = false;
            String number = "";
            if (SelectedViewRecords.Count > 0)
            {
                selectedRow = SelectedViewRecords[0];
                number = selectedRow.Number;
                res = model.MoveRecordDown(number);
            }
            else
                MessageBox.Show("Выберите запись!");
            if (res)
            {
                ViewUpdate();
                Int32 rowIndex = GetIndexByNumber(number) + 1;
                SelectCellByIndex(mainDataGrid, rowIndex, 3);
            }
        }
        #endregion
        #endregion
    }
}
//TODO: Реализовать валидацию вводимых данных, см. Класс DataGrid
