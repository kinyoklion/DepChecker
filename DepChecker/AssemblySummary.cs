using System.Collections.Generic;
using System.Reflection;

namespace DepChecker
{
    readonly struct AssemblySummary
    {
        public bool Exists { get; init; }
        public List<AssemblySummary> Dependencies { get; init; }

        public AssemblyName AssemblyName { get; init; }

        public Source Source { get; init; }

        public override string ToString()
        {
            return $"[{AssemblyName.Name} {AssemblyName.Version}] <- {Source}";
        }
    }
}