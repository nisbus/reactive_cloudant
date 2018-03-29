using System;

namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// An interface for running Lucene Queries
    /// </summary>
    public interface ICanRunQuery : ICanPrintQuery
    {
        /// <summary>
        /// Executes the query and returns untyped results
        /// </summary>
        /// <returns></returns>
        IObservable<LuceneResult> Execute();

        /// <summary>
        /// Executes the query and returns results of the given type T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IObservable<LuceneResult<T>> Execute<T>();

        /// <summary>
        /// Executes the query given and returns untyped results
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns></returns>
        IObservable<LuceneResult> Execute(string query);

        /// <summary>
        /// Executes the query and returns results of the given type T
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        IObservable<LuceneResult<T>> Execute<T>(string query);
    }
}
