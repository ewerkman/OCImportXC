using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ImportXC.Services.Deleters
{
    public interface IItemProvider<T> where T : class
    {
        Task<Tuple<IList<T>, int>> GetAll(string catalogId, int page);
        Task Delete(string catalogId, T item);
    }
}