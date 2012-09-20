// Guids.cs
// MUST match guids.h
using System;

namespace AutoT4MVC
{
    static class GuidList
    {
        public const string guidAutoT4MVCPkgString = "c676817c-46cc-47d3-b03c-8a05f499d4a5";
        public const string guidAutoT4MVCCmdSetString = "3e7abe3a-4955-4c2f-aef7-4672394b69fd";

        public static readonly Guid guidAutoT4MVCCmdSet = new Guid(guidAutoT4MVCCmdSetString);
    };
}