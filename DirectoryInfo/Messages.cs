using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryInfo
{
    public record GetFiles(Guid ScanId, string FolderPath, string ActorId = null);
    public record ResultFromDirectory(Guid ScanId, string[] Files, long totalSize, string ActorId);
    public record RegisterActor(string ActorId, IActorRef ActorRef);
    public record Clear();
    public record GetAll();
    public record GetCount();
    public record Reset();
    public record AddToList(string text);
    public record SetStatusText(string text);
}
