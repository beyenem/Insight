using System;
namespace Insight.Model
{
    [Serializable]
    public class StoreHourlyAggregateEntity
    {
        public string StoreNumber { get; set; }
        public DateTime DateWithHour { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
