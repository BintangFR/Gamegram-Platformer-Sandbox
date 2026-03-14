using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoinUI : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private List<Image> coinImages = new List<Image>();
    [SerializeField] private float inactiveAlpha = 0.5f;
    [SerializeField] private float activeAlpha = 1f;

    private void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            player.CoinChanged += OnCoinChanged;

        Refresh(0);
    }

    private void OnDestroy()
    {
        if (player != null)
            player.CoinChanged -= OnCoinChanged;
    }

    private void OnCoinChanged(int coinCount)
    {
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
}
