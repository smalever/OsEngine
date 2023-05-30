using OsEngine.Entity;
using OsEngine.OsaExtension.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
    /// <summary>
    ///...Бумаги
    /// </summary>
    public class Emitent : BaseVM
    {
        public Emitent(Security security)
        {
            Security = security;
        }
        public string NameSec
        {
            get => Security.Name;
        }

        public Security Security;
    }
}

