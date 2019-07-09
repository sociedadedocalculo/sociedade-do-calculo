using System;
using System.ComponentModel;
using UnityEngine;

namespace Mirror
{
    [EditorBrowsable(EditorBrowsableState.Never), Obsolete("Use NetworkBehaviour.syncInterval field instead. Can be modified in the Inspector too.")]
    [AttributeUsage(AttributeTargets.Class)]
    public class NetworkSettingsAttribute : Attribute
    {
        public float sendInterval = 0.1f;
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class SyncVarAttribute : Attribute
    {
        public string hook;
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CommandAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable; // this is zero
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientRpcAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable; // this is zero
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TargetRpcAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable; // this is zero
    }

    [AttributeUsage(AttributeTargets.Event)]
    public class SyncEventAttribute : Attribute
    {
        public int channel = Channels.DefaultReliable; // this is zero
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ServerAttribute : Attribute
    {
        public override object TypeId => base.TypeId;

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override bool IsDefaultAttribute()
        {
            return base.IsDefaultAttribute();
        }

        public override bool Match(object obj)
        {
            return base.Match(obj);
        }

        public override string ToString()
        {
            return base.ToString();
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ServerCallbackAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientAttribute : Attribute {}

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientCallbackAttribute : Attribute {}

    // For Scene property Drawer
    public class SceneAttribute : PropertyAttribute {}
}
