using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AProjects
{
    class NoteViewModel : INotifyPropertyChanged
    {
        private Model model;
        private String rowNumber;
        private Record modelRecord;
        private String mainText;
        private String noteText;
        private String dateText;
        private String recordTypeText;
        //private NoteWindow noteWindow;


        #region Секция событий
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        //Событие закрытия окна редактирования примечания
        //Используется для обновления основного окна программы
        public event EventHandler<NoteWindowClosedEventArgs> RaiseNoteWindowClosedEvent;
        protected virtual void OnNoteWindowClosedEvent(NoteWindowClosedEventArgs e)
        {
            EventHandler<NoteWindowClosedEventArgs> handler = RaiseNoteWindowClosedEvent;
            if (handler != null)
                handler(this, e);
        }
        #endregion

        public NoteViewModel(Model model, String rowNumber)
        {
            this.model = model;
            this.rowNumber = rowNumber;
            InitWindowProperty();
        }

        /// <summary>
        /// Инициализация окна редактирования примечаний
        /// Извлекает данные из модели и заносит в поля для отображения во View
        /// </summary>
        private void InitWindowProperty()
        {
            modelRecord = model.GetSingleRecord(rowNumber);
            mainText = modelRecord.Content;
            noteText = modelRecord.Note;
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

        public String NoteText
        {
            get => noteText;
            set
            {
                this.noteText = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("NoteText"));
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
                this.RecordTypeText = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("RecordTypeText"));
                }
            }
        }
        #endregion

        #region Секция команд
        //Очистить заметку
        private ICommand _clearNote;
        public ICommand ClearNote => _clearNote ?? (_clearNote = new RelayCommand(ClearNoteCommand));

        //Сохранить заметку
        private ICommand _saveNote;
        public ICommand SaveNote => _saveNote ?? (_saveNote = new RelayCommand(SaveNoteCommand));

        #endregion

        #region Секция методов
        //Очистить заметку
        private void ClearNoteCommand(object parameter)
        {
            model.SetNote(rowNumber, "");
            NoteWindowClosedEventArgs e =  new NoteWindowClosedEventArgs(rowNumber);
            OnNoteWindowClosedEvent(e);
        }

        //Сохранить заметку
        private void SaveNoteCommand(object parameter)
        {
            model.SetNote(rowNumber, noteText);
            NoteWindowClosedEventArgs e = new NoteWindowClosedEventArgs(rowNumber);
            OnNoteWindowClosedEvent(e);
        }
        #endregion
    }
}
