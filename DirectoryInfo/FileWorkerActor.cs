using Akka.Actor;
using DirectoryInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;

static class ProcessorInfo
{
    // The DllImport attribute tells the runtime where to find the function.
    // The function is exported by kernel32.dll.
    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentProcessorNumber();

    public static uint GetCurrentCpuNumber()
    {
        // Call the imported native function
        return GetCurrentProcessorNumber();
    }
}

public class FileWorkerActor : ReceiveActor
{
    private readonly IActorRef _tracker;

    private readonly string _actorId;

    private readonly IActorRef _fileUiActor;
    private readonly IActorRef _actorUiActor;
    private readonly IActorRef _statusTextActor;

    private IActorRef _parentSender;

    private List<string> _files = new();
    private long totalSize = 0;
    private int _pendingSubworkers = 0;

    public FileWorkerActor(IActorRef tracker, string actorId, IActorRef fileUiActor, IActorRef actorUiActor, IActorRef statusTextActor)
    {

        var cpu = ProcessorInfo.GetCurrentCpuNumber();
        int threadId = Thread.CurrentThread.ManagedThreadId;

//        System.Diagnostics.Debug.WriteLine($"Actor {actorId} running on CPU {cpu}, thread {threadId}");

        _tracker = tracker;
        
        _actorId = actorId;

        _fileUiActor = fileUiActor;
        _actorUiActor = actorUiActor;
        _statusTextActor = statusTextActor;

        // Register self with tracker
        _tracker.Tell(new RegisterActor(_actorId, Self));

        // Notify UI about new actor
        _actorUiActor.Tell(new AddToList($"Constructed on CPU {cpu}, thread {threadId}: {actorId}"));

        // Update status text with current number of actors
        //var numActors = _tracker.Ask<int>(new GetCount()).Result;
        //_statusTextActor.Tell(new SetStatusText("Current number of actors: " + numActors));

        Receive<Reset>(message =>
        {
            _files.Clear();
            totalSize = 0;
            //_pendingSubworkers = 0;
            _statusTextActor.Tell(new SetStatusText("Working..."));

        });

        Receive<GetFiles>(message =>
        {
            _parentSender = Sender;

            if (Directory.Exists(message.FolderPath))
            {
                var files = Directory.GetFiles(message.FolderPath);
                foreach (var file in files)
                {
                    _fileUiActor.Tell(new AddToList(file)); // send each file to UI actor
                    totalSize += (int)new FileInfo(file).Length;
                }

                _files.AddRange(files);

                var subDirs = Directory.GetDirectories(message.FolderPath);

                _pendingSubworkers = subDirs.Length;

                if (_pendingSubworkers == 0)
                {
                    var filesAsArray = _files.ToArray();
                    _parentSender.Tell(new ResultFromDirectory(message.ScanId, filesAsArray, totalSize, _actorId));
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
                _parentSender.Tell(new ResultFromDirectory(message.ScanId, System.Array.Empty<string>(), 0, _actorId));
            }
        });

        // This is where we receive file lists from sub-workers
        Receive<ResultFromDirectory>(message =>
        {
            _files.AddRange(message.Files);
            totalSize += message.totalSize;
            _pendingSubworkers--;

            if (_pendingSubworkers == 0)
            {
                var filesAsArray = _files.ToArray();
                // All sub-workers have responded, send the complete file list to the original requester
                _parentSender.Tell(new ResultFromDirectory(message.ScanId, filesAsArray, totalSize, _actorId));
            }

            if (actorId == "root")
            {   
                _statusTextActor.Tell(new SetStatusText($"Total size: {FormatSize(totalSize)}"));
            }
        });
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}
