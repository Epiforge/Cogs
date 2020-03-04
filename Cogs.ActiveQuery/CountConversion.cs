using System;
using System.Collections.Concurrent;
using System.Linq.Expressions;

namespace Gear.ActiveQuery
{
    internal static class CountConversion
    {
        static readonly ConcurrentDictionary<Type, CountConversionDelegate> converters = new ConcurrentDictionary<Type, CountConversionDelegate>();

        static CountConversionDelegate CreateConverter(Type type)
        {
            var countParameter = Expression.Parameter(typeof(int));
            return Expression.Lambda<CountConversionDelegate>(Expression.Convert(Expression.Convert(countParameter, type), typeof(object)), countParameter).Compile();
        }

        public static CountConversionDelegate GetConverter(Type type) => converters.GetOrAdd(type, CreateConverter);
    }
}
