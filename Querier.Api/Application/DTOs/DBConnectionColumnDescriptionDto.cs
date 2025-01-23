namespace Querier.Api.Application.DTOs
{
    /// <summary>
    /// Data transfer object describing a database column's properties and constraints
    /// </summary>
    public class DBConnectionColumnDescriptionDto
    {
        /// <summary>
        /// Name of the database column
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Data type of the column (e.g., varchar, int, datetime)
        /// </summary>
        public string DataType { get; set; }

        /// <summary>
        /// Indicates whether the column can contain NULL values
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Indicates whether the column is part of the table's primary key
        /// </summary>
        public bool IsPrimaryKey { get; set; }

        /// <summary>
        /// Indicates whether the column is a foreign key referencing another table
        /// </summary>
        public bool IsForeignKey { get; set; }

        /// <summary>
        /// Name of the table referenced by this foreign key column
        /// </summary>
        public string ForeignKeyTable { get; set; }

        /// <summary>
        /// Name of the column referenced by this foreign key column
        /// </summary>
        public string ForeignKeyColumn { get; set; }
    }
}