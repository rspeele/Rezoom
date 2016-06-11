namespace Data.Resumption.ADO
{
    public interface IFormattableString
    {
        string Format { get; }
        object[] Arguments { get; }
    }
}