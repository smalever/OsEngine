using OsEngine.OsaExtension.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
    /// <summary>
    ///  названия классоы бумаг на бирже 
    /// </summary>
    public class EmitClasses : BaseVM
    {
        // конструктор 
        public EmitClasses(string ctr)
        {
            ClassEmit = ctr;
        }
        public string ClassEmit
        {
            get => _classEmit;
            set
            {
                _classEmit = value;
                OnPropertyChanged(nameof(ClassEmit));
            }
        }
        private string _classEmit;

    }
}
