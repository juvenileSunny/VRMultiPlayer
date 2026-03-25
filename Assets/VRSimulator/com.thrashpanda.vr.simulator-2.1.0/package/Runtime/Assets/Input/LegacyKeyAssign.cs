using UnityEngine;
using System;

static class LegacyKeyAssign
{
    [Serializable]
    public struct InteractionInput
    {
        [SerializeField]
        public KeyCode grip;

        [SerializeField]
        public KeyCode trigger;

        [SerializeField]
        public KeyCode primaryButton;

        [SerializeField]
        public KeyCode secondaryButton;

        [SerializeField]
        public KeyCode stickUp;

        [SerializeField]
        public KeyCode stickDown;

        [SerializeField]
        public KeyCode stickLeft;

        [SerializeField]
        public KeyCode stickRight;


        public InteractionInput(KeyCode gripKey, KeyCode triggerKey, KeyCode primaryKey, KeyCode secondaryKey, KeyCode stickUp, KeyCode stickDown, KeyCode stickLeft, KeyCode stickRight)
        {
            this.grip = gripKey;
            this.trigger = triggerKey;
            this.primaryButton = primaryKey;
            this.secondaryButton = secondaryKey;
            this.stickUp = stickUp;
            this.stickDown = stickDown;
            this.stickLeft = stickLeft;
            this.stickRight = stickRight;
        }
    }

    [Serializable]
    public struct MovementInput
    {
        [SerializeField]
        public KeyCode forward;

        [SerializeField]
        public KeyCode back;

        [SerializeField]
        public KeyCode left;

        [SerializeField]
        public KeyCode right;

        public MovementInput(KeyCode forward, KeyCode back, KeyCode left, KeyCode right)
        {
            this.forward = forward;
            this.back = back;
            this.left = left;
            this.right = right;
        }
    }

    [Serializable]
    public struct ControllerMovementInput
    {
        [SerializeField]
        public KeyCode toggleHands;

        [SerializeField]
        public KeyCode switchHands;

        [SerializeField]
        public KeyCode toggleHandsRotation;

        [SerializeField]
        public KeyCode switchHandAxis;

        [SerializeField]
        public KeyCode resetControllers;

        public ControllerMovementInput(KeyCode toggleHands, KeyCode switchHands, KeyCode toggleHandsRotation, KeyCode switchHandAxis, KeyCode resetControllers)
        {
            this.toggleHands = toggleHands;
            this.switchHands = switchHands;
            this.toggleHandsRotation = toggleHandsRotation;
            this.switchHandAxis = switchHandAxis;
            this.resetControllers = resetControllers;
        }
    }

    [Serializable]
    public struct InputAssign
    {
        [SerializeField]
        public MovementInput movementKeys;

        [SerializeField]
        public InteractionInput interactionKeys;

        [SerializeField]
        public ControllerMovementInput controllerMovementKeys;

        public InputAssign(MovementInput movementKeys, InteractionInput interactionKeys, ControllerMovementInput controllerMovementKeys)
        {
            this.movementKeys = movementKeys;
            this.interactionKeys = interactionKeys;
            this.controllerMovementKeys = controllerMovementKeys;
        }
    }
}
