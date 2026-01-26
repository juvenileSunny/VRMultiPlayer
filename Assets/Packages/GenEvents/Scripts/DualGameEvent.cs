using System.Collections.Generic;
using UnityEngine;

namespace PanettoneGames.GenEvents
{
    public abstract class DualGameEvent<T, X> : ScriptableObject
    {
        private readonly List<IDualGameEventListener<T, X>> eventListeners = new();

        public void Raise(T item1, X item2)
        {
            for (var i = eventListeners.Count - 1; i >= 0; i--)
                eventListeners[i].OnEventRaised(item1, item2);
        }

        public void RegisterListener(IDualGameEventListener<T, X> listener)
        {
            if (!eventListeners.Contains(listener))
                eventListeners.Add(listener);
        }


        public void UnregisterListener(IDualGameEventListener<T, X> listener)
        {
            if (eventListeners.Contains(listener))
                eventListeners.Remove(listener);
        }
    }
}