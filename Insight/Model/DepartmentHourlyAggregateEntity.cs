using System;
namespace Insight.Model
{
    [Serializable]
    public class DepartmentHourlyAggregateEntity : StoreHourlyAggregateEntity
    {
        public string Department { get; set; }
    }
}
