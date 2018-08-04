using System;
using System.Collections.Generic;
using System.Linq;
using Insight.Model;

namespace Insight.Sales
{
    public class SalesInsight : ISalesInsight
    {
        private const int ConsecutiveHoursDecrease = 3;

        private const int RelativeIncreaseHoursCount = 6;

        private const float FloatingPointComparisionTolerance = 0.0000001f;

        /// <summary>
        /// Displays registers that has state change.The algorithm is
        /// 1. It groups sales data based on store, department and register
        /// 2. Orders the grouped data by time in ascending order
        /// 3. Detect state change and log if it is detected.The algorithm to detect state change
        ///    a. The first SalesEntity is the beginning of the transaction and register state is accepting
        ///    b. If the register is in active for a defined seconds,  gap between the previous and current transaction is calculated,
        ///       if the register is inactive for the defined seconds, the previous transaction register state is "not accepting transaction"
        ///       and the current transaction state is "accepting transaction"
        /// </summary>
        /// <param name="salesEntities"></param>
        public void LogRegisterStateChange(IEnumerable<SalesEntity> salesEntities)
        {
            string accepting = "accepting transaction";
            string notAccepting = "not accepting transaction";

            //Increased the interval to decrease log size
            long rangeInSecondsForStateChange = 60000000;
            var groupedSalesEntities = salesEntities.OrderBy(saleEntity => saleEntity.OrderTime)
                .GroupBy(saleEntity => new {saleEntity.StoreNumber, saleEntity.Department, saleEntity.Register});
            foreach (var groupedSaleEntity in groupedSalesEntities)
            {
                SalesEntity previouSalesEntity = null;
                foreach (var salesEntity in groupedSaleEntity.ToList())
                {
                    if (previouSalesEntity == null)
                    {
                        Console.WriteLine(
                            $"{salesEntity.StoreNumber}  {salesEntity.Department}  {salesEntity.Register}   {salesEntity.OrderTime}  {accepting}");
                    }
                    else
                    {
                        if (salesEntity.OrderTime - rangeInSecondsForStateChange > previouSalesEntity.OrderTime)
                        {
                            Console.WriteLine(
                                $"{previouSalesEntity.StoreNumber}  {previouSalesEntity.Department}  {previouSalesEntity.Register}   {previouSalesEntity.OrderTime}  {notAccepting}");
                            Console.WriteLine(
                                $"{salesEntity.StoreNumber}  {salesEntity.Department}  {salesEntity.Register}   {salesEntity.OrderTime}  {accepting}");

                        }
                    }

                    previouSalesEntity = salesEntity;
                }
            }
        }

