using System.Collections.Generic;
using Insight.Model;

namespace Insight.Sales
{
    public interface ISalesInsight
    {
        void LogRegisterStateChange(IEnumerable<SalesEntity> salesEntities);
        void LogDepartmentSaleDecrease(IList<DepartmentHourlyAggregateEntity> departmentsHourlyAggregated, IList<StoreHourlyAggregateEntity> storeHourlyAggregated);
        void LogHighestRelativePercentIncrease(IList<DepartmentHourlyAggregateEntity> departmentsHourlyAggregated, IList<StoreHourlyAggregateEntity> storeHourlyAggregated);
    }
}
