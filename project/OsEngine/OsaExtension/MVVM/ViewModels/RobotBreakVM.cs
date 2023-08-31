using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.Models;
using OsEngine.OsaExtension.MVVM.View;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    public class RobotBreakVM : BaseVM, IRobotVM
    {
        #region Свойства  =====================================================

        /// <summary>
        /// заголовок вкладки робота 
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

        /// <summary>
        /// действия робота
        /// </summary>
        public ActionBot ActionBot
        {
            get => _actionBot;
            // todo: создать метод опроса и вывада состояния робота в статус бар
            set
            {
                _actionBot = value;
                OnPropertyChanged(nameof(ActionBot));
            }
        }
        private ActionBot _actionBot;

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

        /// <summary>
        /// Цена большого кластера 
        /// </summary>
        public decimal BigСlusterPrice
        {
            get => _bigСlusterPrice;
            set
            {
                _bigСlusterPrice = value;
                OnPropertyChanged(nameof(BigСlusterPrice));
            }
        }
        private decimal _bigСlusterPrice = 0;

        /// <summary>
        /// Верхняя цена позиции
        /// </summary>
        public decimal TopPositionPrice
        {
            get => _topPositionPrice;
            set
            {
                _topPositionPrice = value;
                OnPropertyChanged(nameof(TopPositionPrice));
            }
        }
        private decimal _topPositionPrice = 0;

        /// <summary>
        /// Нижняя цена набра позиции
        /// </summary>
        public decimal BottomPositionPrice
        {
            get => _bottomPositionPrice;
            set
            {
                _bottomPositionPrice = value;
                OnPropertyChanged(nameof(BottomPositionPrice));
            }
        }
        private decimal _bottomPositionPrice = 0;

        /// <summary>
        /// Шаг набра позиции
        /// </summary>
        public decimal SetStep
        {
            get => _setStep;
            set
            {
                _setStep = value;
                OnPropertyChanged(nameof(SetStep));
            }
        }
        private decimal _setStep = 0;

        /// <summary>
        /// Полный объем позиции робота в портфеле 
        /// </summary>
        public decimal FullPositionVolume
        {
            get => _fullPositionVolume;
            set
            {
                _fullPositionVolume = value;
                OnPropertyChanged(nameof(FullPositionVolume));
            }
        }
        private decimal _fullPositionVolume = 7;

        /// <summary>
        /// количесвто частей на набор 
        /// </summary>
        public int PartsPerInput
        {
            get => _partsPerInput;
            set
            {
                _partsPerInput = value;
                OnPropertyChanged(nameof(PartsPerInput));
            }
        }
        private int _partsPerInput = 1;

        /// <summary>
        /// Oбъем на ордер (часть позиции)
        /// </summary>
        public decimal VolumePerOrder
        {
            get => _volumePerOrder;
            set
            {
                _volumePerOrder = value;
                OnPropertyChanged(nameof(VolumePerOrder));
            }
        }
        private decimal _volumePerOrder = 0;



        #endregion конец свойств =============================================

        /// <summary>
        /// список названий портфелей 
        /// </summary>
        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// Выбранная бумага
        /// </summary>
        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;    

        /// <summary>
        /// конструктор для ранне  созданого и сохранеенного робота
        /// </summary>
        public RobotBreakVM(string header, int numberTab)
        {
            NumberTab = numberTab;
            Header = header;
          
            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;
        
            LoadParamsBot(header);

            ServerMaster.ActivateAutoConnection();
        }

        /// <summary>
        /// Создан сервер 
        /// </summary> 
        private void ServerMaster_ServerCreateEvent(IServer server)
        {
            if (server.ServerType == ServerType)
            {
                Server = server;
            }
        }

        #region  Metods ============================================================================

        /// <summary>
        ///  логика набора позийии
        /// </summary>
        private void LogicStartOpenPosition()
        {
            if (BigСlusterPrice == 0) return;
            /*
             *  направление набора позиции
             *  тип шага между набором объема и расчет
             *  расчет объема на ордер
             *  
             *  
             * */


        }

        /// <summary>
        /// Начать получать данные по бумаге
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
        /// загрузка во вкладку параметров из файла сохрана
        /// </summary>
        public void LoadParamsBot(string name)
        {
            if (!Directory.Exists(@"Parametrs\Tabs"))
            {
                return;
            }
            RobotsWindowVM.Log(Header, " LoadParamsBot \n загрузили параметры ");
            string servType = "";
            try
            {
                using (StreamReader reader = new StreamReader(@"Parametrs\Tabs\param_" + NumberTab + ".txt"))
                {
                    Header = reader.ReadLine(); // загружаем заголовок
                    servType = reader.ReadLine(); // загружаем название сервера
                    //StringPortfolio = reader.ReadLine();  // загружаем бумагу 

                    //StopShort = GetDecimalForString(reader.ReadLine());
                    //StartPoint = GetDecimalForString(reader.ReadLine());
                    //StopLong = GetDecimalForString(reader.ReadLine());

                    //CountLevels = (int)GetDecimalForString(reader.ReadLine());

                    //Direction direct = Direction.BUY;
                    //if (Enum.TryParse(reader.ReadLine(), out direct))
                    //{
                    //    Direction = direct;
                    //}

                    //Lot = GetDecimalForString(reader.ReadLine());

                    //StepType step = StepType.PUNKT;
                    //if (Enum.TryParse(reader.ReadLine(), out step))
                    //{
                    //    StepType = step;
                    //}

                    //StepLevel = GetDecimalForString(reader.ReadLine());
                    //TakeLevel = GetDecimalForString(reader.ReadLine());
                    //MaxActiveLevel = (int)GetDecimalForString(reader.ReadLine());
                    //PriceAverege = GetDecimalForString(reader.ReadLine());
                    //Accum = GetDecimalForString(reader.ReadLine());

                    //Levels = JsonConvert.DeserializeAnonymousType(reader.ReadLine(), new ObservableCollection<Level>());

                    //bool check = false;
                    //if (bool.TryParse(reader.ReadLine(), out check))
                    //{
                    //    IsChekCurrency = check;
                    //}
                    //bool run = false;
                    //if (bool.TryParse(reader.ReadLine(), out run))
                    //{
                    //    IsRun = run;
                    //}

                    reader.Close();
                }
            }
            catch (Exception ex)
            {
                RobotsWindowVM.Log(Header, " Ошибка выгрузки параметров = " + ex.Message);
            }
            StartServer(servType);

        }

        /// <summary>
        /// запускаем сервер
        /// </summary>
        void StartServer(string servType)
        {
            if (servType == "" || servType == "null")
            {
                return;
            }
            ServerType type = ServerType.None;
            if (Enum.TryParse(servType, out type))
            {
                ServerType = type;
                ServerMaster.SetNeedServer(type);
                // LoadParamsBot(Header);
            }
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
            _server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent += _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent += _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent += _server_ConnectStatusChangeEvent;

            RobotsWindowVM.Log(Header, " Подключаемся к серверу = " + _server.ServerType);
        }

        private void _server_SecuritiesChangeEvent(List<Security> securities)
        {
            for (int i = 0; i < securities.Count; i++)
            {
                if (securities[i].Name == Header)
                {
                    SelectedSecurity = securities[i];
                    //StartSecuritiy(securities[i]);
                    break;
                }
            }
        }

        private void _server_ConnectStatusChangeEvent(string state)
        {
            if (state == "Connect")
            {
                //StartSecuritiy(SelectedSecurity);
                //SubscribeToServer();
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
            _server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent -= _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;

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
