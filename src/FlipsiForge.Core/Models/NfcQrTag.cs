namespace FlipsiForge.Core.Models;

/// <summary>
/// NFC- oder QR-Tag der einer Filament-Spule zugewiesen werden kann.
/// </summary>
public class NfcQrTag
{
    public int Id { get; set; }
    /// <summary>Tag-Code/ID (z.B. UID eines NFC-Tags oder QR-Code-Inhalt).</summary>
    public string Code { get; set; } = "";
    /// <summary>Typ des Tags: "NFC" oder "QR".</summary>
    public string Type { get; set; } = "NFC";
    /// <summary>ID der zugewiesenen Spule, oder null = frei.</summary>
    public int? AssignedSpoolId { get; set; }
}