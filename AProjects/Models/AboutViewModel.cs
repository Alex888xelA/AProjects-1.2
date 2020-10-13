using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace AProjects
{
    class AboutViewModel
    {

        public event EventHandler RaiseAboutWindowClosedEvent;
        protected virtual void OnAboutWindowClosedEvent(EventArgs e)
        {
            EventHandler handler = RaiseAboutWindowClosedEvent;
            if (handler != null)
                handler(this, e);
        }

        public AboutViewModel()
        {

        }

        //Команда кнопки "Закрыть" окно
        private ICommand _closeAbout;
        public ICommand CloseAbout => _closeAbout ?? (_closeAbout = new RelayCommand(CloseAboutCommand));


        private void CloseAboutCommand(object parameter)
        {
            if(null != RaiseAboutWindowClosedEvent)
            {
                this.RaiseAboutWindowClosedEvent(this, new EventArgs());
            }
        }
    }
}
