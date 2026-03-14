using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private LayerMask wallLayer = ~0;

    [Header("Stomp")]
    [SerializeField] private float stompBounceForce = 8f;
    [SerializeField] private float stompTopTolerance = 0.1f;
    [SerializeField] private float minFallVelocityForStomp = -0.05f;

    [Header("Player Defeat")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Timer timer;
    [SerializeField] private FinishScreen finishScreen;
    [SerializeField] private bool disablePlayerControllerOnDefeat = true;

    private Rigidbody2D rb;
    private Collider2D coll;
    private int moveDirection = -1;
    private bool isDead;
    private bool hasTriggeredLose;
    private bool isInitialized;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();

        if (timer == null)
            timer = FindFirstObjectByType<Timer>(FindObjectsInactive.Include);

        if (finishScreen == null)
            finishScreen = FindFirstObjectByType<FinishScreen>(FindObjectsInactive.Include);
    }

    public void Initialize()
    {
        if (isInitialized)
            return;

        SetFacingDirection(moveDirection);
        isInitialized = true;
    }

    private void FixedUpdate()
    {
        if (!isInitialized || isDead)
            return;

        rb.linearVelocity = new Vector2(moveDirection * moveSpeed, rb.linearVelocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead || hasTriggeredLose)
            return;

        if (TryHandleStomp(collision))
            return;

        if (TryHandlePlayerDefeat(collision))
            return;

        if (!IsInLayerMask(collision.gameObject.layer, wallLayer))
            return;

        if (IsWallHit(collision))
            FlipDirection();
    }

    private bool TryHandleStomp(Collision2D collision)
    {
        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player == null)
            return false;

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        Collider2D playerColl = collision.gameObject.GetComponent<Collider2D>();
        if (playerRb == null || playerColl == null)
            return false;

        bool playerIsFalling = playerRb.linearVelocity.y <= minFallVelocityForStomp;
        if (!playerIsFalling)
            return false;

        bool playerAboveEnemyByBounds = playerColl.bounds.min.y >= coll.bounds.max.y - stompTopTolerance;
        bool topContact = HasTopContact(collision);

        if (!playerAboveEnemyByBounds && !topContact)
            return false;

        Vector2 velocity = playerRb.linearVelocity;
        if (velocity.y < stompBounceForce)
            velocity.y = stompBounceForce;
        playerRb.linearVelocity = velocity;

        Die();
        return true;
    }

    private bool TryHandlePlayerDefeat(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag(playerTag))
            return false;

        hasTriggeredLose = true;

        if (timer != null)
            timer.StopTimer();

        if (finishScreen != null)
            finishScreen.ShowLose();

        if (disablePlayerControllerOnDefeat)
        {
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null)
                player.enabled = false;
        }

        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        if (playerRb != null)
            playerRb.linearVelocity = Vector2.zero;

        return true;
    }

    private bool HasTopContact(Collision2D collision)
    {
        int contactCount = collision.contactCount;
        Vector2 enemyCenter = coll.bounds.center;

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            bool contactOnUpperHalf = contact.point.y >= enemyCenter.y;
            bool mostlyVerticalContact = Mathf.Abs(contact.normal.y) > 0.5f;

            if (contactOnUpperHalf && mostlyVerticalContact)
                return true;
        }

        return false;
    }

    private bool IsWallHit(Collision2D collision)
    {
        int contactCount = collision.contactCount;

        for (int i = 0; i < contactCount; i++)
        {
            ContactPoint2D contact = collision.GetContact(i);
            if (Mathf.Abs(contact.normal.x) > 0.5f)
                return true;
        }

        return false;
    }

    private void FlipDirection()
    {
        moveDirection *= -1;
        SetFacingDirection(moveDirection);
    }

    private void SetFacingDirection(int direction)
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
        transform.localScale = scale;
    }

    private void Die()
    {
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        coll.enabled = false;
        Destroy(gameObject);
    }

    private static bool IsInLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }
}
