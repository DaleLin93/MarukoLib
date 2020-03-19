using System;
using System.IO;
using System.Windows;
using MarukoLib.Interop;
using Microsoft.Win32;
using SharpDX;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.WIC;
using Device = SharpDX.Direct3D11.Device;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using Resource = SharpDX.Direct3D11.Resource;

namespace MarukoLib.DirectX
{

    public abstract class DirectXComponent : Win32Control
    {

        private Device _device;

        public Device Device => _device;

        private SwapChain _swapChain;

        protected SwapChain SwapChain => _swapChain;

        private Texture2D _backBuffer;

        protected Texture2D BackBuffer => _backBuffer;

        protected int SurfaceWidth { get; private set; }

        protected int SurfaceHeight { get; private set; }

        public double DpiScale { get; private set; }

        public event EventHandler D3DInitialized;

        public event EventHandler D3DSizeStartChange;

        public event EventHandler D3DSizeChanged;

        protected virtual void InternalInitialize() {
            DpiScale = GetDpiScale();
            SurfaceWidth = (int)(ActualWidth < 0 ? 0 : Math.Ceiling(ActualWidth * DpiScale));
            SurfaceHeight = (int)(ActualHeight < 0 ? 0 : Math.Ceiling(ActualHeight * DpiScale));

            var swapChainDescription = new SwapChainDescription {
                OutputHandle = Hwnd,
                BufferCount = 2,
                Flags = SwapChainFlags.AllowModeSwitch,
                IsWindowed = true,
                ModeDescription = new ModeDescription(SurfaceWidth, SurfaceHeight, new Rational(60, 1), Format.B8G8R8A8_UNorm),
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput | Usage.Shared
            };

            FeatureLevel[] featureLevels = null;

            if (VersionHelper.IsWindows10OrGreater()) {
                featureLevels = new[]
                {
                    FeatureLevel.Level_12_1,
                    FeatureLevel.Level_12_0,
                    FeatureLevel.Level_11_1,
                    FeatureLevel.Level_11_0,
                    FeatureLevel.Level_10_1,
                    FeatureLevel.Level_10_0,
                    FeatureLevel.Level_9_3,
                    FeatureLevel.Level_9_2,
                    FeatureLevel.Level_9_1
                };
            } else if (VersionHelper.IsWindows7SP1OrGreater()) {
                featureLevels = new[]
                {
                    FeatureLevel.Level_11_1,
                    FeatureLevel.Level_11_0,
                    FeatureLevel.Level_10_1,
                    FeatureLevel.Level_10_0,
                    FeatureLevel.Level_9_3,
                    FeatureLevel.Level_9_2,
                    FeatureLevel.Level_9_1
                };
            }

            try {
                Device.CreateWithSwapChain(DriverType.Hardware,
                    DeviceCreationFlags.BgraSupport, featureLevels, swapChainDescription,
                    out _device, out _swapChain);
            } catch (Exception e) {
                MessageBox.Show(e.ToString());
            }

            using (var factory = _swapChain.GetParent<Factory>()) {
                factory.MakeWindowAssociation(Hwnd, WindowAssociationFlags.IgnoreAll);
            }

            _backBuffer = _swapChain.GetBackBuffer<Texture2D>(0);

            Console.WriteLine(SupportLevel);

            //backBuffer = D3D11.Resource.FromSwapChain<D3D11.Texture2D>(swapChain, 0);
            //renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
        }

        protected virtual void InternalUninitialize() {
            //Utilities.Dispose(ref renderTargetView);
            Utilities.Dispose(ref _backBuffer);
            Utilities.Dispose(ref _swapChain);

            // This is a workaround for an issue in SharpDx3.0.2 (https://github.com/sharpdx/SharpDX/issues/731)
            // Will need to be removed when fixed in next SharpDx release
            //((IUnknown)device).Release();

            Utilities.Dispose(ref _device);
            //GC.Collect(2, GCCollectionMode.Forced);
        }

        protected sealed override void Initialize() {
            InternalInitialize();
            D3DInitialized?.Invoke(this, new EventArgs());
        }

        protected sealed override void UnInitialize() {
            InternalUninitialize();
        }

        protected override void Resized() {
            D3DSizeStartChange?.Invoke(this, new EventArgs());
            BeforeResize();
            try {
                _swapChain.ResizeBuffers(0, 0, 0, Format.Unknown, SwapChainFlags.None);
            } catch (SharpDXException e) {
                MessageBox.Show(e.ToString());
            }
            AfterResize();
            D3DSizeChanged?.Invoke(this, new EventArgs());
        }

