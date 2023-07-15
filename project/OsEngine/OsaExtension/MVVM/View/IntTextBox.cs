using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace OsEngine.OsaExtension.MVVM.View
{
    public class IntTextBox : TextBox
    {
        public IntTextBox()
        {
            this.PreviewTextInput += IntTextBox_PreviewTextInput;
            this.TextChanged += IntTextBox_TextChanged;
        }

        private void IntTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            tb.Select(tb.Text.Length, 0);
        }

        private void IntTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (!Char.IsDigit(e.Text, 0))
            {
                e.Handled = true;
            }
        }
    }
}
