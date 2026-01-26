using UnityEngine;

[CreateAssetMenu(fileName = "NewSlideShowConfig", menuName = "SlideShow/SlideConfig")]
public class SlideShowConfig : ScriptableObject
{
    public SlideData[] slides;
}