        protected virtual void BeforeResize() {
            //Utilities.Dispose(ref renderTargetView);
            Utilities.Dispose(ref _backBuffer);
        }

        protected virtual void AfterResize() {
            _backBuffer = _swapChain.GetBackBuffer<Texture2D>(0);
            //renderTargetView = new D3D11.RenderTargetView(device, backBuffer);
        }

        protected virtual void BeginRender() {
            _device.ImmediateContext.Rasterizer.SetViewport(0, 0, (float)ActualWidth, (float)ActualHeight);
            //device.ImmediateContext.OutputMerger.SetRenderTargets(renderTargetView);
        }

        protected virtual void EndRender() {
            _swapChain.Present(1, PresentFlags.None);
        }

        protected abstract void Render();

        public void SaveImage() {
            var saveFileDialog = new SaveFileDialog {
                DefaultExt = ".png",
                Filter = "PNG|*.png|JPEG|*.jpg",
                FileName = Application.Current.FindResource("Undefined") as string ?? "",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)
            };

            if (saveFileDialog.ShowDialog() == true) {
                SaveImage(saveFileDialog.FileName);
            }
        }

        public void SaveImage(string filename) {
            Guid format;

            var ext = Path.GetExtension(filename);

            switch (ext) {
                case ".png":
                    format = ContainerFormatGuids.Png;
                    break;
                case ".jpg":
                case ".jpeg":
                    format = ContainerFormatGuids.Jpeg;
                    break;
                case ".tiff":
                    format = ContainerFormatGuids.Tiff;
                    break;
                default:
                    format = ContainerFormatGuids.Png;
                    break;
            }

            //SaveWICTextureToFile(Device.ImmediateContext, BackBuffer, ref format, filename);
            SaveWicTextureToFileFix(Device.ImmediateContext, BackBuffer, ref format, filename);
        }

        public string SupportLevel {
            get {
                switch (_device.FeatureLevel) {
                    case FeatureLevel.Level_12_1:
                    case FeatureLevel.Level_12_0:
                    case FeatureLevel.Level_11_1:
                    case FeatureLevel.Level_11_0:
                    case FeatureLevel.Level_10_1:
                    case FeatureLevel.Level_10_0:
                    case FeatureLevel.Level_9_3:
                    case FeatureLevel.Level_9_2:
                    case FeatureLevel.Level_9_1:
                        return _device.FeatureLevel.ToString().Replace("Level_", "DirectX ").Replace('_', '.');
                    default:
                        return "DirectX Not Support";
                }
            }
        }

