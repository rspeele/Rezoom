using System;

namespace Data.Resumption.ADO
{
    public class Command
    {
        public Command(IFormattableString text, bool isQuery, bool mutation, bool idempotent)
        {
            Text = text;
            IsQuery = isQuery;
            Mutation = mutation;
            Idempotent = idempotent;
        }
        public bool Mutation { get; }
        public bool Idempotent { get; }
        public bool IsQuery { get; }
        public IFormattableString Text { get; }

        public static Command Query(FormattableString text, bool mutation = false, bool idempotent = true)
            => new Command
                ( new FormattableStringAdapter(text)
                , isQuery: true
                , mutation: mutation
                , idempotent: idempotent
                );
        public static Command Mutate(FormattableString text, bool idempotent = false)
            => new Command
                ( new FormattableStringAdapter(text)
                , isQuery: false
                , mutation: true
                , idempotent: idempotent
                );

        public static Command Query(IFormattableString text, bool mutation = false, bool idempotent = true)
            => new Command(text, isQuery: true, mutation: mutation, idempotent: idempotent);
        public static Command Mutate(IFormattableString text, bool idempotent = false)
            => new Command(text, isQuery: false, mutation: true, idempotent: idempotent);
    }
}