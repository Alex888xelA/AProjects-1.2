using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows;

namespace AProjects
{
    /// <summary>
    /// Модель структуры данных
    /// Класс соответствует одной строке таблицы данных
    /// </summary>
    public class Record : INotifyPropertyChanged, IComparable<Record>, IEquatable<Record>
    {
        //private Int32 _key; //Ключ записи
        private Int32 _projNumber; //Номер проекта
        private Int32 _jobNumber; //Номер работы
        private Int32 _actNumber; //Номер дела
        private String _recordDate; //Дата записи
        private String _content; //Запись
        private String _alarmTime; //Дата и время срабатывания будильника
        private AlarmRepit _alarmRepit; //Признак повтора будильника
        private String _note; //Примечание
        private RecordType _recordType; //Тип записи - проект/работа/дело
        private RecordStatus _recordStatus; //Статус записи - активный/завершен/архив
        private Int32 _fill; //Индекс заливки
        private Boolean _hide; //Признак: False - показать, True - скрыть

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(String prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        //public Record()
        //{
        //    _projNumber = 0;
        //    _jobNumber = 0;
        //    _actNumber = 0;
        //    _recordDate = "";
        //    _content = "";
        //    _alarmTime = "";
        //    _alarmRepit = AlarmRepit.no;
        //    _note = "";
        //    _recordType = RecordType.project;
        //    _recordStatus = RecordStatus.active;
        //    _fill = 0;
        //    _hide = false;
        //}

        #region //Определение полей
        public Int32 ProjNumber
        {
            get { return _projNumber; }
            set
            {
                _projNumber = value;
                OnPropertyChanged("ProjNumber");
            }
        }

        public Int32 JobNumber
        {
            get { return _jobNumber; }
            set
            {
                _jobNumber = value;
                OnPropertyChanged("JobNumber");
            }
        }

        public Int32 ActNumber
        {
            get { return _actNumber; }
            set
            {
                _actNumber = value;
                OnPropertyChanged("ActNumber");
            }
        }

        public String RecordDate
        {
            get { return _recordDate; }
            set
            {
                _recordDate = value;
                OnPropertyChanged("RecordDate");
            }
        }

        public String Content
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged("Content");
            }
        }

        public String AlarmTime
        {
            get { return _alarmTime; }
            set
            {
                _alarmTime = value;
                OnPropertyChanged("AlarmTime");
            }
        }

        public AlarmRepit AlarmRepit
        {
            get { return _alarmRepit; }
            set
            {
                _alarmRepit = value;
                OnPropertyChanged("AlarmRepit");
            }
        }

        public String Note
        {
            get { return _note; }
            set
            {
                _note = value;
                OnPropertyChanged("Note");
            }
        }

        public RecordType RecordType
        {
            get { return _recordType; }
            set
            {
                _recordType = value;
                OnPropertyChanged("RecordType");
            }
        }

        public RecordStatus RecordStatus
        {
            get { return _recordStatus; }
            set
            {
                _recordStatus = value;
                OnPropertyChanged("RecordStatus");
            }
        }

        public Int32 Fill
        {
            get { return _fill; }
            set
            {
                _fill = value;
                OnPropertyChanged("Fill");
            }
        }

        public Boolean Hide
        {
            get { return _hide; }
            set
            {
                _hide = value;
                OnPropertyChanged("Hide");
            }
        }
        #endregion

        #region Реализация IComparable<>
        public Int32 CompareTo(Record other)
        {
            if (other == null)
            { return 1; }
            if (_projNumber == other.ProjNumber && _jobNumber == other.JobNumber && _actNumber == other.ActNumber)
            { return 0; }
            if (_projNumber > other.ProjNumber)
            { return 1; }
            else if (_projNumber < other.ProjNumber)
            { return -1; }
            if (_jobNumber > other.JobNumber)
            { return 1; }
            else if (_jobNumber < other.JobNumber)
            { return -1; }
            if (_actNumber > other.ActNumber)
            { return 1; }
            else return -1;
        }

        public static bool operator >(Record rec1, Record rec2)
        {
            return rec1.CompareTo(rec2) == 1;
        }

        public static bool operator <(Record rec1, Record rec2)
        {
            return rec1.CompareTo(rec2) == -1;
        }

        public static bool operator >=(Record rec1, Record rec2)
        {
            return rec1.CompareTo(rec2) >= 0;
        }

        public static bool operator <=(Record rec1, Record rec2)
        {
            return rec1.CompareTo(rec2) <= 0;
        }
        #endregion

        #region Реализация IEquatable<>
        public bool Equals(Record obj)
        {
            if ((obj == null || !this.GetType().Equals(obj.GetType())))
            {
                return false;
            }
            Record rec = (Record)obj;
            if (this.CompareTo((Record)obj) != 0)
            {
                return false;
            }
            if (_recordDate != rec.RecordDate ||
                _content != rec.Content ||
                _alarmTime != rec.AlarmTime ||
                _alarmRepit != rec.AlarmRepit ||
                _note != rec.Note ||
                _recordType != rec.RecordType ||
                _recordStatus != rec.RecordStatus ||
                _fill != rec.Fill ||
                _hide != rec.Hide)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return (_projNumber.ToString() +
                _jobNumber.ToString() +
                _actNumber.ToString() +
                _recordDate +
                _content +
                _alarmTime +
                _alarmRepit.ToString() +
                _note +
                _recordType.ToString() +
                _recordStatus.ToString() +
                _fill.ToString() +
                _hide.ToString()).GetHashCode();
        }
        #endregion

