using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
    /// <summary>
    /// действие бота (сейчас)
    /// </summary>
    public enum ActionBot
    {
        /// <summary>
        /// выключен
        /// </summary>
        Stop,
        /// <summary>
        /// открывает позицию 
        /// </summary>
        OpeningPos,
        /// <summary>
        /// закрывает позицию
        /// </summary>
        ClosingPos,
        /// <summary>
        /// набрал позицию
        /// </summary>
        Open,
        /// <summary>
        /// торгует (работает)
        /// </summary>
        Trades,
        /// <summary>
        /// отключен от биржи (сети)
        /// </summary>
        ExchangDisabled

    }
}
