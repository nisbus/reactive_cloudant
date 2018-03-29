namespace ReactiveCloudant.Lucene.Interfaces
{
    /// <summary>
    /// An interface for adding conditions to a Lucene Query
    /// </summary>
    public interface ICanAddQuery : ICanRunQuery, ICanPrintQuery
    {
        /// <summary>
        /// Set an AND constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="condition">The condition to apply</param>
        /// <returns></returns>
        ICanAddQuery And(string field, string condition);

        /// <summary>
        /// Set an OR constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="condition">The condition to apply</param>
        /// <returns></returns>
        ICanAddQuery Or(string field, string condition);
        
        /// <summary>
        /// Set a NOT constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="condition">The condition to apply</param>
        /// <returns></returns>
        ICanAddQuery Not(string field, string condition);

        /// <summary>
        /// Set a PLUS constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="condition">The condition to apply</param>
        /// <returns></returns>
        ICanAddQuery Plus(string field, string condition);

        /// <summary>
        /// Set an MINUS constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="condition">The condition to apply</param>
        /// <returns></returns>
        ICanAddQuery Minus(string field, string condition);

        /// <summary>
        /// Set a FUZZY match constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="condition">The condition to apply</param>
        /// <returns></returns>
        ICanAddQuery Fuzzy(string field, string condition);

        /// <summary>
        /// Set an RANGE constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="lowerRange">The lower range for the constraint</param>
        /// <param name="upperRange">The upper range for the constraint</param>
        /// <returns></returns>
        ICanAddQuery Range(string field, string lowerRange, string upperRange);

        /// <summary>
        /// Set an StartsWith constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="startString">The string to match</param>
        /// <returns></returns>
        ICanAddQuery StartsWith(string field, string startString);

        /// <summary>
        /// Set an StartsWith constraint to a query
        /// </summary>
        /// <param name="field">The field to set the constraint on</param>
        /// <param name="startString">The string to match</param>
        /// <returns></returns>
        ICanAddQuery StartsWithConstrained(string field, string startString);
    }
}
