using System;
using D2D = SharpDX.Direct2D1;
using WIC = SharpDX.WIC;
using DWrite = SharpDX.DirectWrite;
using Utilities = SharpDX.Utilities;

namespace MarukoLib.DirectX
{

    public static class DirectX
    {

        private static D2D.Factory _d2dFactory;

        public static D2D.Factory Factory2D
        {
            set => _d2dFactory = value;
            get => _d2dFactory;
        }

        private static WIC.ImagingFactory _imageFactory;

        public static WIC.ImagingFactory ImageFactory => _imageFactory;

        private static DWrite.Factory _writeFactory;

        public static DWrite.Factory WriteFactory => _writeFactory;

        public static void CreateIndependentResource()
        {
            Factory2D = new D2D.Factory();
            _imageFactory = new WIC.ImagingFactory();
            _writeFactory = new DWrite.Factory();
        }

        public static void ReleaseIndependentResource()
        {
            Utilities.Dispose(ref _imageFactory);
            Utilities.Dispose(ref _writeFactory);
            Utilities.Dispose(ref _d2dFactory);
        }

        public static void EnumAdapter()
        {
            foreach (var a in new SharpDX.DXGI.Factory4().Adapters)
                Console.WriteLine($"{a.Description.Description}");
        }

    }
}
