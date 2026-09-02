using System.Linq.Expressions;
using LinqKit;

namespace APP.Extensions;

public static class QueryableExtensions
{
    public static IQueryable<T> WhereSearch<T>(this IQueryable<T> source, string searchQuery, params Expression<Func<T, string>>[] properties)
    {
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return source;
        }

        // Match per word (not the whole phrase) so a query spanning multiple
        // fields - e.g. a full name typed against separate FirstName/LastName
        // columns - still matches: every word must be found in at least one
        // of the given properties, but not necessarily the same one.
        var words = searchQuery.ToLower().Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            var wordPredicate = PredicateBuilder.New<T>(false);

            foreach (var property in properties)
            {
                var lowerCaseProperty = Expression.Lambda<Func<T, bool>>(
                    Expression.Call(
                        Expression.Call(property.Body, "ToLower", null),
                        "Contains",
                        null,
                        Expression.Constant(word)
                    ),
                    property.Parameters
                );

                wordPredicate = wordPredicate.Or(lowerCaseProperty);
            }

            source = source.Where(wordPredicate);
        }

        return source;
    }
}