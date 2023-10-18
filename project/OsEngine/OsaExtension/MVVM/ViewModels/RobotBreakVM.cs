using MahApps.Metro.Controls;
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
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Forms.DataVisualization.Charting;
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
        private decimal _price;

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
        /// поле логера RobotBreakVM
        /// </summary>
        ILogger _logger;

        /// <summary>
        /// исполняемая позиция
        /// </summary>
        static PositionBot positionRun;

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
        /// метод отработки кнопки стоп
        /// </summary>
        private void StopTradeLogic()
        {
            GetBalansSecur();
            foreach (PositionBot pos in PositionsBots)
            {
                CanselActivOrders();
            }
            if (SelectSecurBalans !=0)
            {
                FinalCloseMarketOpenVolume();
            }
        }

        /// <summary>
        /// продать по меркету все монеты на бирже
        /// </summary>
        private void FinalCloseMarketOpenVolume()
        {
            if (SelectSecurBalans == 0)
            {
                _logger.Error(" SelectSecurBalans == 0 , exit metod {Metod} ",  nameof(FinalCloseMarketOpenVolume));
                return;
            } 

            GetBalansSecur();  // берем открытый объем на бирже 
            decimal finalVolumClose = 0;
            finalVolumClose = SelectSecurBalans;
           
            Side side = Side.None;
            if (finalVolumClose < 0)
            {
                side = Side.Buy;
                _logger.Information("In Position volume {Volume} {side} {Metod} ", SelectSecurBalans, side, nameof(FinalCloseMarketOpenVolume));
            }
            if (finalVolumClose > 0)
            {
                side = Side.Sell;
                _logger.Information("In Position volume {Volume} {side} {Metod} ", SelectSecurBalans, side, nameof(FinalCloseMarketOpenVolume));
            }
            if (finalVolumClose == 0 || side == Side.None )
            {
                SendStrStatus(" Ошибка закрытия объема на бирже");

                _logger.Error(" Error create Market orders to close " +
                    "the volume {finalVolumClose} {side} {Metod} "
                             , finalVolumClose, side, nameof(FinalCloseMarketOpenVolume));
                return;
            }

            Order ordClose = CreateMarketOrder(SelectedSecurity, Price, SelectSecurBalans, side);

            if (ordClose != null && SelectSecurBalans != 0)
            {
                GetBalansSecur();

                if (SelectSecurBalans == 0 || side == Side.None) return;

                bool activ = false;
                activ = ActivOrders();
                if (!activ)
                {
                    SendOrderExchange(ordClose);
                    Thread.Sleep(100);

                    _logger.Information("Sending Market order to close " +
                    " {volume} {numberUser} {@Order} {Metod} ",
                     finalVolumClose, ordClose.NumberUser, ordClose, nameof(FinalCloseMarketOpenVolume));

                    SendStrStatus(" Отправлен Маркет на закрытие объема на бирже");

                    return;
                }
            }
            if (ordClose == null)
            {
                SendStrStatus(" Ошибка закрытия объема на бирже");

                _logger.Error(" Error sending the Market to close the volume {@Order} {Metod} ", ordClose, nameof(FinalCloseMarketOpenVolume));
                
            }
        }

        /// <summary>
        /// отменяет активные ордера 
        /// </summary>
        private void CanselActivOrders()
        {
            bool activ = ActivOrders();  //TODO: разобраться c перепроверкой отмены
            foreach (PositionBot position in PositionsBots)
            {
                List<Order> ordersAll = new List<Order>();
                ordersAll = position.OrdersForOpen;// взять из позиции ордера открытия 
                ordersAll.AddRange(position.OrdersForClose); // добавили ордера закрытия 

                for (int i = 0; i < ordersAll.Count; i++)
                {
                    if (ordersAll[i].State == OrderStateType.Activ && activ)
                    {
                        Server.CancelOrder(ordersAll[i]);
                        _logger.Information("Cancel Activ Orders {Method} Order {@Order} {Number} {NumberMarket} "
                                , nameof(CanselActivOrders), ordersAll[i], ordersAll[i].NumberUser , ordersAll[i].NumberMarket);
                        SendStrStatus(" Отменили ордер на бирже");
                        activ = ActivOrders();
                        //Thread.Sleep(50);
                    }
                }
            }
        }

        /// <summary>
        /// есть активные ордера
        /// </summary>
        private bool ActivOrders()
        {
            bool res = false;
            GetBalansSecur();
            foreach (PositionBot position in PositionsBots)
            {
                List<Order> ordersAll= new List<Order>();
                ordersAll = position.OrdersForOpen;// взять из позиции ордера открытия 
                ordersAll.AddRange(position.OrdersForClose); // добавили ордера закрытия 
                
                for(int i = 0; i < ordersAll.Count; i++)
                {
                    if (ordersAll[i].State == OrderStateType.Activ)
                    {
                        res = true;
                    }
                }
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

            _logger.Information("StartStop = {@IsRun} {Method}",  IsRun, nameof(StartStop));
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
                    while (ActivOrders() || MonitoringOpenVolumeExchange()) // пока есть открытые обемы и ордера на бирже
                    {
                        //StopTradeLogic();
                        //Thread.Sleep(300);
                        break; // test
                    }
                });
            }
        }

        /// <summary>
        ///  отправить ордер на биржу 
        /// </summary>
        private void SendOrderExchange(Order sendOpder) 
        {
            Server.ExecuteOrder(sendOpder);
            Thread.Sleep(100);
            _logger.Information("Send order Exchange {Method} Order {@Order} {NumberUser} ", nameof(SendOrderExchange), sendOpder, sendOpder.NumberUser);

            SendStrStatus(" Ордер отправлен на биржу");
        }

        /// <summary>
        /// добавить открывающий ордер в позицию и на биржу 
        /// </summary>  
        private void SendOpenOrderPosition()
        {
            CalculateVolumeTradesOpen(); // расчет объема

            foreach (PositionBot position in PositionsBots)
            {
                PriceOpenPos = CalculPriceStartPos(position.Side); // расчет цены открытия позиции

                if (BigСlusterPrice == 0 || BottomPositionPrice == 0 || PriceOpenPos.Count == 0)
                {
                    SendStrStatus(" BigСlusterPrice или BottomPositionPrice = 0 ");
                    return;
                }
                foreach (decimal price in PriceOpenPos)
                {
                    Order order = CreateLimitOrder(SelectedSecurity, price, VolumePerOrderOpen, position.Side);
                    if (order != null)
                    {
                        //position.PassOpenOrder =false;
                        position.OrdersForOpen.Add(order);// отправили ордер в позицию
                        SendOrderExchange(order); // отправили ордер на биржу
                        Thread.Sleep(50);
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
        /// выставить ордер закрытия
        /// </summary>
        private void SendCloseOrder(MyTrade myTrade)
        {
            foreach (PositionBot position in PositionsBots) // заходим в позицию
            {
                // проверяем откуда трейд
                if (position.OrdersForOpen != null) // если открывающий выствить тейк
                {
                    /* для этого
                     *  берем открытую позицию
                     *  ищем актиыные ордера на закрытия если их нет
                     *  берем открытый обем позиции
                     *   создаем оредр на его закрытие и отрпавляем его в позицию
                     */

                    for (int i = 0; i < position.OrdersForOpen.Count; i++) 
                    {
                        Order curOrdOpen = position.OrdersForOpen[i];

                        if (curOrdOpen.NumberMarket == myTrade.NumberOrderParent) // принадлежит ордеру открытия
                        {
                            // значит трейд открывающий

                            decimal volumeClose = 0;
                            for (int s = 0; s < position.OrdersForClose.Count; s++)
                            {
                                Order currOrdOpen = position.OrdersForClose[i];

                                if (currOrdOpen.State == OrderStateType.Activ)
                                {
                                    volumeClose += position.OrdersForClose[s].Volume;
                                }
                                //if (currOrdOpen.State != OrderStateType.Activ ||
                                //    currOrdOpen.State != OrderStateType.Patrial ||
                                //    currOrdOpen.State != OrderStateType.Pending)
                                //{
                                //    //
                                //}
                            }
                            // проверяем в ордерах закрытия объема меньше чем открыто на бирже
                            if (Math.Abs(volumeClose) < Math.Abs(position.OpenVolume))
                            {
                                _logger.Information("Called metod  SendCloseLimitOrderPosition" +
                                            " {Method} Order {VolumeForClose} {OpenVolumePosition} "
                                    , nameof(SendCloseOrder), volumeClose, position.OpenVolume);
                                // добавить лимит ордер на закрытие)
                                SendCloseLimitOrderPosition(position , myTrade.Volume);
                            }

                            //if (position.Status == PositionStatus.OPENING // позиция открыта
                            //    && curOrdOpen.State == OrderStateType.Done) // ордер исполнен
                            //{
  
                            //    if (position.OrdersForClose != null && position.OrdersForClose.Count == 0)
                            //    {
                            //        // добавить лимит ордер на закрытие)
                            //        //SendCloseOrderPosition(myTrade.Volume);
                            //    }
                            //    // ищем актиыные ордера на закрытия
                            //    if (position.OrdersForClose != null && position.OrdersForClose.Count > 0)
                            //    {

                            //    }
                            //}
                        }
                    }
                }
                if (position.OrdersForClose != null) // если закрывающий 
                {
                    /*
                    for (int i = 0; i < position.OrdersForClose.Count; i++)
                    {
                        Order curOrdOpen = position.OrdersForClose[i];

                        if (curOrdOpen.NumberMarket == myTrade.NumberOrderParent // принадлежит ордеру закрытия
                            && curOrdOpen.SecurityNameCode == myTrade.SecurityNameCode // наша бумага
                            && curOrdOpen.Volume == myTrade.Volume) // совпадает объем
                        {
                            //проверяем обем позиции, если весь закрыт выключаем
                            if (position.OpenVolume == 0)
                            {
                                position.PassOpenOrder = false;
                                position.PassCloseOrder = false;
                                position.Status = PositionStatus.DONE;
                                SendStrStatus(" Позиция закрылась по профиту");
                                _logger.Information("Position close, STOP open {Method} {@Order} {NumberUser}", 
                                                                  nameof(SendCloseOrder), curOrdOpen, curOrdOpen.NumberUser);
                            }
                        }
                    }*/
                }
            }
        }

        /// <summary>
        /// добавить закрывающий ордер в позицию и на биржу
        /// </summary>  
        private void SendCloseLimitOrderPosition(PositionBot position, decimal volumeClose)
        {
            // базовая проверка
            if (BigСlusterPrice == 0 || BottomPositionPrice == 0 || PriceOpenPos.Count == 0)
            {
                SendStrStatus(" BigСlusterPrice или BottomPositionPrice = 0 ");
                return;
            }

            PriceClosePos = null;
            PriceClosePos = CalculPriceClosePos(position.Side); // расчет цен закрытия позиции

            //объем пришел сверху
            decimal priceClose = 0;
            //выбираем цену закрытия
            int i = position.OrdersForClose.Count;
            priceClose = PriceClosePos[i];
            Debug.WriteLine("цена на закрытие= " + priceClose);

            if (priceClose == 0)
            {
                Debug.WriteLine("цена на закрытие= " + priceClose);
                //Debug.WriteLine(" OrdersForClose.Count == 0)" );
                return;
            }
            // ордер на закрытие в обратную стороны открытия сделки
            Side sideSet = Side.None;
            if (position.Side == Side.Buy)
            {
                sideSet = Side.Sell;
            }
            if (position.Side == Side.Sell)
            {
                sideSet = Side.Buy;
            }

            Order order = CreateLimitOrder(SelectedSecurity, priceClose, volumeClose, sideSet);

            if (order != null && sideSet != Side.None && priceClose != 0 && volumeClose != 0)
            {  // отправить ордер в позицию
                position.OrdersForClose.Add(order);
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
            if (BigСlusterPrice == 0)
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
    
            PositionBot positionBuy = new PositionBot() { Side = Side.Buy };
            PositionBot positionSell = new PositionBot() { Side = Side.Sell };
            
            CalculateVolumeTradesOpen();            

            if (VolumePerOrderOpen != 0  && IsRun == true) // формируем позиции
            {
                PositionsBots.Clear();

                if (Direction == Direction.BUY || Direction == Direction.BUYSELL)
                {
                    positionBuy.Status = PositionStatus.NONE;
                    positionBuy.SecurityName = SelectedSecurity.Name;
                    
                    //AddOpderPosition(positionBuy);
                    PositionsBots.Add(positionBuy);
                }
                if (Direction == Direction.SELL || Direction == Direction.BUYSELL)
                {
                    positionSell.Status = PositionStatus.NONE;                    
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

        ///<summary>
        /// взять текущий объем на бирже выбаной  бумаги
        /// </summary>
        private void GetBalansSecur()
        {
            if (SelectedSecurity == null)return;
            //RobotsWindowVM.Log(Header, " Запущен GetBalansSecur");
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
                stepPrice = (BigСlusterPrice - BottomPositionPrice) / PartsPerInput;
                price = BigСlusterPrice - stepPrice;
                for (int i = 0; i < PartsPerInput; i++)
                {
                    price = Decimal.Round(price, SelectedSecurity.Decimals);
                    _priceOpenPos.Add(price);
                    price = price - stepPrice;
                }
            }
            if (side == Side.Sell)
            {
                stepPrice = (TopPositionPrice - BigСlusterPrice) / PartsPerInput;
                price = BigСlusterPrice + stepPrice;
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

            if (side == Side.Buy)
            {
                stepPrice = (TakePriceLong - TopPositionPrice) / PartsPerExit;
                price = TopPositionPrice + stepPrice;
                for (int i = 0; i < PartsPerExit; i++)
                {
                    price = Decimal.Round(price, SelectedSecurity.Decimals);
                    _priceClosePos.Add(price);
                    price = price + stepPrice;
                }
            }
            if (side == Side.Sell)
            {
                stepPrice = (BottomPositionPrice - TakePriceShort) / PartsPerExit;
                price = BottomPositionPrice - stepPrice;
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
            foreach (PositionBot position in PositionsBots)
            {
                bool newOrderBool = position.NewOrder(checkOrder); // проверяем и обновляем ордер
                position.MonitoringStatusPos(checkOrder); // TODO:  доделать проверку и изменяем статуса позиций
            }
        }

        /// <summary>
        /// проверка и обновление трейдами ордеров 
        /// </summary>
        private void ChekEditOrderPosition(MyTrade newTrade)
        {
            foreach (PositionBot position in PositionsBots)
            {
                position.SetTradeInOrder(newTrade);
            }
        }

        /// <summary>
        /// Изсменение статуса позиции
        /// </summary>
        public void MonitiringStatusBot(Order order)
        {
            foreach (PositionBot position in PositionsBots)
            {
                for (int i = 0; i < position.OrdersForOpen.Count; i++)
                {
                    if (position.OrdersForOpen[i].State == OrderStateType.Activ)
                    {
                        ActionBot = ActionBot.OpeningPos;
                    }
                    if (position.OrdersForOpen[i].State == OrderStateType.Cancel)
                    {
                        ActionBot = ActionBot.Stop;
                        position.PassOpenOrder = true;
                        IsRun = false;
                    }
                    if (position.OrdersForOpen[i].State == OrderStateType.Done)
                    {
                        //ActionBot = ActionBot.Open;
                    }
                }

                for (int i = 0; i < position.OrdersForClose.Count; i++)
                {
                    if (position.OrdersForClose[i].State == OrderStateType.Activ)
                    {
                        ActionBot = ActionBot.ClosingPos;
                    }
                    if (position.OrdersForClose[i].State == OrderStateType.Cancel)
                    {
                        ActionBot = ActionBot.Stop;
                        position.PassOpenOrder = true;
                        IsRun = false;
                    }
                    if (position.OrdersForClose[i].State == OrderStateType.Done)
                    {
                        //ActionBot = ActionBot.Stop;
                    }
                }
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
            if (myTrade.SecurityNameCode == SelectedSecurity.Name)
            {
                ChekEditOrderPosition(myTrade);
                _logger.Information(" Come myTrade {Method} {NumberOrderParent} {@myTrade}", nameof(_server_NewMyTradeEvent),myTrade.NumberOrderParent , myTrade);

                GetBalansSecur();
                // если открылась сделка выставить тейк
                SendCloseOrder(myTrade);
            }
            else                 
            _logger.Information(" Levak ! Secur Trade {@Trade} {Security} {Method}", myTrade, myTrade.SecurityNameCode, nameof(_server_NewOrderIncomeEvent));
        }

        /// <summary>
        /// мой ордер с сервера
        /// </summary>
        private void _server_NewOrderIncomeEvent(Order myOrder)
        {
            if (myOrder.SecurityNameCode == SelectedSecurity.Name)
            {
                ActivOrders();
                CheckMyOrder(myOrder);
            
                MonitiringStatusBot(myOrder);
                GetBalansSecur();
                _logger.Information(" New myOrder {@Order} {NumberUser} {NumberMarket} {Method}", myOrder, myOrder.NumberUser, myOrder.NumberMarket, nameof(_server_NewOrderIncomeEvent));
            }
            else
            _logger.Information(" Levak ! Secur Order {Security} {Method}", myOrder, myOrder.SecurityNameCode, nameof(_server_NewOrderIncomeEvent));
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
                Trade trade = trades.Last();

                Price = trade.Price;

                if (trade.Time.Second % 2 == 0)
                {
                    GetBalansSecur();
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
            _server.PortfoliosChangeEvent += _server_PortfoliosChangeEvent;
            //_server.NewBidAscIncomeEvent += _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent += _server_ConnectStatusChangeEvent;

            _logger.Information(" Connecting to the server = {ServerType} {Method} ", _server.ServerType, nameof(SubscribeToServer));
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
            //_server.NewBidAscIncomeEvent -= _server_NewBidAscIncomeEvent;
            _server.ConnectStatusChangeEvent -= _server_ConnectStatusChangeEvent;

            _logger.Information(" Disnnecting to server = {ServerType} {Method} ", _server.ServerType, nameof(UnSubscribeToServer));
            //RobotsWindowVM.Log(Header, " Отключились от сервера = " + _server.ServerType);
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
                        //RobotsWindowVM.Log(Header, "StartSecuritiy  security = " + series.Security.Name);
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
                e.PropertyName == "Direction")
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
                    writer.WriteLine(StringPortfolio);

                    writer.WriteLine(TakePriceLong);
                    writer.WriteLine(TopPositionPrice);
                    writer.WriteLine(BigСlusterPrice);
                    writer.WriteLine(Direction);

                    writer.WriteLine(BottomPositionPrice);
                    writer.WriteLine(TakePriceShort);

                    writer.WriteLine(FullPositionVolume);

                    writer.WriteLine(PartsPerInput);
                    writer.WriteLine(PartsPerExit);

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
                    BigСlusterPrice = GetDecimalForString(reader.ReadLine());

                    Direction direct = Direction.BUY;
                    if (Enum.TryParse(reader.ReadLine(), out direct))
                    {
                        Direction = direct;
                    }

                    BottomPositionPrice = GetDecimalForString(reader.ReadLine());
                    TakePriceShort = GetDecimalForString(reader.ReadLine());
                    FullPositionVolume = GetDecimalForString(reader.ReadLine());
                    PartsPerInput = (int)GetDecimalForString(reader.ReadLine());
                    PartsPerExit = (int)GetDecimalForString(reader.ReadLine());

                    //StepType step = StepType.PUNKT;
                    //if (Enum.TryParse(reader.ReadLine(), out step))
                    //{
                    //    StepType = step;
                    //}

                    //Levels = JsonConvert.DeserializeAnonymousType(reader.ReadLine(), new ObservableCollection<Level>());

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
