using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerControlUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Button moveLeftButton;
    [SerializeField] private Button moveRightButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private bool isControlEnable = true;
    [SerializeField] private bool enableDebugLogs = true;

    [Header("Editor Keyboard Control")]
    [SerializeField] private bool enableKeyboardInEditor = true;
    [SerializeField] private Key jumpKey = Key.Space;

    public bool IsControlEnable => isControlEnable;

    private bool isInitialized;
    private bool isKeyboardMoveActive;

    public void Initialize(PlayerController value)
    {
        player = value;

        Log("Initialize called. Player=" + (player != null ? player.name : "null"));

        if (isInitialized)
        {
            Log("Initialize skipped (already initialized).");
            return;
        }

        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJumpPressed);
        else
            Log("Jump button is null.");

        if (moveLeftButton != null)
        {
            AddPointerEvent(moveLeftButton, EventTriggerType.PointerDown, _ => OnMoveLeftDown());
            AddPointerEvent(moveLeftButton, EventTriggerType.PointerUp, _ => OnMoveReleased());
            AddPointerEvent(moveLeftButton, EventTriggerType.PointerExit, _ => OnMoveReleased());
        }
        else
        {
            Log("MoveLeft button is null.");
        }

        if (moveRightButton != null)
        {
            AddPointerEvent(moveRightButton, EventTriggerType.PointerDown, _ => OnMoveRightDown());
            AddPointerEvent(moveRightButton, EventTriggerType.PointerUp, _ => OnMoveReleased());
            AddPointerEvent(moveRightButton, EventTriggerType.PointerExit, _ => OnMoveReleased());
        }
        else
        {
            Log("MoveRight button is null.");
        }

        ApplyControlState();
        isInitialized = true;
        Log("Initialize completed.");
    }

    private void Update()
    {
        HandleEditorKeyboardInput();
    }

    public void SetPlayer(PlayerController value)
    {
        player = value;
        Log("SetPlayer: " + (player != null ? player.name : "null"));
    }

    public void SetControlEnable(bool value)
    {
        isControlEnable = value;
        ApplyControlState();

        Log("SetControlEnable: " + isControlEnable);

        if (!isControlEnable && player != null)
            player.Move(0f);

        if (!isControlEnable)
            isKeyboardMoveActive = false;
    }

    private void HandleEditorKeyboardInput()
    {
        if (!Application.isEditor || !enableKeyboardInEditor)
            return;

        if (!isControlEnable || player == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        float horizontal = 0f;

        if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
            horizontal -= 1f;

        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
            horizontal += 1f;

        if (horizontal != 0f)
        {
            player.Move(Mathf.Clamp(horizontal, -1f, 1f));
            isKeyboardMoveActive = true;
        }
        else if (isKeyboardMoveActive)
        {
            player.Move(0f);
            isKeyboardMoveActive = false;
        }

        if (keyboard[jumpKey].wasPressedThisFrame)
            player.Jump();
    }

    private void OnMoveLeftDown()
    {
        Log("OnMoveLeftDown");

        if (!isControlEnable || player == null)
        {
            Log("Left move blocked. isControlEnable=" + isControlEnable + ", player=" + (player != null ? player.name : "null"));
            return;
        }

        player.Move(-1f);
    }

    private void OnMoveRightDown()
    {
        Log("OnMoveRightDown");

        if (!isControlEnable || player == null)
        {
            Log("Right move blocked. isControlEnable=" + isControlEnable + ", player=" + (player != null ? player.name : "null"));
            return;
        }

        player.Move(1f);
    }

    private void OnMoveReleased()
    {
        Log("OnMoveReleased");

        if (player != null)
            player.Move(0f);
        else
            Log("Move release ignored. Player is null.");
    }

    private void OnJumpPressed()
    {
        Log("OnJumpPressed");

        if (!isControlEnable || player == null)
        {
            Log("Jump blocked. isControlEnable=" + isControlEnable + ", player=" + (player != null ? player.name : "null"));
            return;
        }

        player.Jump();
    }

    private void ApplyControlState()
    {
        if (moveLeftButton != null)
            moveLeftButton.interactable = isControlEnable;

        if (moveRightButton != null)
            moveRightButton.interactable = isControlEnable;

        if (jumpButton != null)
            jumpButton.interactable = isControlEnable;

        Log("ApplyControlState: interactable=" + isControlEnable);
    }

    private static void AddPointerEvent(Button button, EventTriggerType type, UnityAction<BaseEventData> callback)
    {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = button.gameObject.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(callback);
        trigger.triggers.Add(entry);
    }

    private void OnDestroy()
    {
        Log("OnDestroy");

        if (jumpButton != null)
            jumpButton.onClick.RemoveListener(OnJumpPressed);

        RemoveTriggers(moveLeftButton);
        RemoveTriggers(moveRightButton);
    }

    private static void RemoveTriggers(Button button)
    {
        if (button == null)
            return;

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger != null)
            trigger.triggers.Clear();
    }

    private void Log(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log("[PlayerControlUI] " + message, this);
    }
}
