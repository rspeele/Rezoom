using System.Data;

namespace Data.Resumption.ADO
{
    public interface IDbTypeRecognizer
    {
        DbType GetDbType(object value);
    }
}