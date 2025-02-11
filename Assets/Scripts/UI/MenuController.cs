using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuController : MonoBehaviour
{
    public GameObject menuCanvas;
    private MarketPriceGenerator marketPriceGenerator;

    // Start is called before the first frame update
    void Start()
    {
        menuCanvas.SetActive(false);
        marketPriceGenerator = GetComponent<MarketPriceGenerator>();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Tab)){
            menuCanvas.SetActive(!menuCanvas.activeSelf);
        }

        // Press 1 to generate prices for day 1
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            marketPriceGenerator.GeneratePricesForDay(1);
        }

        // Press 2 to clear all prices
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            marketPriceGenerator.ClearAllPrices();
        }
    }
}
