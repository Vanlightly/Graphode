using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities
{
    public class IndexedList<TIndex, TEntity>
    {
        private Dictionary<TIndex, List<TEntity>> _data;

        public IndexedList()
        {
            _data = new Dictionary<TIndex, List<TEntity>>();
        }

        public bool HasIndex(TIndex indexValue)
        {
            return _data.ContainsKey(indexValue);
        }

        public void Add(TIndex indexValue, TEntity t)
        {
            if (_data.ContainsKey(indexValue))
                _data[indexValue].Add(t);
            else
                _data.Add(indexValue, new List<TEntity>() { t });
        }

        public void SetList(TIndex indexValue, List<TEntity> t)
        {
            if (_data.ContainsKey(indexValue))
                _data[indexValue] = t;
            else
                _data.Add(indexValue, t);
        }

        public List<TEntity> Get(TIndex indexValue)
        {
            if (_data.ContainsKey(indexValue))
                return _data[indexValue];
            else
                return new List<TEntity>();
        }

        public TEntity FirstOrDefault(TIndex indexValue)
        {
            if (_data.ContainsKey(indexValue))
                return _data[indexValue].FirstOrDefault();
            else
                return default(TEntity);
        }

        public List<TIndex> Indexes
        {
            get { return _data.Keys.ToList(); }
        }

        public Dictionary<TIndex, List<TEntity>>.ValueCollection IndexValues
        {
            get { return _data.Values; }
        }

    }
}
