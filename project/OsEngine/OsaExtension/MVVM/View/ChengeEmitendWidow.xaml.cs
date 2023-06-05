using OsEngine.OsaExtension.MVVM.ViewModels;
using OsEngine.OsTrader.Gui;
using OsEngine.OsTrader.Panels;
using OsEngine.Robots.Trend;
using System.Windows;

namespace OsEngine.OsaExtension.MVVM.View
{
    /// <summary>
    /// Логика взаимодействия для ChengeEmitendWidow.xaml
    /// </summary>
    public partial class ChengeEmitendWidow : Window
    {          

        public ChengeEmitendWidow(IRobotVM robot)
        {
            InitializeComponent();
            DataContext = new ChangeEmitentVM(robot);
        }
    }
}
