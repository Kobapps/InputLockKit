using System.Runtime.CompilerServices;

// The editor tooling and tests need read access to internal record/subscriber bookkeeping so the
// live debugger can display owners and affected components, and tests can assert on internals.
[assembly: InternalsVisibleTo("Kobapps.InputLockKit.Editor")]
[assembly: InternalsVisibleTo("Kobapps.InputLockKit.Tests")]
