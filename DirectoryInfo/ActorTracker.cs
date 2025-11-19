using Akka.Actor;
using DirectoryInfo;
using System.Collections.Generic;

// This actor is used to keep track of other actors (currently just FileWorkerActors)
public class ActorTracker : ReceiveActor
{
    private readonly Dictionary<string, IActorRef> _actors = new();

    public ActorTracker()
    {
        Receive<RegisterActor>(msg => _actors[msg.ActorId] = msg.ActorRef);

        Receive<GetAll>(msg =>
        {
            Sender.Tell(new Dictionary<string, IActorRef>(_actors));
        });
        
        Receive<GetCount>(msg =>
        {
            Sender.Tell(_actors.Count);
        });
    }
}
