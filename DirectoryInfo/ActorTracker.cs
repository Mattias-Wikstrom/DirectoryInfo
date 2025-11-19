using Akka.Actor;
using DirectoryInfo;
using System.Collections.Generic;

public class ActorTracker : ReceiveActor
{
    private readonly Dictionary<string, IActorRef> _actors = new();

    public ActorTracker()
    {
        Receive<RegisterActor>(msg => _actors[msg.ActorId] = msg.ActorRef);
        Receive<string>(msg =>
        {
            if (msg == "GetAll")
            {
                Sender.Tell(new Dictionary<string, IActorRef>(_actors));
            } else if (msg == "GetCount")
            {
                Sender.Tell(_actors.Count);
            }
        });
    }
}
