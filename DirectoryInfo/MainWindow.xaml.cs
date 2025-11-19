using Akka.Actor;
using Akka.Dispatch;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT; // required for WinUI pickers

namespace DirectoryInfo
{
    public sealed partial class MainWindow : Window
    {
        private ActorSystem _actorSystem;
        private IActorRef _tracker;
        private IActorRef _fileUiActor;
        private IActorRef _actorUiActor;
        private IActorRef _statusTextActor;
        private IActorRef _rootWorker;

        private string _selectedFolder;

        public MainWindow()
        {
            this.InitializeComponent();

            ObservableCollection<string> _filesCollection = new ObservableCollection<string>();
            ObservableCollection<string> _actorCollection = new ObservableCollection<string>();
            FilesListBox.ItemsSource = _filesCollection;
            ActorsListBox.ItemsSource = _actorCollection;

            _actorSystem = ActorSystem.Create("FileSystemSystem");
            _tracker = _actorSystem.ActorOf(Props.Create(() => new ActorTracker()), "tracker");

            _fileUiActor = _actorSystem.ActorOf(
                Props.Create(() => new ListBoxActor(_filesCollection, DispatcherQueue)),
                "fileUiActor");

            _actorUiActor = _actorSystem.ActorOf(
                Props.Create(() => new ListBoxActor(_actorCollection, DispatcherQueue)),
                "actorUiActor");

            _statusTextActor = _actorSystem.ActorOf(
                Props.Create(() => new StatusTextActor(StatusText, DispatcherQueue)),
                "statusUiActor");

            _rootWorker = _actorSystem.ActorOf(Props.Create(() => new FileWorkerActor(_tracker, "root", _fileUiActor, _actorUiActor, _statusTextActor)), "rootWorker");
        }

        private async void SelectFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FolderPicker();
            picker.FileTypeFilter.Add("*");

            // This is needed in WinUI 3
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var folder = await picker.PickSingleFolderAsync();

            if (folder != null)
            {
                _selectedFolder = folder.Path;
                SelectFolderButton.Content = $"Folder: {_selectedFolder}";
            }

            if (string.IsNullOrEmpty(_selectedFolder))
            {
                ContentDialog dialog = new()
                {
                    Title = "No folder selected",
                    Content = "Please click 'Select Folder...' first.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                _ = dialog.ShowAsync();
                return;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                ScanFolder(_selectedFolder);
            });
        }


        private async void ScanFolder(string folderPath)
        {
            _fileUiActor.Tell(new Clear());
            _actorUiActor.Tell(new Clear());
            var scanId = Guid.NewGuid();

            _rootWorker.Tell(new GetFiles(scanId, folderPath));
            await Task.Yield();
            //await _tracker.Ask<Dictionary<string, IActorRef>>("GetAll");
        }
    }
}
