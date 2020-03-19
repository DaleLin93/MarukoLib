using System;
using SharpDX;
using SharpDX.DXGI;
using SharpDX.WIC;
using D2D = SharpDX.Direct2D1;
using DWrite = SharpDX.DirectWrite;

namespace MarukoLib.DirectX
{

    public static class Direct2D
    {

        private static D2D.Factory _d2dFactory;

        public static D2D.Factory Factory2D
        {
            set => _d2dFactory = value;
            get => _d2dFactory;
        }

        private static ImagingFactory _imageFactory;

        public static ImagingFactory ImageFactory => _imageFactory;

        private static DWrite.Factory _writeFactory;

        public static DWrite.Factory WriteFactory => _writeFactory;

        public static void CreateIndependentResource()
        {
            Factory2D = new D2D.Factory();
            _imageFactory = new ImagingFactory();
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
            foreach (var a in new Factory4().Adapters)
                Console.WriteLine($"{a.Description.Description}");
        }

    }
}
