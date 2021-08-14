﻿
namespace ExCSS
{
    internal sealed class UrlFunction : DocumentFunction
    {
        readonly Url _expected;

        public UrlFunction(string url) : base(FunctionNames.Url, url)
        {
            _expected = Url.Create(Data);
        }

        public override bool Matches(Url actual)
        {
            return !_expected.IsInvalid && _expected.Equals(actual);
        }
    }
}