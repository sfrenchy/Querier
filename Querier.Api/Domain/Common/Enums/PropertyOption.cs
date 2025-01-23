namespace Querier.Api.Domain.Common.Enums
{
    public enum PropertyOption
    {
        /// <summary>
        /// The property is read only
        /// </summary>
        IsReadOnly = 0,
        /// <summary>
        ///  The property is a foreign key and is a reference to another entity
        /// </summary>
        IsForeignKey = 1,
        /// <summary>
        /// The property is a table key
        /// </summary>
        IsKey = 2,
        /// <summary>
        /// The property is nullable
        /// </summary>
        IsNullable = 3
    }
}
