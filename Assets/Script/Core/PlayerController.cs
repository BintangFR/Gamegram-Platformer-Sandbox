using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 10f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private float groundCheckDistance = 0.1f;

    public event Action<int> CoinChanged;
    public int CoinCount => coinCount;

    private Rigidbody2D rb;
    private Collider2D coll;
    private float horizontalInput;
    private bool isGrounded;
    private bool facingRight = true;
    private int coinCount;
    private bool isInitialized;

    public void Initialize()
    {
        if (isInitialized)
            return;

        rb = GetComponent<Rigidbody2D>();
        coll = GetComponent<Collider2D>();
        isInitialized = true;
    }

    private void Update()
    {
        if (!isInitialized)
            return;

        CheckSurroundings();
    }

    private void FixedUpdate()
    {
        if (!isInitialized)
            return;

        rb.linearVelocity = new Vector2(horizontalInput * moveSpeed, rb.linearVelocity.y);

        if (horizontalInput > 0f && !facingRight)
            Flip();
        else if (horizontalInput < 0f && facingRight)
            Flip();
    }

    public void CollectCoin()
    {
        coinCount++;
        CoinChanged?.Invoke(coinCount);
    }

    private void CheckSurroundings()
    {
        RaycastHit2D hit = Physics2D.BoxCast(
            coll.bounds.center,
            coll.bounds.size,
            0f,
            Vector2.down,
            groundCheckDistance,
            groundLayer
        );

        isGrounded = hit.collider != null;
    }

    private void Flip()
    {
        facingRight = !facingRight;
        Vector3 scaler = transform.localScale;
        scaler.x *= -1f;
        transform.localScale = scaler;
    }

    public void Move(float direction)
    {
        horizontalInput = direction;
    }

    public void Jump()
    {
        if (!isInitialized || !isGrounded)
            return;

        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying || coll == null)
            return;

        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireCube(coll.bounds.center + Vector3.down * groundCheckDistance, coll.bounds.size);
    }
}
