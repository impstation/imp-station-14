using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._Impstation.Notifier.UI;

public sealed partial class NotifierWindow : FancyWindow
{
    public NotifierWindow()
    {
        IoCManager.InjectDependencies(this);
        RobustXamlLoader.Load(this);

    }
}
