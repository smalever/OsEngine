using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.View;
using OsEngine.OsTrader;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.Trend;
using System;
using System.Collections.Generic;
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
        #region Property ==================================================================

        #endregion
        /// <summary>
        /// робот
        /// </summary>
        public EnvelopTrend Bot { get; set; }

        /// <summary>
        /// робот
        /// </summary>
        public string NamSecuriti { get; set; }


        #region Commands ===========================================================

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

        #region Metods ============================================================

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

            EnvelopTrend bot = new EnvelopTrend(name, StartProgram.IsOsTrader);

            Bot = bot;

            NamSecuriti = bot.TabsSimple[0].Securiti.Name;

           // OsTraderMaster.Master.BotTabConnectorDialog();

        }


 

        #endregion
    }
}
