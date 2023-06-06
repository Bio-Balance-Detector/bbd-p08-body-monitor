using CommunityToolkit.Mvvm.Messaging.Messages;

namespace BBD.BodyMonitor.GUI.Messages;

public class AddProductMessage : ValueChangedMessage<bool>
{
    public AddProductMessage(bool value) : base(value)
    {
    }
}

