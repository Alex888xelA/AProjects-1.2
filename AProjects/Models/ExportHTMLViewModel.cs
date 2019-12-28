﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AProjects
{
    class ExportHTMLViewModel : INotifyPropertyChanged
    {
        private Boolean exportSelectedRecords;
        private Boolean exportActiveRecords;
        private Boolean exportCollapsedRecords;
        private Boolean exportFinishedRecords;
        private Boolean exportArhRecords;
        private Boolean exportColors;
        private Boolean exportNotes;
        private Boolean exportAlarms;
        private Boolean notExportSelectedRecords;

        //private Model model;
        //private SpecialObservableCollection<ViewRecord> viewRecords;
        //private List<ViewRecord> selectedViewRecords;

        public ExportHTMLViewModel()
        {
            //this.model = model;
            //this.viewRecords = viewRecords;
            //this.selectedViewRecords = selectedViewRecords;
            ExportSelectedRecords = false;
            ExportActiveRecords = true;
            ExportCollapsedRecords = true;
            ExportFinishedRecords = false;
            ExportArhRecords = false;
            ExportColors = true;
            ExportNotes = false;
            ExportAlarms = false;
    }

    #region Секция событий
    public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] String prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        //Событие закрытия окна Экспорта в HTML
        //Используется для закрытия окна из ViewModel
        public event EventHandler<ExportHTMLWindowEventArgs> RaiseExportHTMLWindowClosedEvent;
        protected virtual void OnExportHTMLWindowClosedEvent(ExportHTMLWindowEventArgs e)
        {
            EventHandler<ExportHTMLWindowEventArgs> handler = RaiseExportHTMLWindowClosedEvent;
            if (handler != null)
                handler(this, e);
        }
        #endregion

        #region Секция полей
        public Boolean ExportSelectedRecords
        {
            get => exportSelectedRecords;
            set
            {
                this.exportSelectedRecords = value;
                NotExportSelectedRecords = !value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportSelectedRecords"));
                }
            }
        }

        public Boolean ExportActiveRecords
        {
            get => exportActiveRecords;
            set
            {
                this.exportActiveRecords = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportActiveRecords"));
                }
            }
        }

        public Boolean ExportCollapsedRecords
        {
            get => exportCollapsedRecords;
            set
            {
                this.exportCollapsedRecords = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportCollapsedRecords"));
                }
            }
        }

        public Boolean ExportFinishedRecords
        {
            get => exportFinishedRecords;
            set
            {
                this.exportFinishedRecords = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportFinishedRecords"));
                }
            }
        }

        public Boolean ExportArhRecords
        {
            get => exportArhRecords;
            set
            {
                this.exportArhRecords = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportArhRecords"));
                }
            }
        }

        public Boolean ExportColors
        {
            get => exportColors;
            set
            {
                this.exportColors = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportColors"));
                }
            }
        }

        public Boolean ExportNotes
        {
            get => exportNotes;
            set
            {
                this.exportNotes = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportNotes"));
                }
            }
        }

        public Boolean ExportAlarms
        {
            get => exportAlarms;
            set
            {
                this.exportAlarms = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("ExportAlarms"));
                }
            }
        }

        public Boolean NotExportSelectedRecords
        {
            get => notExportSelectedRecords;
            set
            {
                this.notExportSelectedRecords = value;
                if (null != PropertyChanged)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("NotExportSelectedRecords"));
                }
            }
        }

        #endregion

        #region Секция команд
        private ICommand _cancelButton;
        public ICommand CancelButton => _cancelButton ?? (_cancelButton = new RelayCommand(CancelButtonCommand));

        private ICommand _exportButton;
        public ICommand ExportButton => _exportButton ?? (_exportButton = new RelayCommand(ExportButtonCommand));
        #endregion

        #region Секция методов
        private void CancelButtonCommand(object parameter)
        {
            if (null != RaiseExportHTMLWindowClosedEvent)
            {
                Dictionary<String, Boolean> exportSettings = new Dictionary<string, Boolean>();
                this.RaiseExportHTMLWindowClosedEvent(this, new ExportHTMLWindowEventArgs(exportSettings));
            }
        }

        private void ExportButtonCommand(object parameter)
        {
            Dictionary<String, Boolean> exportSettings = new Dictionary<string, Boolean>(); //Записали значения параметров экспорта в коллекцию
            //FileDialog dialog = new SaveFileDialog();
            //dialog.FileName = "AProjects"; //Имя файла по умолчанию
            //dialog.DefaultExt = ".html"; //Расширение по умолчанию
            //dialog.Filter = "Веб-страница (.html)|*.html|Все файлы|*.*";
            //if (dialog.ShowDialog() == true)
            //{
                //String fileName = dialog.FileName; //Получили имя файла из диалога
                exportSettings.Add("ExportSelectedRecords", ExportSelectedRecords);
                exportSettings.Add("ExportActiveRecords", ExportActiveRecords);
                exportSettings.Add("ExportCollapsedRecords", ExportCollapsedRecords);
                exportSettings.Add("ExportFinishedRecords", ExportFinishedRecords);
                exportSettings.Add("ExportArhRecords", ExportArhRecords);
                exportSettings.Add("ExportColors", ExportColors);
                exportSettings.Add("ExportNotes", ExportNotes);
                exportSettings.Add("ExportAlarms", ExportAlarms);
            //}
            this.RaiseExportHTMLWindowClosedEvent(this, new ExportHTMLWindowEventArgs(exportSettings));
        }
        #endregion
    }
}
