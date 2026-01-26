using UnityEngine;

namespace PanettoneGames.GenEvents
{
    public abstract class BaseGameEventListener<T, E>
        : MonoBehaviour, IGameEventListener<T>
        where E : BaseGameEvent<T>
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

        public void OnEventRaised(T item)
        {
            if (gameEvent != null)
                gameEvent.Raise(item);
        }
    }

    public interface IGameEventListener<T>
    {
        void OnEventRaised(T item);
    }
}