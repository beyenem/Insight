namespace Insight.Parser
{
    using System.Collections.Generic;
    public interface IDataParser
    {
        IEnumerable<TEntity> JsonParse<TEntity>(string jsonFullPath);
    }
}
