﻿using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    public class RobotBreakVM : BaseVM, IRobotVM
    {
        #region Свойства  =====================================================

        /// <summary>
        /// заголовок робота 
        /// </summary>
        public string Header
        {
            get
            {
                if (SelectedSecurity != null)
                {
                    return SelectedSecurity.Name;

                }
                else
                {
                    return _header;
                }
            }
            set
            {
                _header = value;
                OnPropertyChanged(nameof(Header));
            }
        }
        private string _header;

        /// <summary>
        /// номер вкладки робота
        /// </summary>
        public int NumberTab
        {
            get => _numberTab;
            set
            {
                _numberTab = value;
                OnPropertyChanged(nameof(NumberTab));
            }
        }
        private int _numberTab = 0;

        public NameStrat NameStrat
        {
            get => _nameStrat;
            set
            {
                _nameStrat = value;
                OnPropertyChanged(nameof(NameStrat));
            }
        }
        private NameStrat _nameStrat = NameStrat.BREAKDOWN;

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

        public Security SelectedSecurity
        {
            get => _selectedSecurity;
            set
            {
                _selectedSecurity = value;
                OnPropertyChanged(nameof(SelectedSecurity));
                OnPropertyChanged(nameof(Header));
                if (SelectedSecurity != null)
                {
                    StartSecuritiy(SelectedSecurity); // запуск бумаги 
                    OnSelectedSecurity?.Invoke();
                }
            }
        }
        private Security _selectedSecurity = null;

        /// <summary>
        /// название биржи (кошелька)
        /// </summary>
        public ServerType ServerType
        {
            get
            {
                if (Server == null)
                {
                    return _serverType;
                }
                return Server.ServerType;
            }
            set
            {
                if (value != _serverType)
                {
                    _serverType = value;
                }
            }
        }
        ServerType _serverType = ServerType.None;

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


        #endregion конец свойств =============================================


        /// <summary>
        /// список портфелей 
        /// </summary>
        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();
        /// <summary>
        /// Выбранная бумага
        /// </summary>

        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;

        //event selectedSecurity IRobotVM.OnSelectedSecurity
        //{
        //    add
        //    {
        //        // todo: разобраться с реализацией 

        //    }

        //    remove
        //    {
        //        //throw new NotImplementedException();
        //    }
        //}

        /// <summary>
        /// конструктор для ранне  созданого и сохранеенного робота
        /// </summary>
        public RobotBreakVM(string header, int numberTab)
        {
            //string[]str = header.Split('=');
            NumberTab = numberTab;
            Header = header;

            //LoadParamsBot(header);
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;

        }

        private void ServerMaster_ServerCreateEvent(IServer server)
        {
            if (server.ServerType == ServerType)
            {
                Server = server;
            }
        }


        #region  Metods ============================================================================

        /// <summary>
        /// Начать получать данные по бумге
        /// </summary> 
        private void StartSecuritiy(Security security)
        {
            if (security == null)
            {
                RobotsWindowVM.Log(Header, "StartSecuritiy  security = null ");
                return;
            }

            Task.Run(() =>
            {
                while (true)
                {
                    var series = Server.StartThisSecurity(security.Name, new TimeFrameBuilder(), security.NameClass);
                    if (series != null)
                    {
                        RobotsWindowVM.Log(Header, "StartSecuritiy  security = " + series.Security.Name);
                        // DesirializerLevels();
                        //SaveParamsBot();
                        //GetOrderStatusOnBoard();
                        break;
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        /// <summary>
        /// берет названия кошельков (бирж)
        /// </summary>
        public ObservableCollection<string> GetStringPortfolios(IServer server)
        {
            ObservableCollection<string> stringPortfolios = new ObservableCollection<string>();
            if (server == null)
            {
                RobotsWindowVM.Log(Header, "GetStringPortfolios server == null ");
                return stringPortfolios;
            }
            if (server.Portfolios == null)
            {
                return stringPortfolios;
            }

            foreach (Portfolio portf in server.Portfolios)
            {
                //RobotWindowVM.Log(Header, "GetStringPortfolios  портфель =  " + portf.Number);
                stringPortfolios.Add(portf.Number);
            }
            return stringPortfolios;
        }

        /// <summary>
        ///  подключиться к серверу
        /// </summary>
        private void SubscribeToServer()
        {
            //_server.NewMyTradeEvent += Server_NewMyTradeEvent;
            //_server.NewOrderIncomeEvent += Server_NewOrderIncomeEvent;
            _server.NewTradeEvent += _NewTradeEvent;
            //_server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent += _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent += _server_NewBidAscIncomeEvent;
            //_server.ConnectStatusChangeEvent += _server_ConnectStatusChangeEvent;

            RobotsWindowVM.Log(Header, " Подключаемся к серверу = " + _server.ServerType);
        }
        /// <summary>
        /// пришел новый терейд 
        /// </summary>
        private void _NewTradeEvent(List<Trade> trades)
        {
            if (trades != null && trades[0].SecurityNameCode == SelectedSecurity.Name)
            {
                Trade trade = trades.Last();

                Price = trade.Price;

                if (trade.Time.Second % 10 == 0)
                {
                    //TradeLogic();
                }
            }
        }

        /// <summary>
        ///  отключиться от сервера 
        /// </summary>
        private void UnSubscribeToServer()
        {
            //_server.NewMyTradeEvent -= Server_NewMyTradeEvent;
            //_server.NewOrderIncomeEvent -= Server_NewOrderIncomeEvent;
            _server.NewTradeEvent -= _NewTradeEvent;
            //_server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent -= _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            //_server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;

            RobotsWindowVM.Log(Header, " Отключились от сервера = " + _server.ServerType);
        }

        /// <summary>
        /// выбрать бумагу
        /// </summary>
        void SelectSecurity(object o)
        {
            if (RobotsWindowVM.ChengeEmitendWidow != null)
            {
                return;
            }
            RobotsWindowVM.ChengeEmitendWidow = new ChengeEmitendWidow(this);
            RobotsWindowVM.ChengeEmitendWidow.ShowDialog();
            RobotsWindowVM.ChengeEmitendWidow = null;
            if (_server != null)
            {
                //if (_server.ServerType == ServerType.Binance
                //    || _server.ServerType == ServerType.BinanceFutures)
                //{
                //    IsChekCurrency = true;
                //}
                //else IsChekCurrency = false;
            }
        }
        #endregion

        #region Commands ==============================================================
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
        #endregion end Commands ====================================================
    }
}
