using UnityEngine;
using XCharts.Runtime;
using System.Linq;

namespace FishMarket
{
    [DisallowMultipleComponent]
    public class FishPriceChart : MonoBehaviour
    {
        private LineChart chart;
        private Serie serie;
        private int m_DataNum;

        private void OnEnable()
        {
            InitializeChart();
        }

        private void InitializeChart()
        {
            chart = gameObject.GetComponent<LineChart>();
            if (chart == null)
            {
                chart = gameObject.AddComponent<LineChart>();
                chart.Init();
            }

            chart.GetChartComponent<Title>().text = "";
            chart.GetChartComponent<Title>().subText = "";

            // Disable tooltip
            var tooltip = chart.GetChartComponent<Tooltip>();
            tooltip.show = false;

             var yAxis = chart.GetChartComponent<YAxis>();
             yAxis.splitLine.lineStyle.opacity = 0.2f;
        }

        // Call this method to update the chart with new prices
        public void UpdatePrices(float[] newPrices)
        {
            if (chart == null) InitializeChart();
            
            // Round all prices to whole numbers
            float[] roundedPrices = newPrices.Select(price => Mathf.Round(price)).ToArray();
            
            // Find min and max values in the rounded price array
            float minPrice = Mathf.Min(roundedPrices);
            float maxPrice = Mathf.Max(roundedPrices);
            
            // Add more padding to the min/max (increased from 0.1f to 0.3f for 30% padding)
            float range = maxPrice - minPrice;
            float padding = range * 0.5f;
            
            // Update Y-axis range with rounded values
            var yAxis = chart.GetChartComponent<YAxis>();
            yAxis.min = Mathf.Max(0, Mathf.Floor(minPrice - padding)); // Round down min
            yAxis.max = Mathf.Ceil(maxPrice + padding); // Round up max
            
            chart.ClearData(); // Clear existing data
            
            for (int i = 0; i < roundedPrices.Length; i++)
            {
                chart.AddXAxisData("Day " + (i + 1));
                chart.AddData(0, roundedPrices[i]);
            }

            chart.RefreshChart();
        }
    }
}