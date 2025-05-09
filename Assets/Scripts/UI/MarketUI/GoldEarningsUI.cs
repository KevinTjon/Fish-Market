using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using System;

public class GoldEarningsUI : MonoBehaviour
{
    [Header("Gold Earnings UI")]
    [SerializeField] private TextMeshProUGUI playerGoldText;
    [SerializeField] private TextMeshProUGUI seller1GoldText;
    [SerializeField] private TextMeshProUGUI seller2GoldText;
    [SerializeField] private TextMeshProUGUI seller3GoldText;
    [SerializeField] private TextMeshProUGUI seller4GoldText;

    [Header("Increase Arrow Images")]
    [SerializeField] private Image playerIncreaseArrow;
    [SerializeField] private Image seller1IncreaseArrow;
    [SerializeField] private Image seller2IncreaseArrow;
    [SerializeField] private Image seller3IncreaseArrow;
    [SerializeField] private Image seller4IncreaseArrow;

    private const float transitionDuration = 1.0f;
    private const float waitBeforeTransition = 1.0f;

    private void Start()
    {
        UpdateGoldEarnings();
    }

    // Call this to update all gold earnings UI
    public void UpdateGoldEarnings()
    {
        if (playerGoldText != null)
            playerGoldText.text = "Gold: " + GoldDB.GetGold(0).ToString("F0");
        if (seller1GoldText != null)
            seller1GoldText.text = "Gold: " + GoldDB.GetGold(1).ToString("F0");
        if (seller2GoldText != null)
            seller2GoldText.text = "Gold: " + GoldDB.GetGold(2).ToString("F0");
        if (seller3GoldText != null)
            seller3GoldText.text = "Gold: " + GoldDB.GetGold(3).ToString("F0");
        if (seller4GoldText != null)
            seller4GoldText.text = "Gold: " + GoldDB.GetGold(4).ToString("F0");
    }

    public void ShowGoldTransition()
    {
        StartCoroutine(GoldTransitionCoroutine());
    }

    private IEnumerator GoldTransitionCoroutine()
    {
        // Get previous and current gold for each seller
        float[] prevGold = new float[5];
        float[] currGold = new float[5];
        for (int i = 0; i < 5; i++)
        {
            prevGold[i] = 0f; // If you want to show previous gold, implement a similar GoldQuery.GetPreviousGold
            currGold[i] = GoldDB.GetGold(i);
        }

        // Hide all arrows initially
        SetArrow(playerIncreaseArrow, false);
        SetArrow(seller1IncreaseArrow, false);
        SetArrow(seller2IncreaseArrow, false);
        SetArrow(seller3IncreaseArrow, false);
        SetArrow(seller4IncreaseArrow, false);

        // Show previous day gold
        if (playerGoldText != null)
            playerGoldText.text = "Gold: " + prevGold[0].ToString("F0");
        if (seller1GoldText != null)
            seller1GoldText.text = "Gold: " + prevGold[1].ToString("F0");
        if (seller2GoldText != null)
            seller2GoldText.text = "Gold: " + prevGold[2].ToString("F0");
        if (seller3GoldText != null)
            seller3GoldText.text = "Gold: " + prevGold[3].ToString("F0");
        if (seller4GoldText != null)
            seller4GoldText.text = "Gold: " + prevGold[4].ToString("F0");

        // Wait for 1 second
        yield return new WaitForSeconds(waitBeforeTransition);

        // Animate transition to today's gold
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            float t = elapsed / transitionDuration;
            if (playerGoldText != null)
                playerGoldText.text = "Gold: " + Mathf.Lerp(prevGold[0], currGold[0], t).ToString("F0");
            if (seller1GoldText != null)
                seller1GoldText.text = "Gold: " + Mathf.Lerp(prevGold[1], currGold[1], t).ToString("F0");
            if (seller2GoldText != null)
                seller2GoldText.text = "Gold: " + Mathf.Lerp(prevGold[2], currGold[2], t).ToString("F0");
            if (seller3GoldText != null)
                seller3GoldText.text = "Gold: " + Mathf.Lerp(prevGold[3], currGold[3], t).ToString("F0");
            if (seller4GoldText != null)
                seller4GoldText.text = "Gold: " + Mathf.Lerp(prevGold[4], currGold[4], t).ToString("F0");
            elapsed += Time.deltaTime;
            yield return null;
        }
        // Ensure final value is set
        if (playerGoldText != null)
            playerGoldText.text = "Gold: " + currGold[0].ToString("F0");
        if (seller1GoldText != null)
            seller1GoldText.text = "Gold: " + currGold[1].ToString("F0");
        if (seller2GoldText != null)
            seller2GoldText.text = "Gold: " + currGold[2].ToString("F0");
        if (seller3GoldText != null)
            seller3GoldText.text = "Gold: " + currGold[3].ToString("F0");
        if (seller4GoldText != null)
            seller4GoldText.text = "Gold: " + currGold[4].ToString("F0");

        // Enable arrows if there was an increase
        SetArrow(playerIncreaseArrow, currGold[0] > prevGold[0]);
        SetArrow(seller1IncreaseArrow, currGold[1] > prevGold[1]);
        SetArrow(seller2IncreaseArrow, currGold[2] > prevGold[2]);
        SetArrow(seller3IncreaseArrow, currGold[3] > prevGold[3]);
        SetArrow(seller4IncreaseArrow, currGold[4] > prevGold[4]);
    }

    private void SetArrow(Image arrow, bool enabled)
    {
        if (arrow != null)
            arrow.enabled = enabled;
    }
} 