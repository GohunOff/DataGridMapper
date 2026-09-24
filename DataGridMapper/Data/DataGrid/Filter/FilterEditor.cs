using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MC.Data.DataGrid.Filter
{
    public sealed class FilterEditor
    {
        public string PropertyName { get; private set; }

        public ComboBox OperatorComboBox { get; private set; }

        public TextBox ValueTextBox { get; private set; }

        public ComboBox BooleanComboBox { get; private set; }

        public bool IsBoolean { get; private set; }

        public FilterEditor(
            string propertyName,
            ComboBox operatorComboBox,
            TextBox valueTextBox)
        {
            PropertyName = propertyName;

            OperatorComboBox =
                operatorComboBox;

            ValueTextBox =
                valueTextBox;

            IsBoolean = false;
        }

        public FilterEditor(
           string propertyName,
           ComboBox booleanComboBox)
        {
            PropertyName = propertyName;
            BooleanComboBox = booleanComboBox;
            IsBoolean = true;
        }
    }
}
