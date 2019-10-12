using System;
using System.Windows.Forms;
using SharpDX;
using SharpDX.Windows;
using DW = SharpDX.DirectWrite;
using DXGI = SharpDX.DXGI;
using D2D1 = SharpDX.Direct2D1;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;
using MarukoLib.Logging;

namespace MarukoLib.DirectX
{

    public abstract class SimpleD2DForm : RenderForm, IDisposable
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(SimpleD2DForm));

        protected readonly object RenderContextLock = new object();

        protected SimpleD2DForm()
        {
            Load += Window_OnLoaded;
            Resize += Window_OnResize;
            FormClosing += Window_OnFormClosing; ;
        }

        #region DX Properties

        protected D3D11.Device D3DDevice { get; private set; }

        protected D2D1.Factory D2DFactory { get; private set; }

        protected D2D1.WindowRenderTarget RenderTarget { get; private set; }

        protected D2D1.SolidColorBrush SolidColorBrush { get; private set; }

        protected DW.Factory DwFactory { get; private set; }

        protected D3D.DriverType[] DriverTypes => new[] { D3D.DriverType.Hardware, D3D.DriverType.Warp, D3D.DriverType.Reference };

        protected D3D.FeatureLevel[] FeatureLevels => new[] { D3D.FeatureLevel.Level_10_0 };

        #endregion

        public void ShowAndRunRenderLoop()
        {
            Show();
            RunRenderLoop();
        }

        public void RunRenderLoop() => RenderLoop.Run(this, OnRender);

        protected abstract void Draw(D2D1.RenderTarget renderTarget);

        protected virtual void InitializeDirectXResources()
        {
            var clientSize = ClientSize;

            foreach (var driverType in DriverTypes)
                try
                {
                    D3DDevice = new D3D11.Device(D3D.DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport, FeatureLevels);
                    break;
                }
                catch (Exception e)
                {
                    Logger.Warn("InitializeDirectXResources - device not supported", e, "deviceType", driverType);
                }

            if (D3DDevice == null) throw new NotSupportedException("no supported driver types");

            D2DFactory = new D2D1.Factory();

            RenderTarget = new D2D1.WindowRenderTarget(D2DFactory,
                new D2D1.RenderTargetProperties(new D2D1.PixelFormat(DXGI.Format.Unknown, D2D1.AlphaMode.Ignore)),
                new D2D1.HwndRenderTargetProperties
                {
                    Hwnd = Handle,
                    PixelSize = new Size2(clientSize.Width, clientSize.Height),
                    PresentOptions = D2D1.PresentOptions.Immediately,
                });

            SolidColorBrush = new D2D1.SolidColorBrush(RenderTarget, Color.White);

            DwFactory = new DW.Factory(DW.FactoryType.Shared);
        }

        protected virtual void ResizeRenderTarget()
        {
            var clientSize = ClientSize;
            RenderTarget.Resize(new Size2(clientSize.Width, clientSize.Height));
        }

        protected virtual void DisposeDirectXResources()
        {
            DwFactory?.Dispose();
            RenderTarget?.Dispose();
            D2DFactory?.Dispose();
            D3DDevice?.Dispose();
        }

        protected void OnRender()
        {
            if (RenderTarget?.IsDisposed ?? true) return;
            RenderTarget.BeginDraw();
            Draw(RenderTarget);
            RenderTarget.EndDraw();
        }

        private void Window_OnLoaded(object sender, EventArgs e) => InitializeDirectXResources();

        private void Window_OnResize(object sender, EventArgs e)
        {
            if (D3DDevice?.IsDisposed ?? true) return;
            ResizeRenderTarget();
        }

        private void Window_OnFormClosing(object sender, FormClosingEventArgs e) => DisposeDirectXResources();

    }
}
