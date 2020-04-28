using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using JetBrains.Annotations;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Data;

namespace MarukoLib.Parametrization.Presenters
{

    [SuppressMessage("ReSharper", "PrivateFieldCanBeConvertedToLocalVariable")]
    public class ParameterizedObjectPresenter : MultiParameterPresenter
    {

        private class Adapter : IAdapter
        {

            public event EventHandler ValueChanged;

            [NotNull] private readonly ReferenceCounter _updateLock = new ReferenceCounter();

            [NotNull] private readonly IParameterDescriptor _parameter;

            [NotNull] private readonly IParameterizedObjectFactory _factory;

            [NotNull] private readonly ParameterViewModel[] _subParamViewModels;

            public Adapter([NotNull] IParameterDescriptor parameter, [NotNull] IParameterizedObjectFactory factory,
                [NotNull] ParameterViewModel[] subParamViewModels)
            {
                _parameter = parameter;
                _factory = factory;
                _subParamViewModels = subParamViewModels;

                foreach (var subParam in subParamViewModels)
                    subParam.ValueChanged += SubParam_OnValueChanged;
            }

            public object Value
            {
                get
                {
                    var context = new Context();
                    var errors = new LinkedList<ParameterViewModel>();
                    foreach (var subParam in _subParamViewModels)
                        try
                        {
                            context.Set(subParam.Parameter, subParam.Value);
                        }
                        catch (Exception)
                        {
                            subParam.IsValid = false;
                            errors.AddLast(subParam);
                        }
                    if (errors.Any()) throw new Exception();
                    return _factory.Create(_parameter, context);
                }
                set
                {
                    var context = _factory.Parse(_parameter, (IParameterizedObject)value);
                    lock (_updateLock.Ref())
                        foreach (var subParam in _subParamViewModels)
                            if (context.TryGet(subParam.Parameter, out var val)) subParam.Value = val;
                    ValueChanged?.Invoke(this, EventArgs.Empty);
                }
            }

            public void SetEnabled(bool value)
            {
                if (value)
                {
                    var context = _factory.Parse(_parameter, (IParameterizedObject) Value);
                    foreach (var subParam in _subParamViewModels)
                        subParam.IsEnabled = _factory.IsEnabled(context, subParam.Parameter);
                }
                else
                    foreach (var subParam in _subParamViewModels)
                        subParam.IsEnabled = false;
            }

            public void SetValid(bool value) { }

            private void SubParam_OnValueChanged(object sender, EventArgs e)
            {
                if (!_updateLock.IsReferred)
                    ValueChanged?.Invoke(this, e);
            }

        }

        public static readonly ParameterizedObjectPresenter Instance = new ParameterizedObjectPresenter();

        protected ParameterizedObjectPresenter() { }

        protected override IParameterDescriptor[] GetSubParameters(IParameterDescriptor parameter) 
            => parameter.GetParameterizedObjectFactory().GetParameters(parameter).ToArray();

        protected override IAdapter GetAdapter(IParameterDescriptor parameter, ParameterViewModel[] subParamViewModels) 
            => new Adapter(parameter, parameter.GetParameterizedObjectFactory(), subParamViewModels);

    }
}