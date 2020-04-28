using System;
using System.Windows.Controls;
using MarukoLib.Lang;

namespace MarukoLib.Parametrization.Presenters
{

    public class DateTimePresenter : IPresenter
    {

        private class Accessor : IParameterViewAccessor
        {

            public event EventHandler ValueChanged;

            private readonly IParameterDescriptor _parameter;

            private readonly DatePicker _datePicker;

            public Accessor(IParameterDescriptor parameter, DatePicker datePicker)
            {
                _parameter = parameter;
                _datePicker = datePicker;
                _datePicker.SelectedDateChanged += (sender, args) => ValueChanged?.Invoke(this, args);
            }

            public object Value
            {
                get => _parameter.IsValidOrThrow(_datePicker.SelectedDate);
                set => _datePicker.SelectedDate = (DateTime?) value;
            }

        }

        /// <summary>
        /// Default Value: true
        /// </summary>
        public static readonly NamedProperty<Action<DatePicker>> DatePickerConfiguratorProperty = new NamedProperty<Action<DatePicker>>("DatePickerConfigurator");

        public static readonly DateTimePresenter Instance = new DateTimePresenter();

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            var picker = new DatePicker();
            DatePickerConfiguratorProperty.Get(param.Metadata, null)?.Invoke(picker);
            return new ParameterViewModel(param, picker, new Accessor(param, picker), picker);
        }

    }

}