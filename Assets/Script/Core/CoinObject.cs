using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CoinObject : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private bool rotate = true;
    [SerializeField] private float rotateSpeed = 180f;

    [Header("Collect")]
    [SerializeField] private bool destroyOnCollect = true;

    private Collider2D coll;
    private bool isCollected;

    private void Awake()
    {
        coll = GetComponent<Collider2D>();
        coll.isTrigger = true;
    }

    private void Update()
    {
        if (!rotate || isCollected)
            return;

        transform.Rotate(0f, 0f, rotateSpeed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isCollected)
            return;

        PlayerController player = other.GetComponent<PlayerController>();
        if (player == null)
            return;

        isCollected = true;
        player.CollectCoin();

        coll.enabled = false;

        if (destroyOnCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }
}
