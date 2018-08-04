using Insight.Model;

namespace Insight.Parser
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Newtonsoft.Json;
    public class DataParser : IDataParser
    {
        /// <summary>
        /// Only cast validation is done. if the data is missing, it doesn't throw an exception
        /// </summary>
        /// <typeparam name="TEntity"></typeparam>
        /// <param name="jsonFullPath"></param>
        /// <returns></returns>
        public IEnumerable<TEntity> JsonParse<TEntity>(string jsonFullPath)
        {
            if(string.IsNullOrEmpty(jsonFullPath))
            {
                throw new ArgumentException("Json full path is null or empty");
            }

            using (StreamReader reader = new StreamReader(jsonFullPath))
            {
                string currentLine = null;
                while((currentLine = reader.ReadLine()) != null)
                {
                    // A generic verifier can be done using the method signature
                    // IEnumerable<TEntity> JsonParse<TEntity, TVerifyEntity>(string jsonFullPath);
                    // and discover the non-string TEntity property information and call the generic TryParse method of the type
                    // discovered using reflection. If TryParse fails, then log it and ignore the transaction log.
                    // Add more verification as needed.

                    // Data validation can be done using regular expressions

                    var verifierEntity = JsonConvert.DeserializeObject<InvalidCastVerifyEntity>(currentLine);
                    decimal amount;
                    long orderTime;
                    
                    if (!decimal.TryParse(verifierEntity.Amount, out amount) ||
                        !long.TryParse(verifierEntity.OrderTime, out orderTime))
                    {
                        // Log the currentLine to the logging system for the issue to be detected and handled
                        Console.WriteLine($"Invalid transaction log {currentLine}");
                    }
                    else
                    {
                        yield return JsonConvert.DeserializeObject<TEntity>(currentLine);
                    }
                }
            }
        }
    }
}
