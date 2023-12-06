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
                    OpenPositionLogic();
                }
                else
                {
                    StopTradeLogic();
                }
            }
        }
        private bool _isRun;

        /// <summary>
        /// расчетные цены открытия позиции 
        /// </summary>
        public List<decimal> PriceOpenPos
        {
            get => _priceOpenPos;
            set
            {
                _priceOpenPos = value;
                OnPropertyChanged(nameof(PriceOpenPos));
            }
        }
        private List<decimal> _priceOpenPos = new List<decimal>();

        /// <summary>
        /// расчетные цены закрытия позиции 
        /// </summary>
        public List<decimal> PriceClosePos
        {
            get => _priceClosePos;
            set
            {
                _priceClosePos = value;
                OnPropertyChanged(nameof(PriceClosePos));
            }
        }
        private List<decimal> _priceClosePos = new List<decimal>();

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
        /// тартовая цена наобра позиции
        /// </summary>
        public decimal StartPriceOpenPos
        {
            get =>_startPriceOpenPos;
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
        /// Oбъем на ордер закрытия (части позиции)
        /// </summary>
        public decimal VolumePerOrderClose
        {
            get => _volumePerOrderClose;
            set
            {
                _volumePerOrderClose = value;
                OnPropertyChanged(nameof(VolumePerOrderClose));
            }
        }
        private decimal _volumePerOrderClose = 0;

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
        /// Действия с позициией
        /// </summary>
        public ActionPos ActionPosition
        {
            get => _actionPosition;
            set
            {
                _actionPosition = value;
                OnPropertyChanged(nameof(ActionPosition));
            }
        }
        private ActionPos _actionPosition;

        /// <summary>
        /// список  действий с позицией 
        /// </summary> 
        public List<ActionPos> ActionPositions { get; set; } = new List<ActionPos>()
        {
            ActionPos.Stop, ActionPos.RollOver, ActionPos.AddVolumes
        };

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

        public bool IsChekMonitor
        {
            get => _isChekMonitor;
            set
            {
                _isChekMonitor = value;
                OnPropertyChanged(nameof(IsChekMonitor));
            }
        }
        private bool _isChekMonitor;

        /// <summary>
        /// расстояние до трейлин стопа лонг в % 
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
        /// расстояние до трейлин стопа шорт в % 
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
       
        public static ObservableCollection<Position> PositionsBots { get; set; } = new ObservableCollection<Position>();

        #endregion конец свойств =============================================

        #region Поля ==================================================

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
        private RobotsWindowVM _robotsWindowVM;       

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

            ClearCanceledOrderPosition();
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
                CreateNewPosition(); // создали позиции
                SendOpenOrderPosition();// открытие позиции
            }
        }

        /// <summary>
        /// остановить торговлю закрыть Все позиции
        /// </summary>
        private void StopTradeLogic()
        {
            GetBalansSecur();
            foreach (Position pos in PositionsBots)
            {
                CanselAllPositionActivOrders();

                if (pos.OpenVolume!=0)
                {
                    FinalCloseMarketOpenVolume( pos ,pos.OpenVolume);
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

            foreach (Position position in PositionsBots)
            {
                List<Order> ordersAll = new List<Order>();

                Order ordAdd = new Order();
          
                ordersAll = position.OpenOrders;// взять из позиции ордера открытия 
                
                if(position.CloseOrders != null)
                {
                    ordersAll.AddRange(position.CloseOrders); // добавили ордера закрытия 
                }

                bool levak = true;
                for (int i = 0; i < ordersAll.Count; i++)
                {
                    if (ordersAll[i].NumberUser == order.NumberUser)
                    {
                        levak = false;
                        if (order.State == OrderStateType.Cancel)
                        {
                            DeleteOrderPosition(order);
                        }
                    }
                }

                if (levak && order.State == OrderStateType.Activ)
                {
                    if(position.Direction == Side.Buy && order.Side == Side.Buy ) 
                    {
                        position.AddNewOpenOrder(order);
                        _logger.Information("Send Limit order for Open Orders {Method} {@Order} {NumberUser}",
                                                             nameof(AddOrderPosition), order, order.NumberUser);
                    }
                    if(position.Direction == Side.Sell && order.Side == Side.Sell ) 
                    {
                        position.AddNewOpenOrder(order);
                        _logger.Information("Send Limit order for Open Orders {Method} {@Order} {NumberUser}",
                                                            nameof(AddOrderPosition), order, order.NumberUser);
                    }
                    if (position.Direction == Side.Buy && order.Side == Side.Sell)
                    {
                        position.AddNewCloseOrder(order);
                        _logger.Information("Send Limit order for Close Orders {Method} {@Order} {NumberUser}",
                                                            nameof(AddOrderPosition), order, order.NumberUser);
                    }
                    if(position.Direction == Side.Sell && order.Side == Side.Buy) 
                    {
                        position.AddNewCloseOrder(order);
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

                        _logger.Information("Delete order for Open Orders {Method} {@Order} {NumberUser}",
                                                             nameof(DeleteOrderPosition), order, order.NumberUser);
                    }
                    if (position.Direction == Side.Sell && order.Side == Side.Sell)
                    {// ордер открытия
                        position.OpenOrders.Remove(order);
                        _logger.Information("Delete Limit order for Open Orders {Method} {@Order} {NumberUser}",
                                                            nameof(DeleteOrderPosition), order, order.NumberUser);
                    }
                    if (position.Direction == Side.Buy && order.Side == Side.Sell)
                    { // ордер закрытия
                        position.CloseOrders.Remove(order);
                        _logger.Information("Delete Limit order for Close Orders {Method} {@Order} {NumberUser}",
                                                            nameof(DeleteOrderPosition), order, order.NumberUser);
                    }
                    if (position.Direction == Side.Sell && order.Side == Side.Buy)
                    {// ордер закрытия
                        position.CloseOrders.Remove(order);

                        _logger.Information("Delete Limit order for Close Orders {Method} {@Order} {NumberUser}",
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
                CalculateVolumeClose();
                //проверяем направление сделки 
                if (SelectSecurBalans > 0) // лонг
                {
                    Direction = Side.Buy;
                    positionNew.Direction = Direction;
                    positionNew.State = PositionStateType.Closing;
                    positionNew.SecurityName = SelectedSecurity.Name;

                    PositionsBots.Add(positionNew);

                    _logger.Warning("Сreated a new long position and added PositionsBots  {Method}  {@positionNew}",
                                                                  nameof(MaintenanOpenVolume), positionNew);

                    SendCloseLimitOrderPosition(positionNew, VolumePerOrderClose);
                }
                if (SelectSecurBalans < 0) // шорт
                {
                    Direction = Side.Sell;
                    positionNew.Direction = Direction;
                    positionNew.State = PositionStateType.Closing;
                    positionNew.SecurityName = SelectedSecurity.Name;

                    PositionsBots.Insert(0, positionNew);

                    _logger.Warning("Сreated a new short position and added PositionsBots  {Method}  {@positionNew}",
                                                                nameof(MaintenanOpenVolume), positionNew);

                    SendCloseLimitOrderPosition(positionNew, VolumePerOrderClose);
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
                    ClearOrdersCancel (ref orders);
                    position.OpenOrders = orders;
                }                

                if (position.CloseOrders != null)
                {
                    List<Order> orders = new List<Order>();
                    orders  = position.CloseOrders; // положили ордера закрытия 
                    ClearOrdersCancel(ref orders);
                    position.CloseOrders = orders; // вернули ордера закрытия 
                }
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
            _logger.Information("Clear Orders cancel {Method}", nameof(ClearOrdersCancel));
            orders = newOrders;
        }

        /// <summary>
        /// Закрыть позицию
        /// </summary>
        private void StopPosition( Position position)
        {
            GetBalansSecur();

            CanselPositionActivOrders(position);

            if (position.OpenVolume != 0)
            {
                FinalCloseMarketOpenVolume(position, position.OpenVolume);
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
                _logger.Information("In Position volume {Volume} {side} {Metod} ", finalVolumClose, sideClose, nameof(FinalCloseMarketOpenVolume));
            }
            if (pos.Direction == Side.Sell)
            {
                sideClose = Side.Buy;
                _logger.Information("In Position volume {Volume} {side} {Metod} ", finalVolumClose, sideClose, nameof(FinalCloseMarketOpenVolume));
            }
            if (finalVolumClose == 0 || sideClose == Side.None )
            {
                SendStrStatus(" Ошибка закрытия объема на бирже");

                _logger.Error(" Error create Market orders to close " +
                    "the volume {finalVolumClose} {side} {Metod} "
                             , finalVolumClose, sideClose, nameof(FinalCloseMarketOpenVolume));
                return;
            }

            Order ordClose = CreateMarketOrder(SelectedSecurity, Price, finalVolumClose, sideClose);

            if (ordClose != null )
            {
                if (sideClose == Side.None) return;

                pos.AddNewCloseOrder(ordClose);
                Thread.Sleep(100);
                SendOrderExchange(ordClose);
                Thread.Sleep(100);

                _logger.Information("Sending FINAL Market order to close " +
                " {volume} {numberUser} {@Order} {Metod} ",
                 finalVolumClose, ordClose.NumberUser, ordClose, nameof(FinalCloseMarketOpenVolume));

                SendStrStatus(" Отправлен Маркет на закрытие объема на бирже");
            }
            if (ordClose == null)
            {
                SendStrStatus(" Ошибка закрытия объема на бирже");

                _logger.Error(" Error sending FINAL the Market to close the volume {@Order} {Metod} ", ordClose, nameof(FinalCloseMarketOpenVolume));
                
            }
        }

        /// <summary>
        /// отменяет все активные ордера во всех позициях
        /// </summary>
        private void CanselAllPositionActivOrders()
        {
            if(Server == null) return;
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
            }
        }

        /// <summary>
        /// отменяет активные ордера в позиции
        /// </summary>
        private void CanselPositionActivOrders(Position position)
        {
            if (Server == null) return;
            if (ActivOrders(position))
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
                        _logger.Information("Cancel Activ Orders {Method} Order {@Order} {Number} {NumberMarket} "
                                , nameof(CanselPositionActivOrders), ordersAll[i], ordersAll[i].NumberUser, ordersAll[i].NumberMarket);
                        SendStrStatus(" Отменили ордер на бирже");

                        //Thread.Sleep(50);
                    }
                }
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

            _logger.Information("StartStop = {IsRun} {Method}",  IsRun, nameof(StartStop));
            //RobotsWindowVM.Log(Header, " \n\n StartStop = " + IsRun);

            SaveParamsBot();
            if (IsRun)
            {
                // сейчас логика запускается в свойстве вкл/выкл
                if (PositionsBots.Count != 0)
                {
                    //foreach (PositionBot position in PositionsBots)
                    //{
                    //    position.PassOpenOrder = true;
                    //    position.PassCloseOrder = true;
                    //}
                }
            }
            else
            {
                Task.Run(() =>
                {
                    //while (ActivOrders() || MonitoringOpenVolumeExchange()) // пока есть открытые обемы и ордера на бирже
                    //{
                    //    //StopTradeLogic();
                    //    //Thread.Sleep(300);
                    //    break; // test
                    //}
                });
            }
        }

        /// <summary>
        ///  отправить ордер на биржу 
        /// </summary>
        private void SendOrderExchange(Order sendOpder) 
        {
            Server.ExecuteOrder(sendOpder);
            //Thread.Sleep(100);
            _logger.Information("Send order Exchange {Method} Order {@Order} {NumberUser} ", nameof(SendOrderExchange), sendOpder, sendOpder.NumberUser);

            SendStrStatus(" Ордер отправлен на биржу");
        }

        /// <summary>
        /// добавить открывающий ордер в позицию и на биржу 
        /// </summary>  
        private void SendOpenOrderPosition()
        {
            CalculateVolumeTradesOpen(); // расчет объема

            foreach (Position position in PositionsBots)
            {
                if (position.OpenOrders != null)
                {
                    if (position.OpenOrders.Count != 0) // защита от повтороных добалений ордеров
                    {
                        position.OpenOrders.Clear();
                    }
                }
                PriceOpenPos = CalculPriceStartPos(position.Direction); // расчет цены открытия позиции

                if (StartPriceOpenPos == 0 || BottomPositionPrice == 0 || PriceOpenPos.Count == 0)
                {
                    SendStrStatus(" BigСlusterPrice или BottomPositionPrice = 0 ");
                    return;
                }
                foreach (decimal price in PriceOpenPos)
                {
                    Order order = CreateLimitOrder(SelectedSecurity, price, VolumePerOrderOpen, position.Direction);
                    if (order != null)
                    {
                        //position.PassOpenOrder =false;
                        position.AddNewOpenOrder(order);// отправили ордер в позицию
                        SendOrderExchange(order); // отправили ордер на биржу
                        //Thread.Sleep(50);
                        _logger.Information("Send Open order into position {Method} {@Order} {NumberUser}", nameof(SendOrderExchange), order, order.NumberUser);
                    }
                    else
                    {
                        //todo: разобраться с разрешениями на отправку ордеров
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
            if (position.OpenOrders != null )
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
        private void MaintainingVolumeBalance()
        {
            decimal minVolumeExecut = SelectedSecurity.MinTradeAmount;

            if (PositionsBots.Count == 0) MaintenanOpenVolume();

            foreach (Position position in PositionsBots) // заходим в позицию
            {
                GetVolumeOpen(position);

                VolumeRobExecut = position.OpenVolume;
                decimal volumOrderClose = 0; // по ордерам закрытия объем
                if (position.CloseOrders != null && position.MyTrades.Count > 0)
                {
                    for (int i = 0;i < position.CloseOrders.Count;i++)
                    {
                        volumOrderClose += position.CloseOrders[i].Volume;
                    }
                }

                //  на бирже открытый обем больше обема ордеров закрытия
                // значит где-то ошибка или купили помимо робота
                // доставить лимитку закрытия

                if (SelectSecurBalans > volumOrderClose)
                {
                    GetBalansSecur();

                    _logger.Warning(" Volume on the stock exchange > Volum order close  {Method}  {vol} {@Position} ",
                                                     nameof(MaintainingVolumeBalance));
                    if (IsChekMonitor)
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
                        IsChekMonitor = false;
                    }
                }
                if (!MonitoringOpenVolumeExchange() && !ActivOrders(position)) // если нету робот не работает
                {
                    if (IsRun) // нафига он включен
                    {
                        _logger.Warning(" there is no open volume and active orders on the exchange, the robot is turned off  {Method} {SelectSecurBalans} {@position} ",
                          nameof(MaintainingVolumeBalance), SelectSecurBalans, position );
                        SendStrStatus("Робот выключен");
                        IsRun = false; // выключаем
                        ClearCanceledOrderPosition();
                    }                
                } ;
            }
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

            foreach (Position position in PositionsBots) // заходим в позицию
            {
                if (position.Direction == Side.None)
                {
                    position.Direction = (Side)Direction;
                }
                VolumeRobExecut = position.OpenVolume;

                decimal volumeInTradesOpenOrd = 0; // по терйдам откр объем
                if (position.OpenOrders != null && position.MyTrades.Count > 0)
                {
                    for (int i = 0; i < position.OpenOrders.Count; i++)
                    {
                        if (position.OpenOrders[i].MyTrades == null) continue;

                        for (int j = 0; j < position.OpenOrders[i].MyTrades.Count; j++)
                        {
                            if (position.OpenOrders[i].MyTrades[j] == null) continue;

                            volumeInTradesOpenOrd += position.OpenOrders[i].MyTrades[j].Volume;
                        }
                    }
                }
                decimal volumInOrderClose = 0; // по ордерам закрытия объем
                if (position.CloseOrders != null && position.MyTrades.Count > 0)
                {
                    for (int i = 0; i < position.CloseOrders.Count; i++)
                    {
                         volumInOrderClose += position.CloseOrders[i].Volume;
                    }
                }
                if (volumeInTradesOpenOrd > volumInOrderClose )
                {
                    _logger.Warning(" Open Volume > Volume Close orders  {Method}", nameof(AddCloseOrder));

                    if (IsChekVolumeClose) // разрешено добавить включать руками
                    {
                        decimal vol = Decimal.Round(volumeInTradesOpenOrd - volumInOrderClose, SelectedSecurity.DecimalsVolume);
                        if (vol < minVolumeExecut)
                        {
                            _logger.Error(" Open VolExecut - Close Vol <  MinTradeAmount {Method}", nameof(AddCloseOrder));
                            return;
                        }
                        _logger.Warning("Open Volume is larger than the closing orders {Method} {openVolExecut} {activCloseVol} {@position} {volum}",
                              nameof(AddCloseOrder), volumeInTradesOpenOrd, volumInOrderClose, position, vol);

                        SendCloseLimitOrderPosition(position, vol); // выставили ордер

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
            _logger.Information("ChekTradePosition {@Trade} {NumberTrade} {NumberOrderParent} {volume} {Method}"
                                     , myTrade, myTrade.NumberTrade, myTrade.NumberOrderParent, volume, nameof(GetOpenVolume));
            return volume;
        }

        /// <summary>
        /// выставить ордер закрытия
        /// </summary>
        private void SendCloseOrder(Position position , decimal volume)
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
                                                       " {Method} Order {Volume} {OpenVolumePosition} "
                                                 , nameof(SendCloseOrder), volumeOpen, position.OpenVolume);
                                return;

                            }
                            else
                            {
                                _logger.Error("Volum close < minVolumeExecut {Method}  {volumeOpen} {@Position} "
                                                             , nameof(SendCloseOrder), volumeOpen, position);
                                MaintainingVolumeBalance();
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
            PriceClosePos = null;
            PriceClosePos = CalculPriceClosePos(Direction); // расчет цен закрытия позиции

            //объем пришел сверху
            decimal priceClose = 0;
            //выбираем цену закрытия
            if (position.CloseOrders == null )
            {
                priceClose = PriceClosePos[0];
            }
            else
            {
                int i = position.CloseOrders.Count;
                priceClose = PriceClosePos[i];
            }
            Debug.WriteLine("цена на закрытие= " + priceClose);

            if (priceClose == 0)
            {
                Debug.WriteLine("цена на закрытие= " + priceClose);
                //Debug.WriteLine(" OrdersForClose.Count == 0)" );
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
                //Thread.Sleep(100);
                _logger.Information("Send Limit order for Close {Method} {priceClose} {volumeClose} {@Order} {NumberUser}",
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
            if (PositionsBots!= null)
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

            if (VolumePerOrderOpen != 0  && IsRun == true) // формируем позиции
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
            if(SelectedSecurity == null) return false;
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
            if (IsRun == false || SelectedSecurity == null) return;

            if (position.Direction == Side.Buy && position.OpenVolume != 0) // если есть открытый объем в лонг
            {
                if (IsChekTraelStopLong == true && position.Direction == Side.Buy)
                {
                    decimal stepStop = 0;
                    decimal priceStop = 0;
                    stepStop = StepPersentStopLong * Price / 100;
                    stepStop = Decimal.Round(stepStop, SelectedSecurity.Decimals);
                    decimal entry = position.EntryPrice;
                    //priceStop = entry - stepStop; //расчетная чена стопа 
                    if (Price > PriceStopLong + stepStop)
                    {
                        if (entry == 0) return;
                        decimal p = Price - stepStop;
                        PriceStopLong = Decimal.Round(p, SelectedSecurity.Decimals);
                    }
                }
            }
            if (position.Direction == Side.Sell && position.OpenVolume != 0)
            {
                decimal stepStop = 0;
                //decimal priceStop = 0;
                stepStop = StepPersentStopShort * Price / 100;
                stepStop = Decimal.Round(stepStop, SelectedSecurity.Decimals);
                decimal entry = position.EntryPrice;
                if (IsChekTraelStopShort && position.Direction == Side.Sell)
                {
                    if (entry == 0) return;

                    if (PriceStopShort == 0)
                    {
                        PriceStopShort = Price + stepStop;
                    }
                    if (Price < PriceStopShort - stepStop)
                    {
                        decimal p = Price - stepStop;
                        PriceStopShort = Decimal.Round(p, SelectedSecurity.Decimals);
                    }
                }
            }
        }

        /// <summary>
        /// расчет объема на ордер открытия
        /// </summary>
        private void CalculateVolumeTradesOpen()
        {
            if (SelectedSecurity == null || Price==0 || PartsPerInput==0) return;
            GetBalansSecur();
            VolumePerOrderOpen = 0;
            decimal workLot = 0;
            decimal baks = 0;
            baks = FullPositionVolume/ PartsPerInput; // это в баксах
            decimal moni = baks / Price; // в монете
            workLot = Decimal.Round(moni, SelectedSecurity.DecimalsVolume);
            decimal minVolume = SelectedSecurity.MinTradeAmount;
            if (workLot < minVolume)
            {
                SendStrStatus("Объем ордера меньше допустимого");
                _logger.Error(" Order volume < minVolume {Method}  {workLot}", nameof(CalculateVolumeTradesOpen), workLot);
                // IsRun = false;
            }
            else
            {
                VolumePerOrderOpen = workLot;
            }
        }

        /// <summary>
        /// расчет объема на ордер закрытия
        /// </summary>
        private void CalculateVolumeTradesClose()
        {
            if (SelectedSecurity == null || Price == 0 || PartsPerExit == 0) return;
            GetBalansSecur();
            VolumePerOrderClose = 0;
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
            }
            else
            {
                VolumePerOrderClose = workLot;
            }
        }

        /// <summary>
        /// расчет обема закрытия по монетам на бирже (части)
        /// </summary>
        private void CalculateVolumeClose()
        {
            if (SelectedSecurity == null || Price == 0 || PartsPerExit == 0) return;
            GetBalansSecur();
            VolumePerOrderClose = 0;
            decimal workLot = 0;
            decimal baks = 0;
            baks = SelectSecurBalans / PartsPerExit; // это в баксах
            decimal moni = baks / Price; // в монете

            workLot = Decimal.Round(moni, SelectedSecurity.DecimalsVolume);
            decimal minVolume = SelectedSecurity.MinTradeAmount;
            if (workLot < minVolume)
            {
                SendStrStatus("Объем ордера меньше допустимого");
                _logger.Error(" Order volume < minVolume {Method}  {workLot}", nameof(CalculateVolumeClose), workLot);
                // IsRun = false;
                if (SelectSecurBalans != 0)
                {
                    VolumePerOrderClose = SelectSecurBalans;
                    _logger.Error(" VolumePerOrderClose = SelectSecurBalans; {Method}  {SelectSecurBalans}",
                                                            nameof(CalculateVolumeClose), SelectSecurBalans);
                }
            }
            else
            {
                VolumePerOrderClose = workLot;
                _logger.Information(" VolumePerOrderClose = workLot; {Method}  {workLot}",
                                          nameof(CalculateVolumeClose), workLot);
            }
        }

        /// <summary>
        /// расчитать стартовую цену (начала открытия позиции)
        /// </summary>
        private  List<decimal> CalculPriceStartPos(Side side)
        {
            _priceOpenPos.Clear();

            if (SelectedSecurity == null)
            {
                SendStrStatus(" еще нет бумаги ");
                
                return _priceOpenPos;
            }
            decimal stepPrice = 0;
            decimal price =0;

            if (side == Side.Buy )
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
            return  _priceOpenPos;
        }

        /// <summary>
        /// расчитать цены закрытия позиции
        /// </summary>
        private  List<decimal> CalculPriceClosePos(Side side)
        {
            _priceClosePos = new List<decimal>();

            if (SelectedSecurity == null)
            {
                SendStrStatus(" еще нет бумаги ");

                return _priceClosePos;
            }
            decimal stepPrice = 0;
            decimal price = 0;

            if (side ==Side.None) 
            {
                side = Direction; 
            }
            if (side == Side.Buy)
            {
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
            foreach (Position position in PositionsBots)
            {
                position.SetOrder(checkOrder); // проверяем и обновляем ордер
                // TODO:  придумать проверку и изменяем статуса позиций
                _logger.Information("CheckMyOrder {@order}{OrdNumberUser} {NumberMarket}{Method}",
                                      checkOrder, checkOrder.NumberUser, checkOrder.NumberMarket, nameof(CheckMyOrder));
            }
        }

        /// <summary>
        /// проверка и обновление трейдами ордеров 
        /// </summary>
        private void ChekTradePosition(MyTrade newTrade)
        {
            GetBalansSecur();

            foreach (Position position in PositionsBots)
            {
                VolumeRobExecut = position.OpenVolume;

                position.SetTrade(newTrade);
                _logger.Information("Chek Trade Position {@Trade} {NumberTrade} {NumberOrderParent} {Method}"
                                     , newTrade, newTrade.NumberTrade, newTrade.NumberOrderParent, nameof(ChekTradePosition));

                if (newTrade.SecurityNameCode == SelectedSecurity.Name)
                {
                    if (OpenTrade(newTrade))
                    {
                        decimal vol = GetOpenVolume(newTrade);
                        if (vol != 0) 
                        {
                            SendCloseOrder(position, vol);
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
            
            _logger.Information("Create Limit Order {Method} {@Order} {Number} ", nameof(CreateLimitOrder), order , order.NumberUser);
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
                Price = ask;
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
            if (myTrade.SecurityNameCode == SelectedSecurity.Name)
            {
                ChekTradePosition(myTrade);
               
                _logger.Warning(" Come myTrade {Method} {NumberOrderParent} {@myTrade}", nameof(_server_NewMyTradeEvent),myTrade.NumberOrderParent , myTrade);

                GetBalansSecur();
                IsOnTralProfit(myTrade);
            }
            else                 
            _logger.Warning(" Levak ! Secur Trade {@Trade} {Security} {Method}", myTrade, myTrade.SecurityNameCode, nameof(_server_NewOrderIncomeEvent));
        }  

        /// <summary>
        ///  пришел ордер с сервера
        /// </summary>
        private void _server_NewOrderIncomeEvent(Order order)
        {
            if (SelectedSecurity != null)
            {
                if (order.SecurityNameCode == SelectedSecurity.Name)
                {
                    CheckMyOrder(order);
                    // AddOrderPosition(order);

                    if (order.State != OrderStateType.Fail)
                    {
                        SaveParamsBot();
                    }
                    GetBalansSecur();
                    _logger.Information(" New myOrder {@Order} {NumberUser} {NumberMarket} {Method}",
                                         order, order.NumberUser, order.NumberMarket, nameof(_server_NewOrderIncomeEvent));
                }
                else
                    _logger.Information(" Levak ! Secur {@Order} {Security} {Method}",
                        order, order.SecurityNameCode,nameof(_server_NewOrderIncomeEvent));
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
            if (trades != null && trades[0].SecurityNameCode == SelectedSecurity.Name)
            {
                Trade trade = trades[0];

                //Price = trade.Price;

                if (trade.Time.Second % 3 == 0)
                {
                    MonitoringStop();
                }
                if (trade.Time.Second % 11== 0)
                {
                    MaintainingVolumeBalance();
                    AddCloseOrder();
                }
                if (trade.Time.Second % 5 == 0) GetBalansSecur();
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

            _logger.Warning(" Disnnecting to server = {ServerType} {Method} ", _server.ServerType, nameof(UnSubscribeToServer));
            //RobotsWindowVM.Log(Header, " Отключились от сервера = " + _server.ServerType);
        }

        #endregion

        #region   сервисные методы ===========================

        /// <summary>
        /// проверяем стопы
        /// </summary>
        private void MonitoringStop()
        {
            if (PositionsBots == null) return;
            if (PositionsBots.Count != 0)
            {
                if (Price != 0) // если н0ль - стоп отключен
                {
                    foreach (var pos in PositionsBots)
                    {
                        if (pos.Direction == Side.Buy)
                        {
                            if (IsChekTraelStopLong == true) CalculateTrelingStop(pos);
                        }
                    }

                    if (Price < PriceStopLong && Direction == Side.Buy) //|| Price < PriceStopLong && Direction == Direction.BUYSELL
                    {

                        foreach (var pos in PositionsBots)
                        {
                            if (pos.Direction == Side.Buy && pos.OpenVolume > 0)
                            {
                                StopPosition(pos);
                                _logger.Warning(" Triggered Stop Long Position {@Position}  {Method}",
                                                                       pos, nameof(MonitoringStop));

                                if (pos.State == PositionStateType.Done)// отключаем стоп т.к. позиция уже закрыта
                                {
                                    PriceStopLong = 0;
                                }
                            }
                        }
                    }
                }

                if (Price != 0 && PriceStopShort != 0)
                {
                    foreach (var pos in PositionsBots)
                    {
                        if (pos.Direction == Side.Sell)
                        {
                            if (IsChekTraelStopShort == true) CalculateTrelingStop(pos);
                        }
                    }

                    if (Price > PriceStopShort && Direction == Side.Sell) // || ice > PriceStopShort && Direction == Direction.BUYSELL
                    {
                        foreach (var pos in PositionsBots)
                        {
                            if (pos.Direction == Side.Sell && pos.OpenVolume != 0)
                            {
                                StopPosition(pos);
                                _logger.Warning(" Triggered Stop Short Position {@Position}  {Method}",
                                                                            pos, nameof(MonitoringStop));

                                if (pos.State == PositionStateType.Done)// отключаем стоп т.к. позиция уже закрыта
                                {
                                    PriceStopShort = 0;
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
                            _logger.Information(" SelectSecurBalans = {SelectSecurBalans} {Method} "
                                                           , SelectSecurBalans, nameof(GetBalansSecur));
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
            if (PositionsBots !=null && PositionsBots.Count > 0)
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
                            _logger.Information(" GetOrdersState and ResearchTradesToOrders {@orders}{Method} ",
                                                                                             orders, nameof(CheckMyOrder));
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
                e.PropertyName == "Direction"||
                e.PropertyName == "StepPersentStopLong" ||
                e.PropertyName == "IsChekTraelStopLong" ||

                e.PropertyName == "StepPersentStopShort" ||
                e.PropertyName == "ActionPosition" ||
                //e.PropertyName == "PriceStopShort" || 
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

                    writer.WriteLine(ActionPosition); // 21

                    writer.Close();

                    _logger.Information("Saving parameters {Header} {Method} ", Header , nameof(SaveParamsBot));
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
                        ActionPosition = action;
                    }

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

            _logger.Information(" Dispose {Method}", nameof(Dispose));
        }

        #endregion

        #endregion end metods==============================================

        #region  ЗАГОТОВКИ ==============================================

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

        #endregion end Commands ====================================================
    }
}
