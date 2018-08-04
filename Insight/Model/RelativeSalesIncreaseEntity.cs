using System;

namespace Insight.Model
{
    [Serializable]
    public class RelativeSalesIncreaseEntity : DepartmentHourlyAggregateEntity
    {
        public double PercentRelativeIncrease { get; set; }
        public double HourlyAverage { get; set; }
        public long HourlyCount { get; set; }

        public void Copy(DepartmentHourlyAggregateEntity departmentHourlyAggregateEntity)
        {
            StoreNumber = departmentHourlyAggregateEntity.StoreNumber;
            Department = departmentHourlyAggregateEntity.Department;
            DateWithHour = departmentHourlyAggregateEntity.DateWithHour;
            TotalAmount = departmentHourlyAggregateEntity.TotalAmount;
        }
    }
}
