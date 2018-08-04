using System;
using System.Linq;
using Insight.Sales;

namespace Insight
{
    using System.Collections.Generic;
    using Model;
    using Parser;

    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            IDataParser dataparser = new DataParser();
            IList<SalesEntity> salesEntities = dataparser.JsonParse<SalesEntity>(@"C:\Users\benasm\salesValid.json").ToList();
            ISalesInsight salesInsight = new SalesInsight();
            salesInsight.LogRegisterStateChange(salesEntities);

            DateTime startDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var departmentsHourlyAggregated = salesEntities.GroupBy(saleEntity => new
            {
                saleEntity.StoreNumber,
                saleEntity.Department,
                DateWithHour = new DateTime(startDateTime.AddMilliseconds(saleEntity.OrderTime).Year,
                    startDateTime.AddMilliseconds(saleEntity.OrderTime).Month,
                    startDateTime.AddMilliseconds(saleEntity.OrderTime).Day,
                    startDateTime.AddMilliseconds(saleEntity.OrderTime).Hour, 0, 0)
            }).Select(groupedData => new DepartmentHourlyAggregateEntity()
            {
                StoreNumber = groupedData.Key.StoreNumber,
                Department = groupedData.Key.Department,
                DateWithHour = groupedData.Key.DateWithHour,
                TotalAmount = groupedData.Sum(d => d.Amount)
            }).ToList();
            var storeHourlyAggregated = departmentsHourlyAggregated.GroupBy(departmentsHourlyAggregate => new
            {
                departmentsHourlyAggregate.StoreNumber,
                departmentsHourlyAggregate.DateWithHour
            }).Select(groupedData => new StoreHourlyAggregateEntity()
            {
                StoreNumber = groupedData.Key.StoreNumber,
                DateWithHour = groupedData.Key.DateWithHour,
                TotalAmount = groupedData.Sum(d => d.TotalAmount)
            }).OrderBy(d => d.DateWithHour).ToList();
            salesInsight.LogDepartmentSaleDecrease(departmentsHourlyAggregated, storeHourlyAggregated);
            salesInsight.LogHighestRelativePercentIncrease(departmentsHourlyAggregated, storeHourlyAggregated);
            Console.Write("Press any key to close");
            Console.Read();
        }
    }

}
