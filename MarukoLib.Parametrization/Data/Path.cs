using System.IO;
using MarukoLib.Lang;
using MarukoLib.Parametrization.Presenters;
using Newtonsoft.Json;

namespace MarukoLib.Parametrization.Data
{

    [Presenter(typeof(PathPresenter))]
    public struct Path
    {

        private const string PathKey = "Path";

        public static readonly ITypeConverter<Path, string> TypeConverter = TypeConverter<Path, string>.Of(path => path.Value, path => new Path(path));

        [JsonConstructor]
        public Path([JsonProperty(PathKey)] string path) => Value = path;

        [JsonProperty(PathKey)]
        public string Value { get; set; }

        public bool Exists => Value != null && (File.Exists(Value) || Directory.Exists(Value));

    }

}
