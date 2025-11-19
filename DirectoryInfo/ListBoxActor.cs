using Akka.Actor;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace DirectoryInfo;

public class ListBoxActor : ReceiveActor
{
    public ListBoxActor(ObservableCollection<string> collection, DispatcherQueue dispatcher)
    {
        Receive<AddToList>(item =>
        {
            // Marshal update to UI thread
            dispatcher.TryEnqueue(() =>
            {
                collection.Add(item.text);
            });
        });

        Receive<Clear>(msg =>
        {
            dispatcher.TryEnqueue(() =>
            {
                collection.Clear();
            });
        });
    }
}