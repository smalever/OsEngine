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

        private void TradeLogic()
        {
            if (IsRun)
            {
                CreateNewPosition(); // создали позиции
                AddOpderPosition();
                SendOrderExchange(); // отправили ордер на биржу
            }
             // надо тестить закрытие открытого обема 
        }

        /// <summary>
        /// метод отработки кнопки стоп
        /// </summary>
        private void StopTradeLogic()
        {
            CanсelActivOrders(); // отменили ордера

            CloseMarketOpenVolume(); // закрыли объем по маркет

            //PositionsBots.Clear();
            /*       
             * изменить разрешения 
             */
        }

        /// <summary>
        /// закрыть оп меркету открытый объем
        /// </summary>
        private void CloseMarketOpenVolume()
        {
                while (SelectSecurBalans !=0)
                {
                    foreach (PositionBot pos in PositionsBots)
                    {
                        GetBalansSecur();
                        if (!pos.PassCloseOrder || !pos.PassOpenOrder) break;
                        decimal lotClose = 0;
                        lotClose = SelectSecurBalans;
                        Side side = Side.None;

                        if (lotClose < 0)
                        {
                            side = Side.Buy;
                        }
                        if (lotClose > 0)
                        {
                            side = Side.Sell;
                        }

                        if (lotClose == 0) return;
                        Order ordClose = CreateMarketOrder(SelectedSecurity, Price, lotClose, side);
                        if (ordClose != null )
                        {
                            pos.PassCloseOrder = false;
                            pos.PassOpenOrder = false;

                            Server.ExecuteOrder(ordClose);
                        
                            _logger.Information("Sending the Market to close the volume ", nameof(CloseMarketOpenVolume));                        

                            SendStrStatus(" Отправлен Маркет на закрытие объема на бирже");
                            GetBalansSecur();
                            Thread.Sleep(50);
                            return;
                        }
                        else
                        {
                            SendStrStatus(" Ошибка закрытия объема на бирже");
                            pos.PassCloseOrder = true;
                            pos.PassOpenOrder = true;
                            _logger.Information(" Error sending the Market to close the volume  ", nameof(CloseMarketOpenVolume));
                            //RobotsWindowVM.Log(Header, " Ошибка отправки Маркет на закрытие объема ");
                        }
                    }
                    //Thread.Sleep(1500);

                    //bool flag = true;
                    //foreach (PositionBot pos in PositionsBots)
                    //{
                    //    if (pos.OpenVolume != 0)
                    //    {
                    //        flag = false;
                    //        break;
                    //    }
                    //}
                    //if (flag) break;
                }
        }

        static List<Order> ordersForCancel = new List<Order>();

        private void ChekOrderForCancel() 
        {
            /* собрать номера ордеров на отмену 
             проверить что вернулось по ним сс биржи
             если кансел удалить
             если актив повторить окончание 
            */
        }

        /// <summary>
        /// отменяет активные ордера 
        /// </summary>
        private void CanсelActivOrders()
        {
            bool activ = ActivOrders();
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
                        _logger.Information("Method {Method} Order {@Order}", nameof(CanсelActivOrders), ordersAll[i]);
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
                    while (ActivOrders() || MonitoringOpenVolumePosition()) // пока есть открытые обемы и ордера на бирже
                    {
                        StopTradeLogic();
                        Thread.Sleep(300);
                        break; // test
                    }
                });
            }
        }

        /// <summary>
        ///  отправить ордер на биржу 
        /// </summary>
        private void SendOrderExchange() 
        {
            foreach (PositionBot position in PositionsBots)
            {
                List<Order> orders = position.OrdersForOpen;// взять из позиции ордера

                if (position.PassOpenOrder)
                {
                    position.PassOpenOrder = false; // TODO: осуществить смену статусов позиции и разрешений 

                    for (int i = 0; i < orders.Count; i++)
                    {
                        if (orders[i].State == OrderStateType.None)
                        {
                            // отправить ордер на биржу
                            Server.ExecuteOrder(orders[i]);
                            _logger.Information("Send order Exchange {Method} Order {@Order}", nameof(SendOrderExchange), orders[i]);
                       
                            SendStrStatus(" Ордер отправлен на биржу");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// добавить открывающий ордер в позицию
        /// </summary>  
        private void AddOpderPosition()
        {
            foreach (PositionBot position in PositionsBots)
            {
                PriceOpenPos = CalculPriceStartPos(position.Side);
                if (BigСlusterPrice == 0 || BottomPositionPrice == 0 || PriceOpenPos == 0)
                {
                    SendStrStatus(" BigСlusterPrice или BottomPositionPrice = 0 ");
                    return;
                }
                Order order = CreateLimitOrder(SelectedSecurity, PriceOpenPos, VolumePerOrder, position.Side);
                if (order != null)
                {  // отправить ордер в позицию
                    position.OrdersForOpen.Add(order);
                }
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

            if (MonitoringOpenVolumePosition())
            {
                SendStrStatus(" Есть открытый объем ");
                return;
                #region  проверка на открытые позиции
                //MessageBoxResult result = MessageBox.Show(" Есть открытые позиции! \n Всеравно создать? ", " ВНИМАНИЕ !!! ",
                //MessageBoxButton.YesNo);
                //if (result == MessageBoxResult.No)
                //{
                //    return;
                //}
                #endregion
            }
    
            PositionBot positionBuy = new PositionBot() { Side = Side.Buy };
            PositionBot positionSell = new PositionBot() { Side = Side.Sell };
            
            CalculateVolumeTrades();            

            if (VolumePerOrder != 0  && IsRun == true) // формируем позиции
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
        /// есть открытый объем в позиция
        /// </summary> 
        private bool MonitoringOpenVolumePosition()
        {
            if(SelectedSecurity == null) return false;
            decimal volume = 0;
            GetBalansSecur();
            volume = SelectSecurBalans;
            if (volume > 0) return true;
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
                        //RobotsWindowVM.Log(Header, " баланс SelectedSecurity = " + SelectSecurBalans);
                    }
                }
            }
            //decimal balans = portfolios[0].GetPositionOnBoard()[0].Find(pos =>
            //    pos.SecurityNameCode == _securName).ValueCurrent;
            //return balans;

        }

        /// <summary>
        /// расчет объема на ордер
        /// </summary>
        private void CalculateVolumeTrades()
        {
            if (SelectedSecurity == null || Price==0 || PartsPerInput==0) return;
            GetBalansSecur();
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
        private decimal CalculPriceStartPos(Side side)
        {
            _priceOpenPos = 0;
            if (SelectedSecurity == null)
            {
                SendStrStatus(" уще нет бумаги ");
                
                return _priceOpenPos;
            }
            decimal stepPrice = 0;

            if (side == Side.Buy )
            {       
                stepPrice = (BigСlusterPrice - BottomPositionPrice) / PartsPerInput;
                _priceOpenPos = BigСlusterPrice - stepPrice;
            }
            if (side == Side.Sell)
            {
                stepPrice = (TopPositionPrice - BigСlusterPrice) / PartsPerInput;
                _priceOpenPos = BigСlusterPrice + stepPrice;
            }
            //  todo : бобавить расчет для разнонаправленных сделок

            _priceOpenPos = Decimal.Round(_priceOpenPos, SelectedSecurity.Decimals);

            return  _priceOpenPos;
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
            
            _logger.Information("Method {Method} Order {@Order}", nameof(CreateLimitOrder), order);
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
            //RobotsWindowVM.Log(Header, "CreateMarketOrder\n " + "сформировали  маркет на биржу\n" + GetStringForSave(order));
            RobotsWindowVM.SendStrTextDb(" CreateMarketOrder " + order.NumberUser);
            _logger.Information("Method {Method} Order {@Order}", nameof(CreateMarketOrder), order);
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
            _logger.Information(" Come myTrade {Method} {MyTrade}", nameof(_server_NewMyTradeEvent), myTrade);
            //RobotsWindowVM.Log(Header, "Пришел трейд \n " + GetStringForSave(myTrade));
            GetBalansSecur();
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
            ActivOrders();
            CheckMyOrder(myOrder);
            /*  
             *  продолжить логику 
             *   
             */
            MonitiringStatusBot(myOrder);
            GetBalansSecur();
            _logger.Information(" Come myOrder {@Order} {Method} ", myOrder, nameof(_server_NewOrderIncomeEvent));
            //RobotsWindowVM.Log(Header, "Пришел ордер\n " + GetStringForSave(myOrder));
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
                e.PropertyName == "FullPositionVolume")            
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

                    writer.WriteLine(TopPositionPrice);
                    writer.WriteLine(BigСlusterPrice);
                    writer.WriteLine(Direction);

                    writer.WriteLine(BottomPositionPrice);

                    writer.WriteLine(FullPositionVolume);

                    writer.WriteLine(PartsPerInput);     

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
