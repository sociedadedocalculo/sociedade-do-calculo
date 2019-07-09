// grid based proximity checker. 30x faster than spherecast based checker.
//
// uses 8 neighborhood grid so that entities aren't all loaded abruptly. only
// the ones far away are.
//
// benchmark with 1 player + 1000 monsters = 1001 proximity checks
//   SphereCast: 952ms, 8,3MB GC
//   Grid:       31ms,  4.7MB GC
//
// in other words: very noticeable results with 1000+ entities!
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class NetworkProximityGridChecker : NetworkBehaviour
{
    // static variables common across all grid checkers ////////////////////////
    // view range
    // -> has to be static because we need the same for everyone
    // -> can't be shown in Inspector because Unity doesn't serlialize statics
    public static int visRange = 100;

    // if we see 8 neighbors then 1 entry is visRange/3
    public static int resolution => visRange / 3;

    // the grid
    static Grid2D<NetworkConnection> grid = new Grid2D<NetworkConnection>();

    ////////////////////////////////////////////////////////////////////////////

    [TooltipAttribute("How often (in seconds) that this object should update the set of players that can see it.")]
    public float visUpdateInterval = 1; // in seconds

    [TooltipAttribute("Enable to force this object to be hidden from players.")]
    public bool forceHidden;

    [TooltipAttribute("Which method to use for checking proximity of players.\n\nPhysics3D uses xz to determine proximity.\n\nPhysics2D uses xy to determine proximity.")]
    public NetworkProximityChecker.CheckMethod checkMethod = NetworkProximityChecker.CheckMethod.Physics3D;

    // previous position in the grid
    Vector2Int previous = new Vector2Int(int.MaxValue, int.MaxValue);

    // from original checker
    float m_VisUpdateTime;

    // called when a new player enters
    public override bool OnCheckObserver(NetworkConnection newObserver)
    {
        if (forceHidden)
            return false;

        // calculate projected positions
        Vector2Int projected = ProjectToGrid(transform.position);
        Vector2Int observerProjected = ProjectToGrid(newObserver.playerController.transform.position);

        // distance needs to be at max one of the 8 neighbors, which is
        //   1 for the direct neighbors
        //   1.41 for the diagonal neighbors (= sqrt(2))
        return Vector2Int.Distance(projected, observerProjected) <= Mathf.Sqrt(2);
    }

    Vector2Int ProjectToGrid(Vector3 position)
    {
        // simple rounding for now
        // 3D uses xz (horizontal plane)
        // 2D uses xy
        if (checkMethod == NetworkProximityChecker.CheckMethod.Physics3D)
        {
            return Vector2Int.RoundToInt(new Vector2(position.x, position.z) / resolution);
        }
        else
        {
            return Vector2Int.RoundToInt(new Vector2(position.x, position.y) / resolution);
        }
    }

    // note: this hides base.update, which is fine
    void Update()
    {
        if (!NetworkServer.active) return;

        // has connection to client? then we are a possible observer (player)
        // (monsters don't observer each other)
        if (connectionToClient != null)
        {
            // calculate current grid position
            Vector2Int current = ProjectToGrid(transform.position);

            // changed since last time?
            if (current != previous)
            {
                // update position in grid
                grid.Remove(previous, connectionToClient);
                grid.Add(current, connectionToClient);

                // save as previous
                previous = current;
            }
        }

        // possibly rebuild AFTER updating position in grid, so it's always up
        // to date. otherwise player might have moved and not be in current grid
        // hence OnRebuild wouldn't even find itself there
        if (Time.time - m_VisUpdateTime > visUpdateInterval)
        {
            netIdentity.RebuildObservers(false);
            m_VisUpdateTime = Time.time;
        }
    }

    void OnDestroy()
    {
        // try to remove from grid no matter what.
        // -> no NetworkServer.active check in case OnDestroy gets called after
        //    shutting down the server
        if (connectionToClient != null)
            grid.Remove(ProjectToGrid(transform.position), connectionToClient);
    }

    public override bool OnRebuildObservers(HashSet<NetworkConnection> observers, bool initial)
    {
        if (forceHidden)
            return false;

        // add everyone in 9 neighbour grid
        Vector2Int current = ProjectToGrid(transform.position);
        HashSet<NetworkConnection> hashSet = grid.GetWithNeighbours(current);
        observers.UnionWith(hashSet);

        // always return true when overwriting OnRebuildObservers so that
        // Mirror knows not to use the built in rebuild method.
        return true;
    }

    // called hiding and showing objects on the host
    public override void OnSetLocalVisibility(bool visible)
    {
        foreach (Renderer rend in GetComponentsInChildren<Renderer>())
        {
            rend.enabled = visible;
        }
    }
}
