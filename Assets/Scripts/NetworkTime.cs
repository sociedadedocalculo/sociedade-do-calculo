// Clients need to know the server time for cooldown calculations etc.
// Synchronizing the server time every second or so wouldn't be very precise, so
// we only synchronize an offset that is then used to calculate the server time.
//
// The component should be attached to a NetworkTime GameObject that is always
// in the scene and that has no duplicates.
using UnityEngine;
using UnityEngine.Networking;

// resynchronize every now and then. theoretically 60s would be enough, but if
// the server freezes for a short time then the client's time will be ahead
// until the next sync. so let's sync relatively often so the client is usually
// perfectly in sync.
// (reliable is important, we really do need the time all the time)
[NetworkSettings(sendInterval=5)]
public class NetworkTime : NetworkBehaviour
{
    // add offset to Time.time to get the server time
    public static float offset;

    // server time caclulation
    public static float time { get { return Time.time + offset; } }

    // force dirty bit so that it's synced after sendInterval. otherwise it
    // won't be synced and the time might get out of sync at some point.
    [ServerCallback] void Update() { SetDirtyBit(1); }

    // server-side serialization
    //
    // I M P O R T A N T
    //
    // always read and write the same amount of bytes. never let any errors
    // happen. otherwise readstr/readbytes out of range bugs happen.
    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        writer.Write(Time.time);
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
        offset = reader.ReadSingle() - Time.time;
    }
}
