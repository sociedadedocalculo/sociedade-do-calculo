// Clients need to know the server time for cooldown calculations etc.
// Synchronizing the server time every second or so wouldn't be very precise, so
// we only synchronize an offset that is then used to calculate the server time.
//
// The component should be attached to a NetworkTime GameObject that is always
// in the scene and that has no duplicates.
using UnityEngine;
using Mirror;

// resynchronize every now and then. theoretically 60s would be enough, but if
// the server freezes for a short time then the client's time will be ahead
// until the next sync. so let's sync relatively often so the client is usually
// perfectly in sync.
// (reliable is important, we really do need the time all the time)
public class NetStreamer : NetworkBehaviour
{
    private void Start()
    {
        syncInterval = 0.05f;
    }
}
