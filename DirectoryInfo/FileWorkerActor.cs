using Akka.Actor;
using DirectoryInfo;
using System;
using System.Collections.Generic;
using System.IO;


public class FileWorkerActor : ReceiveActor
{
    private readonly IActorRef _fileUiActor;
    private readonly IActorRef _actorUiActor;
    private readonly IActorRef _statusTextActor;

    private IActorRef _parentSender;
    private List<string> _files = new();
    private int _pendingSubworkers = 0;
    private readonly string _actorId;
    private readonly IActorRef _tracker;

    public FileWorkerActor(IActorRef tracker, string actorId, IActorRef fileUiActor, IActorRef actorUiActor, IActorRef statusTextActor)
    {
        _tracker = tracker;
        _actorId = actorId;
        _fileUiActor = fileUiActor;
        _actorUiActor = actorUiActor;
        _statusTextActor = statusTextActor;

        // Notify UI about new actor
        _actorUiActor.Tell(new AddToList(_actorId));

        // Register self with tracker
        _tracker.Tell(new RegisterActor(_actorId, Self));

        int numActors = _tracker.Ask<int>("GetCount").Result;
        _statusTextActor.Tell(new SetStatusText("Current number of actors: " + numActors));

        Receive<GetFiles>(message =>
        {
            _parentSender = Sender;

            if (Directory.Exists(message.FolderPath))
            {
                var files = Directory.GetFiles(message.FolderPath);
                foreach (var file in files)
                    _fileUiActor.Tell(new AddToList(file)); // send each file to UI actor

                _files.AddRange(files);

                var subDirs = Directory.GetDirectories(message.FolderPath);

                _pendingSubworkers = subDirs.Length;

                if (_pendingSubworkers == 0)
                {
                    _parentSender.Tell(new FileList(message.ScanId, _files.ToArray(), _actorId));
                }
                else
                {
                    int subIndex = 1;
                    foreach (var dir in subDirs)
                    {
                        var actorIdToUse = _actorId == "root" ? $"{_actorId}.{ message.ScanId}" : _actorId;
                        var subActorId = $"{actorIdToUse}.{subIndex++}";
                        var subWorker = Context.ActorOf(Props.Create(() => new FileWorkerActor(_tracker, subActorId, _fileUiActor, _actorUiActor, _statusTextActor)), subActorId);
                        subWorker.Tell(new GetFiles(message.ScanId, dir, subActorId));
                    }
                }
            }
            else
            {
                _parentSender.Tell(new FileList(message.ScanId, System.Array.Empty<string>(), _actorId));
            }
        });

        Receive<FileList>(message =>
        {
            _files.AddRange(message.Files);
            _pendingSubworkers--;

            if (_pendingSubworkers == 0)
            {
                _parentSender.Tell(new FileList(message.ScanId, _files.ToArray(), _actorId));
            }
        });
    }
}