        private void SaveWicTextureToFile(
                                       DeviceContext context,
                                       Texture2D source,
                                       ref Guid guidContainerFormat,
                                       string fileName) {
            var staging = CaptureTexture(context, source, out var desc);

            if (staging == null) return;

            var fs = new FileStream(fileName, FileMode.Create);
            var encoder = new BitmapEncoder(Direct2D.ImageFactory, guidContainerFormat);
            encoder.Initialize(fs);

            var frameEncode = new BitmapFrameEncode(encoder);
            //IPropertyBag2

            frameEncode.Initialize();
            frameEncode.SetSize(desc.Width, desc.Height);
            frameEncode.SetResolution(72.0, 72.0);
            Guid pfGuid;
            Guid targetGuid;

            //bool sRGB = false;
            switch (desc.Format) {
                case Format.R32G32B32A32_Float: pfGuid = PixelFormat.Format128bppRGBAFloat; break;
                case Format.R16G16B16A16_Float: pfGuid = PixelFormat.Format64bppRGBAHalf; break;
                case Format.R16G16B16A16_UNorm: pfGuid = PixelFormat.Format64bppRGBA; break;
                case Format.R10G10B10_Xr_Bias_A2_UNorm: pfGuid = PixelFormat.Format32bppRGBA1010102XR; break; // DXGI 1.1
                case Format.R10G10B10A2_UNorm: pfGuid = PixelFormat.Format32bppRGBA1010102; break;
                case Format.B5G5R5A1_UNorm: pfGuid = PixelFormat.Format16bppBGRA5551; break;
                case Format.B5G6R5_UNorm: pfGuid = PixelFormat.Format16bppBGR565; break;
                case Format.R32_Float: pfGuid = PixelFormat.Format32bppGrayFloat; break;
                case Format.R16_Float: pfGuid = PixelFormat.Format16bppGrayHalf; break;
                case Format.R16_UNorm: pfGuid = PixelFormat.Format16bppGray; break;
                case Format.R8_UNorm: pfGuid = PixelFormat.Format8bppGray; break;
                case Format.A8_UNorm: pfGuid = PixelFormat.Format8bppAlpha; break;

                case Format.R8G8B8A8_UNorm:
                    pfGuid = PixelFormat.Format32bppRGBA;
                    break;

                case Format.R8G8B8A8_UNorm_SRgb:
                    pfGuid = PixelFormat.Format32bppRGBA;
                    //sRGB = true;
                    break;

                case Format.B8G8R8A8_UNorm: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGRA;
                    break;

                case Format.B8G8R8A8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGRA;
                    //sRGB = true;
                    break;

                case Format.B8G8R8X8_UNorm: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGR;
                    break;

                case Format.B8G8R8X8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGR;
                    //sRGB = true;
                    break;

                default:
                    return;
            }

            switch (desc.Format) {
                case Format.R32G32B32A32_Float:
                case Format.R16G16B16A16_Float:
                    targetGuid = PixelFormat.Format24bppBGR;
                    break;
                case Format.R16G16B16A16_UNorm: targetGuid = PixelFormat.Format48bppBGR; break;
                case Format.B5G5R5A1_UNorm: targetGuid = PixelFormat.Format16bppBGR555; break;
                case Format.B5G6R5_UNorm: targetGuid = PixelFormat.Format16bppBGR565; break;

                case Format.R32_Float:
                case Format.R16_Float:
                case Format.R16_UNorm:
                case Format.R8_UNorm:
                case Format.A8_UNorm:
                    targetGuid = PixelFormat.Format8bppGray;
                    break;

                default:
                    targetGuid = PixelFormat.Format24bppBGR;
                    break;
            }

            frameEncode.SetPixelFormat(ref targetGuid);

            #region Write

            var db = context.MapSubresource(staging, 0, MapMode.Read, MapFlags.None, out _);

            if (pfGuid != targetGuid) {
                var formatConverter = new FormatConverter(Direct2D.ImageFactory);
                if (formatConverter.CanConvert(pfGuid, targetGuid)) {
                    var src = new Bitmap(Direct2D.ImageFactory, desc.Width, desc.Height, pfGuid,
                        new DataRectangle(db.DataPointer, db.RowPitch));
                    formatConverter.Initialize(src, targetGuid, BitmapDitherType.None, null, 0, BitmapPaletteType.Custom);
                    frameEncode.WriteSource(formatConverter, new Rectangle(0, 0, desc.Width, desc.Height));
                }
            } else {
                frameEncode.WritePixels(desc.Height, new DataRectangle(db.DataPointer, db.RowPitch));
            }

            context.UnmapSubresource(staging, 0);

            frameEncode.Commit();
            encoder.Commit();

            #endregion

            frameEncode.Dispose();
            encoder.Dispose();

            fs.Close();

        }

