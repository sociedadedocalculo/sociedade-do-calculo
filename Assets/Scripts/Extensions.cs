// This class adds functions to built-in types.
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using System;
using System.Collections.Generic;
using System.Linq;

public static class Extensions
{
    // string to int (returns errVal if failed)
    public static int ToInt(this string value, int errVal = 0)
    {
        Int32.TryParse(value, out errVal);
        return errVal;
    }

    // string to long (returns errVal if failed)
    public static long ToLong(this string value, long errVal = 0)
    {
        Int64.TryParse(value, out errVal);
        return errVal;
    }

    // UI SetListener extension that removes previous and then adds new listener
    // (this version is for onClick etc.)
    public static void SetListener(this UnityEvent uEvent, UnityAction call)
    {
        uEvent.RemoveAllListeners();
        uEvent.AddListener(call);
    }

    // UI SetListener extension that removes previous and then adds new listener
    // (this version is for onEndEdit, onValueChanged etc.)
    public static void SetListener<T>(this UnityEvent<T> uEvent, UnityAction<T> call)
    {
        uEvent.RemoveAllListeners();
        uEvent.AddListener(call);
    }

    // NavMeshAgent helper function that returns the nearest valid point for a
    // given destination. This is really useful for click & wsad movement
    // because the player may click into all kinds of unwalkable paths:
    //
    //       ________
    //      |xxxxxxx|
    //      |x|   |x|
    // P   A|B| C |x|
    //      |x|___|x|
    //      |xxxxxxx|
    //
    // if a player is at position P and tries to go to:
    // - A: the path is walkable, everything is fine
    // - C: C is on a NavMesh, but we can't get there directly. CalcualatePath
    //      will return A as the last walkable point
    // - B: B is not on a NavMesh, CalulatePath doesn't work here. We need to
    //   find the nearest point on a NavMesh first (might be A or C) and then
    //   calculate the nearest valid one (A)
    public static Vector3 NearestValidDestination(this NavMeshAgent agent, Vector3 destination)
    {
        // can we calculate a path there? then return the closest valid point
        NavMeshPath path = new NavMeshPath();
        if (agent.CalculatePath(destination, path))
            return path.corners[path.corners.Length - 1];

        // otherwise find nearest navmesh position first. we use a radius of
        // speed*2 which works fine. afterwards we find the closest valid point.
        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, agent.speed * 2, NavMesh.AllAreas))
            if (agent.CalculatePath(hit.position, path))
                return path.corners[path.corners.Length - 1];

        // nothing worked, don't go anywhere.
        return agent.transform.position;
    }

    // NavMeshAgent's ResetPath() function clears the path, but doesn't clear
    // the velocity immediately. This is a nightmare for finite state machines
    // because we often reset a path, then switch to casting, which would then
    // receive a movement event because velocity still isn't 0 until a few
    // frames later.
    //
    // We need a function that truly stops all movement.
    public static void ResetMovement(this NavMeshAgent agent)
    {
        agent.ResetPath();
        agent.velocity = Vector3.zero;
    }

    // check if a list has duplicates
    // new List<int>(){1, 2, 2, 3}.HasDuplicates() => true
    // new List<int>(){1, 2, 3, 4}.HasDuplicates() => false
    // new List<int>().HasDuplicates() => false
    public static bool HasDuplicates<T>(this List<T> list)
    {
        return list.Count != list.Distinct().Count();
    }

    // find all duplicates in a list
    public static List<U> FindDuplicates<T, U>(this List<T> list, Func<T, U> keySelector)
    {
        return list.GroupBy(keySelector)
                   .Where(group => group.Count() > 1)
                   .Select(group => group.Key).ToList();
    }

    // string.GetHashCode is not guaranteed to be the same on all machines, but
    // we need one that is the same on all machines. simple and stupid:
    public static int GetStableHashCode(this string text)
    {
        unchecked
        {
            int hash = 23;
            foreach (char c in text)
                hash = hash * 31 + c;
            return hash;
        }
    }
}
