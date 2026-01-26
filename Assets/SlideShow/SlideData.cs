using UnityEngine;

[System.Serializable]
public class SlideData
{
    public string title;
    [TextArea(3, 10)]
    public string content;
    public Sprite slideImage;
}
