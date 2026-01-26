using UnityEngine;

namespace PanettoneGames.GenEvents
{
    public class GameObjectEventListener : BaseGameEventListener<GameObject, GameObjectEvent>
    {
    }

    public class IntEventListener : BaseGameEventListener<int, IntEvent>
    {
    }

    public class StringEventListener : BaseGameEventListener<string, StringEvent>
    {
    }

    public class VoidEventListener : BaseGameEventListener<Void, VoidEvent>
    {
    }
}