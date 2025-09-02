namespace properties.Api.Infrastructure.Cache
{
    public static class CacheKeyHelper
    {
        public const string AllOwnersKey = "AllOwners";
        public static string OwnerByIdKey(int id) => $"Owner_{id}";
        
        public const string AllPropertiesKeyPrefix = "PropertiesList_";
        public static string PropertyByIdKey(int id) => $"Property_{id}";
        
        public static string PropertiesListKey() => AllPropertiesKeyPrefix + "All";
        
        public static string GenerateListCacheKey(int pageNumber, int pageSize, string name = null, 
            decimal? minPrice = null, decimal? maxPrice = null, int? year = null, int? ownerId = null)
        {
            var key = $"{AllPropertiesKeyPrefix}Page_{pageNumber}_Size_{pageSize}";
            
            if (!string.IsNullOrWhiteSpace(name))
                key += $"_Name_{name.Trim()}";
                
            if (minPrice.HasValue)
                key += $"_MinPrice_{minPrice}";
                
            if (maxPrice.HasValue)
                key += $"_MaxPrice_{maxPrice}";
                
            if (year.HasValue)
                key += $"_Year_{year}";
                
            if (ownerId.HasValue)
                key += $"_Owner_{ownerId}";
                
            return key;
        }
    }
}
