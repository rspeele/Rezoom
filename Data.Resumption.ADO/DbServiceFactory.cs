using System.Data.Common;
using Data.Resumption.Services;

namespace Data.Resumption.ADO
{
    public abstract class DbServiceFactory : IServiceFactory
    {
        protected abstract DbConnection CreateConnection();
        protected virtual IDbTypeRecognizer CreateDbTypeRecognizer() => new DbTypeRecognizer();

        public LivingService<T>? CreateService<T>(IServiceContext context)
        {
            if (typeof(T) == typeof(DbConnection))
            {
                var conn = CreateConnection();
                return new LivingService<T>((T)(object)conn, ServiceLifetime.ExecutionContext);
            }
            if (typeof(T) == typeof(IDbTypeRecognizer))
            {
                var recognizer = CreateDbTypeRecognizer();
                return new LivingService<T>((T)recognizer, ServiceLifetime.ExecutionContext);
            }
            if (typeof(T) == typeof(CommandBatch))
            {
                var dbConnection = context.GetService<DbConnection>();
                var dbTypeRecognizer = context.GetService<IDbTypeRecognizer>();
                var cmdContext = new CommandBatch(dbConnection, dbTypeRecognizer);
                return new LivingService<T>((T)(object)cmdContext, ServiceLifetime.ExecutionStep);
            }
            return null;
        }
    }
}
