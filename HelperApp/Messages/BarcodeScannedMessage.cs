using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HelperApp.Messages;

public sealed record BarcodeScannedPayload(string PositionCode, string Barcode);

public sealed class BarcodeScannedMessage : ValueChangedMessage<BarcodeScannedPayload>
{
    public BarcodeScannedMessage(BarcodeScannedPayload value) : base(value) { }
}

public sealed record BarcodeProcessedPayload(
    string PositionCode,
    string Barcode,
    bool IsSuccess,
    string Message);

public sealed class BarcodeProcessedMessage : ValueChangedMessage<BarcodeProcessedPayload>
{
    public BarcodeProcessedMessage(BarcodeProcessedPayload value) : base(value) { }
}
