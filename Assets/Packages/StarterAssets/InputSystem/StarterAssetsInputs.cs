using UnityEngine;
using UnityEngine.Events;
using PanettoneGames.GenEvents;
using UnityEngine.EventSystems;
using System.Collections.Generic;


#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		// This is how the tutorial Manager, and all the various parts of the tutorial talk to eachother.
		[Header("Send Input as event to the TutorialManager")]
		public IntEvent tutorialEvents;

		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM
        public void OnMove(InputAction.CallbackContext context)
		{
			if(cursorLocked) {
				// This is where the move event is sent to the tutorial manager. To keep it from spamming events, the tutorial manager
				// has to be looking for this specific event. And all the event id's are stored in the tutorial manager, to make
				// sure you dont have to be running all over the place chaning ids.
				if(tutorialEvents != null && TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.DirectionInputEvent)
					tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.DirectionInputEvent);
				
				MoveInput(context.ReadValue<Vector2>());
			}
		}

		public void OnLook(InputAction.CallbackContext context)
		{
			// if(!cursorLocked) return; 
			// This is where the cursor look around event is sent to the tutorial manager. To keep it from spamming events, the tutorial manager
			// has to be looking for this specific event. And all the event id's are stored in the tutorial manager, to make
			// sure you dont have to be running all over the place chaning ids.
			if(cursorInputForLook)
			{
				if(tutorialEvents != null && TutorialManager.currentEvent == TutorialManager.TutorialEventIDs.MouseInputEvent)
					tutorialEvents.Raise((int)TutorialManager.TutorialEventIDs.MouseInputEvent);
				LookInput(context.ReadValue<Vector2>());
			}
		}

		public bool IsPointingAtUI()
		{
			PointerEventData pointerData = new PointerEventData(EventSystem.current)
			{
				position = new Vector2(Screen.width / 2, Screen.height / 2)
			};

			List<RaycastResult> results = new List<RaycastResult>();
			EventSystem.current.RaycastAll(pointerData, results);

			// Return true only if the topmost hit is a valid UI element
			return results.Count > 0 && results[0].gameObject != null;
		}



		//Used to lock and unlock the cursor when looking at UI elements.
		public void OnGrab(InputAction.CallbackContext context) {
			// Prevents this function from being called multiple times.
			if (!context.performed) return;
			
			Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

			// This is used to press the button on the wall that spawns the mind nodes.
			if (Physics.Raycast(ray, out RaycastHit hit, 5f))
			{
				// Check for a specific tag or component if needed
				if (hit.collider.GetComponent<PrefabSpawn>() != null)
				{
					hit.collider.GetComponent<PrefabSpawn>().SpawnPrefab();
				}
			}

			// Unlock cursor if user is clicking on an InputField or other UI
			bool isHoveringUI = IsPointingAtUI();

			SetCursorState(!isHoveringUI); // Lock if not over UI, unlock if over UI
		}

		//Closes ui stuff.
		public void OnEnter(InputAction.CallbackContext context) {
			SetCursorState(true);
		}

		public void OnJump(InputAction.CallbackContext context)
		{
			if(cursorLocked)
				JumpInput(true);
		}

		// Spriting does not disable automatically. I dont really think this program should require
		// Sprinting anyways. So I am going to turn it off.
		public void OnSprint(InputAction.CallbackContext context)
		{
			// SprintInput(true);
		}

		public void OnSprintCanceled(InputAction.CallbackContext context) 
		{
			// SprintInput(false);
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !newState;
			cursorLocked = newState;
		}
	}
	
}