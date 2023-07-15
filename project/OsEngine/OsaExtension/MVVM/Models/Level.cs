using OsEngine.Entity;
using OsEngine.Market.Servers;
using OsEngine.OsaExtension.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
    public class Level : BaseVM
    {
        #region ======================================Свойства======================================
        /// <summary>
        /// цена уровня
        /// </summary>
        public decimal PriceLevel
        {
            get => _priceLevel;

            set
            {
                _priceLevel = value;
                OnPropertyChanged(nameof(PriceLevel));
            }
        }
        public decimal _priceLevel = 0;

        /// <summary>
        /// направлние сделок на уровне
        /// </summary>
        public Side Side
        {
            get => _side;

            set
            {
                _side = value;
                OnPropertyChanged(nameof(Side));
            }
        }
        public Side _side = 0;

        /// <summary>
        /// Статус сделок на уровне
        /// </summary>
        public PositionStatus StatusLevel
        {
            get => _statusLevel;

            set
            {
                _statusLevel = value;
                OnPropertyChanged(nameof(StatusLevel));
            }
        }
        private PositionStatus _statusLevel;

        /// <summary>
        /// реалькая цена открытой позиции
        /// </summary>
        public decimal OpenPrice
        {
            get => _openPrice;

            set
            {
                _openPrice = value;
                OnPropertyChanged(nameof(OpenPrice));
            }
        }
        public decimal _openPrice = 0;

        /// <summary>
        ///  цена закрытия позиции (прибыли)
        /// </summary>
        public decimal TakePrice
        {
            get => _takePrice;

            set
            {
                _takePrice = value;
                Change();
            }
        }
        public decimal _takePrice = 0;
       
        /// <summary>
        /// объем позиции
        /// </summary>
        public decimal Volume
        {
            get => _volume;

            set
            {
                _volume = value;
                Change();
            }
        }
        public decimal _volume = 0;
        
        public decimal Margin
        {
            get => _margine;

            set
            {
                _margine = value;
                Change();
            }
        }
        public decimal _margine = 0;
        
        public decimal Accum
        {
            get => _accum;

            set
            {
                _accum = value;
                Change();

            }
        }
        public decimal _accum = 0;
        
        /// <summary>
        /// объем ордера открытия поз
        /// <summary>
        public decimal LimitVolume
        {
            get => _limitVolume;
            set
            {
                _limitVolume = value;
                Change();
            }
        }
        private decimal _limitVolume;
       
        /// <summary>
        /// Обем ордера закрытия поз
        /// </summary>
        public decimal TakeVolume
        {
            get => _takeVolume;
            set
            {
                _takeVolume = value;
                Change();
            }
        }
        private decimal _takeVolume;
        
        /// <summary>
        /// разрешение открыть позицию        
        /// </summary>
        public bool PassVolume
        {
            get => _passVolume;

            set
            {
                _passVolume = value;
                Change();
            }
        }
        public bool _passVolume = true;
        
        /// <summary>
        /// разрешение выставить тейк     
        /// </summary>
        public bool PassTake
        {
            get => _passTake;

            set
            {
                _passTake = value;
                Change();
            }
        }
        public bool _passTake = true;
        #endregion

        #region ======================================Поля===========================================

        CultureInfo CultureInfo = new CultureInfo("ru-RU");

        /// <summary>
        ///  список лимиток на закрытие
        /// </summary>
        public List<Order> OrdersForClose = new List<Order>();

        /// <summary>
        /// лимитки на открытие позиций 
        /// </summary>
        public List<Order> OrdersForOpen = new List<Order>();

        /// <summary>
        ///  список моих трейдов принадлежащих уровню
        /// </summary>
        private List<MyTrade> _myTrades = new List<MyTrade>();

        private decimal _calcVolume = 0;

        #endregion

        #region ======================================Методы====================================

        public void SetVolumeStart()
        {
            _calcVolume = Volume;

            if (Volume == 0)
            {
                if (LimitVolume == 0)
                {
                    ClearOrders(ref OrdersForOpen);
                }
                if (TakeVolume == 0)
                {
                    ClearOrders(ref OrdersForClose);
                }
            }
        }

        /// <summary>
        /// принадлежит ли ордер списку
        /// </summary>
        public bool NewOrder(Order newOrder)
        {
            //if(OrdersForOpen == null || OrdersForOpen.Count == 0) return false;
            for (int i = 0; i < OrdersForOpen.Count; i++)
            {
                if (OrdersForOpen[i].NumberMarket == newOrder.NumberMarket)
                {
                    CopyOrder(newOrder, OrdersForOpen[i]);

                    CalculateOrders();

                    StatusLevel = PositionStatus.OPEN;

                    return true;
                }
            }
            for (int i = 0; i < OrdersForClose.Count; i++)
            {
                if (OrdersForClose[i].NumberMarket == newOrder.NumberMarket)
                {
                    CopyOrder(newOrder, OrdersForClose[i]);

                    CalculateOrders();

                    StatusLevel = PositionStatus.DONE;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// принадлежит ли трейд ордеру проверка
        /// </summary>
        private bool IsMyTrade(MyTrade newTrade)
        {
            foreach (Order order in OrdersForOpen)
            {
                if (order.NumberMarket == newTrade.NumberOrderParent)
                {
                    return true;
                }
            }
            foreach (Order order in OrdersForClose)
            {
                if (order.NumberMarket == newTrade.NumberOrderParent)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// проверка объема исполненых ордеров на уровне + смена статусов сделки уровня
        /// </summary>
        public void CalculateOrders()
        {
            decimal activeVolume = 0;
            decimal volumeExecute = 0;

            decimal activeTake = 0;

            bool passLimit = true;
            bool passTake = true;

            foreach (Order order in OrdersForOpen)
            {
                volumeExecute += order.VolumeExecute;
                if (order.State == OrderStateType.Activ
                    || order.State == OrderStateType.Patrial)
                {
                    activeVolume += order.Volume - order.VolumeExecute;
                    StatusLevel = PositionStatus.OPENING;
                }
                else if (order.State == OrderStateType.Pending
                        || order.State == OrderStateType.None)
                {
                    passLimit = false;
                    StatusLevel = PositionStatus.NONE;
                }
            }

            foreach (Order order in OrdersForClose)
            {
                volumeExecute -= order.VolumeExecute;
                if (order.State == OrderStateType.Activ
                    || order.State == OrderStateType.Patrial)
                {
                    activeTake += order.Volume - order.VolumeExecute;
                    StatusLevel = PositionStatus.CLOSING;
                }
                else if (order.State == OrderStateType.Pending
                        || order.State == OrderStateType.None)
                {
                    passTake = false;
                    StatusLevel = PositionStatus.NONE;
                }
            }

            Volume = volumeExecute;

            if (Side == Side.Sell)
            {
                Volume *= -1;
            }

            LimitVolume = activeVolume;
            TakeVolume = activeTake;
            PassVolume = passLimit;
            PassTake = passTake;
        }

        private Order CopyOrder(Order newOrder, Order order)
        {
            order.State = newOrder.State;
            order.TimeCancel = newOrder.TimeCancel;
            order.Volume = newOrder.Volume;
            order.VolumeExecute = newOrder.VolumeExecute;
            order.TimeDone = newOrder.TimeDone;
            order.TimeCallBack = newOrder.TimeCallBack;
            order.NumberUser = newOrder.NumberUser;

            return order;
        }

        /// <summary>
        /// Удаляет ордера Cancel  Done и Fail из списков ордеров 
        /// </summary>
        public void ClearOrders(ref List<Order> orders)
        {
            if (orders == null) return;
            List<Order> newOrders = new List<Order>();

            foreach (Order order in orders)
            {
                if (order != null
                    && order.State != OrderStateType.Cancel
                    && order.State != OrderStateType.Done)
                //&& order.State != OrderStateType.Fail)
                {
                    newOrders.Add(order);
                }
            }
            RobotsWindowVM.SendStrTextDb(" Удалили ордера Cancel и Done");
            orders = newOrders;
        }

        /// <summary>
        /// список свойств для обновления в интефейсе после изменений
        /// </summary>
        private void Change()
        {
            OnPropertyChanged(nameof(Volume));
            OnPropertyChanged(nameof(OpenPrice));
            OnPropertyChanged(nameof(LimitVolume));
            OnPropertyChanged(nameof(PassTake));
            OnPropertyChanged(nameof(TakeVolume));
            OnPropertyChanged(nameof(PassVolume));
            OnPropertyChanged(nameof(TakePrice));
            OnPropertyChanged(nameof(Side));
            OnPropertyChanged(nameof(PriceLevel));
            OnPropertyChanged(nameof(Margin));
            OnPropertyChanged(nameof(Accum));
            OnPropertyChanged(nameof(StatusLevel));
        }

        /// <summary>
        ///  формируем строку для сохранения
        /// </summary>
        public string GetStringForSave()
        {
            string str = "";
            str += "Уровень = \n";
            str += "Volume = " + Volume.ToString(CultureInfo) + " | ";
            str += "PriceLevel = " + PriceLevel.ToString(CultureInfo) + " | ";
            str += "OpenPrice = " + OpenPrice.ToString(CultureInfo) + " | ";
            str += Side + " | ";
            str += "PassVolume = " + PassVolume.ToString(CultureInfo) + " | ";
            str += "PassTake = " + PassTake.ToString(CultureInfo) + " | ";
            str += "LimitVolume = " + LimitVolume.ToString(CultureInfo) + " | ";
            str += "TakeVolume = " + TakeVolume.ToString(CultureInfo) + " | ";
            str += "TakePrice = " + TakePrice.ToString(CultureInfo) + " | ";

            return str;
        }

        /// <summary>
        /// отозвать все ордера с биржи
        /// </summary>
        public void CancelAllOrders(IServer server, string header)
        {
            //CanselCloseOrders(server, getStringForSave);
            //CanselOpenOrders(server, getStringForSave);
            Task.Run(() =>
            {
                while (true)
                {
                    CanselOpenOrders(OrdersForOpen, server);
                    CanselCloseOrders(OrdersForClose, server);

                    string str = "ВКЛЮЧЕН поток для отзыва ордеров \n";
                    Debug.WriteLine(str);

                    Thread.Sleep(3000);
                    if (LimitVolume == 0 && TakeVolume == 0)
                    {
                        string str2 = "Поток для отзыва ордеров ОТКЛЮЧЕН \n";
                        Debug.WriteLine(str2);

                        //RobotWindowVM.Log(Header, "Поток для отзыва ордеров ОТКЛЮЧЕН \n");

                        break;
                    }
                }
            });
        }

        /// <summary>
        /// отозвать  открытые ордера с биржи
        /// </summary>
        private void CanselOpenOrders(List<Order> orders, IServer server)
        {
            foreach (Order order in OrdersForOpen)
            {
                RobotsWindowVM.SendStrTextDb("ConselOrders ForOpen order.NumberUser " + order.NumberUser.ToString());
                if (order != null
                       && order.State == OrderStateType.Activ
                        || order.State == OrderStateType.Patrial
                        || order.State == OrderStateType.Pending)
                {
                    server.CancelOrder(order);

                    RobotsWindowVM.Log(order.SecurityNameCode, " Снимаем лимитку на открытие с биржи \n" + GetStringForSave());
                    Thread.Sleep(30);
                }
            }
        }

        /// <summary>
        /// удаляет частично исполненые ордера на открытие 
        /// </summary>
        public void CanselPatrialOrders(IServer server)
        {
            foreach (Order order in OrdersForOpen)
            {
                RobotsWindowVM.SendStrTextDb("Consel Patrial Orders ForOpen order.NumberUse " + order.NumberUser.ToString());
                if (order != null
                       && order.State == OrderStateType.Patrial)
                {
                    server.CancelOrder(order);

                    RobotsWindowVM.Log(order.SecurityNameCode, " Снимаем частичную лимитку на открытие с биржи \n"
                        + order.NumberUser.ToString());
                    Thread.Sleep(30);
                }
            }
        }

        /// <summary>
        /// отозвать ордера на закрытие с биржи
        /// </summary>
        private void CanselCloseOrders(List<Order> orders, IServer server)
        {
            foreach (Order order in OrdersForClose)
            {
                RobotsWindowVM.SendStrTextDb("CanselOrders ForClose order.NumberUser " + order.NumberUser.ToString());
                if (order.Comment == null)
                {
                    continue;
                }
                if (order != null
                       && order.State == OrderStateType.Activ
                        || order.State == OrderStateType.Patrial
                        || order.State == OrderStateType.Pending)
                {
                    server.CancelOrder(order);
                    RobotsWindowVM.Log(order.SecurityNameCode, " Снимаем тейк на сервере \n" + GetStringForSave());
                    Thread.Sleep(30);
                }
            }
        }

        /// <summary>
        /// проверяет принадлежность трейд к уроню и добавляет если да
        /// </summary>
        public bool AddMyTrade(MyTrade newTrade, Security security)
        {
            foreach (MyTrade trade in _myTrades)
            {
                if (trade.NumberTrade == newTrade.NumberTrade)
                {
                    return false;
                }
            }

            if (IsMyTrade(newTrade))
            {
                _myTrades.Add(newTrade);

                CalculateOrders();

                CalculatePosition(newTrade, security);
                return true;
            }
            return false;
        }

        /// <summary>
        /// расчет объема позиции (по лотам)
        /// </summary>
        private void CalculatePosition(MyTrade myTrade, Security security)
        {
            //decimal volume =0;
            decimal openPrice = 0;
            decimal accum = 0;

            if (_calcVolume == 0)
            {
                OpenPrice = myTrade.Price;
            }
            else if (_calcVolume > 0)
            {
                if (myTrade.Side == Side.Buy)
                {
                    OpenPrice = (_calcVolume * OpenPrice + myTrade.Volume * myTrade.Price) /
                        (_calcVolume + myTrade.Volume);
                }
                else
                {
                    if (myTrade.Volume <= _calcVolume)
                    {
                        accum = (myTrade.Price - OpenPrice) * myTrade.Volume;
                    }
                    else
                    {
                        accum = (myTrade.Price - OpenPrice) * _calcVolume;
                        OpenPrice = myTrade.Price;
                    }
                }
            }
            else if (_calcVolume < 0)
            {
                if (myTrade.Side == Side.Buy)
                {
                    if (myTrade.Volume <= Math.Abs(_calcVolume))
                    {
                        accum = (OpenPrice - myTrade.Price) * myTrade.Volume;
                    }
                    else
                    {
                        accum = (OpenPrice - myTrade.Price) * Math.Abs(_calcVolume);
                        OpenPrice = myTrade.Price;
                    }
                }
                else
                {
                    OpenPrice = (Math.Abs(_calcVolume) * OpenPrice + myTrade.Volume * myTrade.Price) /
                        (Math.Abs(_calcVolume) + myTrade.Volume);
                }
            }

            if (myTrade.Side == Side.Buy)
            {
                _calcVolume += myTrade.Volume;
            }
            else if (myTrade.Side == Side.Sell)
            {
                _calcVolume -= myTrade.Volume;
            }

            if (_calcVolume == 0)
            {
                OpenPrice = 0;
            }

            OpenPrice = Math.Round(OpenPrice, security.Decimals);
            Accum += accum * security.Lot;
        }


        //private bool IsMyTrade(MyTrade myTrade)
        //{
        //    foreach (Order order in OrdersForOpen)
        //    {
        //        if (order.NumberMarket == myTrade.NumberOrderParent)
        //        {
        //            // номер трейда принадлежит ордеру открытия позы на бирже
        //            // StatusLevel = PositionStatus.OPEN;
        //            return true;                   
        //        }
        //    }
        //    foreach (Order order in OrdersForClose)
        //    {
        //        if (order.NumberMarket == myTrade.NumberOrderParent)
        //        {
        //            // номер трейда принадлежит ордеру закрытия позы на бирже
        //            // StatusLevel = PositionStatus.DONE;
        //            return true;
        //        }
        //    }
        //    return false;
        //} 

        #endregion

        #region =================================Делегаты ====================================

        public delegate string DelegateGetStringForSave(Order order);


        #endregion

    }
}
