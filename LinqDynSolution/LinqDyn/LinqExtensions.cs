﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LinqDyn
{
    public static class LinqExtensions
    {
        public static IOrderedQueryable<TSource> OrderBy<TSource>(this IQueryable<TSource> query, string propertyName)
        {
            return OrderBy(query, propertyName, false);
        }
        public static IOrderedQueryable<TSource> OrderByDescending<TSource>(this IQueryable<TSource> query, string propertyName)
        {
            return OrderBy(query, propertyName, true);
        }
        public static IQueryable<TSource> FilterBy<TSource, T>(this IQueryable<TSource> query, T filter) where T : new()
        {
            Expression<Func<TSource, bool>> predicate = (x) => true;
            var sourceType = typeof(TSource);
            var filterType = filter.GetType();


            var filterProps = filterType.GetProperties();
            foreach (var filterProp in filterProps)
            {
                var filterInfo = filterProp.GetCustomAttribute<FilterInfoAttribute>();
                if (filterInfo == null) { filterInfo = new FilterInfoAttribute(FilterOperator.Equal); }
                PropertyInfo sourceProp = sourceType.GetProperty(filterInfo.GetTargetPropertyName() ?? filterProp.Name);

                var currentOperator = filterInfo.GetOperator();
                if (currentOperator == FilterOperator.None) { continue; }
                if (sourceProp == null) { continue; }
                if (filterProp.GetValue(filter) == null) { continue; }
                switch (currentOperator)
                {
                    case FilterOperator.Equal:
                        predicate = FilterExpressions.EqualityFilter<TSource>(sourceProp, filterProp.GetValue(filter));
                        break;
                    case FilterOperator.GreaterOrEqual:
                        predicate = FilterExpressions.GreaterOrEqualFilter<TSource>(sourceProp, filterProp.GetValue(filter));
                        break;
                    case FilterOperator.LessOrEqual:
                        predicate = FilterExpressions.LessOrEqualFilter<TSource>(sourceProp, filterProp.GetValue(filter));
                        break;
                    case FilterOperator.Contains:
                        predicate = FilterExpressions.TextContainsFilter<TSource>(sourceProp, filterProp.GetValue(filter));
                        break;
                    default:
                        predicate = (x) => true;
                        break;
                }
                query = query.Where(predicate);
            }
            return query;
        }

        private static IOrderedQueryable<TSource> OrderBy<TSource>(IQueryable<TSource> query, string propertyName, bool descending)
        {
            string method = descending ? "OrderByDescending" : "OrderBy";
            var type = typeof(TSource);
            var property = type.GetProperty(propertyName);
            if (property == null) { return (IOrderedQueryable<TSource>)query; }
            var parameter = Expression.Parameter(type, "p");
            var body = Expression.MakeMemberAccess(parameter, property);
            var sortExpression = Expression.Lambda(body, parameter);
            var resultExpression = Expression.Call(typeof(Queryable), method, new Type[] { type, property.PropertyType }, query.Expression, Expression.Quote(sortExpression));
            return (IOrderedQueryable<TSource>)query.Provider.CreateQuery<TSource>(resultExpression);
        }

        public static IOrderedEnumerable<TSource> OrderBy<TSource>(this IEnumerable<TSource> query, string propertyName)
        {
            return query.AsQueryable().OrderBy(propertyName).AsEnumerable().OrderBy(e => 1);
        }
        public static IOrderedEnumerable<TSource> OrderByDescending<TSource>(this IEnumerable<TSource> query, string propertyName)
        {
            return query.AsQueryable().OrderByDescending(propertyName).AsEnumerable().OrderByDescending(e => 1);
        }
        public static IEnumerable<TSource> FilterBy<TSource, T>(this IEnumerable<TSource> query, T filter) where T : new()
        {
            return query.AsQueryable().FilterBy(filter).AsEnumerable();
        }
    }
}
