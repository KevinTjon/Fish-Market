using UnityEngine;
using XCharts.Runtime;
using System.Linq;

public class FishPriceChart : MonoBehaviour
{
    private LineChart chart;
    private Serie serie;

    private void Awake()
    {
        InitializeChart();
    }

    private void InitializeChart()
    {
        chart = gameObject.GetComponent<LineChart>();
        if (chart == null)
        {
            chart = gameObject.AddComponent<LineChart>();
        }
        
        chart.Init();
        chart.RemoveAllSerie();

        chart.GetChartComponent<Title>().text = "";
        chart.GetChartComponent<Title>().subText = "";

        // Disable tooltip
        var tooltip = chart.GetChartComponent<Tooltip>();
        tooltip.show = false;

        // Customize Y-axis
        var yAxis = chart.GetChartComponent<YAxis>();
        yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
        yAxis.splitLine.show = true;
        yAxis.splitLine.lineStyle.opacity = 0.2f;
        yAxis.axisLine.show = true;
        yAxis.axisTick.show = true;

        // Customize X-axis
        var xAxis = chart.GetChartComponent<XAxis>();
        xAxis.splitLine.show = false;
        xAxis.axisLine.show = true;
        xAxis.axisTick.show = true;

        // Create initial series
        serie = chart.AddSerie<Line>("Fish Prices");
        serie.symbol.show = true;
        serie.symbol.size = 6;
        serie.lineStyle.width = 2;
        serie.animation.enable = true;

        // Add initial empty data point to prevent index errors
        chart.AddXAxisData("Day 1");
        chart.AddData(0, 0);
    }

    public void UpdatePrices(float[] newPrices)
    {
        if (chart == null || serie == null)
        {
            InitializeChart();
        }
        
        // Round all prices to whole numbers
        float[] roundedPrices = newPrices.Select(price => Mathf.Round(price)).ToArray();
        
        // Find min and max values in the rounded price array
        float minPrice = Mathf.Min(roundedPrices);
        float maxPrice = Mathf.Max(roundedPrices);
        
        // Add more padding to the min/max
        float range = maxPrice - minPrice;
        float padding = range * 0.5f;
        
        // Update Y-axis range with rounded values
        var yAxis = chart.GetChartComponent<YAxis>();
        yAxis.min = Mathf.Max(0, Mathf.Floor(minPrice - padding));
        yAxis.max = Mathf.Ceil(maxPrice + padding);
        
        chart.ClearData();
        
        for (int i = 0; i < roundedPrices.Length; i++)
        {
            chart.AddXAxisData("Day " + (i + 1));
            chart.AddData(0, roundedPrices[i]);
        }

        chart.RefreshChart();
    }
}