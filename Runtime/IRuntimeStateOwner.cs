namespace Xprees.Core
{
    /// Implemented by objects that have transient, non-serialized runtime state
    /// (e.g., event subscriptions, CancellationTokenSources, active caches, UI transient variables)
    /// that should be cleared during a scenario reset.
    public interface IRuntimeStateOwner
    {
        /// Clears transient non-serialized state (subscriptions, cancellation tokens, transient caches).
        void ClearTransientState();
    }
}