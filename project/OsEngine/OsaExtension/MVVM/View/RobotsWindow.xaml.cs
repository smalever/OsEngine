using OsEngine.Market;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using OsEngine.OsaExtension.MVVM.ViewModels;
using MahApps.Metro.Controls;

namespace OsEngine.OsaExtension.MVVM.View
{
    /// <summary>
    /// Логика взаимодействия для RobotsWindow.xaml
    /// </summary>
    public partial class RobotsWindow : MetroWindow
    {
        public static Dispatcher Dispatcher;
        public RobotsWindow()
        {
            Process ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            InitializeComponent();

            Dispatcher = Dispatcher.CurrentDispatcher;

            MainWindow.ProccesIsWorked = true;

            ServerMaster.ActivateLogging();
            this.Closed += RobotWindow_Closed; //событие закрытия окна
            DataContext = new RobotsWindowVM();
        }
        /// <summary>
        /// закрываем все рабочие процессы осы
        /// </summary>
        private void RobotWindow_Closed(object sender, EventArgs e)
        {
            MainWindow.ProccesIsWorked = false;
            Thread.Sleep(7000);
            Process.GetCurrentProcess().Kill();
        }        

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
