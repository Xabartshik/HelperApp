using CommunityToolkit.Mvvm.Messaging.Messages;

namespace HelperApp.Messages;

public sealed class BarcodeScannedMessage : ValueChangedMessage<string>
{
    public BarcodeScannedMessage(string value) : base(value) { }
}
