using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace OsEngine.OsaExtension.Robots.Forbreakdown
{
    /// <summary>
    /// Логика взаимодействия для ForbreakdownUI.xaml
    /// </summary>
    public partial class ForBreakdownUI : Window
    {
        private ForBreakdown _strategy;

        public ForBreakdownUI(ForBreakdown strategy)
        {
            InitializeComponent();
            _strategy = strategy;
            DataContext = strategy;

            CultureInfo culture = new CultureInfo("ru-RU");

            TextBox_ProfitPoint.Text = _strategy.ProfitPoint.ToString(culture);
            TextBox_StartPoint.Text = _strategy.StartPoint.ToString(culture);
            TextBox_StopPoint.Text = _strategy.StopPoint.ToString(culture);

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _strategy.ProfitPoint =Convert.ToDecimal(TextBox_ProfitPoint.Text);
            _strategy.StartPoint = Convert.ToDecimal(TextBox_StartPoint.Text);
            _strategy.StopPoint = Convert.ToDecimal(TextBox_StopPoint.Text);
            _strategy.CalcPoint();
            _strategy.Save();
            Close();
        }
    }
}
