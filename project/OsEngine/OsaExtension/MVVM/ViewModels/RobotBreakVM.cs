using Com.Lmax.Api.MarketData;
using MahApps.Metro.Controls;
using Newtonsoft.Json;
using OsEngine.Charts.CandleChart.Indicators;
using OsEngine.Entity;
using OsEngine.Logging;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.Market.Servers.GateIo.Futures.Response;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.OsaExtension.MVVM.Models;
using OsEngine.OsaExtension.MVVM.View;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
using Order = OsEngine.Entity.Order;
using Position = OsEngine.Entity.Position;
//using Direction = OsEngine.Entity.Position;
using Security = OsEngine.Entity.Security;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    public class RobotBreakVM : BaseVM, IRobotVM, IDisposable
    {
        #region Свойства ВСЕ =====================================================

        #region Свойства всего робота ===================

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
        /// Объем выбраной бумаги на бирже
        /// </summary>
        public decimal SelectSecurBalans
        {
            get => _selectSecurBalans;
            set
            {
                _selectSecurBalans = value;
                OnPropertyChanged(nameof(SelectSecurBalans));
            }
        }
        private decimal _selectSecurBalans;

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

                _logger.Information(" seting IsRun  {Header} =  {IsRun} {Method}", Header, IsRun, nameof(IsRun));

                if (IsRun)
                {
                    //LoadParamsBot(Header);
                    //OpenPositionLogic();
                }
                else
                {
                    //StopTradeLogic();
                }
            }
        }
        private bool _isRun;

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
        private decimal _price = 0;
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
        /// Oбъем купленый роботом 
        /// </summary>
        public decimal VolumeRobExecut
        {
            get => _volumeRobExecut;
            set
            {
                _volumeRobExecut = value;
                OnPropertyChanged(nameof(VolumeRobExecut));
            }
        }
        private decimal _volumeRobExecut = 0;

        /// <summary>
        /// направление сделок 
        /// </summary>
        public Side Direction
        {
            get => _direction;
            set
            {
                _direction = value;
                OnPropertyChanged(nameof(Direction));
            }
        }
        private Side _direction;

        /// <summary>
        /// список  свойств направления сделок
        /// </summary> 
        public List<Side> Directions { get; set; } = new List<Side>()
        {
            Side.Buy, Side.Sell, Side.None
        };

        /// <summary>
        /// список названий портфелей 
        /// </summary>
        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();
        #endregion конец свойств всего робота

        #region Свойства позиции робота ==============================

        #region  свойства ActionPos ---------------------------------
        /// <summary>
        /// Действие с позициией в лонг
        /// </summary>
        public ActionPos ActionPositionLong
        {
            get => _actionPositionLong;
            set
            {
                _actionPositionLong = value;
                OnPropertyChanged(nameof(ActionPositionLong));
            }
        }
        private ActionPos _actionPositionLong;

        /// <summary>
        /// список  действий с позицией в лонг
        /// </summary> 
        public List<ActionPos> ActionPositionsLong { get; set; } = new List<ActionPos>()
        {
           ActionPos.Nothing, ActionPos.ShortenStop, ActionPos.Stop, ActionPos.RollOver, ActionPos.AddVolumes
        };

        /// <summary>
        /// Действие с позициией в шорт
        /// </summary>
        public ActionPos ActionPositionShort
        {
            get => _actionPositionShort;
            set
            {
                _actionPositionShort = value;
                OnPropertyChanged(nameof(ActionPositionShort));
            }
        }
        private ActionPos _actionPositionShort;

        /// <summary>
        /// список  действий с позицией в шорт
        /// </summary> 
        public List<ActionPos> ActionPositionsShort { get; set; } = new List<ActionPos>()
        {
            ActionPos.Nothing, ActionPos.ShortenStop, ActionPos.Stop, ActionPos.RollOver, ActionPos.AddVolumes
        };

        /// <summary>
        /// Действие с позициией при 1 превышении Buy
        /// </summary>
        public ActionPos ActionPosition1Buy
        {
            get => _actionPosition1Buy;
            set
            {
                _actionPosition1Buy = value;
                OnPropertyChanged(nameof(ActionPosition1Buy));
            }
        }
        private ActionPos _actionPosition1Buy;

        /// <summary>
        /// список  действий с позицией при 1 превышении Buy
        /// </summary> 
        public List<ActionPos> ActionPositions1Buy { get; set; } = new List<ActionPos>()
        {
            ActionPos.Nothing, ActionPos.ShortenStop, ActionPos.RollOver, ActionPos.Stop, ActionPos.AddVolumes
        };

        /// <summary>
        /// Действие с позициией при 2 превышении Buy
        /// </summary>
        public ActionPos ActionPosition2Buy
        {
            get => _actionPosition2Buy;
            set
            {
                _actionPosition2Buy = value;
                OnPropertyChanged(nameof(ActionPosition2Buy));
            }
        }
        private ActionPos _actionPosition2Buy;

        /// <summary>
        /// список  действий с позицией при 2 превышении Buy
        /// </summary> 
        public List<ActionPos> ActionPositions2Buy { get; set; } = new List<ActionPos>()
        {
            ActionPos.Nothing, ActionPos.ShortenStop, ActionPos.RollOver, ActionPos.Stop, ActionPos.AddVolumes
        };

        /// <summary>
        /// Действие с позициией при 1 превышении Sell
        /// </summary>
        public ActionPos ActionPosition1Sell
        {
            get => _actionPosition1Sell;
            set
            {
                _actionPosition1Sell = value;
                OnPropertyChanged(nameof(ActionPosition1Sell));
            }
        }
        private ActionPos _actionPosition1Sell;

        /// <summary>
        /// список  действий с позицией при 1 превышении Sell
        /// </summary> 
        public List<ActionPos> ActionPositions1Sell { get; set; } = new List<ActionPos>()
        {
            ActionPos.Nothing, ActionPos.ShortenStop, ActionPos.RollOver, ActionPos.Stop, ActionPos.AddVolumes
        };

        /// <summary>
        /// Действие с позициией при 2 превышении Sell
        /// </summary>
        public ActionPos ActionPosition2Sell
        {
            get => _actionPosition2Sell;
            set
            {
                _actionPosition2Sell = value;
                OnPropertyChanged(nameof(ActionPosition2Sell));
            }
        }
        private ActionPos _actionPosition2Sell;

        /// <summary>
        /// список  действий с позицией при 2 превышении Sell
        /// </summary> 
        public List<ActionPos> ActionPositions2Sell { get; set; } = new List<ActionPos>()
        {
            ActionPos.Nothing, ActionPos.ShortenStop, ActionPos.RollOver, ActionPos.Stop, ActionPos.AddVolumes
        };

        #endregion --------------------------------------------------

        /// <summary>
        ///  средняя цена позиции
        /// </summary>
        public decimal EntryPricePos
        {
            get { return _entryPricePos; }
            set
            {
                _entryPricePos = value;
                OnPropertyChanged(nameof(EntryPricePos));
            }
        }
        private decimal _entryPricePos = 0;

        /// <summary>
        /// Цена профита лонг
        /// </summary>
        public decimal TakePriceLong
        {
            get => _takePriceLong;
            set
            {
                _takePriceLong = value;
                OnPropertyChanged(nameof(TakePriceLong));
            }
        }
        private decimal _takePriceLong;

        /// <summary>
        /// Цена профита шорт
        /// </summary>
        public decimal TakePriceShort
        {
            get => _takePriceShort;
            set
            {
                _takePriceShort = value;
                OnPropertyChanged(nameof(TakePriceShort));
            }
        }
        private decimal _takePriceShort;

        /// <summary>
        /// стартовая цена наобра позиции
        /// </summary>
        public decimal StartPriceOpenPos
        {
            get => _startPriceOpenPos;
            set
            {
                if (value != _startPriceOpenPos)
                {
                    _startPriceOpenPos = value;
                    OnPropertyChanged(nameof(StartPriceOpenPos));
                }
            }
        }
        private decimal _startPriceOpenPos;

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
        /// количесвто частей на выход (закрытие)
        /// </summary>
        public int PartsPerExit
        {
            get => _partsPerExit;
            set
            {
                _partsPerExit = value;
                OnPropertyChanged(nameof(PartsPerExit));
            }
        }
        private int _partsPerExit = 1;

        /// <summary>
        /// Oбъем на ордер открытия (часть позиции)
        /// </summary>
        public decimal VolumePerOrderOpen
        {
            get => _volumePerOrderOpen;
            set
            {
                _volumePerOrderOpen = value;
                OnPropertyChanged(nameof(VolumePerOrderOpen));
            }
        }
        private decimal _volumePerOrderOpen = 0;

        /// <summary>
        /// цена стопов для шота
        /// </summary>
        public decimal PriceStopShort
        {
            get => _priceStopShort;
            set
            {
                _priceStopShort = value;
                OnPropertyChanged(nameof(PriceStopShort));
            }
        }
        private decimal _priceStopShort = 0;

        /// <summary>
        /// цена стопов для лонга
        /// </summary>
        public decimal PriceStopLong
        {
            get => _priceStopLong;
            set
            {
                _priceStopLong = value;
                OnPropertyChanged(nameof(PriceStopLong));
            }
        }
        private decimal _priceStopLong = 0;

        /// <summary>
        /// Oбъем ордеров на открытие позийии 
        /// </summary>
        public decimal VolumeOpen
        {
            get => _volumeOpen;
            set
            {
                _volumeOpen = value;
                OnPropertyChanged(nameof(VolumeOpen));
            }
        }
        private decimal _volumeOpen = 0;
        /// <summary>
        /// вкл выкл трейлинг стоп лонга
        /// </summary>
        public bool IsChekTraelStopLong
        {
            get => _isChekTraelStopLong;
            set
            {
                _isChekTraelStopLong = value;
                OnPropertyChanged(nameof(IsChekTraelStopLong));
            }
        }
        private bool _isChekTraelStopLong;

        /// <summary>
        /// вкл выкл трейлинг стоп шорта 
        /// </summary>
        public bool IsChekTraelStopShort
        {
            get => _isChekTraelStopShort;
            set
            {
                _isChekTraelStopShort = value;
                OnPropertyChanged(nameof(IsChekTraelStopShort));
            }
        }
        private bool _isChekTraelStopShort;

        /// <summary>
        /// проверить объем на закрытие
        /// </summary>
        public bool IsChekVolumeClose
        {
            get => _isChekVolumeClose;
            set
            {
                _isChekVolumeClose = value;
                OnPropertyChanged(nameof(IsChekVolumeClose));
            }
        }
        private bool _isChekVolumeClose;

        /// <summary>
        /// записывать все логи 
        /// </summary>
        public bool IsChekSendAllLogs
        {
            get => _isChekSendAllLogs;
            set
            {
                _isChekSendAllLogs = value;
                OnPropertyChanged(nameof(IsChekSendAllLogs));
            }
        }
        private bool _isChekSendAllLogs;

        /// <summary>
        /// отправлен стоп
        /// </summary>
        private bool _sendStop = false;

        /// <summary>
        /// сработал стоп
        /// </summary>
        private bool _isWorkedStop = false;

        /// <summary>
        /// отправлен закрывающий объем по маркету
        /// </summary>
        public bool _sendCloseMarket = false;

        /// <summary>
        /// расстояние до трейлинг профита в лонг  % 
        /// </summary>
        public decimal StepPersentStopLong
        {
            get => _stepPersentStopLong;
            set
            {
                _stepPersentStopLong = value;
                OnPropertyChanged(nameof(StepPersentStopLong));
            }
        }
        private decimal _stepPersentStopLong = 1;

        /// <summary>
        /// расстояние до трейлин профита шорта в % 
        /// </summary>
        public decimal StepPersentStopShort
        {
            get => _stepPersentStopShort;
            set
            {
                _stepPersentStopShort = value;
                OnPropertyChanged(nameof(StepPersentStopShort));
            }
        }
        private decimal _stepPersentStopShort = 1;

        /// <summary>
        /// расстояние до включения трейлинг профита лонг в % 
        /// </summary>
        public decimal StepPersentStopLongRun
        {
            get => _stepPersentStopLongRun;
            set
            {
                _stepPersentStopLongRun = value;
                OnPropertyChanged(nameof(StepPersentStopLongRun));
            }
        }
        private decimal _stepPersentStopLongRun = 1;

        /// <summary>
        /// расстояние до включения трейлинг профита шорта в % 
        /// </summary>
        public decimal StepPersentStopShortRun
        {
            get => _stepPersentStopShortRun;
            set
            {
                _stepPersentStopShortRun = value;
                OnPropertyChanged(nameof(StepPersentStopShortRun));
            }
        }
        private decimal _stepPersentStopShortRun = 1; 

        /// <summary>
        /// список позиций робота 
        /// </summary>       
        private ObservableCollection<Position> PositionsBots { get; set; } = new ObservableCollection<Position>();

        #endregion конец свойств позиции робота

        #region Свойства для отработки обемов по монете ====================

        /// <summary>
        /// сколько N минут считать объем
        /// </summary>
        public int N_min
        {
            get => _n_min;
            set
            {
                _n_min = value;
                OnPropertyChanged(nameof(N_min));
            }
        }
        private int _n_min = 5;

        /// <summary>
        /// объем покупок выбраной монеты за период
        /// </summary>
        public decimal BidVolumPeriod
        {
            get => _bidVolumPeriod;
            set
            {
                _bidVolumPeriod = value;
                OnPropertyChanged(nameof(BidVolumPeriod));
            }
        }
        private decimal _bidVolumPeriod;

        /// <summary>
        /// объем продаж выбраной монеты за период
        /// </summary>
        public decimal AskVolumPeriod
        {
            get => _askVolumPeriod;
            set
            {
                _askVolumPeriod = value;
                OnPropertyChanged(nameof(AskVolumPeriod));
            }
        }
        private decimal _askVolumPeriod;

        /// <summary>
        /// объемы за N минуту
        /// </summary>
        public decimal AllVolumPeroidMin
        {
            get => _allVolumPeroidMin;
            set
            {
                _allVolumPeroidMin = value;
                OnPropertyChanged(nameof(AllVolumPeroidMin));
            }
        }
        private decimal _allVolumPeroidMin;

        /// <summary>
        /// средний допустимый объем торгов по тикам за N минут
        /// </summary>
        public decimal Avereg
        {
            get => _avereg;
            set
            {
                _avereg = value;
                OnPropertyChanged(nameof(Avereg));
            }
        }
        private decimal _avereg;

        /// <summary>
        /// средний допустимый объем торгов по тикам в $
        /// </summary>
        public int AveregS
        {
            get => _averegS;
            set
            {
                _averegS = value;
                OnPropertyChanged(nameof(AveregS));
            }
        }
        private int _averegS;

        /// <summary>
        /// 1 коэффициент увеличения объема покупок для срабатывания
        /// </summary>
        public decimal RatioBuy1
        {
            get => _ratioBuy1;
            set
            {
                _ratioBuy1 = value;
                OnPropertyChanged(nameof(RatioBuy1));
            }
        }
        private decimal _ratioBuy1 = 1.7m;

        /// <summary>
        /// 1 коэффициент увеличения объема продаж для срабатывания 
        /// </summary>
        public decimal RatioSell1
        {
            get => _ratioSell1;
            set
            {
                _ratioSell1 = value;
                OnPropertyChanged(nameof(RatioSell1));
            }
        }
        private decimal _ratioSell1;

        /// <summary>
        /// 2 коэффициент увеличения объема покупок для срабатывания
        /// </summary>
        public decimal RatioBuy2
        {
            get => _ratioBuy2;
            set
            {
                _ratioBuy2 = value;
                OnPropertyChanged(nameof(RatioBuy2));
            }
        }
        private decimal _ratioBuy2 = 2.5m;

        /// <summary>
        /// 2 коэффициент увеличения объема продаж для срабатывания
        /// </summary>
        public decimal RatioSell2
        {
            get => _ratioSell2;
            set
            {
                _ratioSell2 = value;
                OnPropertyChanged(nameof(RatioSell2));
            }
        }
        private decimal _ratioSell2;

        #endregion конец свойств для отработки объемов по монете

        #region поля отработки объемов по монете ====================

        /// <summary>
        /// покупики больше средней 1 коэф.
        /// </summary>
        private bool Buy1MoreAvereg = false;
        /// <summary>
        /// покупики больше средней 2 коэф.
        /// </summary>
        private bool Buy2MoreAvereg = false;
        /// <summary>
        /// продажи больше средней 1 коэф.
        /// </summary>
        private bool Sell1MoreAvereg = false;
        /// <summary>
        /// продажи больше средней 2 коэф.
        /// </summary>
        private bool Sell2MoreAvereg = false;

        /// <summary>
        /// время учета трейдов
        /// </summary>
        private DateTime dateTradingPeriod = DateTime.MinValue;

        /// <summary>
        /// время отсрочки срабатывания превышен обема коэф 1
        /// </summary>
        DateTime time_add_n_min1 = DateTime.MinValue;

        /// <summary>
        /// время отсрочки срабатывания превышен обема коэф 2
        /// </summary>
        DateTime time_add_n_min2 = DateTime.MinValue;

        #endregion  конец поля отрпботки обемов =====================

        #endregion конец ВСЕ свойств =============================================

        #region Поля ==================================================

        decimal bidVolumPeriod = 0;
        decimal askVolumPeriod = 0;
        decimal allVolumPeroidMin = 0;

        /// <summary>
        /// блокировка отправки ордеров
        /// </summary>
        private object orderSendLock;

        /// <summary>
        /// расчетные цены закрытия позиции 
        /// </summary>
        private List<decimal> _priceClosePos = new List<decimal>();


        /// <summary>
        /// Oбъем на ордер закрытия (части позиции)
        /// </summary>
        private decimal _volumePerOrderClose = 0;

        /// <summary>
        /// расчетные цены открытия позиции 
        /// </summary>
        private List<decimal> _priceOpenPos = new List<decimal>();

        /// <summary>
        /// список типов расчета шага 
        /// </summary>
        public List<StepType> StepTypes { get; set; } = new List<StepType>()
        {
            StepType.PUNKT, StepType.PERCENT
        };

        /// <summary>
        /// поле логера RobotBreakVM
        /// </summary>
        ILogger _logger;

        /// <summary>
        /// названия портфеля для отправки ордера на биржу
        /// </summary>
        Portfolio _portfolio;

        /// <summary>
        /// поле содержащее VM окна отображ всех роботов
        /// </summary>
        private RobotsWindowVM _robotsWindowVM ;

        #endregion

        /// <summary>
        /// конструктор для ранне  созданого и сохранеенного робота
        /// </summary>
        public RobotBreakVM(string header, int numberTab, RobotsWindowVM MainWindows)
        {
            _logger = Serilog.Log.Logger.ForContext<RobotBreakVM>();

            NumberTab = numberTab;
            Header = header;
            _robotsWindowVM = MainWindows;

            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;

            LoadParamsBot(header);

            ServerMaster.ActivateAutoConnection();

            PropertyChanged += RobotBreakVM_PropertyChanged;
            SendStrStatus(" Ожидается подключение к бирже ");

            ClearFailOrderPosition();

            orderSendLock = new object();
        }

        #region  Metods ======================================================================

        #region  методы логики ===============================================

        /// <summary>
        /// открытие позиции
        /// </summary>
        private void OpenPositionLogic()
        {
            if (IsRun)
            {
                SetStopTrue();
                CreateNewPosition(); // создали позиции
                SendOpenOrderPosition();// открытие позиции
            }
        }

        /// <summary>
        /// остановить торговлю закрыть Все позиции робота
        /// </summary>
        private void StopTradeLogic()
        {
            if (IsChekSendAllLogs) _logger.Information("Stop Trade Logic {Header} {Method}"
                                 , Header, nameof(StopTradeLogic));
            GetBalansSecur();

            for (int i = 0; i < PositionsBots.Count && !_sendStop; i++)
            {
                if (SelectSecurBalans != 0 && !_sendStop)
                {
                    decimal openVolume = SelectSecurBalans;

                    FinalCloseMarketOpenVolume(PositionsBots[i], openVolume);
                    SelectSecurBalans = 0;
                    _sendStop = true;
                }

                bool aktivOrders = false;
                aktivOrders = ActivOrders(PositionsBots[i]);

                if (aktivOrders) // привязка к ордерам видимым в роботе
                
                DeleteAllOrdersPositionExchange();                
            }
            IsRun = false;

            ClearFailOrderPosition();
            ClearCanceledOrderPosition();
            ClearingVariablesAfterClosing();
            // перепроверяем монету
        }

        /// <summary>
        /// перепроверка состояния монеты на бирже
        /// </summary>
        private void RecheckingStatusCoinExchange()
        {
            /*
             * запросить баланс монеты
             * запросить активные ордера монеты
             * 
             */
        }

        /// <summary>
        /// отключение бота
        /// </summary>
        private void IsOffBot()
        {
            /* если цена вышла за пределы планируемого диапазона (тейков)
            * отработки патерна робота, лимитки на открытие надо удалить с биржи 
            * и прекратить работу бота */

            if (SelectedSecurity != null && Price != 0 && IsRun)
            {
                if (Price > TakePriceLong || Price < TakePriceShort)
                {
                    _logger.Warning(" Bot OFF, Price > TakePriceLong || Price < TakePriceShort " +
                                    " the price has gone beyond profit  {Method}"
                                                             , nameof(IsOffBot));
                    StopTradeLogic();
                }
            }
        }

        /// <summary>
        /// добавить сторонний ордер в робота
        /// </summary>
        private void AddOrderPosition(Order order)
        {
            /* проверяем наш или левый 
             * проверяем направление ордера и сделки
             * добавляем в сделку 
             */

            for (int i = 0; i < PositionsBots.Count; i++)
            {
                List<Order> ordersAll = new List<Order>();

                ordersAll.Clear();

                //Order ordAdd = new Order();p

                ordersAll = PositionsBots[i].OpenOrders;// взять из позиции ордера открытия 

                if (PositionsBots[i].CloseOrders != null)
                {
                    ordersAll.AddRange(PositionsBots[i].CloseOrders); // добавили ордера закрытия 
                }

                bool levak = true;
                for (int c = 0; c < ordersAll.Count; c++)
                {
                    if (ordersAll[c].NumberUser == order.NumberUser)
                    {
                        levak = false;
                        //if (order.State == OrderStateType.Cancel)
                        //{
                        //    DeleteOrderPosition(order);
                        //}
                    }
                }

                if (levak && order.State == OrderStateType.Activ)
                {
                    if (PositionsBots[i].Direction == Side.Buy && order.Side == Side.Buy)
                    {
                        PositionsBots[i].AddNewOpenOrder(order);
                        _logger.Information("Send Limit order for Open Orders {Method} {@Order} {NumberUser}",
                                                             nameof(AddOrderPosition), order, order.NumberUser);
                    }
                    if (PositionsBots[i].Direction == Side.Sell && order.Side == Side.Sell)
                    {
                        PositionsBots[i].AddNewOpenOrder(order);
                        _logger.Information("Send Limit order for Open Orders {Method} {@Order} {NumberUser}",
                                                            nameof(AddOrderPosition), order, order.NumberUser);
                    }
                    if (PositionsBots[i].Direction == Side.Buy && order.Side == Side.Sell)
                    {
                        PositionsBots[i].AddNewCloseOrder(order);
                        _logger.Information("Send Limit order for Close Orders {Method} {@Order} {NumberUser}",
                                                            nameof(AddOrderPosition), order, order.NumberUser);
                    }
                    if (PositionsBots[i].Direction == Side.Sell && order.Side == Side.Buy)
                    {
                        PositionsBots[i].AddNewCloseOrder(order);
                        _logger.Information("Send Limit order for Close Orders {Method} {@Order} {NumberUser}",
                                                           nameof(AddOrderPosition), order, order.NumberUser);
                    }
                }
            }
        }

        /// <summary>
        /// удаление отменённых ордер из позиции робота
        /// </summary>
        private void DeleteOrderPosition(Order order)
        {
            /* проверяем наш или левый 
             * проверяем направление ордера и сделки
             * удаляем из сделки 
             */

            foreach (Position position in PositionsBots)
            {
                List<Order> ordersAll = new List<Order>();

                Order ordAdd = new Order();

                ordersAll = position.OpenOrders;// взять из позиции ордера открытия 

                if (position.CloseOrders != null)
                {
                    ordersAll.AddRange(position.CloseOrders); // добавили ордера закрытия 
                }

                bool levak = true;
                for (int i = 0; i < ordersAll.Count; i++)
                {
                    if (ordersAll[i].NumberUser == order.NumberUser)
                    {
                        levak = false;
                    }
                }

                if (!levak && order.State == OrderStateType.Cancel)
                {
                    if (position.Direction == Side.Buy && order.Side == Side.Buy)
                    { // ордер открытия
                        position.OpenOrders.Remove(order);

                        if (IsChekSendAllLogs) _logger.Information("Delete order for Open Orders {Method} {@Order} {NumberUser}",
                                                             nameof(DeleteOrderPosition), order, order.NumberUser);
                    }
                    if (position.Direction == Side.Sell && order.Side == Side.Sell)
                    {// ордер открытия
                        position.OpenOrders.Remove(order);
                        if (IsChekSendAllLogs) _logger.Information("Delete Limit order for Open Orders {Method} {@Order} {NumberUser}",
                                                            nameof(DeleteOrderPosition), order, order.NumberUser);
                    }
                    if (position.Direction == Side.Buy && order.Side == Side.Sell)
                    { // ордер закрытия
                        position.CloseOrders.Remove(order);
                        if (IsChekSendAllLogs) _logger.Information("Delete Limit order for Close Orders {Method} {@Order} {NumberUser}",
                                                            nameof(DeleteOrderPosition), order, order.NumberUser);
                    }
                    if (position.Direction == Side.Sell && order.Side == Side.Buy)
                    {// ордер закрытия
                        position.CloseOrders.Remove(order);

                        if (IsChekSendAllLogs) _logger.Information("Delete Limit order for Close Orders {Method} {@Order} {NumberUser}",
                                                           nameof(DeleteOrderPosition), order, order.NumberUser);
                    }
                }
            }
        }

        /// <summary>
        /// сопровождение открытого объема 
        /// </summary>
        private void MaintenanOpenVolume()
        {
            /* позиции нет а обем открытый есть
             * создать ордера закрытия и выставить
             * или позиции нет а ордера на открытие есть
             * или открытый объем и ордера открытия
             * или открытый объем и ордера закрытия 
             * или открытый объем и ордера на закрытие и открытие 
             * сформировать сделку в роботе
             * включить трейлинг  
             */

            if (PositionsBots.Count == 0 && SelectSecurBalans != 0)
            {
                Position positionNew = new Position();
              
                //проверяем направление сделки 
                if (SelectSecurBalans > 0) // лонг
                {
                    Direction = Side.Buy;
                    positionNew.Direction = Direction;
                    positionNew.State = PositionStateType.Closing;
                    positionNew.SecurityName = SelectedSecurity.Name;

                    decimal vol = CalculateVolumeTradesClose();

                    PositionsBots.Add(positionNew);

                    _logger.Warning("Сreated a new long position and added PositionsBots  {Method}  {@positionNew}",
                                                                  nameof(MaintenanOpenVolume), positionNew);

                    SendCloseLimitOrderPosition(positionNew, vol);
                }
                if (SelectSecurBalans < 0) // шорт
                {
                    Direction = Side.Sell;
                    positionNew.Direction = Direction;
                    positionNew.State = PositionStateType.Closing;
                    positionNew.SecurityName = SelectedSecurity.Name;
                    decimal vol = CalculateVolumeTradesClose();
                    PositionsBots.Insert(0, positionNew);

                    _logger.Warning("Сreated a new short position and added PositionsBots  {Method}  {@positionNew}",
                                                                nameof(MaintenanOpenVolume), positionNew);

                    SendCloseLimitOrderPosition(positionNew, vol);
                }
            }
        }

        /// <summary>
        /// отчистка отменённых ордер из позиции робота
        /// </summary>
        private void ClearCanceledOrderPosition()// перепроверить
        {
            foreach (Position position in PositionsBots)
            {
                if (position.OpenOrders != null)
                {
                    List<Order> orders = new List<Order>();
                    orders = position.OpenOrders; // взять из позиции ордера открытия
                    ClearOrdersCancel(ref orders);
                    position.OpenOrders = orders;
                }

                if (position.CloseOrders != null)
                {
                    List<Order> orders = new List<Order>();
                    orders = position.CloseOrders; // положили ордера закрытия 
                    ClearOrdersCancel(ref orders);
                    position.CloseOrders = orders; // вернули ордера закрытия 
                }
            }
        }

        /// <summary>
        /// Удаляет ордера Cancel из списков ордеров 
        /// </summary>
        public void ClearOrdersCancel(ref List<Order> orders)
        {
            if (orders == null) return;
            List<Order> newOrders = new List<Order>();

            foreach (Order order in orders)
            {
                if (order != null
                    && order.State != OrderStateType.Cancel)
                //&& order.State != OrderStateType.Done)
                //&& order.State != OrderStateType.Fail)
                {
                    newOrders.Add(order);
                }
            }
            if (IsChekSendAllLogs) _logger.Information("Clear Orders cancel {Method}", nameof(ClearOrdersCancel));
            orders = newOrders;
        }

        /// <summary>
        ///  удаление ошибочных ордеров из позиции робота
        /// </summary>
        private void ClearFailOrderPosition()// перепроверить
        {
            foreach (Position position in PositionsBots)
            {
                if (position.OpenOrders != null)
                {
                    List<Order> orders = new List<Order>();
                    orders = position.OpenOrders; // взять из позиции ордера открытия
                    ClearOrdersFail(ref orders);
                    position.OpenOrders = orders;
                }

                if (position.CloseOrders != null)
                {
                    List<Order> orders = new List<Order>();
                    orders = position.CloseOrders; // положили ордера закрытия 
                    ClearOrdersFail(ref orders);
                    position.CloseOrders = orders; // вернули ордера закрытия 
                }
            }
        }

        /// <summary>
        /// Удаляет  Fail ордера из списков ордеров 
        /// </summary>
        public void ClearOrdersFail(ref List<Order> orders)
        {
            if (orders == null) return;
            List<Order> newOrders = new List<Order>();

            foreach (Order order in orders)
            {
                if (order != null && order.State != OrderStateType.Fail)
                {
                    newOrders.Add(order);
                }
            }
            if (IsChekSendAllLogs) _logger.Information("Clear Fail Orders {Method}", nameof(ClearOrdersFail));
            orders = newOrders;
        }

        /// <summary>
        /// Закрыть позицию по стопу
        /// </summary>
        private void StopPosition(Position position)
        {
            GetBalansSecur();

            if (!_sendStop)
            {
                _sendStop = true;
                _logger.Warning(" It worked StopPosition {@position} {Metod} "
                                             , position, nameof(StopPosition));

                decimal volume = SelectSecurBalans;

                FinalCloseMarketOpenVolume(position, volume);
            }

            bool aktivOrders = false;
            aktivOrders = ActivOrders(position);

            if (aktivOrders) // привязка к ордерам видимым в роботе
            {
                DeleteAllOrdersPositionExchange();
            }
        }

        /// <summary>
        /// продать по меркету все монеты на бирже
        /// </summary>
        private void FinalCloseMarketOpenVolume(Position pos, decimal volume)
        {
            decimal finalVolumClose = 0;
            finalVolumClose = volume;// берем открытый объем 

            if (SelectSecurBalans == 0) // 
            {
                _logger.Warning(" SelectSecurBalans == 0 {Metod} ", nameof(FinalCloseMarketOpenVolume));
                return;
            }

            Side sideClose = Side.None;
            if (pos.Direction == Side.Buy)
            {
                sideClose = Side.Sell;
                if (IsChekSendAllLogs) _logger.Information("In Position volume {Volume} {side} {Metod} ",
                                       finalVolumClose, sideClose, nameof(FinalCloseMarketOpenVolume));
            }
            if (pos.Direction == Side.Sell)
            {
                sideClose = Side.Buy;
                if (IsChekSendAllLogs) _logger.Information("In Position volume {Volume} {side} {Metod} ",
                                        finalVolumClose, sideClose, nameof(FinalCloseMarketOpenVolume));
            }
            if (finalVolumClose == 0 || sideClose == Side.None)
            {
                SendStrStatus(" Ошибка закрытия объема на бирже");

                _logger.Error(" Error create Market orders to close " +
                    "the volume {finalVolumClose} {side} {Metod} "
                             , finalVolumClose, sideClose, nameof(FinalCloseMarketOpenVolume));
                return;
            }

            Order ordClose = CreateMarketOrder(SelectedSecurity, Price, finalVolumClose, sideClose);

            if (ordClose != null && !_sendCloseMarket)
            {
                
                if (sideClose == Side.None) return;

                pos.AddNewCloseOrder(ordClose);
                //Thread.Sleep(50);               

                lock (orderSendLock)
                {
                    _sendCloseMarket = true;

                    SendOrderExchange(ordClose);

                    //Thread.Sleep(100);

                    _logger.Information("Sending FINAL Market order to close " +
                                    " {volume} {numberUser} {@Order} {Metod} ",
                     finalVolumClose, ordClose.NumberUser, ordClose, nameof(FinalCloseMarketOpenVolume));

                    SendStrStatus(" Отправлен Маркет на закрытие объема на бирже");
                }

    
            }
            if (ordClose == null)
            {
                _sendCloseMarket = false;

                SendStrStatus(" Ошибка закрытия объема на бирже");

                _logger.Error(" Error sending FINAL the Market to close the volume {@Order} {Metod} ",
                                                        ordClose, nameof(FinalCloseMarketOpenVolume));
            }
        }

        /// <summary>
        /// есть активные ордера
        /// </summary>
        private bool ActivOrders(Position position)
        {
            bool res = false;
            GetBalansSecur();

            bool orderClose = position.CloseActiv;//  есть активные ордера закрытия 
            bool orderOpen = position.OpenActiv; // активные ордера открытия 

            if (orderOpen)
            {
                res = true;
            }
            if (orderClose)
            {
                res = true;
            }
            return res;
        }

        /// <summary>
        /// метод отработки кнопки старт/стоп
        /// </summary>
        private void StartStop(object o)
        {
            Thread.Sleep(300);

            IsRun = !IsRun;

            _logger.Information("StartStop  {Header} {IsRun} {Method}", Header, IsRun, nameof(StartStop));
            //RobotsWindowVM.Log(Header, " \n\n StartStop = " + IsRun);

            SaveParamsBot();
            if (IsRun)
            {
                OpenPositionLogic();
            }
            else
            {
                StopTradeLogic();
                SetStopTrue();


                Task.Run(() =>
                {

                });
            }
        }

        /// <summary>
        ///  отправить ордер на биржу 
        /// </summary>
        private void SendOrderExchange(Order sendOpder)
        {
            if (sendOpder.TypeOrder == OrderPriceType.Market) _sendCloseMarket = true;

            Server.ExecuteOrder(sendOpder);
            Thread.Sleep(30);
            if (IsChekSendAllLogs) _logger.Information("Send order Exchange {Method} Order {@Order} {NumberUser} ", nameof(SendOrderExchange), sendOpder, sendOpder.NumberUser);

            SendStrStatus(" Ордер отправлен на биржу");
        }

        /// <summary>
        /// добавить открывающий ордер в позицию и на биржу 
        /// </summary>  
        private void SendOpenOrderPosition()
        {
            CalculateVolumeTradesOpen(); // расчет объема

            for (int i = 0; i < PositionsBots.Count; i++)
            {
                Position position = PositionsBots[i];
                if (position.OpenOrders != null)
                {
                    if (position.OpenOrders.Count != 0) // защита от повтороных добалений ордеров
                    {
                        position.OpenOrders.Clear();
                    }
                }

                List<decimal> _prices;
                _prices = CalculPriceStartPos(position.Direction); // расчет цены открытия позиции

                if (StartPriceOpenPos == 0 || BottomPositionPrice == 0 || _prices.Count == 0)
                {
                    SendStrStatus(" BigСlusterPrice или BottomPositionPrice = 0 ");
                    return;
                }
                foreach (decimal price in _prices)
                {
                    Order order = CreateLimitOrder(SelectedSecurity, price, VolumePerOrderOpen, position.Direction);
                    if (order != null)
                    {
                        //position.PassOpenOrder =false;
                        position.AddNewOpenOrder(order);// отправили ордер в позицию
                        SendOrderExchange(order); // отправили ордер на биржу
                        //Thread.Sleep(50);
                        if (IsChekSendAllLogs) _logger.Information("Send Open order into position {Method} {@Order} {NumberUser}", nameof(SendOrderExchange), order, order.NumberUser);
                    }
                }
            }
        }

        /// <summary>
        /// взять объем ордеров на открытие в позиции робота
        /// </summary>
        private void GetVolumeOpen(Position position)
        {
            decimal volumOrdersOpen = 0; // по ордерам закрытия объем
            if (position.OpenOrders != null)
            {
                for (int i = 0; i < position.OpenOrders.Count; i++)
                {
                    volumOrdersOpen += position.OpenOrders[i].Volume;
                }
            }
            VolumeOpen = volumOrdersOpen;
        }

        /// <summary>
        /// проверять баланс ордеров зак и откр 
        /// </summary>
        private void MaintainingVolumeBalance(Position position)
        {
            decimal minVolumeExecut = SelectedSecurity.MinTradeAmount;

            if (position.SecurityName == null) return;

            if (PositionsBots.Count == 0) MaintenanOpenVolume();

            GetVolumeOpen(position);

            VolumeRobExecut = position.OpenVolume;

            decimal volumOrderClose = 0; // по ордерам закрытия объем
            if (position.CloseOrders != null)
            {
                for (int i = 0; i < position.CloseOrders.Count; i++)
                {
                    volumOrderClose += position.CloseOrders[i].Volume;
                }
            }
            //  на бирже открытый обем больше обема ордеров закрытия
            // значит где-то ошибка или купили помимо робота
            // доставить лимитку закрытия
            if (SelectSecurBalans < volumOrderClose)
            {
                //_logger.Warning(" Volume on the stock exchange < Volum order close  {Method}  {vol} {@Position} ",
                //                                nameof(MaintainingVolumeBalance), volumOrderClose, position);
            }

            if (SelectSecurBalans > volumOrderClose)
            {
                GetBalansSecur();

                if (IsChekSendAllLogs)
                {
                    _logger.Warning(" Volume on the stock exchange > Volum order close {Header} {Method}",
                                                          Header, nameof(MaintainingVolumeBalance));
                }

                if (IsChekVolumeClose)
                {
                    decimal vol = Decimal.Round(SelectSecurBalans - volumOrderClose, SelectedSecurity.DecimalsVolume);
                    if (vol < minVolumeExecut)
                    {
                        _logger.Error("Volum close < min Volume Execut {Method}  {vol} {@Position} ",
                                                  nameof(MaintainingVolumeBalance), vol, position);
                        return;
                    }
                    _logger.Warning(" Open Volume stock market larger than the closing orders  {Method} {SelectSecurBalans} {activCloseVol} {@position} {volum}",
                          nameof(MaintainingVolumeBalance), SelectSecurBalans, volumOrderClose, position, vol);
                    SendCloseLimitOrderPosition(position, vol);
                    IsChekVolumeClose = false;
                }
            }
            if (!MonitoringOpenVolumeExchange() && !ActivOrders(position)) // если нету робот не работает
            {
                if (IsRun) // нафига он включен
                {
                    _logger.Warning(" there is no open volume and active orders on the exchange, the robot is turned off " +
                        "{Header}  {Method} {SelectSecurBalans} {@position} ",
                        Header, nameof(MaintainingVolumeBalance), SelectSecurBalans, position);

                    SendStrStatus("Робот выключен " + Header);
                    IsRun = false; // выключаем

                    ClearingVariablesAfterClosing();
                    ClearCanceledOrderPosition();
                }
            };
        }

        /// <summary>
        ///  костыль на потеряные ордера закрытия
        /// </summary>
        private void AddCloseOrder()
        {
            // если есть разница обемов в открытых терйдах и
            // обемом в ордерах на закрытие
            // значит проебаны выставление ордеров закрытия
            // доставить лимитку закрытия

            if (SelectSecurBalans == 0) return; // если нет открытого на бирже это метод не нужен 

            decimal minVolumeExecut = SelectedSecurity.MinTradeAmount;

            for (int a = 0; a < PositionsBots.Count; a++)
            {
                if (PositionsBots[a].Direction == Side.None)
                {
                    PositionsBots[a].Direction = (Side)Direction;
                }
                VolumeRobExecut = PositionsBots[a].OpenVolume;

                decimal volumeInTradesOpenOrd = 0; // по терйдам откр объем
                if (PositionsBots[a].OpenOrders != null && PositionsBots[a].MyTrades.Count > 0)
                {
                    for (int i = 0; i < PositionsBots[a].OpenOrders.Count; i++)
                    {
                        if (PositionsBots[a].OpenOrders[i].MyTrades == null) continue;

                        for (int j = 0; j < PositionsBots[a].OpenOrders[i].MyTrades.Count; j++)
                        {
                            if (PositionsBots[a].OpenOrders[i].MyTrades[j] == null) continue;

                            volumeInTradesOpenOrd += PositionsBots[a].OpenOrders[i].MyTrades[j].Volume;
                        }
                    }
                }
                decimal volumInOrderClose = 0; // по ордерам закрытия объем
                if (PositionsBots[a].CloseOrders != null && PositionsBots[a].MyTrades.Count > 0)
                {
                    for (int i = 0; i < PositionsBots[a].CloseOrders.Count; i++)
                    {
                        volumInOrderClose += PositionsBots[a].CloseOrders[i].Volume;
                    }
                }
                if (volumeInTradesOpenOrd > volumInOrderClose)
                {
                    _logger.Warning(" Open Volume > Volume Close orders {Header} {Method}", Header, nameof(AddCloseOrder));

                    if (IsChekVolumeClose) // разрешено добавить включать руками
                    {
                        decimal vol = Decimal.Round(volumeInTradesOpenOrd - volumInOrderClose, SelectedSecurity.DecimalsVolume);
                        if (vol < minVolumeExecut)
                        {
                            _logger.Error(" Open VolExecut - Close Vol <  MinTradeAmount {Method}", nameof(AddCloseOrder));
                            return;
                        }
                        _logger.Warning("Open Volume is larger than the closing orders {Header}  {Method} {openVolExecut} {activCloseVol} {@position} {volum}",
                                                            Header, nameof(AddCloseOrder), volumeInTradesOpenOrd, volumInOrderClose, PositionsBots[a], vol);

                        SendCloseLimitOrderPosition(PositionsBots[a], vol); // выставили ордер

                        IsChekVolumeClose = false; // разрешее выключили 
                    }
                }
            }

        }

        /// <summary>
        ///  проверить открывающий ли это трейд 
        /// </summary>
        private bool OpenTrade(MyTrade myTrade)
        {
            foreach (Position position in PositionsBots) // заходим в позицию

            {   // проверяем откуда трейд если открывающий выствить тейк
                if (position.OpenOrders != null)
                {
                    for (int i = 0; i < position.OpenOrders.Count; i++)
                    {
                        Order curOrdOpen = position.OpenOrders[i];

                        if (curOrdOpen.NumberMarket == myTrade.NumberOrderParent) // принадлежит ордеру открытия
                        {
                            // значит трейд открывающий
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        ///  получить объем открытый трейдом
        /// </summary>
        private decimal GetOpenVolume(MyTrade myTrade)
        {
            decimal volume = 0;
            foreach (Position position in PositionsBots)
            {
                VolumeRobExecut = position.OpenVolume;

                if (position.OpenOrders != null)
                {
                    for (int i = 0; i < position.OpenOrders.Count; i++)
                    {
                        Order curOrdOpen = position.OpenOrders[i];

                        if (curOrdOpen.NumberMarket == myTrade.NumberOrderParent)
                        {
                            if (curOrdOpen.State == OrderStateType.Done)
                            {
                                volume = curOrdOpen.VolumeExecute;
                            }
                        }
                    }
                }
            }
            _logger.Information(" Open Volume {Header} {@Trade} {NumberTrade} {NumberOrderParent} {volume} {Method}"
                              , Header, myTrade, myTrade.NumberTrade, myTrade.NumberOrderParent, volume, nameof(GetOpenVolume));
            return volume;
        }

        /// <summary>
        /// выставить ордер закрытия
        /// </summary>
        private void SendCloseOrder(Position position, decimal volume)
        {
            VolumeRobExecut = position.OpenVolume;

            decimal minVolumeExecut = SelectedSecurity.MinTradeAmount;

            if (position.OpenOrders != null)
            {
                decimal volumeOpen = volume;

                if (position.CloseOrders == null)
                {
                    _logger.Information(" the first Called metod  SendCloseLimitOrderPosition" +
                    " {Method}  {OpenVolumePosition} "
                    , nameof(SendCloseOrder), position.OpenVolume);
                    // добавить лимит ордер на закрытие)
                    SendCloseLimitOrderPosition(position, volumeOpen);
                    return;
                }
                if (position.CloseOrders != null)
                {
                    int countOrdClose = position.CloseOrders.Count; // количество ордеров закрытия
                    int countTradesOpen = 0; // количество трейдов открытия
                    for (int coOrOp = 0; coOrOp < position.OpenOrders.Count; coOrOp++)// сколько ордеров открытия 
                    {
                        if (position.OpenOrders[coOrOp].MyTrades == null) continue;

                        countTradesOpen += position.OpenOrders[coOrOp].MyTrades.Count;// вычисляем количество трейдов открытия
                    }
                    if (countOrdClose < countTradesOpen)
                    {
                        decimal volumeForClose = 0;

                        for (int s = 0; s < position.CloseOrders.Count; s++) // смотрим  объемы на закрытие 
                        {
                            volumeForClose += position.CloseOrders[s].Volume;
                        }

                        // проверяем в ордерах закрытия объема меньше чем открыто на бирже
                        if (Math.Abs(volumeForClose) < Math.Abs(position.MaxVolume))// во всех исполненых на открытие
                        {
                            // добавить лимит ордер на закрытие)
                            if (volumeOpen > minVolumeExecut)
                            {
                                SendCloseLimitOrderPosition(position, volumeOpen);

                                _logger.Information("Called metod  SendCloseLimitOrderPosition" +
                                                       " {Header} {Method} Order {Volume} {OpenVolumePosition} ",
                                                Header, nameof(SendCloseOrder), volumeOpen, position.OpenVolume);
                                return;
                            }
                            else
                            {
                                _logger.Error("Volum close < minVolumeExecut {Method}  {volumeOpen} {@Position} "
                                                             , nameof(SendCloseOrder), volumeOpen, position);
                                MaintainingVolumeBalance(position);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// добавить закрывающий ордер в позицию и на биржу
        /// </summary>  
        private void SendCloseLimitOrderPosition(Position position, decimal volumeClose)
        {
            List<decimal> _price = new List<decimal>();
            _price = CalculPriceClosePos(Direction); // расчет цен закрытия позиции

            //объем пришел сверху
            decimal priceClose = 0;
            //выбираем цену закрытия
            if (position.CloseOrders == null)
            {
                priceClose = _price[0];
            }
            else
            {
                int i = position.CloseOrders.Count;
                priceClose = _price[i];
            }
            Debug.WriteLine("цена на закрытие= " + priceClose);

            if (priceClose == 0 || _price.Count == 0)
            {
               //Debug.WriteLine(" OrdersForClose.Count == 0)" );
                Debug.WriteLine(" ! Ошибка расчета цены оредара закрытия ");
                _logger.Error(" Error price order for Close {@Method} {priceClose} " ,
                                  nameof(SendCloseLimitOrderPosition), priceClose);
                return;
            }
            // ордер на закрытие в обратную стороны открытия сделки
            Side sideSet = Side.None;
            if (position.Direction == Side.Buy)
            {
                sideSet = Side.Sell;
            }
            if (position.Direction == Side.Sell)
            {
                sideSet = Side.Buy;
            }

            Order order = CreateLimitOrder(SelectedSecurity, priceClose, volumeClose, sideSet);

            if (order != null && sideSet != Side.None && priceClose != 0 && volumeClose != 0)
            {  // отправить ордер в позицию
                position.AddNewCloseOrder(order);
                Thread.Sleep(100);
                _logger.Information("Send order for ListClose {Method} {priceClose} {volumeClose} {@Order} {NumberUser}",
                                        nameof(SendCloseLimitOrderPosition), priceClose, volumeClose, order, order.NumberUser);
                //position.PassCloseOrder= false;
                SendOrderExchange(order); // отправили ордер на биржу
            }
            else
            {
                Debug.WriteLine(" ! Ошибка отправки оредара закрытия ");
                _logger.Error(" Error Limit order for Close {Method} {priceClose} {volumeClose} {@Order} {NumberUser}",
                                       nameof(SendCloseLimitOrderPosition), priceClose, volumeClose, order, order.NumberUser);
            }
        }

        /// <summary>
        /// создание позиции
        /// </summary>
        private void CreateNewPosition()
        {
            // для новой позиции
            _sendCloseMarket = false; // разрежаем закрывать ее по маркету
            _sendStop = false; // разрешаем отрабтку ей стопов 
            _isWorkedStop = false; // 

            if (PositionsBots != null)
            {
                PositionsBots.Clear();
                //DeleteFileSerial();
            }
            if (StartPriceOpenPos == 0)
            {
                SendStrStatus(" BigСlusterPrice = 0 ");
                return;
            }

            if (MonitoringOpenVolumeExchange())
            {
                SendStrStatus(" Есть открытый объем ");

                _logger.Information("! Open Volume Exchange {Method} {Balans}", nameof(CreateNewPosition), SelectSecurBalans);

                #region  проверка на открытые позиции
                MessageBoxResult result = MessageBox.Show(" Есть открытые позиции! \n Всеравно создать? ", " ВНИМАНИЕ !!! ",
                MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
                #endregion
            }

            Position positionBuy = new Position() { Direction = Side.Buy };
            Position positionSell = new Position() { Direction = Side.Sell };

            CalculateVolumeTradesOpen();

            if (VolumePerOrderOpen != 0 && IsRun == true) // формируем позиции
            {
                //DeleteFileSerial(); 

                if (Direction == Side.Buy) // || Direction == Direction.BUYSELL
                {
                    positionBuy.State = PositionStateType.None;
                    positionBuy.SecurityName = SelectedSecurity.Name;

                    //AddOpderPosition(positionBuy);
                    PositionsBots.Add(positionBuy);
                }
                if (Direction == Side.Sell) // || Direction == Direction.BUYSELL
                {
                    positionSell.State = PositionStateType.None;
                    positionSell.SecurityName = SelectedSecurity.Name;

                    //AddOpderPosition(positionSell);
                    PositionsBots.Insert(0, positionSell);
                }
                //PositionsBots = positionBots;
                SendStrStatus(" Позиция создана");
            }
        }

        /// <summary>
        /// наличие открытого объема на бирже (проверка)
        /// </summary> 
        private bool MonitoringOpenVolumeExchange()
        {
            if (SelectedSecurity == null) return false;
            decimal volume = 0;
            GetBalansSecur();
            volume = SelectSecurBalans;
            if (volume != 0) return true;
            else return false;
        }

        /// <summary>
        ///  есть открытый лонг объем
        /// </summary>
        private bool OpenVolumePositionLong()
        {
            if (SelectedSecurity == null) return false;

            foreach (Position position in PositionsBots)
            {
                VolumeRobExecut = position.OpenVolume;

                if (position.Direction == Side.Buy && position.OpenVolume != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///  есть открытый объем в шорт
        /// </summary>
        private bool OpenVolumePositionShort()
        {
            if (SelectedSecurity == null) return false;

            foreach (Position position in PositionsBots)
            {
                VolumeRobExecut = position.OpenVolume;

                if (position.Direction == Side.Sell && position.OpenVolume != 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// рассчитать цену трейлиг стопа 
        /// </summary>
        private void CalculateTrelingStop(Position position)
        {
            if (IsRun == false || SelectedSecurity == null && SelectSecurBalans == 0) return;
         
            if (position.Direction == Side.Buy && SelectSecurBalans > 0) // если есть открытый объем в лонг
            {
                decimal stepStop = 0;// расстояние до стопа
                decimal stepStopRun = 0;// расстояние до включения стопа

                stepStopRun = StepPersentStopLongRun * Price / 100;
                stepStop = StepPersentStopLong * Price / 100;
                stepStop = Decimal.Round(stepStop, SelectedSecurity.Decimals);

                decimal entry = position.EntryPrice; // средняя цена входа роботом
                if (Price > entry + stepStopRun && IsChekTraelStopLong == false && entry != 0) // включаем трейлинг стоп в лонг
                {
                    IsChekTraelStopLong = true;
                    SendStrStatus("Включили Трейлинг профит в лонг ");
                    _logger.Warning("Turned ON Trael Proit long {Header} {Method} ",
                                 Header, nameof(CalculateTrelingStop));
                }

                if (Price > PriceStopLong + stepStop && IsChekTraelStopLong)
                {
                    //if (entry == 0) return;
                    decimal p = Price - stepStop;
                    PriceStopLong = Decimal.Round(p, SelectedSecurity.Decimals);
                }
            }
            if (position.Direction == Side.Sell && SelectSecurBalans < 0)
            {
                decimal stepStop = 0;
                decimal stepStopRun = 0;// расстояние до включения стопа

                stepStopRun = StepPersentStopShortRun * Price / 100;
                stepStop = StepPersentStopShort * Price / 100;
                stepStop = Decimal.Round(stepStop, SelectedSecurity.Decimals);
                decimal entry = position.EntryPrice;

                if (Price < entry - stepStopRun && IsChekTraelStopShort == false && entry != 0) // включаем трейлинг стоп в шорт
                {
                    IsChekTraelStopShort = true;
                    SendStrStatus("Включили Трейлинг профит в шорт ");
                    _logger.Warning("Turned ON Trael Proit short {Header} {Method} ",
                                        Header, nameof(CalculateTrelingStop));
                }

                if (PriceStopShort == 0 && IsChekTraelStopShort)
                {
                    PriceStopShort = Price + stepStop;
                }
                if (Price < PriceStopShort - stepStop && IsChekTraelStopShort)
                {
                    decimal p = Price - stepStop;
                    PriceStopShort = Decimal.Round(p, SelectedSecurity.Decimals);
                }
            }
        }

        /// <summary>
        /// расчет объема на ордер открытия
        /// </summary>
        private void CalculateVolumeTradesOpen()
        {
            if (SelectedSecurity == null || Price == 0 || PartsPerInput == 0) return;
            GetBalansSecur();
            VolumePerOrderOpen = 0;
            decimal workLot = 0;
            decimal baks = 0;
            baks = FullPositionVolume / PartsPerInput; // это в баксах
            decimal moni = baks / Price; // в монете
            workLot = Decimal.Round(moni, SelectedSecurity.DecimalsVolume);
            decimal minVolume = SelectedSecurity.MinTradeAmount;
            if (workLot < minVolume)
            {
                SendStrStatus("Объем ордера меньше допустимого");
                _logger.Error(" Order volume < minVolume {Method} Minimum {minVolume} {workLot}"
                    , nameof(CalculateVolumeTradesOpen), minVolume, workLot);
                // IsRun = false;
            }
            else
            {
                VolumePerOrderOpen = workLot;
            }
        }

        /// <summary>
        /// расчет объема на ордер закрытия (части)
        /// </summary>
        private decimal CalculateVolumeTradesClose()
        {
            if (SelectedSecurity != null || Price != 0 || PartsPerExit != 0)
            {
                GetBalansSecur();
                _volumePerOrderClose = 0;
                decimal workLot = 0;
                decimal baks = 0;
                baks = FullPositionVolume / PartsPerExit; // это в баксах
                decimal moni = baks / Price; // в монете

                //  взять открытый объем

                if (true) // набран весь обем
                {
                    // todo: сделать расчет объема на ордер выхода
                }
                workLot = Decimal.Round(moni, SelectedSecurity.DecimalsVolume);
                decimal minVolume = SelectedSecurity.MinTradeAmount;
                if (workLot < minVolume)
                {
                    SendStrStatus("Объем ордера меньше допустимого");
                    _logger.Error(" Order volume < minVolume {Method}  {workLot}", nameof(CalculateVolumeTradesClose), workLot);
                    // IsRun = false;
                    if (SelectSecurBalans != 0)
                    {
                        _volumePerOrderClose = SelectSecurBalans;
                        _logger.Error(" Volume PerOrder Close = SelectSecurBalans; {Method}  {SelectSecurBalans}",
                                                                nameof(CalculateVolumeTradesClose), SelectSecurBalans);
                    }
                }
                else
                {
                    _volumePerOrderClose = workLot;
                }
                return workLot;
            }
            return _volumePerOrderClose;
        }

        /// <summary>
        /// расчитать стартовую цену (начала открытия позиции)
        /// </summary>
        private List<decimal> CalculPriceStartPos(Side side)
        {
            _priceOpenPos.Clear();

            if (SelectedSecurity == null)
            {
                SendStrStatus(" еще нет бумаги ");

                return _priceOpenPos;
            }
            decimal stepPrice = 0;
            decimal price = 0;

            if (side == Side.Buy)
            {
                stepPrice = (StartPriceOpenPos - BottomPositionPrice) / PartsPerInput;
                price = StartPriceOpenPos - stepPrice;
                for (int i = 0; i < PartsPerInput; i++)
                {
                    price = Decimal.Round(price, SelectedSecurity.Decimals);
                    _priceOpenPos.Add(price);
                    price = price - stepPrice;
                }
            }
            if (side == Side.Sell)
            {
                stepPrice = (TopPositionPrice - StartPriceOpenPos) / PartsPerInput;
                price = StartPriceOpenPos + stepPrice;
                for (int i = 0; i < PartsPerInput; i++)
                {
                    price = Decimal.Round(price, SelectedSecurity.Decimals);
                    _priceOpenPos.Add(price);
                    price = price + stepPrice;
                }
            }
            return _priceOpenPos;
        }

        /// <summary>
        /// расчитать цены закрытия позиции
        /// </summary>
        private List<decimal> CalculPriceClosePos(Side side)
        {
            //_priceClosePos = new List<decimal>();

            if (SelectedSecurity == null)
            {
                SendStrStatus(" еще нет бумаги ");

                return _priceClosePos;
            }
            decimal stepPrice = 0;
            decimal price = 0;

            if (side == Side.None)
            {
                side = Direction;
            }
            if (side == Side.Buy)
            {
                _priceClosePos.Clear();

                stepPrice = (TakePriceLong - StartPriceOpenPos) / PartsPerExit;
                price = StartPriceOpenPos + stepPrice;
                for (int i = 0; i < PartsPerExit; i++)
                {
                    price = Decimal.Round(price, SelectedSecurity.Decimals);
                    _priceClosePos.Add(price);
                    price = price + stepPrice;
                }
            }
            if (side == Side.Sell)
            {
                _priceClosePos.Clear();

                stepPrice = (StartPriceOpenPos - TakePriceShort) / PartsPerExit;
                price = StartPriceOpenPos - stepPrice;
                for (int i = 0; i < PartsPerExit; i++)
                {
                    price = Decimal.Round(price, SelectedSecurity.Decimals);
                    _priceClosePos.Add(price);
                    price = price - stepPrice;
                }
            }
            return _priceClosePos;
        }

        /// <summary>
        /// проверка и обновление состояния ордера
        /// </summary>
        private void CheckMyOrder(Order checkOrder)
        {
            if (checkOrder.SecurityNameCode == SelectedSecurity.Name)
            {
                if (PositionsBots == null) { return; }

                for (int i = 0; i < PositionsBots.Count; i++)
                {
                    PositionsBots[i].SetOrder(checkOrder); // проверяем и обновляем ордер
                                                           // TODO:  придумать проверку и изменяем статуса позиций
                    if (IsChekSendAllLogs) _logger.Information("Check MyOrder {Header} {@order}{OrdNumberUser} {NumberMarket}{Method}",
                                      Header, checkOrder, checkOrder.NumberUser, checkOrder.NumberMarket, nameof(CheckMyOrder));
                }
            }
        }

        /// <summary>
        /// проверка и обновление трейдами ордеров 
        /// </summary>
        private void ChekTradePosition(MyTrade newTrade)
        {
            GetBalansSecur();
            if (newTrade.SecurityNameCode == SelectedSecurity.Name)
            {
                if (PositionsBots == null) { return; }

                for (int i = 0; i < PositionsBots.Count; i++)
                {
                    VolumeRobExecut = PositionsBots[i].OpenVolume;

                    PositionsBots[i].SetTrade(newTrade);

                    _logger.Information("Chek Trade Position {Header} {@Trade} {NumberTrade} {NumberOrderParent} {Method}"
                            , Header, newTrade, newTrade.NumberTrade, newTrade.NumberOrderParent, nameof(ChekTradePosition));
                    if (OpenTrade(newTrade))
                    {
                        decimal vol = GetOpenVolume(newTrade);
                        if (vol != 0)
                        {
                            SendCloseOrder(PositionsBots[i], vol);
                        }
                    }
                }
            }
            SaveParamsBot();
        }

        /// <summary>
        /// берет названия кошельков (бирж)
        /// </summary>
        public ObservableCollection<string> GetStringPortfolios(IServer server)
        {
            ObservableCollection<string> stringPortfolios = new ObservableCollection<string>();
            if (server == null)
            {
                _logger.Information("GetStringPortfolios server == null {Method}", nameof(GetStringPortfolios));
                //RobotsWindowVM.Log(Header, "GetStringPortfolios server == null ");
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

            _logger.Information("Create Limit Order {Method} {@Order} {Number} ", nameof(CreateLimitOrder), order, order.NumberUser);
            //RobotsWindowVM.Log(Header, "SendLimitOrder\n " + " отправляем лимитку на биржу\n" + GetStringForSave(order));
            RobotsWindowVM.SendStrTextDb(" Создали ордер " + order.NumberUser);

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

            RobotsWindowVM.SendStrTextDb(" CreateMarketOrder " + order.NumberUser);
            _logger.Information("Create Market Order {@Order} {Method}", order, nameof(CreateMarketOrder));
            // ("Method {Method} Exception {@Exception}", nameof(SaveHeaderBot), ex)
            //Server.ExecuteOrder(order);

            return order;
        }

        /// <summary>
        /// проверка ордеров в позициях
        /// </summary>
        private void CheckOrders()
        {
            Task.Run(async () =>  // для опроса состояния позиций) 
            {
                DateTime dt = DateTime.Now;
                while (dt.AddMinutes(1) > DateTime.Now)
                {
                    RebootStatePosition();
                    await Task.Delay(20000);
                }
            });
        }
        #endregion

        #region  методы сервера ===========================

        /// <summary>
        ///  измеились биды аски 
        /// </summary>
        private void _server_NewBidAscIncomeEvent(decimal ask, decimal bid, Security selectSecur)
        {
            if (selectSecur != null && selectSecur.Name == SelectedSecurity.Name)
            {
                if (Price != ask && ask != 0)
                {
                    Price = ask;

                    MonitoringStop();
                    GetBalansSecur();
                    ManagerSelectActionVolumeLogic();
                }
            }
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

        private void _server_SecuritiesChangeEvent(List<Security> securities)
        {
            for (int i = 0; i < securities.Count; i++)
            {
                if (securities[i].Name == Header)
                {
                    SelectedSecurity = securities[i];
                    StartSecuritiy(securities[i]);
                    break;
                }
            }
        }

        private void _server_ConnectStatusChangeEvent(string state) // ещё есть подписка из окна роботов 
        {
            if (state == "Connect")
            {
                SendStrStatus(" Сервер подключен ");
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
            // StartMaintainingVolumeBalance();

            if (myTrade.SecurityNameCode == SelectedSecurity.Name)
            {

                ChekTradePosition(myTrade);

                if (IsChekSendAllLogs) _logger.Warning(" Come myTrade {Header} {Method} {NumberOrderParent} {@myTrade}", Header, nameof(_server_NewMyTradeEvent), myTrade.NumberOrderParent, myTrade);

                GetBalansSecur();
                IsOnTralProfit(myTrade);
            }
            //else
            //if (IsChekSendAllLogs) _logger.Warning(" Secur Trade {Security} {@Trade}  {Method}", myTrade.SecurityNameCode, myTrade,  nameof(_server_NewMyTradeEvent));
        }

        /// <summary>
        ///  пришел ордер с сервера
        /// </summary>
        private void _server_NewOrderIncomeEvent(Order order)
        {
            if (SelectedSecurity != null)
            {
                StartMaintainingVolumeBalance();

                if (order.SecurityNameCode == SelectedSecurity.Name)
                {
                    CheckMyOrder(order);

                    //AddOrderPosition(order);

                    if (order.State != OrderStateType.Fail)
                    {
                        SaveParamsBot();
                    }
                    else ClearFailOrderPosition();

                    GetBalansSecur();
                    if (IsChekSendAllLogs) _logger.Information(" New myOrder  {Header}{@Order} {NumberUser} {NumberMarket} {Method}",
                                       Header, order, order.NumberUser, order.NumberMarket, nameof(_server_NewOrderIncomeEvent));
                }
                //else
                //    _logger.Information(" Levak ! Secur {@Order} {Security} {Method}",
                //        order, order.SecurityNameCode,nameof(_server_NewOrderIncomeEvent));
            }
        }

        /// <summary>
        ///  проверить баланс обема в роботе
        /// </summary>
        private void StartMaintainingVolumeBalance()
        {
            if (PositionsBots != null)
            {
                if (PositionsBots.Count > 0)
                {
                    for (int i = 0; i < PositionsBots.Count; i++)
                    {
                        MaintainingVolumeBalance(PositionsBots[i]);
                    }
                }
            }
        }

        /// <summary>
        /// измениля портфель (кошелек) 
        /// </summary>
        private void _server_PortfoliosChangeEvent(List<Portfolio> portfolios)
        {
            StringPortfolios = GetStringPortfolios(_server); // грузим портфели

            OnPropertyChanged(nameof(StringPortfolios));

            if (StringPortfolios != null && StringPortfolios.Count > 0)
            {
                if (StringPortfolio == "")
                {
                    StringPortfolio = StringPortfolios[0];
                }
                for (int i = 0; i < portfolios.Count; i++)
                {
                    if (portfolios[i].Number == StringPortfolio)
                    {
                        _portfolio = portfolios[i];
                    }
                }
            }
            OnPropertyChanged(nameof(StringPortfolios));
        }

        /// <summary>
        /// пришел новый терейд по бумаге
        /// </summary>
        private void _NewTradeEvent(List<Trade> trades)
        {
            if (trades == null || trades.Count == 0) return;
            if (trades != null && trades[0].SecurityNameCode == SelectedSecurity.Name)
            {
                GetBalansSecur();

                Trade trade = trades[0];  //Price = trade.Price;

                CalculationVolumeInTradeNperiod(trades);

                SetBoolMoreVolumeAvereg();

                if (trade.Time.Second % 3 == 0) //if (trade.Time.Second % 5 == 0) GetBalansSecur();
                {
                    StartMaintainingVolumeBalance();
                    AddCloseOrder();
                }
                if (trade.Time.Second % 11 == 0 && trades[0].SecurityNameCode == SelectedSecurity.Name)
                {
                    IsOffBot();
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
            _server.PortfoliosChangeEvent += _server_PortfoliosChangeEvent;
            _server.NewBidAscIncomeEvent += _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent += _server_ConnectStatusChangeEvent;

            _logger.Warning(" Connecting to the server = {ServerType} {Method} ", _server.ServerType, nameof(SubscribeToServer));
            //RobotsWindowVM.Log(Header, " Подключаемся к серверу = " + _server.ServerType);
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
            _server.PortfoliosChangeEvent -= _server_PortfoliosChangeEvent;
            _server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;

            _logger.Warning(" Disconecting to server = {ServerType} {Method} ", _server.ServerType, nameof(UnSubscribeToServer));
            //RobotsWindowVM.Log(Header, " Отключились от сервера = " + _server.ServerType);
        }

        #endregion

        #region   сервисные методы ===========================

        /// <summary>
        /// менеджер выбраного действия 
        /// </summary>
        private void ManagerSelectActionVolumeLogic()
        {
            if (IsRun == false) return;

            if (PositionsBots != null && PositionsBots.Count > 0)
            {
                for (int i = 0; i < PositionsBots.Count; i++)
                {
                    if (PositionsBots[i].Direction == Side.Buy) // направление позиции покупка
                    {
                        if (Buy1MoreAvereg)
                        {
                            if(ActionPosition1Buy == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);

                                _logger.Warning(" Send StopPosition ActionPosition 1Buy {Method} ",
                                    nameof(ManagerSelectActionVolumeLogic));
                            }
                            if (ActionPosition1Buy == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition1Buy == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition1Buy == ActionPos.AddVolumes)
                            {

                            }
                        }
                        if (Buy2MoreAvereg)
                        {
                            if (ActionPosition2Buy == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);

                                _logger.Warning(" Send StopPosition ActionPosition 2Buy {Method} ",
                                 nameof(ManagerSelectActionVolumeLogic));
                            }
                            if (ActionPosition2Buy == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition2Buy == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition2Buy == ActionPos.AddVolumes)
                            {

                            }
                        }
                        if (Sell1MoreAvereg)
                        {
                            if (ActionPosition1Sell == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);

                                _logger.Warning(" Send StopPosition ActionPosition 1Sell {Method} ",
                                        nameof(ManagerSelectActionVolumeLogic));
                            }
                            if (ActionPosition1Sell == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition1Sell == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition1Sell == ActionPos.AddVolumes)
                            {

                            }
                        }
                        if (Sell2MoreAvereg)
                        {
                            if (ActionPosition2Sell == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);

                                _logger.Warning(" Send StopPosition ActionPosition 2Sell {Method} ",
                                     nameof(ManagerSelectActionVolumeLogic));
                            }
                            if (ActionPosition2Sell == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition2Sell == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition2Sell == ActionPos.AddVolumes)
                            {

                            }
                        }
                    }

                    if (PositionsBots[i].Direction == Side.Sell)  // направление позиции продажа   
                    {
                        if (Buy1MoreAvereg)
                        {
                            if (ActionPosition1Buy == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);
                            }
                            if (ActionPosition1Buy == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition1Buy == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition1Buy == ActionPos.AddVolumes)
                            {

                            }
                        }
                        if (Buy2MoreAvereg)
                        {
                            if (ActionPosition2Buy == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);
                            }
                            if (ActionPosition2Buy == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition2Buy == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition2Buy == ActionPos.AddVolumes)
                            {

                            }
                        }
                        if (Sell1MoreAvereg)
                        {
                            if (ActionPosition1Sell == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);
                            }
                            if (ActionPosition1Sell == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition1Sell == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition1Sell == ActionPos.AddVolumes)
                            {

                            }
                        }
                        if (Sell2MoreAvereg)
                        {
                            if (ActionPosition2Sell == ActionPos.Stop && _isWorkedStop == false)
                            {
                                _isWorkedStop = true;
                                StopPosition(PositionsBots[i]);
                            }
                            if (ActionPosition2Sell == ActionPos.ShortenStop)
                            {

                            }
                            if (ActionPosition2Sell == ActionPos.RollOver)
                            {

                            }
                            if (ActionPosition2Sell == ActionPos.AddVolumes)
                            {

                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// установка значений от объема торгов по монете
        /// </summary>
        private void SetBoolMoreVolumeAvereg()
        {   // что бы в зависимости от объема торгов перключать логику

            if (IsRun == false || N_min == null ) return;
            if (AllVolumPeroidMin == 0 || Avereg == 0 ||
                BidVolumPeriod == 0 || AskVolumPeriod == 0) return;

            #region заготовки для дельты 
            /*
            if (AskVolumPeriod > BidVolumPeriod * Ratio1) // объем продажи больше объема покупок
                                                          // * 2 минусовая дельта
            {
                // че-то  делаем, на забор например 
                _logger.Warning(" Negative delta Ratio 1 {Header} {Method} ",
                                 Header, nameof(VolumeLogicManager));
            }
            if (AskVolumPeriod > BidVolumPeriod * Ratio2) // объем продажи больше объема покупок *
                                                          // 2 минусовая дельта
            {
                // че-то  делаем, на забор например 
                _logger.Warning(" Negative delta Ratio 2 {Header} {Method} ",
                  Header, nameof(VolumeLogicManager));
            }

            if (AskVolumPeriod * Ratio1 < BidVolumPeriod) // объем продажи больше объема покупок *
                                                          // плюсовая дельта
            {
                // че-то  делаем, на забор например 

                _logger.Warning(" Positive delta Ratio 2 {Header} {Method} ",
                                     Header, nameof(VolumeLogicManager));
            }
            if (AskVolumPeriod * Ratio2 < BidVolumPeriod) // объем продажи больше объема покупок *
                                                          // Плюсовая дельта
            {
                // че-то  делаем, на забор например 
                _logger.Warning(" Positive delta Ratio 2 {Header} {Method} ",
                                 Header, nameof(VolumeLogicManager));
            }
            */
            #endregion

            if (bidVolumPeriod > Avereg * RatioBuy1) // покупки больше средней
            {
                Buy1MoreAvereg = true;
                if (time_add_n_min1 < DateTime.Now)
                {
                    time_add_n_min1 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                    if (SelectSecurBalans == 0) return;
                    _logger.Warning(" Bid VolumPeriod > Avereg * Ratio 1" +
                        " {BidVolumPeriod} {Avereg} {Ratio1} {time_add_n_min} {Header} {Method} ",
                        bidVolumPeriod, Avereg, RatioBuy1, time_add_n_min1, Header, nameof(SetBoolMoreVolumeAvereg));
                }
            }
            else { Buy1MoreAvereg = false; }

            if (bidVolumPeriod > Avereg * RatioBuy2) // покупки больше средней
            {
                Buy2MoreAvereg = true;
                if (time_add_n_min2 < DateTime.Now)
                {
                    time_add_n_min2 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                    if (SelectSecurBalans == 0) return;
                    _logger.Warning(" Bid VolumPeriod > Avereg * Ratio 2" +
                        " {BidVolumPeriod} {Avereg} {RatioBuy2} {time_add_n_min} {Header} {Method} ",
                        bidVolumPeriod, Avereg, RatioBuy2, time_add_n_min2, Header, nameof(SetBoolMoreVolumeAvereg));
                }
            }
            else { Buy2MoreAvereg = false; }

            if (askVolumPeriod > Avereg * RatioSell1) // продажи больше средней
            {
                Sell1MoreAvereg = true;

                if (time_add_n_min1 < DateTime.Now)
                {
                    time_add_n_min1 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                    if (SelectSecurBalans == 0) return;
                    _logger.Warning(" Ask VolumPeriod > Avereg * Ratio 1" +
                        " {AskVolumPeriod} {Avereg} {RatioSell1} {time_add_n_min} {Header} {Method} ",
                        askVolumPeriod, Avereg, RatioSell1, time_add_n_min1, Header, nameof(SetBoolMoreVolumeAvereg));
                }
            }
            else { Sell1MoreAvereg = false; }

            if (askVolumPeriod > Avereg * RatioSell2) // продажи больше средней
            {
                Sell2MoreAvereg = true;

                if (time_add_n_min2 < DateTime.Now)
                {
                    time_add_n_min2 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                    if (SelectSecurBalans == 0) return;
                    _logger.Warning(" Ask VolumPeriod > Avereg * Ratio 2" +
                        " {AskVolumPeriod} {Avereg} {RatioSell2} {time_add_n_min} {Header} {Method} ",
                        askVolumPeriod, Avereg, RatioSell2, time_add_n_min2, Header, nameof(SetBoolMoreVolumeAvereg));
                }
            }
            else { Sell2MoreAvereg = false; }

            #region  в баксах--------------------------------------------------------

            decimal askVolumPeriodS = 0;
            decimal bidVolumPeriodS = 0;
            
            if (AveregS != 0 && Price != 0 && BidVolumPeriod != 0 && AskVolumPeriod !=0)
            {
                askVolumPeriodS = askVolumPeriod * Price;
                bidVolumPeriodS = bidVolumPeriod * Price; // биды в баксах

                if (bidVolumPeriodS > AveregS * RatioBuy1) // покупки больше средней * 1 коэф
                {
                    Buy1MoreAvereg = true;

                    if (time_add_n_min1 < DateTime.Now)
                    {
                        time_add_n_min1 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                        if (SelectSecurBalans == 0) return;
                        _logger.Warning(" Bid Volume Peroid in $ * RatioBuy 1 > medium in $" +
                            " {bidVolumPeriodS} {AveregS} {time_add_n_min} {Header} {Method} ",
                            bidVolumPeriodS, AveregS, time_add_n_min1, Header, nameof(SetBoolMoreVolumeAvereg));
                    }
                }
                else { Buy1MoreAvereg = false; }

                if (bidVolumPeriodS > AveregS * RatioBuy2) // покупки больше 2 средней
                {
                    Buy2MoreAvereg = true;

                    if (time_add_n_min1 < DateTime.Now)
                    {
                        time_add_n_min1 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                        if (SelectSecurBalans == 0) return;
                        _logger.Warning(" Bid Volume Peroid in $ * RatioBuy 2 > medium in $ "  +
                        " {bidVolumPeriodS} {AveregS} {time_add_n_min} {Header} {Method} ",
                        bidVolumPeriodS, AveregS, time_add_n_min1, Header, nameof(SetBoolMoreVolumeAvereg));
                    }
                }
                else { Buy2MoreAvereg = false; }

                if (askVolumPeriodS > AveregS * RatioSell1) // продажи больше 1 средней
                {
                    Sell1MoreAvereg = true;

                    if (time_add_n_min1 < DateTime.Now)
                    {
                        time_add_n_min1 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                        if (SelectSecurBalans == 0) return;
                        _logger.Warning("Ask Volume Peroid in $ * RatioBuy 1 > medium in $" +
                            " {askVolumPeriodS} {AveregS} {time_add_n_min} {Header} {Method} ",
                            askVolumPeriodS, AveregS, time_add_n_min1, Header, nameof(SetBoolMoreVolumeAvereg));
                    }
                }
                else { Sell1MoreAvereg = false; }

                if (askVolumPeriodS > AveregS * RatioSell2) // продажи больше 2 средней
                {
                    Sell2MoreAvereg = true;

                    if (time_add_n_min1 < DateTime.Now)
                    {
                        time_add_n_min1 = DateTime.Now.AddMinutes(N_min - 1); // время сработки + N минут
                        if (SelectSecurBalans == 0) return;
                        _logger.Warning(" Ask Volume Peroid in $ * RatioBuy 2 > medium in $ " +
                        " {askVolumPeriodS} {AveregS} {time_add_n_min} {Header} {Method} ",
                        askVolumPeriodS, AveregS, time_add_n_min1, Header, nameof(SetBoolMoreVolumeAvereg));
                    }
                }
                else { Sell2MoreAvereg = false; }
            }

            #endregion конец в бакcах------------------------------------------------------------
        }

        /// <summary>
        /// подсчет объема по тикам в N период времени
        /// </summary>
        private async Task CalculationVolumeInTradeNperiod(List<Trade> trades)
        {
            // берем определенный промежуток времени назад
            // собираем за это время покупки
            // собираем за это время продажи
            // собираем за это время все объемы 

            if (trades == null || N_min == null) return;
            if (trades.Count == 0) return;

            DateTime time_add_n_min = dateTradingPeriod.AddMinutes(N_min); // время трейда + N минут
            if (time_add_n_min == null || time_add_n_min == DateTime.MinValue ) return;

            for (int i = 0; i < trades.Count; i++)
            {
                //Thread.Yield();
                if (trades[i].Time < time_add_n_min)
                {
                    if (trades[i].Side == Side.Buy)
                    {
                        bidVolumPeriod += trades[i].Volume;
                    }
                    if (trades[i].Side == Side.Sell)
                    {
                        askVolumPeriod += trades[i].Volume;
                    }
                    if (trades.Count -1 == i && IsRun == true)
                    {
                        allVolumPeroidMin = bidVolumPeriod + askVolumPeriod;
                        BidVolumPeriod = bidVolumPeriod;
                        AskVolumPeriod = askVolumPeriod;
                        AllVolumPeroidMin = allVolumPeroidMin;
                    }
                }

                else
                {
                    bidVolumPeriod = 0;
                    askVolumPeriod = 0;
                    allVolumPeroidMin = 0;

                    dateTradingPeriod = trades[i].Time;
                }
            }
            /*
            for (int i = 0; i < trades.Count; i++)
            {
                if (trades[i].Time < time_add_n_min)
                {
                    if (trades[i].Side == Side.Buy)
                    {
                        //decimal b = trades[i].Volume;
                        _bidVolumPeriod += trades[i].Volume;
                    }
                    if (trades[i].Side == Side.Sell)
                    {
                        //decimal a = trades[i].Volume;
                        _askVolumPeriod += trades[i].Volume;
                    }
                    _allVolumPeroidMin = _bidVolumPeriod + _askVolumPeriod;

                    BidVolumPeriod = _bidVolumPeriod;
                    AskVolumPeriod = _askVolumPeriod;
                    AllVolumPeroidMin = _allVolumPeroidMin;

                }

                else
                {
                    dateTradingPeriod = trades[i].Time;
                    AllVolumPeroidMin = 0;
                    BidVolumPeriod = 0;
                    AskVolumPeriod = 0;
                }
            }
            */
        }

        /// <summary>
        /// очистка переменных после закрытия позиции
        /// </summary>
        private void ClearingVariablesAfterClosing()
        {
            SelectSecurBalans = 0;
            IsChekTraelStopLong = false;
            IsChekTraelStopShort = false;
            PriceStopLong = 0;
            PriceStopShort = 0;
            if (!IsRun) PositionsBots.Clear();
            ActionPosition1Buy = ActionPos.Nothing;
            ActionPosition1Sell = ActionPos.Nothing;
            ActionPosition2Buy = ActionPos.Nothing;
            ActionPosition2Sell = ActionPos.Nothing;
            BidVolumPeriod = 0;
            AskVolumPeriod = 0;
            AllVolumPeroidMin = 0;

            _logger.Information(" Clearing Value Variables {Header} {Method} ",
                                 Header, nameof(ClearingVariablesAfterClosing));

            //if (PositionsBots[0].State == PositionStateType.Done ||
            //    PositionsBots[0].State == PositionStateType.Deleted){}      
        }

        /// <summary>
        /// разрешения на стоп
        /// </summary>
        private void SetStopTrue()
        {
            _sendCloseMarket = false;
            _isWorkedStop = false;
            _sendStop = false;
        }

        /// <summary>
        /// удалить все открытые ордера по монете на бирже
        /// </summary>
        public void DeleteAllOrdersPositionExchange()
        {
            if (PositionsBots != null && PositionsBots.Count > 0)
            {
                if (Server != null)
                {
                    if (Server.ServerType == ServerType.BinanceFutures)
                    {
                        AServer aServer = (AServer)Server;

                        for (int i = 0; i < PositionsBots.Count; i++)
                        {
                            Security security = new Security();
                            security.Name = PositionsBots[i].SecurityName;
                            bool yes = ActivOrders(PositionsBots[i]);
                            if (yes)
                            {
                                aServer.ServerRealization.CancelAllOrdersToSecurity(security); // удалить ордера по монете
                                _logger.Information(" Canceled All Orders security on Exchange {Header} {security} {Method} "
                                                               , Header, security.Name, nameof(DeleteAllOrdersPositionExchange));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// проверяем стопы
        /// </summary>
        private void MonitoringStop()
        {
            if (PositionsBots == null) return;
            if (PositionsBots.Count != 0 && SelectSecurBalans != 0)
            {
                if (Price != 0) // если н0ль - стоп отключен
                {
                    foreach (var pos in PositionsBots)
                    {
                        if (pos.Direction == Side.Buy)
                        {
                            CalculateTrelingStop(pos);
                        }
                    }

                    if (Price < PriceStopLong && Direction == Side.Buy) //|| Price < PriceStopLong && Direction == Direction.BUYSELL
                    {
                        for (int i = 0; i < PositionsBots.Count && !_isWorkedStop; i++)
                        {
                            if (PositionsBots[i].Direction == Side.Buy && SelectSecurBalans > 0
                                && _isWorkedStop == false && _sendCloseMarket == false)
                            {
                                if (ActionPositionLong == ActionPos.Stop)
                                {
                                    _isWorkedStop = true;
                                    StopPosition(PositionsBots[i]);
                                    _logger.Warning(" Triggered Stop Long Position  {Header} {PriceStopLong} {Price} {@Position}  {Method}",
                                                          Header, PriceStopLong, Price, PositionsBots[i], nameof(MonitoringStop));
                                }
                                if (PositionsBots[i].State == PositionStateType.Done)// отключаем стоп т.к. позиция уже закрыта
                                {
                                    StopTradeLogic();
                                }
                            }
                        }
                    }
                }

                if (Price != 0)
                {
                    foreach (var pos in PositionsBots)
                    {
                        if (pos.Direction == Side.Sell)
                        {
                            CalculateTrelingStop(pos);
                        }
                    }

                    if (Price > PriceStopShort && Direction == Side.Sell && PriceStopShort != 0) // || ice > PriceStopShort && Direction == Direction.BUYSELL
                    {
                        for (int i = 0; i < PositionsBots.Count && !_isWorkedStop; i++)
                        {
                            if (PositionsBots[i].Direction == Side.Sell && SelectSecurBalans < 0
                                && _isWorkedStop == false && _sendCloseMarket == false)
                            {
                                if (ActionPositionShort == ActionPos.Stop)
                                {
                                    _isWorkedStop = true;

                                    StopPosition(PositionsBots[i]);
                                    _logger.Warning(" Triggered Stop Short Position {Header} {ActionPositionShort} {Price} {@Position}  {Method}"
                                                                  , Header, ActionPositionShort, Price, PositionsBots[i], nameof(MonitoringStop));

                                }

                                if (PositionsBots[i].State == PositionStateType.Done)// отключаем стоп т.к. позиция уже закрыта
                                {
                                    StopTradeLogic();
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// включить трейлинг если есть ещё активные на закрытие 
        /// </summary>
        private void IsOnTralProfit(MyTrade myTrade)
        {
            foreach (Position position in PositionsBots) // заходим в позицию
            {
                // проверяем откуда трейд 
                if (position.CloseOrders != null)
                {
                    for (int i = 0; i < position.CloseOrders.Count; i++)
                    {
                        Order curOrdClose = position.CloseOrders[i];

                        if (curOrdClose.NumberMarket == myTrade.NumberOrderParent) // принадлежит ордеру закрытия
                        {
                            // значит трейд закрывающий
                            int countOrder = position.CloseOrders.Count;

                            for (int s = 0; s < countOrder; s++) // смотрим ордера закрытия
                            {
                                if (position.CloseOrders[s].State == OrderStateType.Activ && countOrder > 1) // ищем актиыные  больше одного
                                {
                                    if (position.Direction == Side.Buy)
                                    {
                                        IsChekTraelStopLong = true;

                                        _logger.Warning(" Enabled trailing profit Long {Method} ", nameof(IsOnTralProfit));
                                    }
                                    if (position.Direction == Side.Sell)
                                    {
                                        IsChekTraelStopShort = true;

                                        _logger.Warning(" Enabled trailing profit Short {Method} ", nameof(IsOnTralProfit));
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        ///<summary>
        /// взять текущий объем на бирже выбаной  бумаги
        /// </summary>
        private void GetBalansSecur()
        {
            if (SelectedSecurity == null) return;

            List<Portfolio> portfolios = new List<Portfolio>();
            if (Server.Portfolios != null)
            {
                portfolios = Server.Portfolios;
            }
            if (portfolios.Count > 0 && portfolios != null
                && _selectedSecurity != null)
            {
                //SelectSecurBalans = 0;
                int count = portfolios[0].GetPositionOnBoard().Count;
                string nam = SelectedSecurity.Name;
                string suf = "_BOTH";
                string SecurName = nam + suf;
                for (int i = 0; i < count; i++)
                {
                    string seсurCode = portfolios[0].GetPositionOnBoard()[i].SecurityNameCode;
                    if (seсurCode == SecurName)
                    {
                        decimal d = portfolios[0].GetPositionOnBoard()[i].ValueCurrent;
                        if (d != SelectSecurBalans)
                        {
                            SelectSecurBalans = d; // отправка значения в свойство
                            _logger.Information("Balans SelectSecur = {SelectSecurBalans} {Header} {Method} "
                                                       , SelectSecurBalans, Header, nameof(GetBalansSecur));
                        }
                    }
                }
            }
            //decimal balans = portfolios[0].GetPositionOnBoard()[0].Find(pos =>
            //    pos.SecurityNameCode == _securName).ValueCurrent;
            //return balans;
        }

        /// <summary>
        /// Восстановления состояния ордеров и трейдов в позициях поле выключения
        /// </summary>
        public void RebootStatePosition()
        {
            if (PositionsBots != null && PositionsBots.Count > 0)
            {
                // отправляем запрос состояния ордеров и их трейдов на бирже
                if (Server != null)
                {
                    if (Server.ServerType == ServerType.BinanceFutures)
                    {
                        AServer aServer = (AServer)Server;

                        List<Order> orders = new List<Order>(); // ордера статусы которых надо опросить

                        foreach (Position position in PositionsBots)
                        {
                            if (position.OpenOrders != null)
                            {
                                if (position.OpenOrders.Count != 0)
                                {
                                    GetStateActivOrdeps(position.OpenOrders, ref orders);
                                }
                            }
                            if (position.CloseOrders != null)
                            {
                                if (position.CloseOrders.Count != 0)
                                {
                                    GetStateActivOrdeps(position.CloseOrders, ref orders);
                                }
                            }
                        }
                        if (orders.Count > 0)
                        {
                            aServer.ServerRealization.GetOrdersState(orders); // запросить статусы выбранных оредров
                            aServer.ServerRealization.ResearchTradesToOrders(orders); // и их трейдов
                            if (IsChekSendAllLogs) _logger.Information(" GetOrdersState and ResearchTradesToOrders {Header} {@orders}{Method} ",
                                                                                          Header, orders, nameof(RebootStatePosition));
                        }
                    }
                }
            }
        }

        /// <summary>
        ///  выбирает активные ордера для опроса 
        /// </summary>
        private void GetStateActivOrdeps(List<Order> orders, ref List<Order> stateOrders)
        {
            foreach (Order order in orders)
            {
                if (order != null)
                {
                    if (order.State == OrderStateType.Activ ||
                       order.State == OrderStateType.Patrial ||
                       order.State == OrderStateType.Pending)
                    {
                        stateOrders.Add(order);
                    }
                }
            }
        }

        /// <summary>
        /// выбрать нужные ордера для проверки
        /// </summary>
        private List<Order> SelectRequiredOrders()
        {
            //выбираем номера ордеров из
            //активных сделок(есть открытый объем или активные ордера)
            List<Order> selectOrders = new List<Order>();

            foreach (Position position in PositionsBots)
            {
                List<Order> ordersAll = new List<Order>();
                if (position.OpenActiv)
                {
                    ordersAll = position.OpenOrders;// взять из позиции ордера открытия 
                }
                if (position.CloseActiv)
                {
                    ordersAll.AddRange(position.CloseOrders); // добавили ордера закрытия 
                }
                for (int i = 0; i < ordersAll.Count; i++)
                {
                    if (ordersAll[i].State == OrderStateType.Activ ||
                        ordersAll[i].State == OrderStateType.Patrial ||
                        ordersAll[i].State == OrderStateType.None ||
                        ordersAll[i].State == OrderStateType.Pending)
                    {
                        selectOrders.Add(ordersAll[i]);
                    }
                }
            }
            _logger.Information("Select the required orders {Method} Order {@Orders} "
                                         , nameof(SelectRequiredOrders), selectOrders);
            return selectOrders;
        }

        /// <summary>
        /// Начать получать данные по бумаге
        /// </summary> 
        private void StartSecuritiy(Security security)
        {
            if (security == null)
            {
                _logger.Error("StartSecuritiy  security = null {Method} ", nameof(StartSecuritiy));
                //RobotsWindowVM.Log(Header, "StartSecuritiy  security = null ");
                return;
            }

            Task.Run(() =>
            {
                while (true)
                {
                    var series = Server.StartThisSecurity(security.Name, new TimeFrameBuilder(), security.NameClass);
                    if (series != null)
                    {
                        _logger.Information("StartSecuritiy security = {Security} {Method} ", series.Security.Name, nameof(StartSecuritiy));

                        CheckOrders();
                        //SaveParamsBot();
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
                        _logger.Information("Portfolio selected = {Portfolio} {Method} ", portf.Number, nameof(GetPortfolio));
                        //RobotsWindowVM.Log(Header, " Выбран портфель =  " + portf.Number);
                        return portf;
                    }
                }
            }
            _logger.Error("GetStringPortfolios = null {Method} ", nameof(GetPortfolio));
            //RobotsWindowVM.Log(Header, "GetStringPortfolios  портфель = null ");
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
                e.PropertyName == "FullPositionVolume" ||
                e.PropertyName == "TakePriceLong" ||
                e.PropertyName == "TakePriceShort" ||
                e.PropertyName == "PartsPerExit" ||
                e.PropertyName == "Direction" ||
                e.PropertyName == "StepPersentStopLong" ||
                e.PropertyName == "IsChekTraelStopLong" ||

                e.PropertyName == "StepPersentStopShort" ||
                e.PropertyName == "ActionPositionLong" ||
                e.PropertyName == "PriceStopShort" ||
                e.PropertyName == "N_min" ||
                e.PropertyName == "Avereg" ||
                e.PropertyName == "AveregS" ||
                e.PropertyName == "RatioBuy1" ||
                e.PropertyName == "RatioBuy2" ||
                e.PropertyName == "RatioSell1" ||
                e.PropertyName == "RatioSell2" ||
                e.PropertyName == "IsChekSendAllLogs" ||
                e.PropertyName == "ActionPositionShort" ||
                e.PropertyName == "ActionPosition1Buy" ||
                e.PropertyName == "ActionPosition2Buy" ||
                e.PropertyName == "ActionPosition1Sell" ||
                e.PropertyName == "ActionPosition2Sell" ||
                //e.PropertyName == "IsChekSendAllLogs" ||
                e.PropertyName == "StepPersentStopLongRun" ||
                e.PropertyName == "StepPersentStopShortRun" ||
                e.PropertyName == "IsChekTraelStopShort")
            {
                SaveParamsBot();
            }
        }

        /// <summary>
        /// сохранение параметров робота
        /// </summary>
        public void SaveParamsBot()
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
                    writer.WriteLine(StringPortfolio); // 4 line

                    writer.WriteLine(TakePriceLong);
                    writer.WriteLine(TopPositionPrice);
                    writer.WriteLine(StartPriceOpenPos);
                    writer.WriteLine(Direction);        // 8 line

                    writer.WriteLine(BottomPositionPrice);
                    writer.WriteLine(TakePriceShort);

                    writer.WriteLine(FullPositionVolume);// 11 line

                    writer.WriteLine(PartsPerInput);
                    writer.WriteLine(PartsPerExit);

                    writer.WriteLine(StepPersentStopLong);
                    writer.WriteLine(IsChekTraelStopLong);

                    writer.WriteLine(StepPersentStopShort);
                    writer.WriteLine(IsChekTraelStopShort);

                    writer.WriteLine(PriceStopShort);
                    writer.WriteLine(PriceStopLong);

                    writer.WriteLine(JsonConvert.SerializeObject(PositionsBots)); // 20 line in the file

                    writer.WriteLine(ActionPositionLong); // 21 действие с позицией в лонг

                    writer.WriteLine(IsChekSendAllLogs); // 22 состояние чек бокса телеги

                    writer.WriteLine(N_min);

                    writer.WriteLine(Avereg);

                    writer.WriteLine(RatioBuy1); // 25 первый коэф превышения

                    writer.WriteLine(RatioBuy2);

                    writer.WriteLine(AveregS);

                    writer.WriteLine(RatioSell1); // 28 первый коэф превышения в  баксах

                    writer.WriteLine(RatioSell2);

                    writer.WriteLine(ActionPositionShort); // 30 действие с позицией в шорт

                    writer.WriteLine(ActionPosition1Buy); // 31 действие с позицией при 1 превышении Buy

                    writer.WriteLine(ActionPosition2Buy); // 32 действие с позицией при 2 превышении Buy

                    writer.WriteLine(ActionPosition1Sell); //  действие с позицией при 1 превышении Sell

                    writer.WriteLine(ActionPosition2Sell); // 34 действие с позицией при 2 превышении Sell

                    writer.WriteLine(StepPersentStopLongRun);

                    writer.WriteLine(StepPersentStopShortRun);

                    writer.Close();

                    if (IsChekSendAllLogs) _logger.Information("Saving parameters {Header} {Method} "
                        , Header, nameof(SaveParamsBot));

                    //RobotsWindowVM.Log(Header, "SaveParamsBot  \n cохраненили  параметры ");
                }
            }
            catch (Exception ex)
            {
                _logger.Error("Error saving parameters {Error} {Method} ", ex.Message, nameof(SaveParamsBot));
                //RobotsWindowVM.Log(Header, " Ошибка сохранения параметров = " + ex.Message);
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

                    TakePriceLong = GetDecimalForString(reader.ReadLine());
                    TopPositionPrice = GetDecimalForString(reader.ReadLine());
                    StartPriceOpenPos = GetDecimalForString(reader.ReadLine());

                    Side direct = Side.None;
                    if (Enum.TryParse(reader.ReadLine(), out direct))
                    {
                        Direction = direct;
                    }

                    BottomPositionPrice = GetDecimalForString(reader.ReadLine());
                    TakePriceShort = GetDecimalForString(reader.ReadLine());
                    FullPositionVolume = GetDecimalForString(reader.ReadLine());
                    PartsPerInput = (int)GetDecimalForString(reader.ReadLine());
                    PartsPerExit = (int)GetDecimalForString(reader.ReadLine());

                    StepPersentStopLong = GetDecimalForString(reader.ReadLine());
                    bool IsChek = false;
                    if (bool.TryParse(reader.ReadLine(), out IsChek))
                    {
                        IsChekTraelStopLong = IsChek;
                    }

                    StepPersentStopShort = GetDecimalForString(reader.ReadLine());
                    bool IsCh = false;
                    if (bool.TryParse(reader.ReadLine(), out IsCh))
                    {
                        IsChekTraelStopShort = IsCh;
                    }

                    PriceStopShort = GetDecimalForString(reader.ReadLine());
                    PriceStopLong = GetDecimalForString(reader.ReadLine());

                    PositionsBots = JsonConvert.DeserializeAnonymousType(reader.ReadLine(), new ObservableCollection<Position>());

                    ActionPos action = ActionPos.Stop;
                    if (Enum.TryParse(reader.ReadLine(), out action))
                    {
                        ActionPositionLong = action;
                    }

                    bool chek = true;
                    if (bool.TryParse(reader.ReadLine(), out chek))
                    {
                        IsChekSendAllLogs = chek;
                    }

                    N_min = (int)GetDecimalForString(reader.ReadLine());
                    Avereg = GetDecimalForString(reader.ReadLine());
                    RatioBuy1 = GetDecimalForString(reader.ReadLine());
                    RatioBuy2 = GetDecimalForString(reader.ReadLine());

                    AveregS = (int)GetDecimalForString(reader.ReadLine());
                    RatioSell1 = GetDecimalForString(reader.ReadLine());
                    RatioSell2 = GetDecimalForString(reader.ReadLine());

                    ActionPositionShort = ActionPos.Stop;
                    if (Enum.TryParse(reader.ReadLine(), out action))
                    {
                        ActionPositionShort = action;
                    }

                    ActionPosition1Buy = ActionPos.Stop;
                    if (Enum.TryParse(reader.ReadLine(), out action))
                    {
                        ActionPosition1Buy = action;
                    }

                    ActionPosition2Buy = ActionPos.Stop;
                    if (Enum.TryParse(reader.ReadLine(), out action))
                    {
                        ActionPosition2Buy = action;
                    }

                    ActionPosition1Sell = ActionPos.Stop;
                    if (Enum.TryParse(reader.ReadLine(), out action))
                    {
                        ActionPosition1Sell = action;
                    }

                    ActionPosition2Sell = ActionPos.Stop;
                    if (Enum.TryParse(reader.ReadLine(), out action))
                    {
                        ActionPosition2Sell = action;
                    }

                    StepPersentStopLongRun = GetDecimalForString(reader.ReadLine());

                    StepPersentStopShortRun = GetDecimalForString(reader.ReadLine());

                    //StepType step = StepType.PUNKT;
                    //if (Enum.TryParse(reader.ReadLine(), out step))
                    //{
                    //    StepType = step;
                    //}

                    reader.Close();
                    _logger.Information("LoadParamsBot {Method} {Header}", nameof(LoadParamsBot), Header);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(" Error LoadParamsBot {Error} {Method} ", ex.Message, nameof(LoadParamsBot));
                //RobotsWindowVM.Log(Header, " Ошибка выгрузки параметров = " + ex.Message);
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

        public void Dispose()
        {   //todo: прикрутить удаление файлов настроек и сохрана 

            ServerMaster.ServerCreateEvent -= ServerMaster_ServerCreateEvent;
            PropertyChanged -= RobotBreakVM_PropertyChanged;

            if (IsChekSendAllLogs) _logger.Information(" Dispose {Method}", nameof(Dispose));
        }

        #endregion сервисные конец

        #endregion end metods==============================================

        #region  ЗАГОТОВКИ ==============================================

        #region хлам
        //for (int i = 0; i < ordersAll.Count && ordersAll.Count > 0; i++ )
        //{
        //    if (ordersAll[i].State == OrderStateType.Cancel)
        //    {
        //        if (position.Direction == Side.Buy && ordersAll[i].Side == Side.Buy)
        //        { // ордер открытия
        //            position.OpenOrders.Remove(ordersAll[i]);

        //            _logger.Information("Delete order for Open Orders {Method} {@Order} {NumberUser}",
        //                                                 nameof(ClearCanseledOrderPosition), ordersAll[i], ordersAll[i].NumberUser);
        //        }
        //        if (position.Direction == Side.Sell && ordersAll[i].Side == Side.Sell)
        //        {// ордер открытия
        //            position.OpenOrders.Remove(ordersAll[i]);
        //            _logger.Information("Delete Limit order for Open Orders {Method} {@Order} {NumberUser}",
        //                                                nameof(ClearCanseledOrderPosition), ordersAll[i], ordersAll[i].NumberUser);
        //        }
        //        if (position.Direction == Side.Buy && ordersAll[i].Side == Side.Sell)
        //        { // ордер закрытия
        //            position.CloseOrders.Remove(ordersAll[i]);
        //            _logger.Information("Delete Limit order for Close Orders {Method} {@Order} {NumberUser}",
        //                                                nameof(ClearCanseledOrderPosition), ordersAll[i], ordersAll[i].NumberUser);
        //        }
        //        if (position.Direction == Side.Sell && ordersAll[i].Side == Side.Buy)
        //        {// ордер закрытия
        //            position.CloseOrders.Remove(ordersAll[i]);

        //            _logger.Information("Delete Limit order for Close Orders {Method} {@Order} {NumberUser}",
        //                                               nameof(ClearCanseledOrderPosition), ordersAll[i], ordersAll[i].NumberUser);
        //        }
        //    }
        //}
        #endregion

        /// <summary>
        /// вычисляет средний объем за определенный период 
        /// </summary>
        private void AverageVolumePeriod(int period)
        {/*
            if (_tabClusterSpot.VolumeClusters.Count < BarOldPeriod.ValueInt + 2) // защита от отсутствия необходимых данных
            {
                return;
            }
            decimal volumBackPeriod = 0; // объем за период, обнуляем в начале

            int startIndex = _tabClusterSpot.VolumeClusters.Count - 2;
            int endIndex = _tabClusterSpot.VolumeClusters.Count - 2 - period;

            for (int i = startIndex; i > endIndex; i--)
            {
                HorizontalVolumeCluster clasterPeriod = _tabClusterSpot.VolumeClusters[i]; // объём  в кластере
                HorizontalVolumeLine vol = clasterPeriod.MaxSummVolumeLine; // линия с максимальным объемом
                volumBackPeriod += vol.VolumeSumm; // суммируем за весь период
            }
            decimal zn = Okruglenie(volumBackPeriod / period, 4); // вычисляем среднее и
            AverageVolumeBaсk = zn;                              // отправляем данные в переменную например 
        }

        /// <summary>
        /// отменяет все активные ордера во всех позициях
        /// </summary>
        private void CanselAllPositionActivOrders()
        {
            if (Server == null) return;
            foreach (Position position in PositionsBots)
            {
                List<Order> ordersAll = new List<Order>();
                if (position.OpenActiv)
                {
                    ordersAll = position.OpenOrders;// взять из позиции ордера открытия 
                }
                if (position.CloseActiv)
                {
                    ordersAll.AddRange(position.CloseOrders); // добавили ордера закрытия 
                }
                for (int i = 0; i < ordersAll.Count; i++)
                {
                    if (ordersAll[i].State == OrderStateType.Activ ||
                        ordersAll[i].State == OrderStateType.Patrial ||
                        ordersAll[i].State == OrderStateType.None ||
                        ordersAll[i].State == OrderStateType.Pending)
                    {
                        Server.CancelOrder(ordersAll[i]);
                        _logger.Information("Cancel All positions Activ Orders {Method} Order {@Order} {Number} {NumberMarket} "
                                , nameof(CanselAllPositionActivOrders), ordersAll[i], ordersAll[i].NumberUser, ordersAll[i].NumberMarket);
                        SendStrStatus(" Отменили ордер на бирже");

                        //Thread.Sleep(50);
                    }
                }
            }*/
        }

        /// <summary>
        /// удалить файл сериализвции 
        /// </summary>
        public void DeleteFileSerial()
        {
            if (!OpenVolumePositionLong() && !OpenVolumePositionShort())
            {
                string fileName = @"Parametrs\Tabs\positions_" + Header + "=" + NumberTab + ".json";
                if (File.Exists(fileName) && PositionsBots.Count == 1)
                {
                    foreach (var position in PositionsBots)
                    {
                        if (!ActivOrders(position))
                        {
                            try
                            {
                                File.Delete(fileName);
                            }
                            catch (Exception e)
                            {
                                _logger.Error("Deletion  failed: {error} {Method}", e.Message, nameof(DeleteFileSerial));
                            }
                        }
                        else _logger.Error(" Activ Orders not Deletion failed {Method}", nameof(DeleteFileSerial));
                    }
                }
            }
        }

        /// <summary>
        /// сохраяие сделок в файл 
        /// </summary>
        public void SerializerPosition()
        {
            if (PositionsBots == null || PositionsBots.Count == 0) return;

            if (!Directory.Exists(@"Parametrs\Tabs"))
            {
                Directory.CreateDirectory(@"Parametrs\Tabs");
            }

            DataContractJsonSerializer PositionsBotsSerialazer = new DataContractJsonSerializer(typeof(ObservableCollection<Position>));

            using (var file = new FileStream(@"Parametrs\Tabs\positions_" + Header + "=" + NumberTab + ".json"
                                            , FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                PositionsBotsSerialazer.WriteObject(file, PositionsBots);

                _logger.Information("Serializer in file Positions {Method} {@PositionsBots}",
                                                nameof(SerializerPosition), PositionsBots);
            }
        }

        /// <summary>
        /// загружаеи из фала сохраненные сделки
        /// </summary>
        public void DesirializerPosition()
        {
            //if (PositionsBots == null || PositionsBots.Count == 0) return;

            if (!File.Exists(@"Parametrs\Tabs\positions_" + Header + "=" + NumberTab + ".json"))
            {
                _logger.Error("Desirializer  Positions  Error no file - positions.json {Method} {@PositionsBots}"
                                                                    , nameof(DesirializerPosition), PositionsBots);
                return;
            }

            DataContractJsonSerializer PositionsBotsDsSerialazer = new DataContractJsonSerializer(typeof(ObservableCollection<Position>));
            using (var file = new FileStream(@"Parametrs\Tabs\positions_" + Header + "=" + NumberTab + ".json"
                                            , FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
            {
                ObservableCollection<Position> PositionDeseriolazer = PositionsBotsDsSerialazer.ReadObject(file)
                                                                            as ObservableCollection<Position>;
                if (PositionDeseriolazer != null)
                {
                    PositionsBots = PositionDeseriolazer;

                    _logger.Information("Desirializer  Positions from file - positions.json {Method} {@PositionsBots}",
                                                                        nameof(DesirializerPosition), PositionsBots);
                }
            }
        }

        /// <summary>
        /// проверить состояние ордеров из хранилища RobotsWindowVM.Orders
        /// </summary>
        public void CheckMissedOrders()
        {
            if (SelectedSecurity == null) return;
            if (RobotsWindowVM.Orders == null || RobotsWindowVM.Orders.Count == 0) return;

            foreach (var val in RobotsWindowVM.Orders)
            {
                if (val.Key == SelectedSecurity.Name)
                {
                    foreach (var value in val.Value)
                    {
                        _server_NewOrderIncomeEvent(value.Value);
                    }
                }
            }
        }

        /// <summary>
        /// проверить состояние моих трейдов из хранилища RobotsWindowVM.MyTrades
        /// </summary>
        public void CheckMissedMyTrades()
        {
            if (SelectedSecurity == null) return;
            if (RobotsWindowVM.MyTrades == null || RobotsWindowVM.MyTrades.Count == 0) return;

            foreach (var val in RobotsWindowVM.MyTrades)
            {
                if (val.Key == SelectedSecurity.Name)
                {
                    foreach (var value in val.Value)
                    {
                        _server_NewMyTradeEvent(value.Value);
                    }
                }
            }
        }

        /// <summary>
        ///  перезапись состояния оредра с биржи в мое хранилище
        /// </summary>
        public Order CopyOrder(Order newOrder, Order order)
        {
            order.State = newOrder.State;
            order.TimeCancel = newOrder.TimeCancel;
            order.Volume = newOrder.Volume;
            order.VolumeExecute = newOrder.VolumeExecute;
            order.TimeDone = newOrder.TimeDone;
            order.TimeCallBack = newOrder.TimeCallBack;
            order.NumberUser = newOrder.NumberUser;
            order.NumberMarket = newOrder.NumberMarket;
            order.Comment = newOrder.Comment;

            //_logger.Information(" Copy Order {@order}{OrdNumberUser} {NumberMarket}      {@newOrder}    {NewNumberUser}     {NumberMarket} {@MyTrades}  {Method} "
            //, order, order.NumberUser, order.NumberMarket, newOrder, newOrder.NumberUser, newOrder.NumberMarket, newOrder.MyTrades, nameof(CopyOrder));
            return order;
        }


        #endregion конец  заготовки ===============================================================

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

        #endregion end Commands ====================================================import numpy as np

    }
}


