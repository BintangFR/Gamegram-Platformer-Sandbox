using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private List<Image> coinImages = new List<Image>();
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private float activeAlpha = 1f;
    [SerializeField] private bool enableDebugLogs;

    private void OnEnable()
    {
        TryBindPlayer();
        Refresh(player != null ? player.CoinCount : 0);
    }

    private void Update()
    {
        if (player == null)
            TryBindPlayer();
    }

    private void OnDisable()
    {
        UnbindPlayer();
    }

    public void SetPlayer(PlayerController value)
    {
        if (player == value)
            return;

        UnbindPlayer();
        player = value;
        BindPlayer();
        Refresh(player != null ? player.CoinCount : 0);
    }

    private void TryBindPlayer()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

        BindPlayer();
    }

    private void BindPlayer()
    {
        if (player == null)
            return;

        player.CoinChanged -= OnCoinChanged;
        player.CoinChanged += OnCoinChanged;

        Log("Bound to player: " + player.name + " | CoinCount=" + player.CoinCount);
    }

    private void UnbindPlayer()
    {
        if (player == null)
            return;

        player.CoinChanged -= OnCoinChanged;
    }

    private void OnCoinChanged(int coinCount)
    {
        Log("OnCoinChanged: " + coinCount);
        Refresh(coinCount);
    }

    private void Refresh(int collectedCount)
    {
        for (int i = 0; i < coinImages.Count; i++)
        {
            Image image = coinImages[i];
            if (image == null)
                continue;

            Color color = image.color;
            color.a = i < collectedCount ? activeAlpha : inactiveAlpha;
            image.color = color;
        }
    }

    private void Log(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log("[CoinUI] " + message, this);
    }
}
