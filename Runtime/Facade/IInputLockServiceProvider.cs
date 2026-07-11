namespace Kobapps.InputLockKit
{
    /// <summary>
    /// Resolves the <see cref="IInputLockService"/> that the <see cref="InputLock"/> facade and the
    /// lockable components should talk to. Register a custom provider (e.g. a scene-scoped instance,
    /// or one bound by a DI container) via <see cref="InputLock.SetProvider"/>. When no provider is
    /// set, the process-wide <see cref="InputLockRuntime.Default"/> service is used.
    /// </summary>
    public interface IInputLockServiceProvider
    {
        IInputLockService Service { get; }
    }
}
