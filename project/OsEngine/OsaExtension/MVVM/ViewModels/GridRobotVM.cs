using Newtonsoft.Json;
using OsEngine.OsaExtension.MVVM.Commands;
using OsEngine.Entity;
using OsEngine.Market;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.View;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Action = OsEngine.OsaExtension.MVVM.Models.Action;
using Direction = OsEngine.OsaExtension.MVVM.Models.Direction;
using Level = OsEngine.OsaExtension.MVVM.Models.Level;
using OsEngine.OsaExtension.MVVM.Models;

namespace OsEngine.OsaExtension.MVVM.ViewModels
{
    public class GridRobotVM : BaseVM, IRobotVM
    {
        /// <summary>
        /// конструктор для созданого и сохранеенного робота
        /// </summary>
        public GridRobotVM(string header, int numberTab)
        {
            //string[]str = header.Split('=');
            NumberTab = numberTab;
            Header = header;


            LoadParamsBot(header);
            //ClearOrd();
            SelectSecurBalans = 0;

            ServerMaster.ServerCreateEvent += ServerMaster_ServerCreateEvent;

        }
        /// <summary>
        /// конструктор для нового робота 
        /// </summary>
        public GridRobotVM(int numberTab)
        {
            NumberTab = numberTab;
        }

        #region Свойства ================================================================================== 


        /// <summary>
        /// список портфелей 
        /// </summary>
        public ObservableCollection<string> StringPortfolios { get; set; } = new ObservableCollection<string>();

        /// <summary>
        /// колекция уровней 
        /// </summary>
        public ObservableCollection<Level> Levels { get; set; } = new ObservableCollection<Level>();

        //string str = "Levels колекция  = " + Levels.Count;
        //Debug.WriteLine(str);

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

        public NameStrat NameStrat
        {
            get => _nameStrat;
            set
            {
                _nameStrat = value;
                OnPropertyChanged(nameof(NameStrat));
            }
        }
        private NameStrat _nameStrat = NameStrat.GRID;

