using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerControlUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private Button moveLeftButton;
    [SerializeField] private Button moveRightButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private bool isControlEnable = true;

    public bool IsControlEnable => isControlEnable;

    private bool isInitialized;

    public void Initialize(PlayerController value)
    {
        player = value;

        if (isInitialized)
            return;

        if (jumpButton != null)
            jumpButton.onClick.AddListener(OnJumpPressed);

        if (moveLeftButton != null)
        {
            AddPointerEvent(moveLeftButton, EventTriggerType.PointerDown, _ => OnMoveLeftDown());
            AddPointerEvent(moveLeftButton, EventTriggerType.PointerUp, _ => OnMoveReleased());
            AddPointerEvent(moveLeftButton, EventTriggerType.PointerExit, _ => OnMoveReleased());
        }

        if (moveRightButton != null)
        {
            AddPointerEvent(moveRightButton, EventTriggerType.PointerDown, _ => OnMoveRightDown());
            AddPointerEvent(moveRightButton, EventTriggerType.PointerUp, _ => OnMoveReleased());
            AddPointerEvent(moveRightButton, EventTriggerType.PointerExit, _ => OnMoveReleased());
        }

        ApplyControlState();
        isInitialized = true;
    }

    public void SetPlayer(PlayerController value)
    {
        player = value;
    }

    public void SetControlEnable(bool value)
    {
        isControlEnable = value;
        ApplyControlState();

        if (!isControlEnable && player != null)
            player.Move(0f);
    }

    private void OnMoveLeftDown()
    {
        if (!isControlEnable || player == null)
            return;

        player.Move(-1f);
    }

    private void OnMoveRightDown()
    {
        if (!isControlEnable || player == null)
            return;

        player.Move(1f);
    }

    private void OnMoveReleased()
    {
        if (player != null)
            player.Move(0f);
    }

    private void OnJumpPressed()
    {
        if (!isControlEnable || player == null)
            return;

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
}
