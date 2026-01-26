using UnityEngine;

namespace PanettoneGames.GenEvents
{
    //GameObject
    [CreateAssetMenu(fileName = "New GameObject,GameObject Event", menuName = "Game Events/GameObject, GameObject Event", order = 52)]
    public class DualGameObjectEvent : DualGameEvent<GameObject, GameObject>
    {
    }
}