        #region Реализация Clone()
        public Record Clone()
        {
            Record clone = new Record();
            clone.ProjNumber = this.ProjNumber;
            clone.JobNumber = this.JobNumber;
            clone.ActNumber = this.ActNumber;
            clone.RecordDate = this.RecordDate;
            clone.Content = this.Content;
            clone.AlarmTime = this.AlarmTime;
            clone.AlarmRepit = this.AlarmRepit;
            clone.Note = this.Note;
            clone.RecordType = this.RecordType;
            clone.RecordStatus = this.RecordStatus;
            clone.Fill = this.Fill;
            clone.Hide = this.Hide;
            return clone;
        }
        #endregion
    }

    public enum AlarmRepit { no, day, week, month, year }
    public enum RecordType { project, job, act }
    public enum RecordStatus { active, finished, arx }
    public enum ExportMode { Html, Csv }

    public class ViewRecord : INotifyPropertyChanged, IComparable<ViewRecord>, IEquatable<ViewRecord>
    {
        private String _number; //Номер проекта_Дела_Работы P.J.A
        private String _recordDate; //Дата записи
        private String _content; //Запись
        private String _alarmTime; //Дата и время срабатывания будильника
        private String _note; //Примечание
        private Int32 _visualStyle; //Предназначен для выбора стиля оформления строки в DataGrid
        private Boolean _expandIcon; //Признак отображения иконки раскрытия группы строк. False - отсутствует, True - присутствует

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(String prop = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #region Определение полей
        public String Number
        {
            get { return _number; }
            set
            {
                _number = value;
                OnPropertyChanged(_number);
            }
        }

        public String RecordDate
        {
            get { return _recordDate; }
            set
            {
                _recordDate = value;
                OnPropertyChanged(_number);
            }
        }

        public String Content
        {
            get { return _content; }
            set
            {
                _content = value;
                OnPropertyChanged(_number);
            }
        }

        public String AlarmTime
        {
            get { return _alarmTime; }
            set
            {
                _alarmTime = value;
                OnPropertyChanged(_number);
            }
        }

        public String Note
        {
            get { return _note; }
            set
            {
                _note = value;
                OnPropertyChanged(_number);
            }
        }

        public Int32 VisualStyle
        {
            get { return _visualStyle; }
            set
            {
                _visualStyle = value;
                OnPropertyChanged(_visualStyle.ToString());
            }
        }

        public Boolean ExpandIcon
        {
            get { return _expandIcon; }
            set
            {
                _expandIcon = value;
                OnPropertyChanged(_expandIcon.ToString());
            }
        }
        #endregion

        #region Реализация IComparable<>
        public Int32 CompareTo(ViewRecord other)
        {
            if (other == null)
            { return 1; }
            if (_number == other.Number)
            { return 0; }
            String[] otherSrting = other.Number.Split('.');
            String[] number = _number.Split('.');
            //Отладочный ----------------------------------------------------------------------
            if (otherSrting.Count() != 3 || number.Count() != 3)
            {
                MessageBox.Show("Ошибка разбиения Number в CompareTo(ViewRecord other) \n otherSrting.Count() = " + otherSrting.Count().ToString() + "\n number.Count() = " + number.Count().ToString());
            }
            //-------------------------------------------------------------------------------
            Int32[] otherInt = new Int32[3];
            Int32[] numberInt = new Int32[3];
            for (int i = 0; i < otherSrting.Count(); i++)
            {
                otherInt[i] = Int32.Parse(otherSrting[i]);
                numberInt[i] = Int32.Parse(number[i]);
            }

            if (numberInt[0] > otherInt[0])
            { return 1; }
            else if (numberInt[0] < otherInt[0])
            { return -1; }
            if (numberInt[1] > otherInt[1])
            { return 1; }
            else if (numberInt[1] < otherInt[1])
            { return -1; }
            if (numberInt[2] > otherInt[2])
            { return 1; }
            else return -1;
        }

        public static bool operator >(ViewRecord rec1, ViewRecord rec2)
        {
            return rec1.CompareTo(rec2) == 1;
        }

        public static bool operator <(ViewRecord rec1, ViewRecord rec2)
        {
            return rec1.CompareTo(rec2) == -1;
        }

        public static bool operator >=(ViewRecord rec1, ViewRecord rec2)
        {
            return rec1.CompareTo(rec2) >= 0;
        }

        public static bool operator <=(ViewRecord rec1, ViewRecord rec2)
        {
            return rec1.CompareTo(rec2) <= 0;
        }
        #endregion

        #region Реализация IEquatable<>
        public bool Equals(ViewRecord obj)
        {
            if ((obj == null || !this.GetType().Equals(obj.GetType())))
            {
                return false;
            }
            ViewRecord rec = (ViewRecord)obj;
            if (this.CompareTo((ViewRecord)obj) != 0)
            {
                return false;
            }
            if (_number != rec.Number ||
                _recordDate != rec.RecordDate ||
                _content != rec.Content ||
                _alarmTime != rec.AlarmTime ||
                _note != rec.Note)
            {
                return false;
            }
            return true;
        }

        public override int GetHashCode()
        {
            return (_number + _recordDate + _content + _alarmTime + _note).GetHashCode();
        }
        #endregion
    }

    public struct UndoAction
    {
        public Record RecordData;
        public ActionType UndoActionType;

        public UndoAction(ActionType actionType, Record recordData)
        {
            UndoActionType = actionType;
            RecordData = recordData;
        }
    }

    public enum ActionType { addRecord, delRecord, editRecord }

    public class IOData
    {
        public List<Record> data;
        public IOData()
        {
             data = new List<Record>();
        }
    }

    public class Alarm
    {
        public String dateTime;
        public String recordNumber;
    }
}
