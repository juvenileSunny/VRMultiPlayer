using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;


namespace XRMultiplayer.MiniGames
{
    public class LectureSessionManager : MiniGameBase
    {
        [Header("Activation Settings")]
        public float activationDelaySecondsOnPlayerCount = 3f;  // customizable delay

        public int minPlayersToActivate = 2;
        public List<Transform> teleportStartAreas;
        public float teleportStartRadius = 1f;

        [Header("Collision Alert Settings")]
        public Collider alertTriggerCollider;   // Assign the specific object’s collider

        private bool alertTriggered = false;


        [Header("Scene Objects")]
        public List<GameObject> objectsToActivateOnPlayerCount;
        public List<GameObject> objectsToDeactivateOnPlayerCount;
        public List<GameObject> objectsToActivateOnGameStart;
        public List<GameObject> objectsToActivateOnGameFinish;
        public List<GameObject> objectsToDeactivateOnGameFinish;
        public SlideTTSAgent slideAgent;



        [Header("Recording Settings")]
        public bool enableTransformRecording = false;
        public List<Transform> objectsToRecord;

        private Dictionary<string, float> playerJoinTimes = new();
        private Dictionary<string, float> playerDurations = new();
        private List<string> playersInSession = new();

        private Dictionary<Transform, List<Vector3>> transformRecords = new();
        private bool gameStarted = false;
        private bool playerCountActivated = false;

        private bool activationDelayStartedOnPlayerCount = false;

        private void Update()
        {
            if (!playerCountActivated && !activationDelayStartedOnPlayerCount && CountPlayersInStartZones() >= minPlayersToActivate)
            {
                StartCoroutine(DelayedActivation());
                activationDelayStartedOnPlayerCount = true;
            }

            if (enableTransformRecording && gameStarted)
            {
                RecordTransforms();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player") && !alertTriggered)
            {
                Debug.Log("Player collided with alert trigger");
                alertTriggered = true;
                ShowAlertMessage();
                Invoke(nameof(HideAlertMessage), 3f);
            }
        }

        private void ShowAlertMessage()
        {
            Debug.Log("Player triggered alert zone - session in progress");
            PlayerHudNotification.Instance.ShowText("Please finish the current session before starting the next one!");
        }

        private void HideAlertMessage()
        {
            alertTriggered = false;
        }



        private IEnumerator DelayedActivation()
        {
            Debug.Log($"Waiting {activationDelaySecondsOnPlayerCount} seconds before activating objects...");
            yield return new WaitForSeconds(activationDelaySecondsOnPlayerCount);

            if (CountPlayersInStartZones() >= minPlayersToActivate)
            {
                foreach (var obj in objectsToActivateOnPlayerCount)
                    if (obj != null) obj.SetActive(true);
                foreach (var obj in objectsToDeactivateOnPlayerCount)
                    if (obj != null) obj.SetActive(false);

                playerCountActivated = true;
                Debug.Log("Objects activated after delay.");
                PlayerHudNotification.Instance.ShowText("Hello everyone, the lecture is starting now!");
                yield return new WaitForSeconds(1);
            }
            else
            {
                activationDelayStartedOnPlayerCount = false; // reset for next check
                PlayerHudNotification.Instance.ShowText("Wait for other players to join!!");
                yield return new WaitForSeconds(1);
            }
        }




        private int CountPlayersInStartZones()
        {
            int count = 0;
            foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
            {
                foreach (var area in teleportStartAreas)
                {
                    if (Vector3.Distance(player.transform.position, area.position) <= teleportStartRadius)
                    {
                        count++;
                        // Debug.Log("Number of players in start zones: " + count  );
                        break;
                    }
                }
            }
            return count;
        }

        private void ActivateObjectsOnPlayerCount()
        {
            foreach (var obj in objectsToActivateOnPlayerCount)
                if (obj != null) obj.SetActive(true);

            playerCountActivated = true;
            Debug.Log("Player-count-based activation triggered.");
        }

        public override void StartGame()
        {
            base.StartGame();
            gameStarted = true;

            foreach (var obj in objectsToActivateOnGameStart)
                if (obj != null) obj.SetActive(true);

            Debug.Log("Game officially started.");
        }

        public override void FinishGame(bool submitScore = true)
        {
            base.FinishGame(submitScore);
            gameStarted = false;

            foreach (var obj in objectsToDeactivateOnGameFinish)
                if (obj != null) obj.SetActive(false);

            foreach (var obj in objectsToActivateOnGameFinish)
                if (obj != null) obj.SetActive(true);
            

            foreach (var kvp in playerJoinTimes)
            {
                playerDurations[kvp.Key] = Time.time - kvp.Value;
                Debug.Log($"Player {kvp.Key} session duration: {playerDurations[kvp.Key]:F2}s");
            }
        }

        public void LocalPlayerJoined(string playerId)
        {
            if (!playerJoinTimes.ContainsKey(playerId))
            {
                playerJoinTimes[playerId] = Time.time;
                playersInSession.Add(playerId);
                Debug.Log($"Player {playerId} joined at {Time.time:F2}");
            }
        }

        public float GetSessionTime(string playerId)
        {
            return playerDurations.TryGetValue(playerId, out float time) ? time : 0f;
        }

        private void RecordTransforms()
        {
            foreach (var t in objectsToRecord)
            {
                if (!transformRecords.ContainsKey(t))
                    transformRecords[t] = new List<Vector3>();

                transformRecords[t].Add(t.position);
            }
        }

        public Dictionary<Transform, List<Vector3>> GetTransformLogs() => transformRecords;
    }
}
