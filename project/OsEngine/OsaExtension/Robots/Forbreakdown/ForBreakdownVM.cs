using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.Models;
using OsEngine.OsaExtension.MVVM.View;
using OsEngine.OsTrader;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.Trend;
using OsEngine.OsTrader.Panels.Tab;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static OsEngine.OsaExtension.MVVM.ViewModels.MainWindowRobWpfVM;
using OsEngine.OsaExtension.MVVM.ViewModels;
using OsEngine.OsaExtension.MVVM;

namespace OsEngine.OsaExtension.Robots.Forbreakdown
{
    public class ForBreakdownVM : BaseVM, IRobotVM
    {
        public ForBreakdownVM()
        {
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;

        }

        event GridRobotVM.selectedSecurity IRobotVM.OnSelectedSecurity
        {
            add
            {
                // заглушка 
            }

            remove
            {
               
            }
        }

        /// <summary>
        /// создан сервер 
        /// </summary>
        private void ServerMaster_ServerCreateEvent(IServer server)
        {
            server.NewTradeEvent += Server_NewTradeEvent;
        }

        /// <summary>
        /// пришел трейд 
        /// </summary>
        private void Server_NewTradeEvent(List<Trade> trades)
        {
            if (NameSecurityBot == null)
            {
                InitPropertiBot(); // todo: для теста 
            }
            if (NameSecurityBot != null)
            {
                if (trades[0].SecurityNameCode == NameSecurityBot)
                {
                    Trade trade = trades.Last();

                    Price = trade.Price;
                }
            }
        }
        #region Свойства =========================================================

        public string Header { get; set; }
        public int NumberTab { get; set; }

        /// <summary>
        /// бумага на которую подписана вкладка TabsSimple[0]
        /// </summary>
        public string NameSecurityBot
        {
            get => _securityBot;
            set
            {
                _securityBot = value;
                OnPropertyChanged(nameof(NameSecurityBot));
                OnPropertyChanged(nameof(Header));

            }
        }
        private string _securityBot = null;

        /// <summary>
        /// описание бота 
        /// </summary>
        public string DescriptionBot
        {
            get => _descriptionBot;
            set
            {
                _descriptionBot = value;
                OnPropertyChanged(nameof(DescriptionBot));
            }
        }
        private string _descriptionBot;

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

        /// <summary>
        /// список BotPanels 
        /// </summary>
        public List<BotPanel> ListBotPanels
        {
            get => _listBotPanels;
            set
            {
                _listBotPanels = value;
                OnPropertyChanged(nameof(ListBotPanels));
            }
        }
        private List<BotPanel> _listBotPanels = OsTraderMaster.Master.PanelsArray;

        #endregion  end Свойства =========================================================


        ObservableCollection<Position> PositionsOpenAll = new ObservableCollection<Position>();

        /// <summary>
        /// инициализация свойств робота 
        /// </summary>
        void InitPropertiBot() // TODO: КОРЯВО РАБОТАЕТ  при смене бумаги в роботе (надо вызывать при смене)
        {
            int count = ListBotPanels.Count;

            for (int i = 0; i < count; i++)
            {
                if (ListBotPanels[i].NameStrategyUniq == Header)
                {
                    GetTabSimplSecurName(ListBotPanels[i]);
                }
            }
        }
        // TODO: можно предать любые данные  вкладки TabsSimple в свойства
        /// <summary>
        /// взять название бумаги в TabsSimple
        /// </summary> 
        void GetTabSimplSecurName(BotPanel bot)
        {
            foreach (var TabsSimple in bot.TabsSimple)
            {
                if (TabsSimple.Securiti != null)
                {
                    NameSecurityBot = TabsSimple.Connector.SecurityName;
                    var qwe = TabsSimple.PositionsOpenAll;
                }
            }
        }

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


