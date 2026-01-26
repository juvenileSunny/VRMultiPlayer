using System.Collections.Generic;
using UnityEngine;
using PanettoneGames.GenEvents;

public class TriggerobjectCount : MonoBehaviour
{
    [Header("Hookup for Tutorial Manager")]
    public IntEvent tutorialEvents;
    public bool debugMessages = false;
    [Header("Once you hit this count the object will perform its function")]
    public int targetCount = 3;

    // This will store all objects that enter the trigger area
    private HashSet<Collider> objectsInTrigger = new HashSet<Collider>();

    // Called when another collider enters the trigger
    private void OnTriggerEnter(Collider other)
    {
        // Add the object to the HashSet (automatically handles duplicates)
        objectsInTrigger.Add(other);
        if(debugMessages)
            Debug.Log("Object entered: " + other.gameObject.name);

        if(GetObjectCount() >= targetCount) {
            Debug.Log("You got them all NICE!");
            if(TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.ObjectOnTableEvent)
				tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.ObjectOnTableEvent);
        }
    }

    // Called when another collider stays inside the trigger
    private void OnTriggerStay(Collider other)
    {
        // You can perform actions here for the objects that are staying inside the trigger
        if(debugMessages)
            Debug.Log("Object still inside: " + other.gameObject.name);
    }

    // Called when another collider exits the trigger
    private void OnTriggerExit(Collider other)
    {
        // Remove the object from the HashSet when it exits
        objectsInTrigger.Remove(other);
        if(debugMessages)
            Debug.Log("Object exited: " + other.gameObject.name);
    }


    // Optional: Check how many objects are inside the trigger
    public int GetObjectCount()
    {
        return objectsInTrigger.Count;
    }

    // Optional: Loop through objects inside the trigger and perform some action
    public void PerformActionOnObjects()
    {
        foreach (var obj in objectsInTrigger)
        {
            // Do something with obj
            Debug.Log("Performing action on: " + obj.gameObject.name);
        }
    }
}
