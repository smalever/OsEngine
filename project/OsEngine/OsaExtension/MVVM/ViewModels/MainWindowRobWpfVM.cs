using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.Models;
using OsEngine.OsaExtension.MVVM.View;
using OsEngine.OsaExtension.Robots.Forbreakdown;
using OsEngine.OsTrader;
using OsEngine.OsTrader.Gui;
using OsEngine.OsTrader.Panels;
using OsEngine.OsTrader.Panels.Tab;
using OsEngine.Robots.Trend;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms;
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
            //_master = OsTraderMaster.Master;
            _master.BotCreateEvent += _master_BotCreateEvent;
            _master.BotDeleteEvent += _master_BotDeleteEvent;

            CreateVM_Bot();
            
        }

        /// <summary>
        /// поле окна выбора инструмента
        /// </summary>
        public static ChengeEmitendWidow ChengeEmitendWidow = null;



        #region Property ==================================================================

        /// <summary>
        /// св менеджера роботов
        /// </summary>   
        public OsTraderMaster ManagerBot
        { 
            get => _master;
            set
            {
                _master = value;
                OnPropertyChanged(nameof(ManagerBot));
            }
        }
        OsTraderMaster _master = new OsTraderMaster(StartProgram.IsOsTrader);

        /// <summary>
        /// ВМ-ки роботов
        /// </summary>
        public static ObservableCollection<IRobotVM> Robots
        { 
            get
            {                
                return _robots;
            }  
            set
            {
                _robots = value;         
            } 
        }
        private static ObservableCollection<IRobotVM> _robots = new ObservableCollection<IRobotVM>();

  
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

        #region  Методы =====================================================

  
        public void CreateVM_Bot()
        {
            List<BotPanel> ListBotsOsen = ManagerBot.PanelsArray;
            if (ListBotsOsen == null) return;
            int count = ListBotsOsen.Count;
            if (count == 0) return;

            /*  если есть список BotPanel бежим по нему и
             *  создаем ВМ для этой вкладки (робота осы) 
             */
            foreach (BotPanel panel in ListBotsOsen) // перебрали все BotPanel осы
            {
                ForBreakdownVM myrob = new ForBreakdownVM(); // создал экземпляр въюхи WPF робота

                myrob.Header = panel.NameStrategyUniq; // присвоил заголовку  робота WPF имя панели осы
             

                Robots.Add(myrob);// отправил экземпляр в колекцию с роботами WPF
            }

        }  

        private void _master_BotDeleteEvent(BotPanel bot)
        {

        }

        private void _master_BotCreateEvent(BotPanel bot)
        {

        }  

        /// <summary>
        ///  подключение к серверу 
        /// </summary>
        void ServerConect(object o)
        {
            ServerMaster.ShowDialog(false);
        }

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
            _master.CreateNewBot();
        }

        #endregion конец  Методы ==============================================


        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;

    }
}
