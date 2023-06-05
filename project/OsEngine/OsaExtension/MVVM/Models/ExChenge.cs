using OsEngine.Market;
using OsEngine.OsaExtension.MVVM.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OsEngine.OsaExtension.MVVM.Models
{
    public class ExChenge : BaseVM
    {
        /// <summary>
        /// Биржа
        /// </summary>
        public ExChenge(ServerType type)
        {
            Server = type;
        }
        public ServerType Server
        {
            get => _server;
            set
            {
                _server = value;
                OnPropertyChanged(nameof(Server));
            }
        }
        private ServerType _server;
    }
}
