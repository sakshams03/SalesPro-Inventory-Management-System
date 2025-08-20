using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Provider
{
    public class QueryDefintionProvider: IQueryDefintionProvider
    {
        private readonly string _location, _category, _subCategory;
        public QueryDefintionProvider(string location, string category, string subCategory )
        {
            _category = category;
            _location = location;
            _subCategory = subCategory;
        }
        public QueryDefinition CreateQueryDefintion()
        {
            string queryText = "SELECT * FROM c WHERE c.Location = @location";
            var queryDef = new QueryDefinition(queryText).WithParameter("@location", _location);

            if (!string.IsNullOrEmpty(_category))
            {
                queryText += " AND c.Category = @category";
                queryDef = queryDef.WithParameter("@category", _category);
            }

            if (!string.IsNullOrEmpty(_subCategory))
            {
                queryText += " AND c.Subcategory = @subCategory";
                queryDef = queryDef.WithParameter("@subCategory", _subCategory);
            }

            queryDef = new QueryDefinition(queryText)
                            .WithParameter("@location", _location)
                            .WithParameter("@category", _category)
                            .WithParameter("@subCategory", _subCategory);
            return queryDef;

        }
    }
}