        private Result SaveWicTextureToFileFix(
                                       DeviceContext context,
                                       Texture2D source,
                                       ref Guid guidContainerFormat,
                                       string fileName) {
            if (fileName == null)
                return Result.InvalidArg;

            var res = CaptureTextureFix(context, source, out var desc, out var staging);
            if (res.Failure) return res;

            Guid pfGuid;
            //bool sRGB = false;
            Guid targetGuid;

            switch (desc.Format) {
                case Format.R32G32B32A32_Float: pfGuid = PixelFormat.Format128bppRGBAFloat; break;
                case Format.R16G16B16A16_Float: pfGuid = PixelFormat.Format64bppRGBAHalf; break;
                case Format.R16G16B16A16_UNorm: pfGuid = PixelFormat.Format64bppRGBA; break;
                case Format.R10G10B10_Xr_Bias_A2_UNorm: pfGuid = PixelFormat.Format32bppRGBA1010102XR; break; // DXGI 1.1
                case Format.R10G10B10A2_UNorm: pfGuid = PixelFormat.Format32bppRGBA1010102; break;
                case Format.B5G5R5A1_UNorm: pfGuid = PixelFormat.Format16bppBGRA5551; break;
                case Format.B5G6R5_UNorm: pfGuid = PixelFormat.Format16bppBGR565; break;
                case Format.R32_Float: pfGuid = PixelFormat.Format32bppGrayFloat; break;
                case Format.R16_Float: pfGuid = PixelFormat.Format16bppGrayHalf; break;
                case Format.R16_UNorm: pfGuid = PixelFormat.Format16bppGray; break;
                case Format.R8_UNorm: pfGuid = PixelFormat.Format8bppGray; break;
                case Format.A8_UNorm: pfGuid = PixelFormat.Format8bppAlpha; break;

                case Format.R8G8B8A8_UNorm:
                    pfGuid = PixelFormat.Format32bppRGBA;
                    break;

                case Format.R8G8B8A8_UNorm_SRgb:
                    pfGuid = PixelFormat.Format32bppRGBA;
                    //sRGB = true;
                    break;

                case Format.B8G8R8A8_UNorm: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGRA;
                    break;

                case Format.B8G8R8A8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGRA;
                    //sRGB = true;
                    break;

                case Format.B8G8R8X8_UNorm: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGR;
                    break;

                case Format.B8G8R8X8_UNorm_SRgb: // DXGI 1.1
                    pfGuid = PixelFormat.Format32bppBGR;
                    //sRGB = true;
                    break;

                default:
                    return Result.GetResultFromWin32Error(unchecked((int)0x80070032));
            }

            // Create file
            var fs = new FileStream(fileName, FileMode.Create);
            var encoder = new BitmapEncoder(Direct2D.ImageFactory, guidContainerFormat);
            encoder.Initialize(fs);


            var frameEncode = new BitmapFrameEncode(encoder);
            frameEncode.Initialize();
            frameEncode.SetSize(desc.Width, desc.Height);
            frameEncode.SetResolution(72.0, 72.0);


            switch (desc.Format) {
                case Format.R32G32B32A32_Float:
                case Format.R16G16B16A16_Float:
                    targetGuid = PixelFormat.Format24bppBGR;
                    break;
                case Format.R16G16B16A16_UNorm: targetGuid = PixelFormat.Format48bppBGR; break;
                case Format.B5G5R5A1_UNorm: targetGuid = PixelFormat.Format16bppBGR555; break;
                case Format.B5G6R5_UNorm: targetGuid = PixelFormat.Format16bppBGR565; break;

                case Format.R32_Float:
                case Format.R16_Float:
                case Format.R16_UNorm:
                case Format.R8_UNorm:
                case Format.A8_UNorm:
                    targetGuid = PixelFormat.Format8bppGray;
                    break;

                default:
                    targetGuid = PixelFormat.Format24bppBGR;
                    break;
            }

            frameEncode.SetPixelFormat(ref targetGuid);

            #region Write

            var db = context.MapSubresource(staging, 0, MapMode.Read, MapFlags.None, out _);

            if (pfGuid != targetGuid) {
                var formatConverter = new FormatConverter(Direct2D.ImageFactory);

                if (formatConverter.CanConvert(pfGuid, targetGuid)) {
                    var src = new Bitmap(Direct2D.ImageFactory, desc.Width, desc.Height, pfGuid,
                        new DataRectangle(db.DataPointer, db.RowPitch));
                    formatConverter.Initialize(src, targetGuid, BitmapDitherType.None, null, 0, BitmapPaletteType.Custom);
                    frameEncode.WriteSource(formatConverter, new Rectangle(0, 0, desc.Width, desc.Height));
                }

            } else {
                frameEncode.WritePixels(desc.Height, new DataRectangle(db.DataPointer, db.RowPitch));
            }

            context.UnmapSubresource(staging, 0);

            frameEncode.Commit();
            encoder.Commit();

            #endregion

            frameEncode.Dispose();
            encoder.Dispose();

            fs.Close();

            return Result.Ok;
        }

        private Texture2D CaptureTexture(DeviceContext deviceContext, Texture2D source, out Texture2DDescription desc) {
            var d3dDevice = deviceContext.Device;
            // debug: i got it!
            //D3D11.Texture2D texture = source.QueryInterface<D3D11.Texture2D>();
            Texture2D staging;

            desc = source.Description;

            if (desc.SampleDescription.Count > 1) {
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;

                var temp = new Texture2D(d3dDevice, desc);

                var fmt = EnsureNotTypeless(desc.Format);

                var support = d3dDevice.CheckFormatSupport(fmt);

                if ((support & FormatSupport.MultisampleResolve) == 0) return null;

                for (var item = 0; item < desc.ArraySize; ++item) {
                    for (var level = 0; level < desc.MipLevels; ++level) {
                        var index = Resource.CalculateSubResourceIndex(level, item, desc.MipLevels);
                        deviceContext.ResolveSubresource(temp, index, source, index, fmt);
                    }
                }

                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;

                staging = new Texture2D(d3dDevice, desc);
                deviceContext.CopyResource(temp, staging);
            } else if (desc.Usage == ResourceUsage.Staging &&
                      desc.CpuAccessFlags == CpuAccessFlags.Read) {
                staging = source;
            } else {
                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;
                staging = new Texture2D(d3dDevice, desc);
                deviceContext.CopyResource(source, staging);
            }

            return staging;
        }

