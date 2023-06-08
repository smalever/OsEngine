using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.View;
using OsEngine.OsTrader;
using OsEngine.OsTrader.Gui;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.Trend;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms.Integration;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    /// <summary>
    /// VM для главного окна с роботами WPF
    /// </summary>
    public class MainWindowRobWpfVM : BaseVM
    {
        public MainWindowRobWpfVM() 
        {
            InitBotPanel();
            InitSetingBotPanel();
        }

        /// <summary>
        /// поле окна выбора инструмента
        /// </summary>
        public static ChengeEmitendWidow ChengeEmitendWidow = null;

        #region Property ==================================================================

        /// <summary>
        /// Коллекция с роботами
        /// </summary>
        public ObservableCollection<IRobotVM> Robots { get; set; } = new ObservableCollection<IRobotVM>();

        public ObservableCollection<BotPanel> botPanels { get; set; } = new ObservableCollection<BotPanel>();


        #endregion

        #region Commands ===========================================================


        //public event GridRobotVM.selectedSecurity OnSelectedSecurity;

        private DelegateCommand comandServerConect;
        /// <summary>
        /// отправка метода для соединения с сервером
        /// </summary>
        public DelegateCommand ComandServerConect
        {
            get
            {
                if (comandServerConect == null)
                {
                    comandServerConect = new DelegateCommand(ServerConect);
                }
                return comandServerConect;
            }
        }

        private DelegateCommand commandСreateBot;

        /// <summary>
        /// делегат для создания ботов
        /// </summary>
        public DelegateCommand CommandСreateBot
        {
            get 
            {
                if (commandСreateBot == null)
                {
                    commandСreateBot = new DelegateCommand(СreateBot);
                }
                return commandСreateBot;
            }
        }
        private DelegateCommand commandTest;

        /// <summary>
        /// делегат для тестовых методов 
        /// </summary>
        public DelegateCommand CommandTest
        {
            get
            {
                if (commandTest == null)
                {
                    commandTest = new DelegateCommand(TestMetod);
                }
                return commandTest;
            }
        }
        #endregion

        /// <summary>
        ///  подключение к серверу 
        /// </summary>
        void ServerConect(object o)
        {
            ServerMaster.ShowDialog(false);
        }
        /// <summary>
        /// поле содеражащие роботов
        /// </summary>
        private OsTraderMaster _botTradeMaster;

        /// <summary>
        /// для тестов 
        /// </summary>
        void TestMetod(object o) 
        {
   
        }
        /// <summary>
        /// создать робота кнопка на гл окне
        /// </summary>
        void СreateBot(object o)
        {
            СreateBot();
        }
        void СreateBot()
        {
            _botTradeMaster.CreateNewBot();
        }
        /// <summary>
        /// инициализация менеджера управления роботами
        /// </summary>
        void InitBotPanel()
        {
            // инициализировали менеджер списока панелей
            _botTradeMaster = new OsTraderMaster(StartProgram.IsOsTrader);
        }
        /// <summary>
        /// присвоили заголовкам  роботов WPF имена панелей осы
        /// </summary>
        void InitSetingBotPanel()
        {

            // список панелей с роботами
            List<BotPanel> listBots = _botTradeMaster.PanelsArray; // взяли из менеджера список панелей осы

            foreach (BotPanel panel in listBots) // пребрали все BotPanel осы
            {
                BaseBotbVM myrob = new BaseBotbVM(); // создал экземпляр въюхи WPF робота

                myrob.Header = panel.NameStrategyUniq; ; // присвоил заголовку  робота WPF имя панели осы

                //myrob.DescriptionBot = bots[1].NameStrategyUniq;

                Robots.Add(myrob);// отправил экземпляр в колекцию с роботами WPF
            }
        }

        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;

    }
}
