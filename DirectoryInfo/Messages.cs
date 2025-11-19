using Akka.Actor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryInfo
{
    public record GetFiles(Guid ScanId, string FolderPath, string ActorId = null);
    public record FileList(Guid ScanId, string[] Files, string ActorId);
    public record RegisterActor(string ActorId, IActorRef ActorRef);
    public record Clear();
    public record AddToList(string text);
    public record SetStatusText(string text);
}
