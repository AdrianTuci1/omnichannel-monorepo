namespace OdooBridge.Services;

/// <summary>Rezumatul unei treceri de sincronizare (număr de înregistrări create/actualizate/omise).</summary>
public sealed class SyncReport
{
    public int Created { get; set; }

    public int Updated { get; set; }

    public int Skipped { get; set; }
}
