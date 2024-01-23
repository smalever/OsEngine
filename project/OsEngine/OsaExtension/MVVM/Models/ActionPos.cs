using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
    /// <summary>
    /// Дейстаия с позицией 
    /// </summary>
    public enum ActionPos
    {
        /// <summary>
        /// ничего не делать
        /// </summary>
        Nothing,

        Stop,

        /// <summary>
        /// добавить объем 
        /// </summary>
        AddVolumes,

        /// <summary>
        /// перевернуть позицию
        /// </summary>
        RollOver, 

        /// <summary>
        /// уменьшить стоп
        /// </summary>
        ShortenStop 
    }
}
