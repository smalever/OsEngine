using OsEngine.Entity;
using OsEngine.OsaExtension.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
  
    /// <summary>
    /// позиция робота по инструменту 
    /// </summary>
    public class PositionBot : BaseVM
    {
        //public PositionBot()
        //{
        //    Status = PositionStatus.NONE;
        //}

        #region ====== Свойства Position ==========================================================================

        /// <summary>
        /// направлние позиции 
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
        /// Код инструмента, для которого открыта позиция
        /// </summary>
        public string SecurityName
        {
            get
            {
                if (_ordersForOpen != null && _ordersForOpen.Count != 0)
                {
                    return _ordersForOpen[0].SecurityNameCode;
                }
                return _securityName;
            }
            set
            {
                if (_ordersForOpen != null && _ordersForOpen.Count != 0)
                {
                    return;
                }
                _securityName = value;
            }
        }
        private string _securityName;

        /// <summary>
        /// статус - состояние позиции 
        /// </summary>
        public PositionStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
                OnPropertyChanged(nameof(Status));
            }
        }
        private PositionStatus _status;

        /// <summary>
        /// ордера на открытие позиции
        /// </summary>
        public List<Order> OrdersForOpen
        {
            get
            {
                return _ordersForOpen;
            }
        }
        private List<Order> _ordersForOpen = new List<Order>();

        /// <summary>
        ///  список ордеров на закрытие позиции
        /// </summary>
        public List<Order> OrdersForClose
        {
            get
            {
                return _ordersForClose;
            }
        }
        private List<Order> _ordersForClose = new List<Order>();

        /// <summary>
        /// Position number / номер позиции
        /// </summary>
        public int Number;

        /// <summary>
        /// имя робота создавшего сделку 
        /// </summary>
        public string NameBot;

        /// <summary>
        /// Comment / коментарий 
        /// </summary>
        public string Comment;

        /// <summary>
        /// объем исполненый (открытый) в сделке 
        /// </summary>
        public decimal OpenVolume 
        {
            get
            {
                if (OrdersForClose == null)
                {
                    decimal volume = 0;

                    for (int i = 0; _ordersForOpen != null && i < _ordersForOpen.Count; i++)
                    {
                        volume += _ordersForOpen[i].VolumeExecute;
                    }
                    return volume;
                }

                decimal valueClose = 0;

                if (OrdersForClose != null)
                {
                    for (int i = 0; i < OrdersForClose.Count; i++)
                    {
                        valueClose += OrdersForClose[i].VolumeExecute;
                    }
                }

                decimal volumeOpen = 0;

                for (int i = 0; _ordersForOpen != null && i < _ordersForOpen.Count; i++)
                {
                    volumeOpen += _ordersForOpen[i].VolumeExecute;
                }

                decimal value = volumeOpen - valueClose;

                return value;
            }
        }

        /// <summary>
        /// Position opening price / цена открытия позиции (средняя)
        /// </summary>
        public decimal EntryPrice
        {
            get
            {
                if (_ordersForOpen == null ||
                    _ordersForOpen.Count == 0)
                {
                    return 0;
                }

                decimal price = 0;
                decimal volume = 0;
                for (int i = 0; i < _ordersForOpen.Count; i++)
                {
                    decimal volumeEx = _ordersForOpen[i].VolumeExecute;
                    if (volumeEx != 0)
                    {
                        volume += volumeEx;
                        price += volumeEx * _ordersForOpen[i].PriceReal;
                    }
                }
                if (volume == 0)
                {
                    return _ordersForOpen[0].Price;
                }

                return price / volume;
            }
        }

        /// <summary>
        /// разрешение отправить открывающий ордер    
        /// </summary>
        public bool PassOpenOrder
        {
            get => _passOpenOrder;

            set
            {
                _passOpenOrder = value;
                //Change();
                //OnPropertyChanged(nameof(PassVolume));
            }
        }
        public bool _passOpenOrder = true;

        /// <summary>
        /// разрешение отправить закрывающий ордер    
        /// </summary>
        public bool PassCloseOrder
        {
            get => _passCloseOrder;

            set
            {
                _passCloseOrder = value;                
            }
        }
        public bool _passCloseOrder = true;

        #endregion  end Свойства ========================

        #region ======================================Поля===========================================

        CultureInfo CultureInfo = new CultureInfo("ru-RU"); 

        /// <summary>
        ///  список трейдов принадлежащих позиции 
        /// </summary>
        private List<MyTrade> _myTrades = new List<MyTrade>();


        #endregion поля 
        #region Metods =================================================================

        /// <summary>
        /// Слежение (изсменение) статуса позиции
        /// </summary>
        private void MonitiringStatusPos(Order order)
        {
            if (OrdersForOpen == null || OrdersForOpen.Count == 0) return;
            for (int i = 0; i < OrdersForOpen.Count; i++)
            {
                if (OrdersForOpen[i].State == OrderStateType.Activ)
                {
                    Status = PositionStatus.OPENING;
                }
            }
            if (OrdersForClose == null || OrdersForClose.Count == 0) return;
            for (int i = 0; i < OrdersForClose.Count; i++)
            {
                if (OrdersForClose[i].State == OrderStateType.Activ)
                {
                    Status = PositionStatus.CLOSING;
                }
            }
        }
        /// <summary>
        ///  перезапись состояния оредра с биржи в мое хранилище
        /// </summary>
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
        /// принадлежит ли ордер списку
        /// </summary>
        public bool NewOrder(Order newOrder)
        {
            if (OrdersForOpen == null || OrdersForOpen.Count == 0) return false;
            for (int i = 0; i < OrdersForOpen.Count; i++)
            {
                if (OrdersForOpen[i].NumberUser == newOrder.NumberUser)
                {
                    CopyOrder(newOrder, OrdersForOpen[i]);
                    return true;
                }
            }
            for (int i = 0; i < OrdersForClose.Count; i++)
            {
                if (OrdersForClose[i].NumberUser == newOrder.NumberUser)
                {
                    CopyOrder(newOrder, OrdersForClose[i]);
                    return true;
                }
            }
            return false;
        }
        #endregion end metods

    }

    /// <summary>
    /// перечисление состояний статусов позиции в роботе
    /// </summary>
    public enum PositionStatus
    {
        /// <summary>
        /// не открыта (новая)
        /// </summary>
        NONE,

        /// <summary>
        /// открывается
        /// </summary>
        OPENING,

        /// <summary>
        /// закрыта (исполнена)
        /// </summary>
        DONE,

        /// <summary>
        /// ошибка
        /// </summary>
        OpeningFAIL,

        /// <summary>
        /// открыта
        /// </summary>
        OPEN,

        /// <summary>
        /// закрывается
        /// </summary>
        CLOSING,

        /// <summary>
        /// ошибка на закрытии
        /// </summary>
        ClosingFAIL,

        /// <summary>
        /// удалена
        /// </summary>
        DELETET
    }

}
