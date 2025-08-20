using Microsoft.Azure.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Provider
{
    public interface IQueryDefintionProvider
    {
        QueryDefinition CreateQueryDefintion();
    }
}
