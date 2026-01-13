using Game.UI.Localization;

namespace C2VM.TrafficLightsEnhancement.Domain;

public class MessageDialog : Game.UI.MessageDialog
{
    public MessageDialog(string message)
        : base(
            title: MakeSimpleLocalizedString("Scenarios"),
            message: MakeSimpleLocalizedString(message),
            confirmAction: MakeSimpleLocalizedString("OK")
        ){}

    public MessageDialog(string title, string message, LocalizedString confirmAction)
        : base(
            title: MakeSimpleLocalizedString(title),
            message: MakeSimpleLocalizedString(message),
            confirmAction: confirmAction
        ){}

   

    private static LocalizedString MakeSimpleLocalizedString(string text)
    {
        return new LocalizedString(text, text, null);
    }
}