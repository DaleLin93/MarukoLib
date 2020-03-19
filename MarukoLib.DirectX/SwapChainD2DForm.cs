using System;
using System.Windows.Forms;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Windows;
using DW = SharpDX.DirectWrite;
using D2D1 = SharpDX.Direct2D1;
using D3D = SharpDX.Direct3D;
using D3D11 = SharpDX.Direct3D11;

namespace MarukoLib.DirectX
{

    public abstract class SwapChainD2DForm : RenderForm, IDisposable
    {

        protected readonly object RenderContextLock = new object();

        protected SwapChainD2DForm()
        {
            Load += Window_OnLoaded;
            Resize += Window_OnResize;
        }

        #region DX Properties

        protected PresentParameters PresentParameters { get; } = new PresentParameters();

        protected D3D11.Device D3DDevice { get; private set; }

        protected D3D11.DeviceContext D3DDeviceContext { get; private set; }

        protected SwapChain1 SwapChain { get; private set; }

        protected D3D11.RenderTargetView RenderTargetView { get; private set; }

        protected D2D1.Factory D2DFactory { get; private set; }

        protected D2D1.RenderTarget RenderTarget { get; private set; }

        protected D2D1.SolidColorBrush SolidColorBrush { get; private set; }

        protected DW.Factory DwFactory { get; private set; }

        #endregion

        public new void Show()
        {
            ((Control) this).Show();
            RenderLoop.Run(this, OnRender);
        }

        public new void Dispose()
        {
            lock (RenderContextLock)
                DisposeDirectXResources();
            base.Dispose();
        }

        protected abstract void Draw(D2D1.RenderTarget renderTarget);

        protected virtual void InitializeDirectXResources()
        {
            var clientSize = ClientSize;
            var backBufferDesc = new ModeDescription(clientSize.Width, clientSize.Height,
                new Rational(60, 1), Format.R8G8B8A8_UNorm);

            var swapChainDesc = new SwapChainDescription
            {
                ModeDescription = backBufferDesc,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.RenderTargetOutput,
                BufferCount = 1,
                OutputHandle = Handle,
                SwapEffect = SwapEffect.Discard,
                IsWindowed = !IsFullscreen
            };

            D3D11.Device.CreateWithSwapChain(D3D.DriverType.Hardware, D3D11.DeviceCreationFlags.BgraSupport,
                new[] { D3D.FeatureLevel.Level_10_0 }, swapChainDesc,
                out var device, out var swapChain);
            D3DDevice = device;
            D3DDeviceContext = D3DDevice.ImmediateContext;

            SwapChain = new SwapChain1(swapChain.NativePointer);

            D2DFactory = new D2D1.Factory();
            using (var backBuffer = SwapChain.GetBackBuffer<D3D11.Texture2D>(0))
            {
                RenderTargetView = new D3D11.RenderTargetView(D3DDevice, backBuffer);
                RenderTarget = new D2D1.RenderTarget(D2DFactory, backBuffer.QueryInterface<Surface>(),
                        new D2D1.RenderTargetProperties(new D2D1.PixelFormat(Format.Unknown, D2D1.AlphaMode.Premultiplied)))
                    {TextAntialiasMode = D2D1.TextAntialiasMode.Cleartype};
            }
            DwFactory = new DW.Factory(DW.FactoryType.Shared);

            SolidColorBrush = new D2D1.SolidColorBrush(RenderTarget, Color.White);
        }

        protected virtual void DisposeDirectXResources()
        {
            DwFactory.Dispose();
            RenderTarget.Dispose();
            RenderTargetView.Dispose();
            D2DFactory.Dispose();
            SwapChain.Dispose();
            D3DDeviceContext.Dispose();
            D3DDevice.Dispose();
        }

        protected void OnRender()
        {
            lock (RenderContextLock)
            {
                if (RenderTarget?.IsDisposed ?? true) return;
                RenderTarget.BeginDraw();
                Draw(RenderTarget);
                RenderTarget.EndDraw();
                SwapChain.Present(1, PresentFlags.None, PresentParameters);
            }
        }

        private void Window_OnLoaded(object sender, EventArgs e)
        {
            lock (RenderContextLock)
                InitializeDirectXResources();
        }

        private void Window_OnResize(object sender, EventArgs e)
        {
            lock (RenderContextLock)
            {
                if (D3DDeviceContext?.IsDisposed ?? true)
                    return;
                DisposeDirectXResources();
                InitializeDirectXResources();
            }
        }

    }
}
