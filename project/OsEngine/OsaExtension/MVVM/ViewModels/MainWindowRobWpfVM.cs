using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.View;
using OsEngine.OsTrader;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.Trend;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    /// <summary>
    /// VM для главного окна с роботами WPF
    /// </summary>
    public class MainWindowRobWpfVM : BaseVM
    {
        public MainWindowRobWpfVM() 
        {
                        
        }

        /// <summary>
        /// поле окна выбора инструмента
        /// </summary>
        public static ChengeEmitendWidow ChengeEmitendWidow = null;

        #region Property ==================================================================
        public ObservableCollection<IRobotVM> Robots { get; set; } = new ObservableCollection<IRobotVM>();

        #endregion

        #region Commands ===========================================================


        //public event GridRobotVM.selectedSecurity OnSelectedSecurity;

        private DelegateCommand comandServerConect;
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
        #endregion

        /// <summary>
        ///  подключение к серверу 
        /// </summary>
        void ServerConect(object o)
        {
            ServerMaster.ShowDialog(false);
        }

        /// <summary>
        /// созание робота
        /// </summary>
        void СreateBot(object o)
        {
            
            string name = "EnvelopTrend";

            //EnvelopTrend bot = new EnvelopTrend(name, StartProgram.IsOsTrader);
            var bot = new TestRobVM();

            Robots.Add(bot);
            //Bot = bot;

            //NamSecuriti = bot.TabsSimple[0].Securiti.Name;

           
        }
        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;


    }
}
