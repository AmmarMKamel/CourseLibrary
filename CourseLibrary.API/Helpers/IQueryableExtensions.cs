using CourseLibrary.API.Services;
using System.Linq.Dynamic.Core;

namespace CourseLibrary.API.Helpers;

public static class IQueryableExtensions
{
    public static IQueryable<T> ApplySort<T>(
        this IQueryable<T> source,
        string orderBy,
        Dictionary<string, PropertyMappingValue> mappingDictionary)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        if (mappingDictionary == null)
        {
            throw new ArgumentNullException(nameof(mappingDictionary));
        }

        if (string.IsNullOrWhiteSpace(orderBy))
        {
            return source;
        }

        // e.g. "orderBy=name, mainCategory desc"
        var orderByString = string.Empty;

        var orderByAfterSplit = orderBy.Split(',');

        foreach (var orderByClause in orderByAfterSplit)
        {
            var trimmedOrderByClause = orderByClause.Trim();

            var orderDescending = trimmedOrderByClause.EndsWith(" desc");

            var indexOfFirstSpace = trimmedOrderByClause.IndexOf(" ");
            var propertyName = indexOfFirstSpace == -1
                ? trimmedOrderByClause
                : trimmedOrderByClause.Remove(indexOfFirstSpace);

            if (!mappingDictionary.ContainsKey(propertyName))
            {
                throw new ArgumentException($"Key mapping for {propertyName} is missing.");
            }

            var propertyMappingValue = mappingDictionary[propertyName];

            if (propertyMappingValue == null)
            {
                throw new ArgumentNullException(nameof(propertyMappingValue));
            }

            if (propertyMappingValue.Revert)
            {
                orderDescending = !orderDescending;
            }

            foreach (var destinationProperty in propertyMappingValue.DestinationProperties)
            {
                orderByString = orderByString +
                    (string.IsNullOrWhiteSpace(orderByString) ? string.Empty : ", ") +
                    destinationProperty +
                    (orderDescending ? " descending" : " ascending");
            }
        }

        return source.OrderBy(orderByString);
    }
}