        /// <summary>
        /// Выбранная бумага
        /// </summary>
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
        /// базовый класс пары
        /// </summary>
        public Security NameClass
        {
            get => _nameClass;
            set
            {
                _nameClass = value;
                OnPropertyChanged(nameof(NameClass));
                OnPropertyChanged(nameof(Header));
                if (NameClass != null)
                {
                    // string klass = SelectedSecurity.NameClass;
                    //OnSelectedSecurity?.Invoke();
                }
            }
        }
        private Security _nameClass = null;

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
#pragma warning disable CS0472 // Результат значения всегда одинаковый, так как значение этого типа никогда не равно NULL
                if (value != null && value != _serverType)
                {
                    _serverType = value;
                }
#pragma warning restore CS0472 // Результат значения всегда одинаковый, так как значение этого типа никогда не равно NULL
            }
        }
        ServerType _serverType = ServerType.None;

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

                _portfolio = GetPortfolio(_stringportfolio);
            }
        }
        private string _stringportfolio = "";

        /// <summary>
        /// точка страта работы робота (цена)
        /// </summary>
        public decimal StartPoint
        {
            get => _startPoint;
            set
            {
                _startPoint = value;
                OnPropertyChanged(nameof(StartPoint));
            }
        }
        private decimal _startPoint;

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
        /// количество уровней 
        /// </summary>
        public int CountLevels
        {
            get => _сountLevels;
            set
            {
                _сountLevels = value;
                OnPropertyChanged(nameof(CountLevels));
            }
        }
        private int _сountLevels;

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
        /// свойство список направления сделок
        /// </summary> 
        public List<Direction> Directions { get; set; } = new List<Direction>()
        {
            Direction.BUY, Direction.SELL, Direction.BUYSELL
        };

        /// <summary>
        ///  Lot
        /// </summary>
        public decimal Lot
        {
            get => _lot;
            set
            {
                _lot = value;
                OnPropertyChanged(nameof(Lot));
            }
        }
        private decimal _lot;

        /// <summary>
        /// тип расчета шага уровня 
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
        /// Шаг уровня 
        /// </summary>
        public decimal StepLevel
        {
            get => _stepLevel;
            set
            {
                _stepLevel = value;
                OnPropertyChanged(nameof(StepLevel));
            }
        }
        private decimal _stepLevel;

        /// <summary>
        /// Профит уровня 
        /// </summary>
        public decimal TakeLevel
        {
            get => _takeLevel;
            set
            {
                _takeLevel = value;
                OnPropertyChanged(nameof(TakeLevel));
            }
        }

        private decimal _takeLevel;

        /// <summary>
        /// количество активных уровней 
        /// </summary>
        public int MaxActiveLevel
        {
            get => _maxActiveLevel;
            set
            {
                _maxActiveLevel = value;
                OnPropertyChanged(nameof(MaxActiveLevel));
            }
        }
        private int _maxActiveLevel;

        /// <summary>
        /// всего позиций 
        /// </summary>
        public decimal AllPositionsCount
        {
            get => _allPositionsCount;
            set
            {
                _allPositionsCount = value;
                OnPropertyChanged(nameof(AllPositionsCount));
            }
        }
        private decimal _allPositionsCount;

        /// <summary>
        /// Средняя цена 
        /// </summary>
        public decimal PriceAverege
        {
            get => _priceAverege;
            set
            {
                _priceAverege = value;
                OnPropertyChanged(nameof(PriceAverege));
            }
        }
        private decimal _priceAverege;

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
        /// Комиссия  
        /// </summary>
        public decimal VarMargine
        {
            get => _varMargine;
            set
            {
                _varMargine = value;
                OnPropertyChanged(nameof(VarMargine));
            }
        }
        private decimal _varMargine;

        /// <summary>
        /// Прибыль 
        /// </summary>
        public decimal Accum
        {
            get => _accum;
            set
            {
                _accum = value;
                OnPropertyChanged(nameof(Accum));
            }
        }
        private decimal _accum;

        /// <summary>
        /// Итого  
        /// </summary>
        public decimal Total
        {
            get => _total;
            set
            {
                _total = value;
                OnPropertyChanged(nameof(Total));
            }
        }
        private decimal _total;

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
            }
        }
        private bool _isRun;

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
        /// 
        /// </summary>
        public bool IsChekCurrency
        {
            get => _isChekCurrency;
            set
            {
                _isChekCurrency = value;
                OnPropertyChanged(nameof(IsChekCurrency));
            }
        }
        private bool _isChekCurrency;

        public bool IsReadOnly
        {
            get => _isReadOnly;
            set
            {
                _isReadOnly = value;
                OnPropertyChanged(nameof(IsReadOnly));
            }
        }
        private bool _isReadOnly;
        /// <summary>
        /// отключение возможности редактирования комбобокса направления сделки
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }
        private bool _isEnabled;

        /// <summary>
        /// расчетная точка стопов для шота
        /// </summary>
        public decimal StopShort
        {
            get => _stopShort;
            set
            {
                _stopShort = value;
                OnPropertyChanged(nameof(StopShort));
            }
        }
        private decimal _stopShort = 0;

        /// <summary>
        /// расчетная точка стопов для лонга
        /// </summary>
        public decimal StopLong
        {
            get => _stopLong;
            set
            {
                _stopLong = value;
                OnPropertyChanged(nameof(StopLong));
            }
        }
        private decimal _stopLong = 0;

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

        #endregion

        #region Поля =======================================================================================

        decimal _bestBid;
        decimal _bestAsk;

        Portfolio _portfolio;

        public List<Side> Sides { get; set; } = new List<Side>()
        {
            Side.Buy,
            Side.Sell,
        };

        CultureInfo CultureInfo = new CultureInfo("ru-RU");
        #endregion

        #region Команды =====================================================================================

        private DelegateCommand _commandStartStop;
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
        private DelegateCommand _сommandCalculate;
        public DelegateCommand CommandCalculate
        {
            get
            {
                if (_сommandCalculate == null)
                {
                    _сommandCalculate = new DelegateCommand(Calculate);
                }
                return _сommandCalculate;
            }
        }

        private DelegateCommand commandClosePositions;
        public DelegateCommand CommandClosePositions
        {
            get
            {
                if (commandClosePositions == null)
                {
                    commandClosePositions = new DelegateCommand(ClosePosition);
                }
                return commandClosePositions;
            }
        }

        private DelegateCommand commandAddRow;
        public DelegateCommand CommandAddRow
        {
            get
            {
                if (commandAddRow == null)
                {
                    commandAddRow = new DelegateCommand(AddRow);
                }
                return commandAddRow;
            }
        }
        private DelegateCommand commandTestApi;
        public DelegateCommand CommandTestApi
        {
            get
            {
                if (commandTestApi == null)
                {
                    commandTestApi = new DelegateCommand(TestApi);
                }
                return commandTestApi;
            }
        }

        #endregion

        #region все Методы =====================================================================================

        #region ===== логика ==============================================================

        /// <summary>
        /// расчитывает уровни (цены открытия и профитов)
        /// </summary>
        void Calculate(object o)
        {
            decimal volume = 0;
            decimal stepTake = 0;

            foreach (Level level in Levels)
            {
                volume += Math.Abs(level.Volume);
            }
            if (volume > 0)
            {
                MessageBoxResult result = MessageBox.Show(" Есть открытые позиции! \n Всеравно пресчитать? ", " ВНИМАНИЕ !!! ",
                    MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }

            ObservableCollection<Level> levels = new ObservableCollection<Level>();

            decimal currBuyPrice = StartPoint;
            decimal currSellPrice = StartPoint;

            if (CountLevels <= 0 || SelectedSecurity == null)
            {
                return;
            }
            RobotsWindowVM.Log(Header, " \n\n Пересчитываем уровни  ");
            for (int i = 0; i < CountLevels; i++)
            {
                Level levelBuy = new Level() { Side = Side.Buy };
                Level levelSell = new Level() { Side = Side.Sell };

                if (StepType == StepType.PUNKT)
                {
                    currBuyPrice -= StepLevel * SelectedSecurity.PriceStep;
                    currSellPrice += StepLevel * SelectedSecurity.PriceStep;

                    stepTake = TakeLevel * SelectedSecurity.PriceStep;
                }
                else if (StepType == StepType.PERCENT)
                {
                    currBuyPrice -= StepLevel * currBuyPrice / 100;
                    currBuyPrice = Decimal.Round(currBuyPrice, SelectedSecurity.Decimals);

                    currSellPrice += StepLevel * currSellPrice / 100;
                    currSellPrice = Decimal.Round(currSellPrice, SelectedSecurity.Decimals);

                    stepTake = TakeLevel * currBuyPrice / 100;
                    stepTake = Decimal.Round(stepTake, SelectedSecurity.Decimals);

                }
                levelSell.PriceLevel = currSellPrice;
                levelBuy.PriceLevel = currBuyPrice;

                if (Direction == Direction.BUY || Direction == Direction.BUYSELL)
                {
                    levelBuy.TakePrice = levelBuy.PriceLevel + stepTake;
                    levels.Add(levelBuy);
                }
                if (Direction == Direction.SELL || Direction == Direction.BUYSELL)
                {
                    levelSell.TakePrice = levelSell.PriceLevel - stepTake;
                    levels.Insert(0, levelSell);
                }
                RobotsWindowVM.Log(Header, "Уровень =  " + levels.Last().GetStringForSave());
            }
            Levels = levels;
            OnPropertyChanged(nameof(Levels));

            CalculateStop();

            SaveParamsBot();
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
            //RobotsWindowVM.ChengeEmitendWidow = new ChengeEmitendWidow(this);
            RobotsWindowVM.ChengeEmitendWidow.ShowDialog();
            RobotsWindowVM.ChengeEmitendWidow = null;
            if (_server != null)
            {
                if (_server.ServerType == ServerType.Binance
                    || _server.ServerType == ServerType.BinanceFutures)
                {
                    IsChekCurrency = true;
                }
                else IsChekCurrency = false;
            }
        }

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
                        SaveParamsBot();
                        //GetOrderStatusOnBoard();
                        break;
                    }
                    Thread.Sleep(1000);
                }
            });
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
        /// добавить строку уровня
        /// </summary>
        private void AddRow(object o)
        {
            if (IsRun)
            {
                MessageBox.Show("Перейдите в режим редактирования!  ");
            }
            Levels.Add(new Level());
        }
        private void TestApi(object o)
        {
            //GetOrderStatusOnBoard();
        }

        /// <summary>
        /// закрываем все позиции 
        /// </summary>
        private void ClosePosition(object o)
        {
            MessageBoxResult resut = MessageBox.Show("Закрыть все позиции по " + Header, " Уверен? ", MessageBoxButton.YesNo);
            if (resut == MessageBoxResult.Yes)
            {
                GetBalansSecur();
                IsRun = false;
                foreach (Level level in Levels)
                {
                    level.CancelAllOrders(Server, Header);
                    LevelTradeLogicClose(level, Action.CLOSE);

                    if (SelectSecurBalans != 0)
                    {
                        // level.PassTake = false;
                        LevelTradeLogicClose(level, Action.CLOSE);
                    }
                }
            }
        }

        /// <summary>
        /// проверка стопа
        /// </summary>
        private void ExaminationStop()
        {
            if (SelectSecurBalans == 0 || IsRun == false) return;

            if (StopLong != 0 && Price != 0)
            {
                if (Price < StopLong && Direction == Direction.BUY ||
                    Price < StopLong && Direction == Direction.BUYSELL)
                {
                    IsRun = false;
                    StopLong = 0;
                    string str = " Сработал СТОП лонга \n IsRun = false , сохранились \n ";
                    Debug.WriteLine(str);
                    foreach (Level level in Levels)
                    {
                        level.CancelAllOrders(Server, Header);
                        string str3 = "level long = " + level.PriceLevel;
                        Debug.WriteLine(str3);

                        string str2 = "Всего уровней = " + Levels.Count;
                        Debug.WriteLine(str2);

                        LevelTradeLogicClose(level, Action.STOP);
                        RobotsWindowVM.Log(Header, " Сработал СТОП ЛОНГ ");
                        StopLong = 0;
                    }
                    return;
                }
            }
            if (StopShort != 0 && Price != 0)
            {
                if (Price > StopShort && Direction == Direction.SELL ||
                    Price > StopShort && Direction == Direction.BUYSELL)
                {
                    IsRun = false;
                    SaveParamsBot();
                    string str = " Сработал СТОП шорта, IsRun = false , сохранились \n ";
                    Debug.WriteLine(str);
                    StopShort = 0;
                    foreach (Level level in Levels)
                    {
                        level.CancelAllOrders(Server, Header);

                        RobotsWindowVM.Log(Header, "ExaminationStop \n " + " Сработал СТОП ШОРТА ");
                        string str4 = "level Short = " + level.PriceLevel;
                        Debug.WriteLine(str4);

                        string str2 = "Всего уровней = " + Levels.Count;
                        Debug.WriteLine(str2);

                        LevelTradeLogicClose(level, Action.STOP);
                        StopShort = 0;
                    }
                }
            }
        }

        private void StartStop(object o)
        {
            Thread.Sleep(300);

            IsRun = !IsRun;

            RobotsWindowVM.Log(Header, " \n\n StartStop = " + IsRun);

            SaveParamsBot();
            if (IsRun)
            {
                foreach (Level level in Levels)
                {
                    level.SetVolumeStart();
                    level.PassVolume = true;
                    level.PassTake = true;
                }
            }
            else
            {
                Task.Run(() =>
                {
                    while (true)
                    {
                        foreach (Level level in Levels)
                        {
                            level.CancelAllOrders(Server, Header);
                            Thread.Sleep(50);
                        }
                        Thread.Sleep(1500);

                        bool flag = true;
                        foreach (Level level in Levels)
                        {
                            if (level.LimitVolume != 0 ||
                            level.TakeVolume != 0)
                            {
                                flag = false;
                                break;
                            }
                        }
                        if (flag) break;
                    }
                });
            }
        }

        /// <summary>
        /// после трейда или ордера с бижжи по бумаге гоняет по уровням  логику отправки ореров на открытип и закрытие 
        /// </summary>
        private void TradeLogic()
        {
            if (IsRun == false || SelectedSecurity == null)
            {
                return;
            }
            foreach (Level level in Levels)
            {
                LevelTradeLogicOpen(level);

                LevelTradeLogicClose(level, Action.TAKE);
            }
        }
        private decimal GetStepLevel()
        {
            decimal stepLevel = 0;
            if (StepType == StepType.PUNKT)
            {
                stepLevel = StepLevel * SelectedSecurity.PriceStep;
            }
            else if (StepType == StepType.PERCENT)
            {
                stepLevel = StepLevel * Price / 100;
                stepLevel = Decimal.Round(stepLevel, SelectedSecurity.Decimals);
            }
            return stepLevel;
        }

        /// <summary>
        /// расчитать цену стопов
        /// </summary>
        private void CalculateStop()
        {
            decimal stepLevel = 0;
            if (StepType == StepType.PUNKT)
            {
                stepLevel = StepLevel * SelectedSecurity.PriceStep;
            }
            else if (StepType == StepType.PERCENT)
            {
                stepLevel = StepLevel * Price / 100;
                stepLevel = Decimal.Round(stepLevel, SelectedSecurity.Decimals);
            }
            StopLong = StartPoint - stepLevel * (CountLevels + 1);
            StopShort = StartPoint + stepLevel * (CountLevels + 1);
        }

        /// <summary>
        /// логика отправки  лимит ордера на открытия на уровнях
        /// </summary>
        private void LevelTradeLogicOpen(Level level)
        {
            if (IsRun == false || SelectedSecurity == null)
            {
                return;
            }

            decimal stepLevel = GetStepLevel();

            decimal borderUp = Price + stepLevel * MaxActiveLevel;

            decimal borderDown = Price - stepLevel * MaxActiveLevel;

            if (level.PassVolume && level.PriceLevel != 0
                  && Math.Abs(level.Volume) + level.LimitVolume < Lot)
            {
                if ((level.Side == Side.Buy && level.PriceLevel >= borderDown)
                   || (level.Side == Side.Sell && level.PriceLevel <= borderUp))
                {
                    decimal lot = CalcWorkLot(Lot, level.PriceLevel);

                    decimal worklot = lot - Math.Abs(level.Volume) - level.LimitVolume;
                    RobotsWindowVM.Log(Header, "LevelTradeLogicOpen ");
                    RobotsWindowVM.Log(Header, " Уровень = " + level.GetStringForSave());
                    RobotsWindowVM.Log(Header, "Рабочий лот =  " + worklot);
                    RobotsWindowVM.Log(Header, "IsChekCurrency =  " + IsChekCurrency);

                    level.PassVolume = false;

                    Order order = SendLimitOrder(SelectedSecurity, level.PriceLevel, worklot, level.Side);
                    if (order != null)
                    {
                        level.OrdersForOpen.Add(order);

                        RobotsWindowVM.Log(Header, " Отправляем лимитку в level.OrdersForOpen " + GetStringForSave(order));
                        Thread.Sleep(10);
                    }
                    else
                    {
                        level.PassVolume = true;
                    }
                }
            }
        }

        /// <summary>
        /// логика отправки ордеров на закрытия на уровнях
        /// </summary>
        private void LevelTradeLogicClose(Level level, Action action)
        {
            decimal stepLevel = GetStepLevel();

            if (action == Action.STOP)
            {
                Side side = Side.None;

                if (level.Volume > 0)
                {
                    side = Side.Sell;
                }
                else if (level.Volume < 0)
                {
                    side = Side.Buy;
                }

                decimal worklot = Math.Abs(level.Volume);// -level.TakeVolume
                if (IsChekCurrency && worklot * level.PriceLevel > 6 || !IsChekCurrency)
                {
                    Order order = SendMarketOrder(SelectedSecurity, Price, worklot, side);
                    if (order != null)
                    {
                        level.PassVolume = false;
                        level.OrdersForClose.Add(order);
                        RobotsWindowVM.Log(Header, "Отправлен Маркет ордер в  OrdersForClose \n  "
                                                    + GetStringForSave(order));
                    }
                    else level.PassVolume = true;
                }
                else if (worklot * level.PriceLevel <= 6 && worklot != 0)
                {
                    RobotsWindowVM.Log(Header, "ВНИМАНИЕ объём МАРКЕТ ордера  бред!! <= 6 $ \n ордер не отправлен\n" +
                        "action == Action.STOP \n " +
                        " worklot  =  " + worklot);
                    // надо добавить объем в позицию и закрыть ёё
                    // бред такого не должно случаться 
                    return;
                }
            }

            if (action == Action.CLOSE)
            {
                decimal price = 0;
                Side side = Side.None;

                if (level.Volume > 0)
                {
                    price = _bestAsk;

                    side = Side.Sell;
                }
                else if (level.Volume < 0)
                {
                    price = _bestBid;

                    side = Side.Buy;
                }
                level.PassTake = false;

                decimal worklot = Math.Abs(level.Volume);
                if (IsChekCurrency && worklot * level.PriceLevel > 6 || !IsChekCurrency)
                {
                    if (price == 0)
                    {
                        level.PassTake = true;
                        return;
                    }
                    level.PassTake = false;
                    Order order = SendMarketOrder(SelectedSecurity, price, worklot, side);
                    if (order != null)
                    {
                        RobotsWindowVM.Log(Header, " SendMarketOrder в режиме  Close \n"
                             + GetStringForSave(order));

                        RobotsWindowVM.SendStrTextDb(" SendMarketOrder в режиме  Close\n " + order.NumberUser);

                    }
                    else
                    {
                        level.PassVolume = true;
                    }
                }
            }

            if (IsRun == false || SelectedSecurity == null && action == Action.TAKE)
            {
                return;
            }

            // выбрана бумага и робот включен следует логика выставления тейков 

            if (level.PassTake && level.PriceLevel != 0)
            {
                if (level.Volume != 0 && level.TakeVolume != Math.Abs(level.Volume))
                {
                    decimal price = 0;
                    Side side = Side.None;

                    if (level.Volume > 0)
                    {
                        if (action == Action.TAKE)
                        {
                            price = level.TakePrice;
                        }
                        side = Side.Sell;
                    }
                    else if (level.Volume < 0)
                    {
                        if (action == Action.TAKE)
                        {
                            price = level.TakePrice;
                        }
                        side = Side.Buy;
                    }
                    level.PassTake = false;

                    RobotsWindowVM.Log(Header, "Уровень = " + level.GetStringForSave());

                    decimal worklot = Math.Abs(level.Volume) - level.TakeVolume;
                    RobotsWindowVM.Log(Header, "Рабочий лот =  " + worklot);
                    RobotsWindowVM.Log(Header, "IsChekCurrency =  " + IsChekCurrency);

                    if (IsChekCurrency && worklot * level.PriceLevel > 6 || !IsChekCurrency)
                    {
                        Order order = SendLimitOrder(SelectedSecurity, price, worklot, side);
                        if (order != null)
                        {
                            level.PassTake = false;
                            level.OrdersForClose.Add(order);
                            RobotsWindowVM.Log(Header, " сохраняем Тэйк ордер в OrdersForClose " + GetStringForSave(order));
                        }
                        else
                        {
                            level.PassTake = true;
                            return;
                        }
                    }
                    else if (worklot * level.PriceLevel <= 6 && worklot != 0)
                    {
                        RobotsWindowVM.Log(Header, "ВНИМАНИЕ action ТЭЙК ордер меньше 6 $ не отрпавлен \n" +
                            " worklot  =  " + worklot);
                        // ждем Н секунд
                        // Thread.Sleep(3000);
                        level.CalculateOrders();
                        // проверяем открытый объем на уровне - Volum и объем лимитки тейка 
                        // и если 
                        if (Math.Abs(level.Volume) == Math.Abs(level.TakeVolume) && level.Volume != 0)
                        {
                            level.CanselPatrialOrders(Server);
                        }
                        // TODO: переставить ордер - удалить маленький, докупить обем и закрыть весь обем 
                        // надо обдумать логику 
                    }
                }
            }
        }

        /// <summary>
        /// раcчет позиций 
        /// </summary>
        private void CalculateMargin()
        {
            if (Levels == null) return;

            if (Levels.Count == 0 || SelectedSecurity == null) return;

            decimal volume = 0;
            decimal accum = 0;
            decimal margine = 0;
            decimal averagePrice = 0;

            foreach (Level level in Levels)
            {
                if (level.Volume != 0)
                {
                    averagePrice = (level.OpenPrice * level.Volume + averagePrice * volume)
                        / (level.Volume + volume);

                    level.Margin = (Price - level.OpenPrice) * level.Volume * SelectedSecurity.Lot;
                }
                volume += level.Volume;
                accum += level.Accum;
                margine += level.Margin;
            }

            AllPositionsCount = Math.Round(volume, SelectedSecurity.Decimals);
            PriceAverege = Math.Round(averagePrice, SelectedSecurity.Decimals);
            Accum = Math.Round(accum, SelectedSecurity.Decimals);
            VarMargine = Math.Round(margine, SelectedSecurity.Decimals);
            Total = Accum + VarMargine;

        }

        /// <summary>
        /// расчитывает количество монет (лот)
        /// </summary>
        private decimal CalcWorkLot(decimal lot, decimal price)
        {
            decimal workLot = lot;
            if (IsChekCurrency)
            {
                workLot = lot / price;
            }
            workLot = decimal.Round(workLot, SelectedSecurity.DecimalsVolume);

            return workLot;
        }

        /// <summary>
        ///  отправить лимитный оредер на биржу 
        /// </summary>
        private Order SendLimitOrder(Security sec, decimal prise, decimal volume, Side side)
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
            RobotsWindowVM.Log(Header, "SendLimitOrder\n " + " отправляем лимитку на биржу\n" + GetStringForSave(order));
            RobotsWindowVM.SendStrTextDb(" SendLimitOrder " + order.NumberUser);

            Server.ExecuteOrder(order);

            return order;
        }

        /// <summary>
        ///  отправить Маркетный оредер на биржу 
        /// </summary>
        private Order SendMarketOrder(Security sec, decimal prise, decimal volume, Side side)
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
            RobotsWindowVM.Log(Header, "SendMarketOrder\n " + " отправляем маркет на биржу\n" + GetStringForSave(order));
            RobotsWindowVM.SendStrTextDb(" SendMarketOrder " + order.NumberUser);
            Server.ExecuteOrder(order);

            return order;
        }

        /// <summary>
        ///  подключиться к серверу
        /// </summary>
        private void SubscribeToServer()
        {
            _server.NewMyTradeEvent += Server_NewMyTradeEvent;
            _server.NewOrderIncomeEvent += Server_NewOrderIncomeEvent;
            _server.NewTradeEvent += Server_NewTradeEvent;
            _server.SecuritiesChangeEvent += _server_SecuritiesChangeEvent;
            _server.PortfoliosChangeEvent += _server_PortfoliosChangeEvent;
            _server.NewBidAscIncomeEvent += _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent += _server_ConnectStatusChangeEvent;

            RobotsWindowVM.Log(Header, " Подключаемся к серверу = " + _server.ServerType);
        }

        /// <summary>
        ///  отключиться от сервера 
        /// </summary>
        private void UnSubscribeToServer()
        {
            _server.NewMyTradeEvent -= Server_NewMyTradeEvent;
            _server.NewOrderIncomeEvent -= Server_NewOrderIncomeEvent;
            _server.NewTradeEvent -= Server_NewTradeEvent;
            _server.SecuritiesChangeEvent -= _server_SecuritiesChangeEvent;
            _server.PortfoliosChangeEvent -= _server_PortfoliosChangeEvent;
            _server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;

            RobotsWindowVM.Log(Header, " Отключились от сервера = " + _server.ServerType);
        }

        #endregion

        #region  ===== сервисные ========================================================================
        /// <summary>
        /// удалять ошибочные и законченные 
        /// </summary>
        private void ClearOrd()
        {
            foreach (Level level in Levels)
            {
                level.ClearOrders(ref level.OrdersForOpen);
                level.ClearOrders(ref level.OrdersForClose);
            }
        }

        ///<summary>
        /// взять текущий объем на бирже выбаной  бумаги
        /// </summary>
        private void GetBalansSecur()
        {
            List<Portfolio> portfolios = new List<Portfolio>();
            if (Server.Portfolios != null)
            {
                portfolios = Server.Portfolios;
            }
            if (portfolios.Count > 0 && portfolios != null
                && _selectedSecurity != null)
            {
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
                        SelectSecurBalans = d; // отправка значения в свойство
                    }
                }
            }
            //decimal balans = portfolios[0].GetPositionOnBoard()[0].Find(pos =>
            //    pos.SecurityNameCode == _securName).ValueCurrent;
            //return balans;

        }

        /// <summary>
        ///  формируем строку для сохранения ордера
        /// </summary>
        private string GetStringForSave(Order order)
        {
            string str = "";
            str += "ордер = \n";
            str += order.SecurityNameCode + " | ";
            str += order.PortfolioNumber + " | ";
            str += order.TimeCreate + " | ";
            str += order.State + " | ";
            str += order.Side + " | ";
            str += "Объем = " + order.Volume + " | ";
            str += "Цена = " + order.Price + " | ";
            str += "Мой Номер = " + order.NumberUser + " | ";
            str += "Номер биржи = " + order.NumberMarket + " | ";
            //str += order.SecurityNameCode + " | ";

            return str;
        }

        /// <summary>
        ///  формируем строку для сохранения моих трейдов 
        /// </summary>
        private string GetStringForSave(MyTrade myTrade)
        {
            string str = "";
            str += "мой трейд = \n";
            str += myTrade.SecurityNameCode + " | ";
            str += myTrade.Side + " | ";
            str += "Объем = " + myTrade.Volume + " | ";
            str += "Цена = " + myTrade.Price + " | ";
            str += "NumberOrderParent = " + myTrade.NumberOrderParent + " | ";
            str += "NumberTrade = " + myTrade.NumberTrade + " | ";

            return str;
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
                    writer.WriteLine(Header);
                    writer.WriteLine(ServerType);
                    writer.WriteLine(StringPortfolio);

                    writer.WriteLine(StopShort);
                    writer.WriteLine(StartPoint);
                    writer.WriteLine(StopLong);

                    writer.WriteLine(CountLevels);
                    writer.WriteLine(Direction);
                    writer.WriteLine(Lot);
                    writer.WriteLine(StepType);
                    writer.WriteLine(StepLevel);
                    writer.WriteLine(TakeLevel);
                    writer.WriteLine(MaxActiveLevel);
                    writer.WriteLine(PriceAverege);
                    writer.WriteLine(Accum);

                    writer.WriteLine(JsonConvert.SerializeObject(Levels));

                    writer.WriteLine(IsChekCurrency);
                    writer.WriteLine(IsRun);

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
                    Header = reader.ReadLine(); // загружаем заголовок
                    servType = reader.ReadLine(); // загружаем название сервера
                    StringPortfolio = reader.ReadLine();  // загружаем бумагу 

                    StopShort = GetDecimalForString(reader.ReadLine());
                    StartPoint = GetDecimalForString(reader.ReadLine());
                    StopLong = GetDecimalForString(reader.ReadLine());

                    CountLevels = (int)GetDecimalForString(reader.ReadLine());

                    Direction direct = Direction.BUY;
                    if (Enum.TryParse(reader.ReadLine(), out direct))
                    {
                        Direction = direct;
                    }

                    Lot = GetDecimalForString(reader.ReadLine());

                    StepType step = StepType.PUNKT;
                    if (Enum.TryParse(reader.ReadLine(), out step))
                    {
                        StepType = step;
                    }

                    StepLevel = GetDecimalForString(reader.ReadLine());
                    TakeLevel = GetDecimalForString(reader.ReadLine());
                    MaxActiveLevel = (int)GetDecimalForString(reader.ReadLine());
                    PriceAverege = GetDecimalForString(reader.ReadLine());
                    Accum = GetDecimalForString(reader.ReadLine());

                    Levels = JsonConvert.DeserializeAnonymousType(reader.ReadLine(), new ObservableCollection<Level>());

                    bool check = false;
                    if (bool.TryParse(reader.ReadLine(), out check))
                    {
                        IsChekCurrency = check;
                    }
                    bool run = false;
                    if (bool.TryParse(reader.ReadLine(), out run))
                    {
                        IsRun = run;
                    }

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
        ///  преобразует строку из файла сохранения в децимал 
        /// </summary>
        private decimal GetDecimalForString(string str)
        {
            decimal value = 0;
            decimal.TryParse(str, out value);
            return value;
        }

        /// <summary>
        /// является ли уровень активным 
        /// </summary>
        private bool ActiveLevelAre()
        {
            if (Levels == null || Levels.Count == 0) return false;
            foreach (Level level in Levels)
            {
                if (level.StatusLevel == PositionStatus.OPENING ||
                    level.StatusLevel == PositionStatus.OPEN ||
                    level.StatusLevel == PositionStatus.CLOSING)
                {
                    return true;
                }
            }
            return false;
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
        /// проверить состояние ордеров
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
                        Server_NewOrderIncomeEvent(value.Value);
                    }
                }
            }
        }

        /// <summary>
        /// проверить состояние моих трейдов
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
                        Server_NewMyTradeEvent(value.Value);
                    }
                }
            }
        }

        /// <summary>
        /// запросить статусы выбранных оредров 
        /// </summary>
        private void GetStateOrdeps()
        {
            if (Server != null)
            {
                if (Server.ServerType == ServerType.BinanceFutures)
                {
                    AServer aServer = (AServer)Server;

                    List<Order> orders = new List<Order>(); // ордера статусы которых надо опросить

                    foreach (Level level in Levels)
                    {
                        GetStateOrdeps(level.OrdersForOpen, ref orders);
                        GetStateOrdeps(level.OrdersForClose, ref orders);
                    }
                    if (orders.Count > 0)
                    {
                        aServer.ServerRealization.GetOrdersState(orders);
                    }
                }
            }
        }

        /// <summary>
        ///  выбирает ордера для опроса 
        /// </summary>
        private void GetStateOrdeps(List<Order> orders, ref List<Order> stateOrders)
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

        #endregion

        #region ======= события сервера ======================================================================

        /// <summary>
        /// сервер сменил статус
        /// </summary>
        private void _server_ConnectStatusChangeEvent(string status)
        {
            if (_server != null)
            {
                if (Server.ServerStatus == ServerConnectStatus.Connect)
                {
                    GetStateOrdeps();
                    //  ЕЩЁ Смотри реализацию в робот виндов вм
                    //GetOrderStatusOnBoard();
                    // DesirializerDictionaryOrders();
                }
                else IsRun = false;
            }
        }

        /// <summary>
        /// изменились лучшие цены 
        /// </summary>
        private void _server_NewBidAscIncomeEvent(decimal bid, decimal ask, Security namesecur)
        {
            _bestBid = 0;
            _bestAsk = 0;
            if (namesecur.Name == SelectedSecurity.Name)
            {
                _bestAsk = ask;
                _bestBid = bid;
                //TradeLogic();
            }
        }

        private void Server_NewTradeEvent(List<Trade> trades)
        {
            if (trades != null && trades[0].SecurityNameCode == SelectedSecurity.Name)
            {
                Trade trade = trades.Last();

                Price = trade.Price;

                CalculateMargin();
                ExaminationStop();

                if (trade.Time.Second % 10 == 0)
                {
                    TradeLogic();
                }
            }
        }

        /// <summary>
        /// пришел ответ с биржи по ордеру 
        /// </summary>
        private void Server_NewOrderIncomeEvent(Order order)
        {
            if (order == null || _portfolio == null || SelectedSecurity == null) return;
            if (order.SecurityNameCode == SelectedSecurity.Name
                && order.ServerType == Server.ServerType) // 
            {
                //  дальше запись в лог ответа с биржи по ордеру и уровню 
                bool rec = true;
                if (order.State == OrderStateType.Activ
                    && order.TimeCallBack.AddSeconds(2) < Server.ServerTime)
                {
                    rec = false;
                }
                if (rec)
                {
                    RobotsWindowVM.Log(Header, "NewOrderIncomeEvent = " + GetStringForSave(order));
                }
                if (order.NumberMarket != "")
                {
                    foreach (Level level in Levels)
                    {
                        bool newOrderBool = level.NewOrder(order);

                        if (newOrderBool)
                        {
                            RobotsWindowVM.Log(Header, " Обновился Уровень = " + level.GetStringForSave());
                        }
                    }
                }
                SaveParamsBot();
            }
        }

        /// <summary>
        ///  пришел мой трейд перевыставляем ордера по уровням
        /// </summary>
        private void Server_NewMyTradeEvent(MyTrade myTrade)
        {
            if (myTrade == null ||
                SelectedSecurity == null ||
                myTrade.SecurityNameCode != SelectedSecurity.Name) { return; }

            foreach (Level level in Levels)
            {
                bool res = level.AddMyTrade(myTrade, SelectedSecurity);
                if (res)
                {
                    RobotsWindowVM.Log(Header, GetStringForSave(myTrade));

                    if (myTrade.Side == level.Side)
                    {
                        LevelTradeLogicClose(level, Action.TAKE);
                    }
                    else
                    {
                        LevelTradeLogicOpen(level);
                    }
                    SaveParamsBot();
                }
            }
        }
        /// <summary>
        /// Сервер мастер создан сервер
        /// </summary>
        private void ServerMaster_ServerCreateEvent(IServer server)
        {
            if (server.ServerType == ServerType)
            {
                Server = server;
            }
        }

        /// <summary>
        /// список подключенных бумаг на сервере изменися 
        /// </summary>
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

        /// <summary>
        /// изменлся портфель на сервере
        /// </summary>
        private void _server_PortfoliosChangeEvent(List<Portfolio> portfolios)
        {
            GetBalansSecur();// запросить объем монеты на бирже 
            if (portfolios == null || portfolios.Count == 0) // нет новых портфелей 
            {
                return;
            }

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
        #endregion

        #endregion ===========================================================================================

        #region ============================================События============================================

        public delegate void selectedSecurity();
        public event selectedSecurity OnSelectedSecurity;

        #endregion
    }
}
