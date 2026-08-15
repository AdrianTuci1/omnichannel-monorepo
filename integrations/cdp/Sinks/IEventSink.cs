using Cdp.Worker.Events;

namespace Cdp.Worker.Sinks;

/// <summary>
/// Contractul unei destinații de evenimente consumate de CDP.
/// </summary>
public interface IEventSink
{
    /// <summary>
    /// Persistă un eveniment de domeniu. Implementarea trebuie să fie thread-safe.
    /// </summary>
    void Append(DomainEvent evt);
}
