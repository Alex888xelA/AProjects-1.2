using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace AProjects
{
    class SignalViewModel : INotifyPropertyChanged
    {
        private Model model;
        private String rowNumber;
        private Record modelRecord;
        private String mainText;
        private String dateText;
        private String recordTypeText;
        private String signalString;
        private AlarmRepit alarmRepit;
        private Int32 hourSelectedIndex;
        private Int32 minuteSelectedIndex;
        DateTime dateTime;
        private Boolean[] repeatIndex;
        private Boolean[] REPEATINDEXCLEAR = new Boolean[5] { false, false, false, false, false };
        private Nullable<DateTime> selectedDT;
        private Boolean activeAlarm; //Признак, что окно инициализировано при срабатывании будильника

        #region Секция событий
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        //Событие закрытия окна редактирования сигнала
        //Используется для обновления основного окна программы
        public event EventHandler<SignalWindowClosedEventArgs> RaiseSignalWindowClosedEvent;
        protected virtual void OnSignalWindowClosedEvent(SignalWindowClosedEventArgs e)
        {
            EventHandler<SignalWindowClosedEventArgs> handler = RaiseSignalWindowClosedEvent;
            if (handler != null)
                handler(this, e);
        }


        private void hourList_PropertyChanged(Object sender, PropertyChangedEventArgs e)
        {
            //System.Windows.MessageBox.Show("\n Name = " + e.PropertyName); //sender.GetType().GetProperties()[0].GetValue()

        }

        #endregion

        public SignalViewModel(Model model, String rowNumber, Boolean alarmFlag = false)
        {
            this.model = model;
            this.rowNumber = rowNumber;
            this.ActiveAlarm = alarmFlag;
            modelRecord = model.GetSingleRecord(rowNumber);
            mainText = modelRecord.Content;
            String[] data = modelRecord.RecordDate.Split(new char[] { ' ' });
            dateText = data[0];
            switch (modelRecord.RecordType)
            {
                case RecordType.project:
                    recordTypeText = "Проект:";
                    break;
                case RecordType.job:
                    recordTypeText = "Работа:";
                    break;
                case RecordType.act:
                    recordTypeText = "Дело:";
                    break;
                default:
                    Debug.Assert(false, "NoteViewModel.InitWindow(). Ошибка типа записи.");
                    break;
            }
            signalString = modelRecord.AlarmTime;
            alarmRepit = modelRecord.AlarmRepit;
            SetTime();
        }

        private void SetTime()
        {
            if (signalString == "")
            {
                dateTime = DateTime.Now;
                selectedDT = DateTime.Now;
            }
            else
            {
                dateTime = DateTime.Parse(signalString);
            }
            HourSelectedIndex = dateTime.Hour;
            MinuteSelectedIndex = dateTime.Minute;
            RepeatIndex = REPEATINDEXCLEAR;
            switch (alarmRepit)
            {
                case AlarmRepit.no:
                    RepeatIndex[0] = true;
                    break;
                case AlarmRepit.day:
                    RepeatIndex[1] = true;
                    break;
                case AlarmRepit.week:
                    RepeatIndex[2] = true;
                    break;
                case AlarmRepit.month:
                    RepeatIndex[3] = true;
                    break;
                case AlarmRepit.year:
                    RepeatIndex[4] = true;
                    break;
            }
            selectedDT = dateTime.Date;
        }

        #region Секция полей
        public String MainText
        {
            get => mainText;
            set
            {
                this.mainText = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("MainText"));
                }
            }
        }

        public String RowNumber
        {
            get => rowNumber;
            set
            {
                this.rowNumber = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("RowNumber"));
                }
            }
        }

        public String DateText
        {
            get => dateText;
            set
            {
                this.dateText = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("DateText"));
                }
            }
        }

        public String RecordTypeText
        {
            get => recordTypeText;
            set
            {
                this.recordTypeText = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("RecordTypeText"));
                }
            }
        }

        public Int32 HourSelectedIndex
        {
            get => hourSelectedIndex;
            set
            {
                this.hourSelectedIndex = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("HourSelectedIndex"));
                }
            }
        }

        public Int32 MinuteSelectedIndex
        {
            get => minuteSelectedIndex;
            set
            {
                this.minuteSelectedIndex = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("MinuteSelectedIndex"));
                }
            }
        }

        public Boolean[] RepeatIndex
        {
            get => repeatIndex;
            set
            {
                this.repeatIndex = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("RepeatIndex"));
                }
            }
        }

        public Nullable<DateTime> SelectedDT
        {
            get => selectedDT;
            set
            {
                this.selectedDT = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("SelectedDT"));
                }
            }
        }

        public Boolean ActiveAlarm
        {
            get => activeAlarm;
            set
            {
                this.activeAlarm = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ActiveAlarm"));
                }
            }
        }
        #endregion

        #region Секция команд
        //Отмена
        private ICommand _signalUndo;
        public ICommand SignalUndo => _signalUndo ?? (_signalUndo = new RelayCommand(SignalUndoCommand));

        //Удалить
        private ICommand _signalDel;
        public ICommand SignalDel => _signalDel ?? (_signalDel = new RelayCommand(SignalDelCommand));

        //Сохранить
        private ICommand _signalSave;
        public ICommand SignalSave => _signalSave ?? (_signalSave = new RelayCommand(SignalSaveCommand));

        //Повтор - Нет
        private ICommand _signalRepeatNo;
        public ICommand SignalRepeatNo => _signalRepeatNo ?? (_signalRepeatNo = new RelayCommand(SignalRepeatNoCommand));

        //Повтор - день
        private ICommand _signalRepeatDay;
        public ICommand SignalRepeatDay => _signalRepeatDay ?? (_signalRepeatDay = new RelayCommand(SignalRepeatDayCommand));

        //Повтор - неделя
        private ICommand _signalRepeatWeek;
        public ICommand SignalRepeatWeek => _signalRepeatWeek ?? (_signalRepeatWeek = new RelayCommand(SignalRepeatWeekCommand));

        //Повтор - Месяц
        private ICommand _signalRepeatMonth;
        public ICommand SignalRepeatMonth => _signalRepeatMonth ?? (_signalRepeatMonth = new RelayCommand(SignalRepeatMonthCommand));

        //Повтор - Год
        private ICommand _signalRepeatYear;
        public ICommand SignalRepeatYear => _signalRepeatYear ?? (_signalRepeatYear = new RelayCommand(SignalRepeatYearCommand));
        #endregion

        #region Секция методов
        //Отмена
        private void SignalUndoCommand(object parameter)
        {
            SignalWindowClosedEventArgs e = new SignalWindowClosedEventArgs(rowNumber);
            OnSignalWindowClosedEvent(e);
        }

        //Удалить
        private void SignalDelCommand(object parameter)
        {
            model.SetSignal(rowNumber, "", AlarmRepit.no);
            SignalWindowClosedEventArgs e = new SignalWindowClosedEventArgs(rowNumber);
            OnSignalWindowClosedEvent(e);
        }

        //Сохранить
        private void SignalSaveCommand(object parameter)
        {
            DateTime resDate;
            DateTime resDT;
            AlarmRepit resAlarmRepit = AlarmRepit.year;
            if (null != selectedDT)
            {
                resDate = selectedDT.Value;
            }
            else
            {
                resDate = DateTime.Now;
                Debug.Assert(true, "SignalViewModel.SignalSaveCommand(). Ошибка занчения DateTime");
            }
                
            resDT = resDate.Date;
            DateTime resDT_H = resDT.AddHours((Double)hourSelectedIndex);
            DateTime resDT_HM = resDT_H.AddMinutes((Double)minuteSelectedIndex);
            DateTime resDT_HMR = DateTime.Now;
            if (repeatIndex[0])
            {
                resAlarmRepit = AlarmRepit.no;
            }
            else if (repeatIndex[1])
            {
                resAlarmRepit = AlarmRepit.day;
            }
            else if (repeatIndex[2])
                {
                    resAlarmRepit = AlarmRepit.week;
                }
            else if (repeatIndex[3])
            {
                resAlarmRepit = AlarmRepit.month;
            }
            if (resDT_HM < DateTime.Now && resAlarmRepit != AlarmRepit.no)
            {
                switch (resAlarmRepit)
                {
                    case AlarmRepit.day:
                        resDT_HMR = resDT_HM.AddDays(1);
                        break;
                    case AlarmRepit.week:
                        resDT_HMR = resDT_HM.AddDays(7);
                        break;
                    case AlarmRepit.month:
                        resDT_HMR = resDT_HM.AddMonths(1);
                        break;
                    case AlarmRepit.year:
                        resDT_HMR = resDT_HM.AddYears(1);
                        break;
                }
            }
            else
            {
                resDT_HMR = resDT_HM;
            }
            model.SetSignal(rowNumber, resDT_HMR.ToString(), resAlarmRepit);
            SignalWindowClosedEventArgs e = new SignalWindowClosedEventArgs(rowNumber);
            OnSignalWindowClosedEvent(e);
        }

        //Повтор - Нет
        private void SignalRepeatNoCommand(object parameter)
        {
            RepeatIndex = REPEATINDEXCLEAR;
            RepeatIndex[0] = true;
        }

        //Повтор - день
        private void SignalRepeatDayCommand(object parameter)
        {
            RepeatIndex = REPEATINDEXCLEAR;
            RepeatIndex[1] = true;
        }

        //Повтор - неделя
        private void SignalRepeatWeekCommand(object parameter)
        {
            RepeatIndex = REPEATINDEXCLEAR;
            RepeatIndex[2] = true;
        }

        //Повтор - Месяц
        private void SignalRepeatMonthCommand(object parameter)
        {
            RepeatIndex = REPEATINDEXCLEAR;
            RepeatIndex[3] = true;
        }

        //Повтор - Год
        private void SignalRepeatYearCommand(object parameter)
        {
            RepeatIndex = REPEATINDEXCLEAR;
            RepeatIndex[4] = true;
        }
        #endregion
    }
}
