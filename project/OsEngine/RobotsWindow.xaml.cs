using OsEngine.Market;
using System;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows;
using OsEngine.OsaExtension.MVVM.ViewModels;
using MahApps.Metro.Controls;
using Serilog;
using System.IO;
using Serilog.Formatting.Compact;

namespace OsEngine.OsaExtension.MVVM.View
{
    /// <summary>
    /// Логика взаимодействия для RobotsWindow.xaml
    /// </summary>
    public partial class RobotsWindow : MetroWindow
    {
        /// <summary>
        /// поле логера RobotsWindow       
        /// </summary>
        ILogger _logger;

        public static Dispatcher Dispatcher;
        public RobotsWindow()
        {
            Process ps = Process.GetCurrentProcess();
            ps.PriorityClass = ProcessPriorityClass.RealTime;

            InitializeComponent();

            // загружаем логер в стат свойство
            Log.Logger = BilderLogger();
            _logger = Log.Logger.ForContext<RobotsWindow>();

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
            _logger.Information("Method {Method}", nameof(RobotWindow_Closed));

            MainWindow.ProccesIsWorked = false;
            Thread.Sleep(7000);
            Process.GetCurrentProcess().Kill();
        }

        private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        /// <summary>
        /// создает Логер 
        /// </summary>
        private ILogger BilderLogger()
        {
            if (!Directory.Exists(@"Logs"))
            {
                Directory.CreateDirectory(@"Logs");
            }

            DateTime dateTime = DateTime.Now;

            ILogger logger = new LoggerConfiguration()
                    .WriteTo.File(new CompactJsonFormatter(), @"Logs\" + dateTime.ToShortDateString() + "_bot.log", 
                            rollingInterval: RollingInterval.Day, //  временной интревал записи в файл
                            restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Verbose) // уровень записываемых сообщений
                    .CreateLogger(); 

            return logger;
        }
    }
}