        private Result CaptureTextureFix(DeviceContext deviceContext,
            Texture2D source,
            out Texture2DDescription desc,
            out Texture2D staging) {
            desc = new Texture2DDescription();
            staging = null;

            if (deviceContext == null || source == null)
                return Result.InvalidArg;

            var resType = source.Dimension;

            if (resType != ResourceDimension.Texture2D) {
                //string message = SharpDX.Diagnostics.ErrorManager.GetErrorMessage(0);
                //return Result.GetResultFromWin32Error(ERROR_NOT_SUPPORTED)
            }

            desc = source.Description;

            var d3dDevice = deviceContext.Device;
            //Texture2D staging = null;

            if (desc.SampleDescription.Count > 1) {
                desc.SampleDescription.Count = 1;
                desc.SampleDescription.Quality = 0;

                Texture2D temp;

                try {
                    temp = new Texture2D(d3dDevice, desc);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }

                var fmt = EnsureNotTypeless(desc.Format);

                FormatSupport support;
                try {
                    support = d3dDevice.CheckFormatSupport(fmt);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }

                if ((support & FormatSupport.MultisampleResolve) == 0)
                    return Result.Fail;

                for (var item = 0; item < desc.ArraySize; ++item) {
                    for (var level = 0; level < desc.MipLevels; ++level) {
                        var index = Resource.CalculateSubResourceIndex(level, item, desc.MipLevels);
                        deviceContext.ResolveSubresource(temp, index, source, index, fmt);
                    }
                }

                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;

                try {
                    staging = new Texture2D(d3dDevice, desc);
                    deviceContext.CopyResource(temp, staging);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }
            } else if (desc.Usage == ResourceUsage.Staging && desc.CpuAccessFlags == CpuAccessFlags.Read) {
                staging = source;
            } else {
                desc.BindFlags = 0;
                desc.OptionFlags &= ResourceOptionFlags.TextureCube;
                desc.CpuAccessFlags = CpuAccessFlags.Read;
                desc.Usage = ResourceUsage.Staging;

                try {
                    staging = new Texture2D(d3dDevice, desc);
                    deviceContext.CopyResource(source, staging);
                } catch (SharpDXException e) {
                    return e.ResultCode;
                }
            }

            return Result.Ok;
        }


        private Format EnsureNotTypeless(Format format) {
            switch (format) {
                case Format.R32G32B32A32_Typeless: return Format.R32G32B32A32_Float;
                case Format.R32G32B32_Typeless: return Format.R32G32B32_Float;
                case Format.R16G16B16A16_Typeless: return Format.R16G16B16A16_UNorm;
                case Format.R32G32_Typeless: return Format.R32G32_Float;
                case Format.R10G10B10A2_Typeless: return Format.R10G10B10A2_UNorm;
                case Format.R8G8B8A8_Typeless: return Format.R8G8B8A8_UNorm;
                case Format.R16G16_Typeless: return Format.R16G16_UNorm;
                case Format.R32_Typeless: return Format.R32_Float;
                case Format.R8G8_Typeless: return Format.R8G8_UNorm;
                case Format.R16_Typeless: return Format.R16_UNorm;
                case Format.R8_Typeless: return Format.R8_UNorm;
                case Format.BC1_Typeless: return Format.BC1_UNorm;
                case Format.BC2_Typeless: return Format.BC2_UNorm;
                case Format.BC3_Typeless: return Format.BC3_UNorm;
                case Format.BC4_Typeless: return Format.BC4_UNorm;
                case Format.BC5_Typeless: return Format.BC5_UNorm;
                case Format.B8G8R8A8_Typeless: return Format.B8G8R8A8_UNorm;
                case Format.B8G8R8X8_Typeless: return Format.B8G8R8X8_UNorm;
                case Format.BC7_Typeless: return Format.BC7_UNorm;
                default: return format;
            }
        }

        private double GetDpiScale() {
            var source = PresentationSource.FromVisual(this);
            return source?.CompositionTarget?.TransformToDevice.M11 ?? 1;
        }

        public double Dpi => 96.0 * GetDpiScale();

        internal class HResults {
            // ReSharper disable InconsistentNaming
            public const int D2DERR_RECREATE_TARGET = unchecked((int)0x8899000C);
            public const int DXGI_ERROR_DEVICE_REMOVED = unchecked((int)0x887A0005);
            // ReSharper restore InconsistentNaming
        }

    }
}
