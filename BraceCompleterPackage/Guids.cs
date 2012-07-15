// Guids.cs
// MUST match guids.h
using System;

namespace JoelSpadin.BraceCompleter
{
    static class GuidList
    {
        public const string guidBraceCompleterPkgString = "083430bf-b706-4190-aa67-d273a6ceead6";
        public const string guidBraceCompleterCmdSetString = "7b2438ff-f29a-43cd-b2b6-79e7568b3e94";

        public static readonly Guid guidBraceCompleterCmdSet = new Guid(guidBraceCompleterCmdSetString);
    };
}