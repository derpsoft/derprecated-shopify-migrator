using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack;
using ServiceStack.DataAnnotations;

namespace Derprecated.ShopifyMigrator.Models
{
    public class Product : IAuditable
    {
        private List<ProductMeta> _meta;

        public Product()
        {
            MetaDictionary = new Dictionary<string, ProductMeta>();
        }

        [PrimaryKey]
        [AutoIncrement]
        public int Id { get; set; }

        public long ShopifyId { get; set; }

        [Ignore]
        public List<string> Fields => MetaDictionary.Keys.ToList();

        [Ignore]
        public string Title => Get("title", string.Empty);

        [Ignore]
        public string Description => Get("description", string.Empty);

        private Dictionary<string, ProductMeta> MetaDictionary { get; set; }

        [Reference]
        public List<ProductMeta> Meta
        {
            get { return _meta; }
            set
            {
                _meta = value;
                MetaDictionary = value.ToSafeDictionary(m => m.Key);
            }
        }

        public DateTime CreateDate { get; set; }
        public DateTime ModifyDate { get; set; }
        public ulong RowVersion { get; set; }

        private void Set(string key, object value)
        {
            var meta = MetaDictionary.GetOrAdd(key.ToLowerInvariant(), k => new ProductMeta {Key = k});
            meta.Set(value);
        }

        private T Get<T>(string key, T fallback = default(T))
        {
            var m = MetaDictionary.GetValueOrDefault(key.ToLowerInvariant());
            return m == default(ProductMeta) ? fallback : m.Get<T>();
        }

        /// <summary>
        ///     Merge fields from source into this.
        /// </summary>
        /// <param name="source"></param>
        public void Merge(Dto.Product source)
        {
            Set("title", source.Title);
            Set("description", source.BodyHtml);
        }

        public static Product From(Dto.Product source)
        {
            var dest = new Product {ShopifyId = source.Id};

            dest.Merge(source);

            return dest;
        }

        public void OnInsert()
        {
            OnUpsert();
        }

        public void OnUpdate()
        {
            OnUpsert();
        }

        private void OnUpsert()
        {
            _meta = MetaDictionary.Values.ToList();
        }
    }
}