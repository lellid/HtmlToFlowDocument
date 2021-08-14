﻿using System;

namespace ExCSS
{
    internal sealed class DomainFunction : DocumentFunction
    {
        readonly string _subdomain;
        public DomainFunction(string url) : base(FunctionNames.Domain, url)
        {
            _subdomain = "." + url;
        }

        public override bool Matches(Url url)
        {
            var domain = url.HostName;
            return domain.Isi(Data) || domain.EndsWith(_subdomain, StringComparison.OrdinalIgnoreCase);
        }
    }
}