        /// <summary>
        /// выбрать бумагу
        /// </summary>
        void SelectSecurity(object o)
        {
            string nameBot = (string)o;

            int count = ListBotPanels.Count;

            for (int i = 0; i < count; i++)
            {
                if (ListBotPanels[i].NameStrategyUniq == nameBot)
                {// todo: разобраться как сделать выбор по номеру вкладки TabsSimple[]

                    OsTraderMaster.Master._activPanel.ActivTab = ListBotPanels[i].TabsSimple[0];
                    OsTraderMaster.Master.BotTabConnectorDialog();
                    break;
                }
            }
        }

        private DelegateCommand _individualParamsBot;
        /// <summary>
        ///  делегат вызова параметров робота
        /// </summary>
        public DelegateCommand CommandIndividualParamsBot
        {
            get
            {
                if (_individualParamsBot == null)
                {
                    _individualParamsBot = new DelegateCommand(ParamsBot);
                }
                return _individualParamsBot;
            }
        }

        /// <summary>
        /// вызывает диалог параметров 
        /// </summary>
        void ParamsBot(object o)
        {
            string nameBot = (string)o;

            int count = ListBotPanels.Count;

            for (int i = 0; i < count; i++)
            {
                if (ListBotPanels[i].NameStrategyUniq == nameBot)
                {
                    OsTraderMaster.Master._activPanel = ListBotPanels[i];
                    OsTraderMaster.Master.BotShowParametrsDialog();
                    break;
                }
            }
        }

        private DelegateCommand _commandDeleteBot;
        /// <summary>
        /// делегат удаления робота 
        /// </summary>
        public DelegateCommand CommandDeleteBot
        {
            get
            {
                if (_commandDeleteBot == null)
                {
                    _commandDeleteBot = new DelegateCommand(DeleteBot);
                }
                return _commandDeleteBot;
            }
        }

        /// <summary>
        /// удалить робота по имени вкладки 
        /// </summary>
        void DeleteBot(object o)
        {
            string nameBot = (string)o;

            int count = ListBotPanels.Count;

            for (int i = 0; i < count; i++)
            {
                if (ListBotPanels[i].NameStrategyUniq == nameBot)
                {
                    OsTraderMaster.Master.DeleteByNum(i);
                    break;
                }
            }
        }
        #region ===== Сущностия для своего окна вызова инструмента(бумаги)

        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();

        private void StartSecuritiy(Security security)
        {
            //if (security == null)
            //{
            //    RobotWindowVM.Log(Header, "StartSecuritiy  security = null ");
            //    return;
            //}
            //Task.Run(() =>
            //{
            //    while (true)
            //    {
            //        var series = Server.StartThisSecurity(security.Name, new TimeFrameBuilder(), security.NameClass);
            //        if (series != null)
            //        {
            //            //RobotWindowVM.Log(Header, "StartSecuritiy  security = " + series.Security.Name);
            //            //Save();
            //            //break;
            //        }
            //        Thread.Sleep(1000);
            //    }
            //});
        }

        public event selectedSecurity OnSelectedSecurity;

        /// <summary>
        ///  подключиться к серверу
        /// </summary>
        private void SubscribeToServer()
        {
            //_server.NewMyTradeEvent += Server_NewMyTradeEvent;
            //_server.NewOrderIncomeEvent += Server_NewOrderIncomeEvent;
            //_server.NewCandleIncomeEvent += Server_NewCandleIncomeEvent;
            //_server.NewTradeEvent += _server_NewTradeEvent;
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
            //_server.NewTradeEvent -= _server_NewTradeEvent;
            //_server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent -= _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            //_server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;
        }

        public ObservableCollection<string> GetStringPortfolios(IServer server)
        {
            ObservableCollection<string> stringPortfolios = new ObservableCollection<string>();
            if (server == null)
            {
                return stringPortfolios;
            }
            if (server.Portfolios == null)
            {
                return stringPortfolios;
            }

            foreach (Portfolio portf in server.Portfolios)
            {
                stringPortfolios.Add(portf.Number);
            }
            return stringPortfolios;
        }

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

        public NameStrat NameStrat { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        private IServer _server = null;

        #endregion

    }
}
