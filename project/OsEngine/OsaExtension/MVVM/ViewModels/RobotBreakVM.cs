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
using System.Windows.Documents;

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
                    //TradeLogic();
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

        #endregion конец свойств =============================================

        #region Поля ==================================================

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
        }


        #region  Metods ======================================================================
        #region  методы логики ===============================================

        /// <summary>
        ///  логика набора позиций
        /// </summary>
        private void LogicStartOpenPosition()
        {
            if (BigСlusterPrice == 0)
            {
                SendStrStatus(" BigСlusterPrice = 0 ");
                return;
            }
            /*             
             *  расчет объема на ордер           
             */
            CalculPriceStartPos();
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
                //StartSecuritiy(SelectedSecurity);
                //SubscribeToServer();
            }
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
                    LogicStartOpenPosition();
                }
            }
        }

        #endregion
        #region   сервисные методы ===========================

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

        #endregion end Commands ====================================================
    }
}
