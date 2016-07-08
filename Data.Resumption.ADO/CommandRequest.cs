using System;
using System.Threading.Tasks;
using Data.Resumption.DataRequests;
using Data.Resumption.Services;

namespace Data.Resumption.ADO
{
    public class CommandRequest : DataRequest<CommandResponse>
    {
        private readonly Command _command;

        public CommandRequest(Command command)
        {
            _command = command;
        }

        public override object Identity => FormattableString.Invariant(_command.Text);
        public override object DataSource => typeof(CommandContext);
        public override bool Mutation => _command.Mutation;
        public override bool Idempotent => _command.Idempotent;
        public override object SequenceGroup => typeof(CommandContext);

        public override Func<Task<CommandResponse>> Prepare(IServiceContext context)
            => context.GetService<CommandContext>().Prepare(_command);
    }
}
