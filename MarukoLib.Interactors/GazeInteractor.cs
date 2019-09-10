using MarukoLib;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using Tobii.Interaction;
using Tobii.Interaction.Wpf;
using WheelchairGazeController.Filters;

namespace WheelchairGazeController.Interactors
{
    public class GazeInteractor : AbstractInteractor
    {

        private static readonly InteractableObject.MetaKey<WpfInteractor> InteractorKey =
            new InteractableObject.MetaKey<WpfInteractor>();

        private static readonly InteractableObject.MetaKey<GazeAwareBehavior> GazeAwareBehaviorKey =
            new InteractableObject.MetaKey<GazeAwareBehavior>();

        private static readonly InteractableObject.MetaKey<EventHandler<HasGazeChangedEventArgs>> HandlerKey = 
            new InteractableObject.MetaKey<EventHandler<HasGazeChangedEventArgs>>();

        private readonly GazePointDataStream _stream;

        private readonly PointFilter _pointFilter;

        private readonly WpfInteractorAgent _agent;

        private readonly double _screenScaleFactor = GraphicsUtils.GetScreenScaleFactor();

        private Point _lastGazePoint;

        private bool _gazeLost = true;

        public GazeInteractor(FrameworkElement owner, WpfInteractorAgent agent, PointFilter pointFilter = null) : base(owner)
        {
            _agent = agent;
            _pointFilter = pointFilter ?? (PointFilter)IdentityPointFilter.INSTANCE;

            /* 初始化 */
            //var interactor = _agent.AddInteractorFor(owner);
            //interactor.WithGazeAware().HasGaze(() => _gazeLost = false).LostGaze(() => _gazeLost = true);
            //_stream = interactor.GetGazePointDataStream();
            //_stream.Next += GazePointStream_Next;
        }

        public override Point? CurrentPosition { get { return _gazeLost ? null : (Point?)_lastGazePoint; } }

        public override void Dispose()
        {
            _agent.RemoveInteractor(Owner);
        }

        protected override void PostCreateObj(InteractableObject obj)
        {
            EventHandler<HasGazeChangedEventArgs> handler = (sender, e) =>
            {
                if (e.HasGaze)
                    SetActive(obj);
                else
                    SetInactive(obj);
            };
            HandlerKey.Set(obj, handler);
            var interactor = _agent.FindInteractor(obj.element) ?? _agent.AddInteractorFor(obj.element);
            InteractorKey.Set(obj, interactor);
            var gazeAwareBehavior = interactor.WithGazeAware(); // not sure it will be reused or not, so store it
            GazeAwareBehaviorKey.Set(obj, gazeAwareBehavior);
            gazeAwareBehavior.HasGazeChanged += handler;
        }

        protected override void PreDestroyObj(InteractableObject obj)
        {
            GazeAwareBehaviorKey.Get(obj).HasGazeChanged -= HandlerKey.Get(obj);
        }

        private void GazePointStream_Next(object sender, StreamData<GazePointData> data)
        {
            Point centeredGazePoint = new Point(data.Data.X, data.Data.Y).Divide(_screenScaleFactor);
            Point filteredGazePoint = _pointFilter.Step(centeredGazePoint);
            _lastGazePoint = filteredGazePoint;
        }

    }

}
