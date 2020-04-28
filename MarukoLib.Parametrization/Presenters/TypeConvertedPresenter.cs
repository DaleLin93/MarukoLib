using System;
using System.Collections;
using System.Linq;
using JetBrains.Annotations;
using MarukoLib.Lang;

namespace MarukoLib.Parametrization.Presenters
{

    public class TypeConvertedPresenter : IPresenter
    {

        private sealed class TypeConvertedParameter : OverridenParameter
        {

            public TypeConvertedParameter(IParameterDescriptor originalParameter, ITypeConverter typeConverter, IReadonlyContext metadata) : base(originalParameter)
            {
                TypeConverter = typeConverter;
                Metadata = metadata ?? EmptyContext.Instance;
            }

            public ITypeConverter TypeConverter { get; }

            public override string Id => OriginalParameter.Id;

            public override string Name => OriginalParameter.Name;

            public override string Unit => OriginalParameter.Unit;

            public override string Description => OriginalParameter.Description;

            public override Type ValueType => TypeConverter.OutputType;

            public override bool IsNullable => OriginalParameter.IsNullable;

            public override object DefaultValue => TypeConverter.ConvertForward(OriginalParameter.DefaultValue);

            public override IEnumerable SelectableValues => OriginalParameter.SelectableValues?
                .Cast<object>().Select(value => TypeConverter.ConvertForward(value));

            public override IReadonlyContext Metadata { get; }

            public override bool IsValid(object value) => OriginalParameter.IsValid(TypeConverter.ConvertBackward(value));

        }

        private class Adapter : IParameterViewAdapter
        {

            public event EventHandler ValueChanged;

            private readonly TypeConvertedParameter _parameter;

            private readonly ParameterViewModel _viewModel;

            public Adapter(TypeConvertedParameter parameter, ParameterViewModel viewModel)
            {
                _parameter = parameter;
                _viewModel = viewModel;

                _viewModel.ValueChanged += ParameterViewModel_OnValueChanged;
            }

            public bool IsEnabled
            {
                get => _viewModel.IsEnabled;
                set => _viewModel.IsEnabled = value;
            }

            public bool IsValid
            {
                get => _viewModel.IsValid;
                set => _viewModel.IsValid = value;
            }

            public object Value
            {
                get => _parameter.TypeConverter.ConvertBackward(_viewModel.Value);
                set => _viewModel.Value = _parameter.TypeConverter.ConvertForward(value);
            }

            private void ParameterViewModel_OnValueChanged(object sender, EventArgs e) => ValueChanged?.Invoke(this, e);

        }

        public static readonly ContextProperty<ITypeConverter> TypeConverterProperty = new NamedProperty<ITypeConverter>("TypeConverter");

        public static readonly NamedProperty<IReadonlyContext> ConvertedContextProperty = new NamedProperty<IReadonlyContext>("ConvertedContext", EmptyContext.Instance);

        public static readonly TypeConvertedPresenter Instance = new TypeConvertedPresenter();

        public static bool HasTypeConverter([NotNull] IParameterDescriptor param) => TryGetTypeConverter(param, out _);

        public static bool TryGetTypeConverter([NotNull] IParameterDescriptor parameter, out ITypeConverter converter)
        {
            var flag = TypeConverterProperty.TryGet(parameter.Metadata, out converter) && converter != null;
            if (!flag) return false;
            try
            {
                converter = converter.Validate(parameter.ValueType, null);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Invalid type converter ({converter.InputType.Name}<==>{converter.OutputType.Name}) for parameter '{parameter.Id}'.", e);
            }
            return true;
        }

        public ParameterViewModel Present(IParameterDescriptor param)
        {
            if (!TryGetTypeConverter(param, out var converter)) throw new ArgumentException();
            var converted = new TypeConvertedParameter(param, converter, ConvertedContextProperty.Get(param.Metadata));
            var viewModel = converted.Present();
            return new ParameterViewModel(param, viewModel.Element, new Adapter(converted, viewModel));
        }

    }

    public static class TypeConvertedPresenterExt
    {

        public static T UseTypeConvertedPresenter<T>([NotNull] this T contextBuilder, [NotNull] ITypeConverter converter, 
            [CanBeNull] IReadonlyContext convertedContext = null) where T : IContextBuilder
        {
            contextBuilder.SetPresenter(TypeConvertedPresenter.Instance);
            contextBuilder.Set(TypeConvertedPresenter.TypeConverterProperty, converter);
            contextBuilder.SetPropertyNotNull(TypeConvertedPresenter.ConvertedContextProperty, convertedContext);
            return contextBuilder;
        }

    }

}