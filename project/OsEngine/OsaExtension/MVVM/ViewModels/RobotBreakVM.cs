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
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using Order = OsEngine.Entity.Order;
using Security = OsEngine.Entity.Security;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    public class RobotBreakVM : BaseVM, IRobotVM
    {
        #region Свойства  =====================================================

        /// <summary>
        /// вкл\выкл
        /// </summary>
        public bool IsRun
        {
            get => _isRun;
            set
            {
                _isRun = value;
                OnPropertyChanged(nameof(IsRun));

                if (IsRun)
                {
                    TradeLogic();
                }
                else
                {
                    StopTradeLogic();
                }
            }
        }
        private bool _isRun;

        /// <summary>
        /// расчетная цена открытия позиции 
        /// </summary>
        public decimal PriceOpenPos
        {
            get => _priceOpenPos;
            set
            {
                _priceOpenPos = value;
                OnPropertyChanged(nameof(PriceOpenPos));
            }
        }
        private decimal _priceOpenPos = 0;

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
            get =>_bigСlusterPrice;
            set
            {
                if (value != _bigСlusterPrice)
                {
                    _bigСlusterPrice = value;
                    OnPropertyChanged(nameof(BigСlusterPrice));
                }                    
            }
        }
        private decimal _bigСlusterPrice;

        /// <summary>
        /// Верхняя цена позиции
        /// </summary>
        public decimal TopPositionPrice
        {
            get => _topPositionPrice;
            set
            {
                if (value != _topPositionPrice)
                {
                    _topPositionPrice = value;
                    OnPropertyChanged(nameof(TopPositionPrice));
                } 
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
                if (value != _bottomPositionPrice) 
                {
                    _bottomPositionPrice = value;
                    OnPropertyChanged(nameof(BottomPositionPrice));
                }                
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

        /// <summary>
        /// направление сделок 
        /// </summary>
        public Direction Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                OnPropertyChanged(nameof(Direction));
            }
        }
        private Direction _direction;

        /// <summary>
        /// список  свойств направления сделок
        /// </summary> 
        public List<Direction> Directions { get; set; } = new List<Direction>()
        {
            Direction.BUY, Direction.SELL, Direction.BUYSELL
        };

        /// <summary>
        /// тип расчета шага на вход 
        /// </summary>
        public StepType StepType
        {
            get => _stepType;
            set
            {
                _stepType = value;
                OnPropertyChanged(nameof(StepType));
            }
        }
        private StepType _stepType;

        /// <summary>
        /// название портфеля (счета)
        /// </summary>
        public string StringPortfolio
        {
            get => _stringportfolio;
            set
            {
                _stringportfolio = value;
                OnPropertyChanged(nameof(StringPortfolio));
                // TODO: надо разобраться с загрузкой значения (без файла сох) 
                _portfolio = GetPortfolio(_stringportfolio);
            }
        }
        private string _stringportfolio = "";

        /// <summary>
        /// список типов расчета шага 
        /// </summary>
        public List<StepType> StepTypes { get; set; } = new List<StepType>()
        {
            StepType.PUNKT, StepType.PERCENT
        };

        /// <summary>
        /// список названий портфелей 
        /// </summary>
        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();
        
        /// <summary>
        /// список позиций робота 
        /// </summary>
        public static ObservableCollection<PositionBot> PositionsBots { get; set; } = new ObservableCollection<PositionBot>();

        #endregion конец свойств =============================================

        #region Поля ==================================================

        /// <summary>
        /// названия портфеля для отправки ордера на биржу
        /// </summary>
        Portfolio _portfolio;

        /// <summary>
        /// поле содержащее VM окна отображ всех роботов
        /// </summary>
        private RobotsWindowVM _robotsWindowVM;       

        #endregion
     
        /// <summary>
        /// конструктор для ранне  созданого и сохранеенного робота
        /// </summary>
        public RobotBreakVM(string header, int numberTab, RobotsWindowVM MainWindows)
        {
            NumberTab = numberTab;
            Header = header;
            _robotsWindowVM = MainWindows;

            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;
        
            LoadParamsBot(header);

            ServerMaster.ActivateAutoConnection();

            PropertyChanged += RobotBreakVM_PropertyChanged;    
            SendStrStatus(" Ожидается подключение к бирже ");
        }

        #region  Metods ======================================================================
        #region  методы логики ===============================================

        private void TradeLogic()
        {
            CreateNewPosition(); // создали позиции
            SendOrderExchange(); // отправили ордер на биржу
        }

        /// <summary>
        /// метод отработки кнопки стоп
        /// </summary>
        private void StopTradeLogic()
        {
            
        }

        /// <summary>
        /// метод отработки кнопки старт/стоп
        /// </summary>
        private void StartStop(object o)
        {
            Thread.Sleep(300);

            IsRun = !IsRun;

            RobotsWindowVM.Log(Header, " \n\n StartStop = " + IsRun);

            SaveParamsBot();
            if (IsRun)
            {
                // сейчас логика запускается в свойстве вкл/выкл

                //level.SetVolumeStart();
                //level.PassVolume = true;
                //level.PassTake = true;               
            }
            else
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        // StopLogic();  
                    }
                });
            }
        }

        /// <summary>
        ///  отправить ордер на биржу 
        /// </summary>
        private void SendOrderExchange()
        {
            PositionBot position = PositionsBots.Last();
            List<Order> orders = position.OrdersForOpen;

            if (position.PassOpenOrder)
            {
                position.PassOpenOrder = false;

                foreach (Order order in orders) // взять из позиции ордер
                {
                    if (order.State == OrderStateType.None)
                    {
                        // отправить ордер на биржу
                        Server.ExecuteOrder(order);
                        position.Status = PositionStatus.OPENING;
                        SendStrStatus(" Ордер отправлен на биржу");
                    }
                }
            }
        }

        /// <summary>
        /// добавить открывающий ордер в позицию
        /// </summary>  
        private void AddOpderPosition(PositionBot position)
        {
            if (Direction == Direction.BUY || Direction == Direction.BUYSELL)
            {// создать ордер
                Order order = CreateLimitOrder(SelectedSecurity, PriceOpenPos, VolumePerOrder, Side.Buy);
                // отправить ордер в позицию
                position.OrdersForOpen.Add(order);
            }
            if (Direction == Direction.SELL || Direction == Direction.BUYSELL)
            {// создать ордер
                Order order = CreateLimitOrder(SelectedSecurity, PriceOpenPos, VolumePerOrder, Side.Sell);
                // отправить ордер в позицию
                position.OrdersForOpen.Add(order);
            }
        }

        /// <summary>
        /// создание позиции
        /// </summary>
        private void CreateNewPosition()
        {
            if (BigСlusterPrice == 0)
            {
                SendStrStatus(" BigСlusterPrice = 0 ");
                return;
            }

            if (MonitoringOpenVolumePosition() == true)
            {
                SendStrStatus(" Есть открытый объем ");
                return;
                //MessageBoxResult result = MessageBox.Show(" Есть открытые позиции! \n Всеравно создать? ", " ВНИМАНИЕ !!! ",
                //MessageBoxButton.YesNo);
                //if (result == MessageBoxResult.No)
                //{
                //    return;
                //}
            }
            ObservableCollection<PositionBot> positionBots = new ObservableCollection<PositionBot>();

            PositionBot positionBuy = new PositionBot() { Side = Side.Buy };
            PositionBot positionSell = new PositionBot() { Side = Side.Sell };

            CalculPriceStartPos();
            CalculateVolumeTrades();

            if (VolumePerOrder != 0 && PriceOpenPos != 0 && IsRun == true) // формируем позиции
            {
                if (Direction == Direction.BUY || Direction == Direction.BUYSELL)
                {
                    positionBuy.Status = PositionStatus.NONE;
                    positionBuy.SecurityName = SelectedSecurity.Name;
                    AddOpderPosition(positionBuy);
                    positionBots.Add(positionBuy);
                }
                if (Direction == Direction.SELL || Direction == Direction.BUYSELL)
                {
                    positionSell.Status = PositionStatus.NONE;                    
                    positionSell.SecurityName = SelectedSecurity.Name;
                    AddOpderPosition(positionSell);
                    positionBots.Insert(0, positionSell);
                }
                PositionsBots = positionBots;
                SendStrStatus(" Позиция создана");
            }            
        }

        /// <summary>
        /// проверка открытого объема в позиция
        /// </summary> 
        private bool MonitoringOpenVolumePosition()
        {
            decimal volume = 0;
            foreach (PositionBot pos in PositionsBots)
            {
                volume += Math.Abs(pos.OpenVolume);
            }
            if (volume > 0) return true;
            else return false;
        }

        /// <summary>
        /// расчет объема на ордер
        /// </summary>
        private void CalculateVolumeTrades()
        {
            VolumePerOrder = 0;
            decimal workLot = 0;
            decimal baks = 0;
            baks = FullPositionVolume/ PartsPerInput; // это в баксах
            decimal moni = baks / Price; // в монете
            workLot = Decimal.Round(moni, SelectedSecurity.DecimalsVolume);
            decimal minVolume = SelectedSecurity.MinTradeAmount;
            if (workLot < minVolume)
            {
                SendStrStatus("Объем ордера меньше допустимого");
                // IsRun = false;
            }
            else
            {
                VolumePerOrder = workLot;
            }
        }

        /// <summary>
        /// расчитать стартовую цену (начала открытия позиции)
        /// </summary>
        private void CalculPriceStartPos()
        {
            _priceOpenPos = 0;
            decimal stepPrice = 0;

            if (Direction == Direction.BUY)
            {
                if (BigСlusterPrice == 0 || BottomPositionPrice == 0)
                {
                    SendStrStatus(" BigСlusterPrice или BottomPositionPrice = 0 ");
                    return; 
                }

                stepPrice = (BigСlusterPrice - BottomPositionPrice) / PartsPerInput;
                _priceOpenPos = BigСlusterPrice - stepPrice;
            }
            if (Direction == Direction.SELL)
            {
                if (BigСlusterPrice == 0 || TopPositionPrice == 0)
                {
                    SendStrStatus(" BigСlusterPrice или TopPositionPrice = 0 ");
                    return;
                }

                stepPrice = (TopPositionPrice - BigСlusterPrice) / PartsPerInput;
                _priceOpenPos = BigСlusterPrice + stepPrice;
            }
            //  todo : бобавить расчет для разнонаправленных сделок

            _priceOpenPos = Decimal.Round(_priceOpenPos, SelectedSecurity.Decimals);

            PriceOpenPos = _priceOpenPos;
        }

        /// <summary>
        /// проверка ордера
        /// </summary>
        private void CheckMyOrder(Order checkOrder)
        {
            for (int i = 0; i < PositionsBots.Count; i++)
            {
                bool newOrderBool = PositionsBots[i].NewOrder(checkOrder);
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
        ///  сформировать лимитный оредер
        /// </summary>
        private Order CreateLimitOrder(Security sec, decimal prise, decimal volume, Side side)
        {
            if (string.IsNullOrEmpty(StringPortfolio))
            {
                // сообщение в лог  сделать 
                MessageBox.Show(" еще нет портфеля ");
                return null;
            }
            Order order = new Order()
            {
                Price = prise,
                Volume = volume,
                Side = side,
                PortfolioNumber = StringPortfolio,
                TypeOrder = OrderPriceType.Limit,
                NumberUser = NumberGen.GetNumberOrder(StartProgram.IsOsTrader),
                SecurityNameCode = sec.Name,
                SecurityClassCode = sec.NameClass,
            };
            //RobotsWindowVM.Log(Header, "SendLimitOrder\n " + " отправляем лимитку на биржу\n" + GetStringForSave(order));
            RobotsWindowVM.SendStrTextDb(" Создали ордер " + order.NumberUser);

            //Server.ExecuteOrder(order);

            return order;
        }

        /// <summary>
        /// сформировать Маркетный оредер 
        /// </summary>
        private Order CreateMarketOrder(Security sec, decimal prise, decimal volume, Side side)
        {
            if (string.IsNullOrEmpty(StringPortfolio))
            {
                // сообщение в лог  сделать 
                MessageBox.Show(" еще нет портфеля ");
                return null;
            }
            Order order = new Order()
            {
                Price = prise,
                Volume = volume,
                Side = side,
                PortfolioNumber = StringPortfolio,
                TypeOrder = OrderPriceType.Market,
                NumberUser = NumberGen.GetNumberOrder(StartProgram.IsOsTrader),
                SecurityNameCode = sec.Name,
                SecurityClassCode = sec.NameClass,
            };
            //RobotsWindowVM.Log(Header, "SendMarketOrder\n " + " отправляем маркет на биржу\n" + GetStringForSave(order));
            RobotsWindowVM.SendStrTextDb(" CreateMarketOrder " + order.NumberUser);
            //Server.ExecuteOrder(order);

            return order;
        }

        #endregion
        #region  методы сервера ===========================

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
                
                SendStrStatus(" Сервер подключен ");
                //StartSecuritiy(SelectedSecurity);
                //SubscribeToServer();
            }
            else if (state == "Disconnect")
            {
                SendStrStatus(" Сервер отключен ");
            }
        }

        /// <summary>
        /// мой трейд с сервера
        /// </summary>
        private void _server_NewMyTradeEvent(MyTrade myTrade)
        {
            /* проверить обем трейда
             * продолжить логику 
             * 
             */
        }

        /// <summary>
        /// мой ордер с сервера
        /// </summary>
        private void _server_NewOrderIncomeEvent(Order myOrder)
        {
            CheckMyOrder(myOrder);
            /*  
             *  продолжить логику 
             *   
             */
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

                if (trade.Time.Second % 2 == 0)
                {
                    // test  LogicStartOpenPosition();
                }
            }
        }

        /// <summary>
        ///  подключиться к серверу
        /// </summary>
        private void SubscribeToServer()
        {
            _server.NewMyTradeEvent += _server_NewMyTradeEvent;
            _server.NewOrderIncomeEvent += _server_NewOrderIncomeEvent;
            _server.NewTradeEvent += _NewTradeEvent;
            _server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent += _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent += _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent += _server_ConnectStatusChangeEvent;

            RobotsWindowVM.Log(Header, " Подключаемся к серверу = " + _server.ServerType);
        }

        /// <summary>
        ///  отключиться от сервера 
        /// </summary>
        private void UnSubscribeToServer()
        {
            _server.NewMyTradeEvent -= _server_NewMyTradeEvent;
            _server.NewOrderIncomeEvent -= _server_NewOrderIncomeEvent;
            _server.NewTradeEvent -= _NewTradeEvent;
            _server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            //_server.PortfoliosChangeEvent -= _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;

            RobotsWindowVM.Log(Header, " Отключились от сервера = " + _server.ServerType);
        } 

        #endregion
        #region   сервисные методы ===========================

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
                        SaveParamsBot();
                        //GetOrderStatusOnBoard();
                        break;
                    }
                    Thread.Sleep(1000);
                }
            });
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
        /// берет номер портфеля  
        /// </summary>
        private Portfolio GetPortfolio(string number)
        {
            if (Server != null && Server.Portfolios != null)
            {
                foreach (Portfolio portf in Server.Portfolios)
                {
                    if (portf.Number == number)
                    {
                        RobotsWindowVM.Log(Header, " Выбран портфель =  " + portf.Number);
                        return portf;
                    }
                }
            }

            RobotsWindowVM.Log(Header, "GetStringPortfolios  портфель = null ");
            return null;
        }

        /// <summary>
        ///  обновились значения PropertyChange
        /// </summary>
        private void RobotBreakVM_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "BottomPositionPrice" || // список на которые надо реагировать
                e.PropertyName == "PartsPerInput" ||
                e.PropertyName == "TopPositionPrice" ||
                e.PropertyName == "BigСlusterPrice" ||
                e.PropertyName == "FullPositionVolume")            
            {
                SaveParamsBot();
            }  
        }

        /// <summary>
        /// сохранение параметров робота
        /// </summary>
        private void SaveParamsBot()
        {
            if (!Directory.Exists(@"Parametrs\Tabs"))
            {
                Directory.CreateDirectory(@"Parametrs\Tabs");
            }

            try
            {
                using (StreamWriter writer = new StreamWriter(@"Parametrs\Tabs\param_" + NumberTab + ".txt", false))
                {
                    writer.WriteLine(IsRun);
                    writer.WriteLine(Header);
                    writer.WriteLine(ServerType);
                    writer.WriteLine(StringPortfolio);

                    writer.WriteLine(TopPositionPrice);
                    writer.WriteLine(BigСlusterPrice);
                    writer.WriteLine(Direction);

                    writer.WriteLine(BottomPositionPrice);

                    writer.WriteLine(FullPositionVolume);

                    writer.WriteLine(PartsPerInput);     

                    writer.Close();
                    RobotsWindowVM.Log(Header, "SaveParamsBot  \n cохраненили  параметры ");
                }
            }
            catch (Exception ex)
            {
                RobotsWindowVM.Log(Header, " Ошибка сохранения параметров = " + ex.Message);
            }
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
                    bool run = false;
                    if (bool.TryParse(reader.ReadLine(), out run))
                    {
                        IsRun = run;
                    }
                    Header = reader.ReadLine(); // загружаем заголовок
                    servType = reader.ReadLine(); // загружаем название сервера
                    StringPortfolio = reader.ReadLine();

                    TopPositionPrice = GetDecimalForString(reader.ReadLine());
                    BigСlusterPrice = GetDecimalForString(reader.ReadLine());

                    Direction direct = Direction.BUY;
                    if (Enum.TryParse(reader.ReadLine(), out direct))
                    {
                        Direction = direct;
                    }

                    BottomPositionPrice = GetDecimalForString(reader.ReadLine());
                    FullPositionVolume = GetDecimalForString(reader.ReadLine());
                    PartsPerInput = (int)GetDecimalForString(reader.ReadLine());

                    //StepType step = StepType.PUNKT;
                    //if (Enum.TryParse(reader.ReadLine(), out step))
                    //{
                    //    StepType = step;
                    //}

                    //Levels = JsonConvert.DeserializeAnonymousType(reader.ReadLine(), new ObservableCollection<Level>());

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
        /// отправляет строку в статус окна с роботами
        /// </summary> 
        private void SendStrStatus(string txt)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            txt = Header + " " + timestamp + " ->" + txt;
            _robotsWindowVM.SendStrStatus(txt);
        }

        /// <summary>
        ///  преобразует строку из файла сохранения в децимал 
        /// </summary>
        private decimal GetDecimalForString(string str)
        {
            decimal value = 0;
            decimal.TryParse(str, out value);
            return value;
        }

        #endregion
        #endregion end metods==============================================

        /// <summary>
        /// Выбранная бумага
        /// </summary>
        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;

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

        /// <summary>
        /// делегат для команды старт/стоп
        /// </summary>
        public DelegateCommand CommandStartStop
        {
            get
            {
                if (_commandStartStop == null)
                {
                    _commandStartStop = new DelegateCommand(StartStop);
                }
                return _commandStartStop;
            }
        }
        private DelegateCommand _commandStartStop;

        #endregion end Commands ====================================================
    }
}
