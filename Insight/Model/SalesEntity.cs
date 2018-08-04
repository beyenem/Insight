namespace Insight.Model
{
    using Newtonsoft.Json;
    public class SalesEntity
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("order_id")]
        public string OrderId { get; set; }
        [JsonProperty("order_time")]
        public long OrderTime { get; set; }
        [JsonProperty("store_number")]
        public string StoreNumber { get; set; }
        [JsonProperty("department")]
        public string Department { get; set; }
        [JsonProperty("register")]
        public string Register { get; set; }
        [JsonProperty("amount")]
        public decimal Amount { get; set; }
        [JsonProperty("upc")]
        public string Upc { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
    }
}
