using SharpDX;
using D2D = SharpDX.Direct2D1;
using DXGI = SharpDX.DXGI;

namespace MarukoLib.DirectX
{

    public class Direct2DComponent : DirectXComponent
    {

        private readonly object _lock = new object();

        private D2D.RenderTarget _renderTarget;

        //private D2D.Bitmap1 bitmap;

        protected D2D.RenderTarget RenderTarget => _renderTarget;

        //private D2D.Device d2dDevice;

        //private D2D.DeviceContext deviceContext;

        protected sealed override void InternalInitialize()
        {
            base.InternalInitialize();
            lock (_lock)
                CreateResource();
        }

        protected sealed override void InternalUninitialize()
        {
            lock (_lock)
                ReleaseResource();
            base.InternalUninitialize();
        }

        protected virtual void CreateResource()
        {
            //***
            using (var surface = BackBuffer.QueryInterface<DXGI.Surface>())
            {
                _renderTarget = new D2D.RenderTarget(global::MarukoLib.DirectX.Direct2D.Factory2D, surface, new D2D.RenderTargetProperties()
                {
                    PixelFormat = new D2D.PixelFormat(DXGI.Format.Unknown, D2D.AlphaMode.Premultiplied),
                });
            }
            _renderTarget.AntialiasMode = D2D.AntialiasMode.PerPrimitive;
            
            /***/

            /***
            using (var surface = BackBuffer.QueryInterface<DXGI.Surface>())
            {
                deviceContext = new D2D.DeviceContext(surface, new SharpDX.Direct2D1.CreationProperties()
                {
                    DebugLevel = SharpDX.Direct2D1.DebugLevel.Information,
                    Options = SharpDX.Direct2D1.DeviceContextOptions.None,
                    ThreadingMode = SharpDX.Direct2D1.ThreadingMode.SingleThreaded
                }
                );
                bitmap = new SharpDX.Direct2D1.Bitmap1(deviceContext, surface, new SharpDX.Direct2D1.BitmapProperties1()
                {
                    BitmapOptions = SharpDX.Direct2D1.BitmapOptions.Target | SharpDX.Direct2D1.BitmapOptions.CannotDraw,
                    PixelFormat = new SharpDX.Direct2D1.PixelFormat(DXGI.Format.B8G8R8A8_UNorm, SharpDX.Direct2D1.AlphaMode.Ignore),
                    DpiX = (float)Dpi,
                    DpiY = (float)Dpi
                }
                );
                deviceContext.Target = bitmap;
            }
            
            deviceContext.AntialiasMode = D2D.AntialiasMode.PerPrimitive;
            /***/
        }

        protected virtual void ReleaseResource()
        {
            //Utilities.Dispose(ref bitmap);
            //Utilities.Dispose(ref deviceContext);
            Utilities.Dispose(ref _renderTarget);
        }

        protected override void BeforeResize()
        {
            lock (_lock)
                ReleaseResource();
            base.BeforeResize();
        }

        protected override void AfterResize()
        {
            base.AfterResize();
            lock (_lock)
                CreateResource();
        }

        protected override void Render()
        {
            RenderTarget.Clear(Color.Black);
        }

        public virtual void Draw()
        {
            lock (_lock)
            {
                if (_renderTarget != null)
                    try
                    {
                        BeginRender();
                        Render();
                        EndRender();
                    }
                    catch (SharpDXException e)
                    {
                        System.Windows.MessageBox.Show(e.ToString());
                    }
            }
        }
    }
}
