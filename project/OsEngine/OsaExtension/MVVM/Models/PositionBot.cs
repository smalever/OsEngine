using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
  
    /// <summary>
    /// позиция робота по инструменту 
    /// </summary>
    public class PositionBot
    {
        public PositionBot()
        {
            Status = PositionStatus.NONE;
        }

        #region ====== Свойства Positions ==========================================================================

        /// <summary>
        /// Код инструмента, для которого открыта позиция
        /// </summary>
        public string SecurityName
        {
            get
            {
                if (_ordersPos != null && _ordersPos.Count != 0)
                {
                    return _ordersPos[0].SecurityNameCode;
                }
                return _securityName;
            }
            set
            {
                if (_ordersPos != null && _ordersPos.Count != 0)
                {
                    return;
                }
                _securityName = value;
            }
        }
        private string _securityName;

        /// <summary>
        /// является ли позиция активной 
        /// </summary>
        private bool ActivePos()
        {
            //if (Levels == null || Levels.Count == 0) return false;
            //foreach (Level level in Levels)
            //{
            //    if (level.StatusLevel == PositionStatus.OPENING ||
            //        level.StatusLevel == PositionStatus.OPEN ||
            //        level.StatusLevel == PositionStatus.CLOSING)
            //    {
            //        return true;
            //    }
            //}
            return false;
        }

        /// <summary>
        /// статус - состояние позиции 
        /// </summary>
        public PositionStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
            }
        }
        private PositionStatus _status;

        /// <summary>
        /// ордера позиции 
        /// </summary>
        public List<Order> OrdersPos
        {
            get
            {
                return _ordersPos;
            }
        }
        private List<Order> _ordersPos;

        #endregion  end Свойства ========================
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
