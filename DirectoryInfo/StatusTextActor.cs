using Akka.Actor;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using Microsoft.VisualBasic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DirectoryInfo;

public class StatusTextActor : ReceiveActor
{
    public StatusTextActor(TextBlock statusText, DispatcherQueue dispatcher)
    {
        Receive<SetStatusText>(item =>
        {
            dispatcher.TryEnqueue(() =>
            {
                statusText.Text = item.text;
            });
        });
    }
}