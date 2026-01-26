using UnityEngine;

namespace PanettoneGames.GenEvents
{
    public abstract class DualGameEventListener<T, X, E>
        : MonoBehaviour, IDualGameEventListener<T, X>
        where E : DualGameEvent<T, X>
    {
        [SerializeField] private E gameEvent;
        public E GameEvent => gameEvent;

        private void OnEnable()
        {
            if (gameEvent == null) return;
            GameEvent.RegisterListener(this);
        }

        private void OnDisable()
        {
            if (gameEvent == null) return;
            GameEvent.UnregisterListener(this);
        }

        public void OnEventRaised(T item1, X item2)
        {
            if (gameEvent != null)
                gameEvent.Raise(item1, item2);
        }
    }

    public interface IDualGameEventListener<T, X>
    {
        void OnEventRaised(T item1, X item2);
    }
}