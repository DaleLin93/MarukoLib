using System.Diagnostics.CodeAnalysis;
using System.Net;
using JetBrains.Annotations;
using MarukoLib.Parametrization.Presenters;

namespace MarukoLib.Parametrization.Data
{

    [Presenter(typeof(EndpointPresenter))]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public struct Endpoint
    {

        public string Address;

        public int Port;

        public Endpoint([CanBeNull] IPEndPoint endPoint) : this(endPoint?.Address, endPoint?.Port ?? 0) { }

        public Endpoint([CanBeNull] IPAddress address, int port = 0) : this(address?.ToString(), port) { }

        public Endpoint([CanBeNull] string address, int port = 0)
        {
            Address = address;
            Port = port;
        }

        public static Endpoint Loopback(int port) => new Endpoint(IPAddress.Loopback, port);

        public IPAddress ToIPAddress() => IPAddress.Parse(Address);

        public IPEndPoint ToIPEndPoint() => new IPEndPoint(ToIPAddress(), Port);

    }

}
