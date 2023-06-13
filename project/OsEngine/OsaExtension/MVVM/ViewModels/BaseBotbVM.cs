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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static OsEngine.OsaExtension.MVVM.ViewModels.MainWindowRobWpfVM;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    public class BaseBotbVM : BaseVM, IRobotVM
    {
        public BaseBotbVM()
        { 
                   
        }

        public string Header { get; set; }
        public int NumberTab { get; set; }

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

        /// <summary>
        /// список BotPanels 
        /// </summary>
        List<BotPanel> ListBots = OsTraderMaster.Master.PanelsArray;

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

        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// выбрать бумагу
        /// </summary>
        void SelectSecurity(object o)
        {
            if (MainWindowRobWpfVM.ChengeEmitendWidow != null)
            {
                return;
            }
            MainWindowRobWpfVM.ChengeEmitendWidow = new ChengeEmitendWidow(this);
            MainWindowRobWpfVM.ChengeEmitendWidow.ShowDialog();
            MainWindowRobWpfVM.ChengeEmitendWidow = null;
  
        }

        private DelegateCommand _individualParamsBot;
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
        void ParamsBot(object o)
        {
            string nameBot = (string)o;

            int count = ListBots.Count;

            for (int i = 0; i < count; i++)
            {
                if (ListBots[i].NameStrategyUniq == nameBot)
                {
                    OsTraderMaster.Master.BotShowParametrsDialog();
                    break;
                }
            }
        }

        private DelegateCommand _commandDeleteBot;
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
  
            int count = ListBots.Count;
 
            for (int i = 0; i < count; i++)
            {
                if (ListBots[i].NameStrategyUniq == nameBot)
                {
                    OsTraderMaster.Master.DeleteByNum(i);
                    break;
                }
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
        /// <summary>
        ///  трейды для тестов 
        /// </summary>
        private void _server_NewTradeEvent(List<Trade> trades)
        {
            if (trades[0].SecurityNameCode == SelectedSecurity.Name)
            {
                Trade trade = trades.Last();

                Price = trade.Price;
            }  
        }
        private void StartSecuritiy(Security security)
        {
            //if (security == null)
            //{
            //    RobotWindowVM.Log(Header, "StartSecuritiy  security = null ");
            //    return;
            //}
            Task.Run(() =>
            {
                while (true)
                {
                    var series = Server.StartThisSecurity(security.Name, new TimeFrameBuilder(), security.NameClass);
                    if (series != null)
                    {
                        //RobotWindowVM.Log(Header, "StartSecuritiy  security = " + series.Security.Name);
                        //Save();
                        //break;
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        public event selectedSecurity OnSelectedSecurity;

    }
}
