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
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;
            
        }

        /// <summary>
        /// поле окна выбора инструмента
        /// </summary>
        public static ChengeEmitendWidow ChengeEmitendWidow = null;

        #region Property ==================================================================

        /// <summary>
        /// Сервер 
        /// </summary>
        public IServer Server
        {
            get => _server;
            set
            {
                if (Server != null)
                {
                    UnSubscribeToServer();
                    _server = null;
                }
                _server = value;
                OnPropertyChanged(nameof(ServerType));

                SubscribeToServer(); // подключаемя к бир
            }
        }
        private IServer _server = null;

        /// <summary>
        /// Рыночная цена бумаги 
        /// </summary>
        public decimal Price
        {
            get => _price;
            set
            {
                _price = value;
                OnPropertyChanged(nameof(Price));
            }
        }
        private decimal _price;

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


        private DelegateCommand _commandSelectSecurity;
        public DelegateCommand CommandSelectSecurity
        {
            get
            {
                if (_commandSelectSecurity == null)
                {
                    _commandSelectSecurity = new DelegateCommand(SelectSecurity);
                }
                return _commandSelectSecurity;
            }
        }
        #endregion


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

        #region Metods ============================================================

        /// <summary>
        /// выбрать бумагу
        /// </summary>
        void SelectSecurity(object o)
        {
            if (MainWindowRobWpfVM.ChengeEmitendWidow != null)
            {
                return;
            }
            MainWindowRobWpfVM.ChengeEmitendWidow = new ChengeEmitendWidow();
            MainWindowRobWpfVM.ChengeEmitendWidow.ShowDialog();
            MainWindowRobWpfVM.ChengeEmitendWidow = null;
            if (_server != null)
            {
                if (_server.ServerType == ServerType.Binance
                    || _server.ServerType == ServerType.BinanceFutures)
                {
                    // IsChekCurrency = true;
                }
                //else IsChekCurrency = false;
            }
        }

        /// <summary>
        ///  подключиться к серверу
        /// </summary>
        private void SubscribeToServer()
        {
            //_server.NewMyTradeEvent += Server_NewMyTradeEvent;
            //_server.NewOrderIncomeEvent += Server_NewOrderIncomeEvent;
            //_server.NewCandleIncomeEvent += Server_NewCandleIncomeEvent;
            _server.NewTradeEvent += _server_NewTradeEvent;
            //_server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent += _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent += _server_NewBidAscIncomeEvent;
            //_server.ConnectStatusChangeEvent += _server_ConnectStatusChangeEvent;            
        }

        /// <summary>
        ///  отключиться от сервера 
        /// </summary>
        private void UnSubscribeToServer()
        {
            //_server.NewMyTradeEvent -= Server_NewMyTradeEvent;
            //_server.NewOrderIncomeEvent -= Server_NewOrderIncomeEvent;
            //_server.NewCandleIncomeEvent -= Server_NewCandleIncomeEvent;
            _server.NewTradeEvent -= _server_NewTradeEvent;
            //_server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent -= _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            //_server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;
        }

        private void ServerMaster_ServerCreateEvent(IServer server)
        {
            if (_server == null) return;

            _server.NewTradeEvent += _server_NewTradeEvent;
        }

        private void _server_NewTradeEvent(List<Trade> trades)
        {
            Trade trade = trades.Last();

            Price = trade.Price;
        }

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

           
        }
        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;




        #endregion
    }
}
