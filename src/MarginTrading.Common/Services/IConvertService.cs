using System;
using AutoMapper;

namespace MarginTrading.Common.Services
{
    public interface IConvertService
    {
        TResult Convert<TSource, TResult>(TSource source, Action<IMappingOperationOptions<TSource, TResult>> opts);
        TResult Convert<TSource, TResult>(TSource source);
        
        /// <summary>
        /// Convert object, specifying some of the ctor args of the <typeparam name="TResult"/>
        /// from another object <paramref name="argumentsObject"/>.<br/>
        /// Used when the args cannot be resolved automatically.<br/>
        /// Note that this method is slower than others.
        /// </summary>
        TResult ConvertWithConstructorArgs<TSource, TResult>(TSource source, object argumentsObject);

        TResult Convert<TResult>(object source);
    }
}