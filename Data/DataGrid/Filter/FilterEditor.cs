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

        public ComboBox EnumComboBox { get; private set; }

        public bool IsBoolean
        {
            get { return BooleanComboBox != null; }
        }

        public bool IsEnum
        {
            get { return EnumComboBox != null; }
        }

        private FilterEditor()
        {
        }

        public static FilterEditor CreateText(
            string propertyName,
            ComboBox operatorComboBox,
            TextBox valueTextBox)
        {
            return new FilterEditor
            {
                PropertyName = propertyName,
                OperatorComboBox = operatorComboBox,
                ValueTextBox = valueTextBox
            };
        }

        public static FilterEditor CreateBoolean(
            string propertyName,
            ComboBox comboBox)
        {
            return new FilterEditor
            {
                PropertyName = propertyName,
                BooleanComboBox = comboBox
            };
        }

        public static FilterEditor CreateEnum(
            string propertyName,
            ComboBox comboBox)
        {
            return new FilterEditor
            {
                PropertyName = propertyName,
                EnumComboBox = comboBox
            };
        }
    }

}
