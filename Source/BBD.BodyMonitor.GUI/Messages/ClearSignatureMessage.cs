using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BBD.BodyMonitor.GUI.Messages;

public class ClearSignatureMessage : ValueChangedMessage<bool>
{
    public ClearSignatureMessage(bool value) : base(value)
    {
    }
}

