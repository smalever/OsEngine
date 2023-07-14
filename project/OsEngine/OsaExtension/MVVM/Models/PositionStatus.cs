using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
    /// <summary>
    /// перечисление состояний статусов позиции на уровне 
    /// </summary>
    public enum PositionStatus
    {
        /// <summary>
        /// нет позиции
        /// </summary>
        NONE,

        /// <summary>
        /// открывается
        /// </summary>
        OPENING,

        /// <summary>
        /// закрыта
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
