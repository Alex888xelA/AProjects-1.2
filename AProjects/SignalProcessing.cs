using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace AProjects
{
    /// <summary>
    /// Обработка будильников (таймер)
    /// </summary>
    class SignalProcessing
    {
        AlarmDT currentDT; //Дата и время, для которого запущен таймер
        private DispatcherTimer signalTimer;

        public SignalProcessing()
        {
            signalTimer = new DispatcherTimer();
            signalTimer.Tick += new EventHandler(signalTimer_Tick);
            //signalTimer.Interval = new TimeSpan(0, 0, 10);
            //signalTimer.Start();
        }

        //Событие срабатывания таймера 
        //Используется для обновления передачи сигнала во ViewModel
        public event EventHandler<AlarmTimerTickEventArgs> RaiseAlarmTimerTickEvent;
        protected virtual void OnAlarmTimerTickEvent(AlarmTimerTickEventArgs e)
        {
            EventHandler<AlarmTimerTickEventArgs> handler = RaiseAlarmTimerTickEvent;
            if (handler != null)
                handler(this, e);
        }

        private void signalTimer_Tick(object sender, EventArgs e)
        {
            //MessageBox.Show("Сработал таймер");
            AlarmTimerTickEventArgs arg = new AlarmTimerTickEventArgs(currentDT.recordNumber);
            OnAlarmTimerTickEvent(arg);
        }

        public void UpdateTimers(List<Alarm> alarms)
        {
            if (alarms.Count > 0) //Будильник имеются, проверяем и перезапускаем таймер
            {
                DateTime dT;
                List<AlarmDT> alarmList = new List<AlarmDT>();
                foreach (Alarm alarm in alarms) //Если будильник не просрочен, заносим его в alarmList
                {
                    dT = DateTime.Parse(alarm.dateTime);
                    if (dT > DateTime.Now)
                    {
                        AlarmDT alarmDT = new AlarmDT();
                        alarmDT.dateTime = dT;
                        alarmDT.recordNumber = alarm.recordNumber;
                        alarmList.Add(alarmDT);
                    }
                }
                if (alarmList.Count > 0)
                {
                    AlarmDT minAlarmDT = alarmList[0];
                    foreach (AlarmDT alarmDTRecord in alarmList) //Ищем запись с минимальной задержкой срабатывания
                    {
                        if (alarmDTRecord.dateTime < minAlarmDT.dateTime)
                        {
                            minAlarmDT = alarmDTRecord;
                        }
                    }
                    currentDT = minAlarmDT;
                    TimerRun(currentDT.dateTime);
                }
                else
                    TimerStop(); //Отсутствуют записи с непросроченными будильниками
            }
            else //Будильников нет, останавливаем таймер
            {
                TimerStop();
            }
        }

        private void TimerRun(DateTime dateTime)
        {
            if (null != dateTime && DateTime.Now < dateTime)
            {
                TimeSpan timeSpan = new TimeSpan();
                timeSpan = dateTime - DateTime.Now;
                signalTimer.Interval = timeSpan;
                signalTimer.Start();
            }
        }

        private void TimerStop()
        {
            signalTimer.Stop();
        }

        private class AlarmDT
        {
            public DateTime dateTime;
            public String recordNumber;
        }
    }
}