        /// <summary>
        /// Displays the department with total sales rank decreases compared to others for three consecutive hours. Two data sets are required
        /// 1. Hourly aggregated sales of the stores
        /// 2. Hourly aggregated sales of each department within a store.
        ///
        /// First aggregate the sales data by department (detail aggregation first), then use the aggregated department data to aggregate by store.
        /// The department's total sales rank is calculated using "total department sale" / "total store sale" . Departments
        /// with higher sales rank performs better.The ranking calculation might change if there are more data points.Example, expensive items
        /// impacts total sales.This can be resolved by considering count.However, department with food items and daily used items creates a huge transaction logs
        /// that impacts the count.This can be better defined by a domain expert.However, this method only considers total sales.
        /// </summary>
        /// <param name="departmentsHourlyAggregated"></param>
        /// <param name="storeHourlyAggregated"></param>
        public void LogDepartmentSaleDecrease(IList<DepartmentHourlyAggregateEntity> departmentsHourlyAggregated, IList<StoreHourlyAggregateEntity> storeHourlyAggregated)
        {
            if (departmentsHourlyAggregated == null || storeHourlyAggregated == null)
            {
                throw new ArgumentNullException();
            }

            // Get the stores hours of operation from store hourly aggreagted data as it has the lowest data size and sorted by date
            var storesHoursOfOperation = storeHourlyAggregated.Select(d => d.DateWithHour).ToList();
            var lastOrderTime = storesHoursOfOperation.Last();
            var uniqueStoreList = storeHourlyAggregated.Select(d => d.StoreNumber).Distinct();

            // Description of the below algorithm
            // 1. Loops through all stores
            // 2. Gets distinct list of departments within the store and loops through all departments
            // 3. Loops through all stores operation hour. This can be changed to store operation hours as stores might have different opening hours
            // 4. Gets three hours of consecutive aggregated department data for the store based on the current operation hour and next three hours from the current hour.
            // 5. Gets the three hours of current department data and compare against the rest of the department three hours data
            // 6. Compare two departments if both have three consecutive hours data 
            // 7. If the three consecutive hours data of the current department is lower compared the target department, log it
            foreach (var storeNumber in uniqueStoreList)
            {
                var uniqueStoreDepartments = departmentsHourlyAggregated.Where(d => d.StoreNumber.Equals(storeNumber))
                    .Select(d => d.Department).Distinct().ToList();
                foreach (var department in uniqueStoreDepartments)
                {
                    foreach (var storesHourOfOperation in storesHoursOfOperation)
                    {
                        if (storesHourOfOperation.AddHours(ConsecutiveHoursDecrease) <= lastOrderTime)
                        {
                            var nextConsecutiveHoursDepartment = departmentsHourlyAggregated.Where(d =>
                                d.StoreNumber.Equals(storeNumber) && storesHourOfOperation <= d.DateWithHour &&
                                storesHourOfOperation.AddHours(ConsecutiveHoursDecrease) > d.DateWithHour).ToList();
                            var currentConsecutiveHoursDepartment =
                                nextConsecutiveHoursDepartment.Where(d => d.Department.Equals(department)).ToList();
                            if (currentConsecutiveHoursDepartment.Count() == ConsecutiveHoursDecrease)
                            {
                                foreach (var decreaseDepartment in uniqueStoreDepartments)
                                {
                                    if (!department.Equals(decreaseDepartment))
                                    {
                                        var tobeCompareConsecutiveHoursDepartment =
                                            nextConsecutiveHoursDepartment.Where(d =>
                                                d.Department.Equals(decreaseDepartment)).ToList();
                                        if (tobeCompareConsecutiveHoursDepartment.Count() == ConsecutiveHoursDecrease)
                                        {
                                            var threeConsecativeDecrease = true;
                                            foreach (var currentConsecutiveHourDepartment in
                                                currentConsecutiveHoursDepartment)
                                            {
                                                var currentStoreTotalSales = storeHourlyAggregated.First(d =>
                                                    d.StoreNumber.Equals(storeNumber) && d.DateWithHour ==
                                                    currentConsecutiveHourDepartment.DateWithHour).TotalAmount;
                                                var tobeCompareConsecutiveHourDepartment =
                                                    tobeCompareConsecutiveHoursDepartment.First(d =>
                                                        d.DateWithHour == currentConsecutiveHourDepartment
                                                            .DateWithHour);
                                                if (currentStoreTotalSales == 0 ||
                                                    currentConsecutiveHourDepartment.TotalAmount /
                                                    currentStoreTotalSales >
                                                    tobeCompareConsecutiveHourDepartment.TotalAmount /
                                                    currentStoreTotalSales)
                                                {
                                                    threeConsecativeDecrease = false;
                                                    break;
                                                }
                                            }

                                            if (threeConsecativeDecrease)
                                            {
                                                Console.WriteLine(
                                                    $"Store number: {storeNumber} department: {department} shows three consecative hours decrease compared to department: {decreaseDepartment}");
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="departmentsHourlyAggregated"></param>
        /// <param name="storeHourlyAggregated"></param>
        public void LogHighestRelativePercentIncrease(IList<DepartmentHourlyAggregateEntity> departmentsHourlyAggregated,
            IList<StoreHourlyAggregateEntity> storeHourlyAggregated)
        {
            if (departmentsHourlyAggregated == null || storeHourlyAggregated == null)
            {
                throw new ArgumentNullException();
            }

            IList<RelativeSalesIncreaseEntity> departmentsRelativeSalesIncrease = new List<RelativeSalesIncreaseEntity>();
            var uniqueStoreList = storeHourlyAggregated.Select(d => d.StoreNumber).Distinct().ToList();
            foreach (var storeNumber in uniqueStoreList)
            {
                var storeHoursOfOperations = storeHourlyAggregated.Where(d => d.StoreNumber.Equals(storeNumber))
                    .Select(d => d.DateWithHour).Distinct().OrderBy(d => d).ToList();
                var uniqueStoreDepartments = departmentsHourlyAggregated.Where(d => d.StoreNumber.Equals(storeNumber))
                    .Select(d => d.Department).Distinct().ToList();
                var storeDepartmentsHourlyAggreagte =
                    departmentsHourlyAggregated.Where(d => d.StoreNumber.Equals(storeNumber)).ToList();
                foreach (var department in uniqueStoreDepartments)
                {
                    var departmentHourlyAggregate = storeDepartmentsHourlyAggreagte
                        .Where(d => d.Department.Equals(department)).ToList();
                    foreach (var storeHourOfOperations in storeHoursOfOperations)
                    {
                        var previousStoreOperations =
                            storeHoursOfOperations.Where(d => d < storeHourOfOperations).OrderByDescending(d => d).Take(RelativeIncreaseHoursCount).ToList();
                        var currentDepartmentHourlyAggregate =
                            departmentHourlyAggregate.FirstOrDefault(d => d.DateWithHour == storeHourOfOperations);
                        if (currentDepartmentHourlyAggregate != null)
                        {
                            int hourlyCount = 0;
                            double hourlyAverage = 0;
                            var previousDepartmentHourlyAggregate = departmentHourlyAggregate
                                .Where(d => previousStoreOperations.Contains(d.DateWithHour)).ToList();
                            if (previousDepartmentHourlyAggregate.Any())
                            {
                                hourlyCount = previousDepartmentHourlyAggregate.Count;
                                hourlyAverage = (double) previousDepartmentHourlyAggregate.Average(d => d.TotalAmount);
                            }

                            var relativeSalesIncreaseEntity = new RelativeSalesIncreaseEntity()
                            {
                                HourlyCount = hourlyCount,
                                HourlyAverage = hourlyAverage,
                                PercentRelativeIncrease = hourlyCount == 0? 0 : ((double)currentDepartmentHourlyAggregate.TotalAmount - hourlyAverage) * 100 / hourlyAverage
                            };

                            relativeSalesIncreaseEntity.Copy(currentDepartmentHourlyAggregate);
                            departmentsRelativeSalesIncrease.Add(relativeSalesIncreaseEntity);
                        }
                    }
                }
            }

            LogStoreWithHighestPercentRelativeIncrease(departmentsRelativeSalesIncrease);
            LogDepartmentsWithinStoreWithHighestPercentRelativeIncrease(uniqueStoreList, departmentsRelativeSalesIncrease);
        }

        /// <summary>
        /// Generates hourly percent sales increase for each store hourly for hours that has previous 6 hours average sales.
        /// Select stores with the highest percent increase and log them.
        /// </summary>
        /// <param name="departmentsRelativeSalesIncrease"></param>
        private void LogStoreWithHighestPercentRelativeIncrease(IList<RelativeSalesIncreaseEntity> departmentsRelativeSalesIncrease)
        {
            if (departmentsRelativeSalesIncrease == null)
            {
                throw new ArgumentNullException();
            }

            var storesRelativeSalesIncrease = departmentsRelativeSalesIncrease
                .Where( d => d.HourlyCount == RelativeIncreaseHoursCount)
                .GroupBy(departmentRelativeSalesIncrease => new
                {
                    departmentRelativeSalesIncrease.StoreNumber,
                    departmentRelativeSalesIncrease.DateWithHour
                })
                .Select(groupedData => new
                {
                    StoreNumber = groupedData.Key.StoreNumber,
                    DateWithHour = groupedData.Key.DateWithHour,
                    HourlyCountAverage = groupedData.Average(d => d.HourlyCount),
                    HourlyPercentIncreaseAverage = groupedData.Average(d => d.PercentRelativeIncrease)
                }).ToList();

            var storesHourlyHighestPercentIncrease = storesRelativeSalesIncrease
                .Where(storeRelativeSalesIncrease => storeRelativeSalesIncrease.HourlyPercentIncreaseAverage > 0)
                .GroupBy(storeRelativeSalesIncrease => storeRelativeSalesIncrease.DateWithHour)
                .Select(groupedData => new
                {
                    DateWithHour = groupedData.Key,
                    HighestHourlyPercentIncrease = groupedData.Max(d => d.HourlyPercentIncreaseAverage)
                }).OrderBy(d => d.DateWithHour);
            foreach (var storeHourlyHighestPercentIncrease in storesHourlyHighestPercentIncrease)
            {
                var storesWithHighestPercentIncreaseForTheHour = storesRelativeSalesIncrease.Where(d =>
                    d.DateWithHour == storeHourlyHighestPercentIncrease.DateWithHour &&
                    Math.Abs(d.HourlyPercentIncreaseAverage - storeHourlyHighestPercentIncrease.HighestHourlyPercentIncrease) < FloatingPointComparisionTolerance);
                foreach (var store in storesWithHighestPercentIncreaseForTheHour)
                {
                   Console.WriteLine($"Order hour: {store.DateWithHour} store:  {store.StoreNumber} increased by {Math.Round(store.HourlyPercentIncreaseAverage, 2)}%"); 
                }
            }
        }

        /// <summary>
        /// It compares the highest percent increase for departments within a store. First, it finds out the maximum highest increase for a given date and
        /// hour if 6 hours previous average is calculated. Then it filters out departments that hit that maximum percent increase and logs to the standard
        /// output.
        /// </summary>
        /// <param name="uniqueStoreList"></param>
        /// <param name="departmentsRelativeSalesIncrease"></param>
        private void LogDepartmentsWithinStoreWithHighestPercentRelativeIncrease(
            IList<string> uniqueStoreList, 
            IList<RelativeSalesIncreaseEntity> departmentsRelativeSalesIncrease)
        {
            if (departmentsRelativeSalesIncrease == null || uniqueStoreList == null)
            {
                throw new ArgumentNullException();
            }

            foreach (var storeNumber in uniqueStoreList)
            {
                var storeDepartmentsRelativeSalesIncrease = departmentsRelativeSalesIncrease.Where(d =>
                    d.StoreNumber.Equals(storeNumber) &&
                    d.PercentRelativeIncrease > 0 &&
                    d.HourlyCount == RelativeIncreaseHoursCount).ToList();

                var departmentsHourlyHighestPercentIncrease = storeDepartmentsRelativeSalesIncrease
                    .GroupBy(d => d.DateWithHour)
                    .Select(groupedData => new
                    {
                        DateWithHour = groupedData.Key,
                        HighestHourlyPercentIncrease = groupedData.Max(d => d.PercentRelativeIncrease)
                    }).OrderBy(d => d.DateWithHour).ToList();
                foreach (var departmentHourlyHighestPercentIncrease in departmentsHourlyHighestPercentIncrease)
                {
                    var departmentsWithHighestPercentIncreaseForTheHour = storeDepartmentsRelativeSalesIncrease.Where(d =>
                        d.DateWithHour == departmentHourlyHighestPercentIncrease.DateWithHour &&
                        Math.Abs(d.PercentRelativeIncrease -
                                 departmentHourlyHighestPercentIncrease.HighestHourlyPercentIncrease) <
                        FloatingPointComparisionTolerance);
                    foreach (var dept in departmentsWithHighestPercentIncreaseForTheHour
                    )
                    {
                        Console.WriteLine($@"Order hour: {dept.DateWithHour} store: {dept.StoreNumber} and department:{dept.Department} increased by {Math.Round(dept.PercentRelativeIncrease, 2)}%");
                    }
                }
            }
        }
    }
}
