#nullable enable
using System;
using System.Collections.Generic;

namespace Tinify.Unofficial
{
    public sealed record PreserveOperation
    {
        public PreserveOperation(PreserveOptions options)
        {
            var list = new List<string>(3);
            if (options.HasFlag(PreserveOptions.Copyright)) list.Add("copyright");
            if (options.HasFlag(PreserveOptions.Creation)) list.Add("creation");
            if (options.HasFlag(PreserveOptions.Location)) list.Add("location");
            Options = list.ToArray();
        }

        internal string[] Options { get; }
    }

    [Flags]
    public enum PreserveOptions
    {
        None = 0,
        Copyright = 1 << 0,
        Creation = 1 << 1,
        Location = 1 << 2
    }
}
#nullable restore