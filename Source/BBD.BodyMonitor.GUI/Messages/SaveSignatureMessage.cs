using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BBD.BodyMonitor.GUI.Messages;

public class SaveSignatureMessage : ValueChangedMessage<int>
{
    public SaveSignatureMessage(int value) : base(value)
    {
    }
}

