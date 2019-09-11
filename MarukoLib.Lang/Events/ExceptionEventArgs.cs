namespace MarukoLib.Lang.Events
{

    public class ExceptionEventArgs : System.EventArgs
    {

        public ExceptionEventArgs(object exception) => ExceptionObject = exception;

        public object ExceptionObject { get; }

        public bool Handled { get; set; }

    }

}
