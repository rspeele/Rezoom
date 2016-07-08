using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Resumption.ADO
{
    /// <summary>
    /// Builds up a batch of SQL strings to run in a single IDbCommand.
    /// </summary>
    internal class CommandContext : IDisposable
    {
        /// <summary>
        /// We put this after each command to terminate it.
        /// The extra characters are intended to guard against accidental issues like
        /// unclosed string literals or block comments spilling into the next command.
        /// </summary>
        private const string CommandTerminator = ";--'*/;";
        private readonly IDbTypeRecognizer _typeRecognizer;
        private readonly DbCommand _command;
        private readonly StringBuilder _sqlCommands = new StringBuilder();
        private int _queryCount = 0;
        private Task<List<CommandResponse>> _executing = null;

        public CommandContext(DbConnection connection, IDbTypeRecognizer typeRecognizer)
        {
            _typeRecognizer = typeRecognizer;
            _command = connection.CreateCommand();
            _command.Connection = connection;
        }

        public Func<Task<CommandResponse>> Prepare(Command command)
        {
            if (_executing != null)
                throw new InvalidOperationException("Command is already executing");
            var parameterValues = command.Text.GetArguments();
            var parameterNames = new object[parameterValues.Length];
            for (var i = 0; i < parameterValues.Length; i++)
            {
                var dbParamName = $"@__DRBATCHPARAM{_command.Parameters.Count}";
                var dbParam = _command.CreateParameter();
                dbParam.ParameterName = dbParamName;
                dbParam.Value = parameterValues[i];
                dbParam.DbType = _typeRecognizer.GetDbType(parameterValues[i]);
                _command.Parameters.Add(dbParam);
                parameterNames[i] = dbParamName;
            }
            var sqlReferencingParams = string.Format(command.Text.Format, parameterNames);
            _sqlCommands.AppendLine(sqlReferencingParams);
            _sqlCommands.AppendLine(CommandTerminator);
            var resultSetIndex = _queryCount++;
            return () => GetResultSet(resultSetIndex);
        }

        private async Task<List<CommandResponse>> GetResultSets()
        {
            _command.CommandText = _sqlCommands.ToString();
            using (var reader = await _command.ExecuteReaderAsync())
            {
                var results = new List<CommandResponse>();
                do
                {
                    var fieldNames = Enumerable.Range(0, reader.FieldCount)
                        .Select(reader.GetName)
                        .ToArray();
                    var rows = new List<IReadOnlyList<object>>();
                    while (await reader.ReadAsync())
                    {
                        var row = new object[reader.FieldCount];
                        reader.GetValues(row);
                        rows.Add(row);
                    }
                    results.Add(new CommandResponse(fieldNames, rows));
                } while (await reader.NextResultAsync());
                return results;
            }
        }

        private async Task<CommandResponse> GetResultSet(int index)
        {
            if (_executing != null)
            {
                var allResults = await _executing;
                return allResults[index];
            }
            _executing = GetResultSets();
            var results = await _executing;
            return results[index];
        }

        public void Dispose() => _command.Dispose();
    }
}