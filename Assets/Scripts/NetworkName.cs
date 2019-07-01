// Synchronizing an entity's name is crucial for components that need the proper
// name in the Start function (e.g. to load the skillbar by name).
//
// Simply using OnSerialize and OnDeserialize is the easiest way to do it. Using
// a SyncVar would require Start, Hooks etc.
using System.Collections.Generic;
using Mirror;

[NetworkSettings(channel=Channels.DefaultUnreliable)] // unreliable is enough
public class NetworkName : NetworkBehaviour
{
    // server-side serialization
    //
    // I M P O R T A N T
    //
    // always read and write the same amount of bytes. never let any errors
    // happen. otherwise readstr/readbytes out of range bugs happen.
    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        writer.Write(name);
        return true;
    }

    // client-side deserialization
    //
    // I M P O R T A N T
    //
    // always read and write the same amount of bytes. never let any errors
    // happen. otherwise readstr/readbytes out of range bugs happen.
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        name = reader.ReadString();
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override bool Equals(object other)
    {
        return base.Equals(other);
    }

    public override string ToString()
    {
        return base.ToString();
    }

    public override bool InvokeCommand(int cmdHash, NetworkReader reader)
    {
        return base.InvokeCommand(cmdHash, reader);
    }

    public override bool InvokeRPC(int rpcHash, NetworkReader reader)
    {
        return base.InvokeRPC(rpcHash, reader);
    }

    public override bool InvokeSyncEvent(int eventHash, NetworkReader reader)
    {
        return base.InvokeSyncEvent(eventHash, reader);
    }

    public override void OnNetworkDestroy()
    {
        base.OnNetworkDestroy();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
    }

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();
    }

    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
    }

    public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initialize)
    {
        return base.OnRebuildObservers(observers, initialize);
    }

    public override void OnSetLocalVisibility(bool vis)
    {
        base.OnSetLocalVisibility(vis);
    }

    public override bool OnCheckObserver(NetworkConnection conn)
    {
        return base.OnCheckObserver(conn);
    }
}
