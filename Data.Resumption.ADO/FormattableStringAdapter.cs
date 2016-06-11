using System;

namespace Data.Resumption.ADO
{
    internal class FormattableStringAdapter : IFormattableString
    {
        private readonly FormattableString _formattableString;

        public FormattableStringAdapter(FormattableString formattableString)
        {
            _formattableString = formattableString;
        }

        public string Format => _formattableString.Format;
        public object[] Arguments => _formattableString.GetArguments();
    }
}