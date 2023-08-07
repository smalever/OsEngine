using OsEngine.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
  
    /// <summary>
    /// позиция по инструменту 
    /// </summary>
    public class Positions
    {
        public Positions()
        {
            Status = PositionStatus.NONE;
        }

        public PositionStatus Status
        {
            get { return _status; }
            set
            {
                _status = value;
            }
        }
        private PositionStatus _status;
    }

    /// <summary>
    /// перечисление состояний статусов позиции на уровне 
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
