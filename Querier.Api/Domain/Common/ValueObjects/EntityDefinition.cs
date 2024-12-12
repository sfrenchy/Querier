using System.Collections.Generic;

namespace Querier.Api.Models.Common
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

    public class PropertyDefinition
    {
        /// <summary>
        /// The list of available items
        /// </summary>
        private List<PropertyItemDefinition> _availableItems;

        /// <summary>
        /// The name of the property
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The CSharp type of the property (as string)
        /// </summary>
        public string Type { get; set; } = "string?";

        /// <summary>
        /// List of property options
        /// </summary>
        public List<PropertyOption> Options { get; set; } = new List<PropertyOption>() { PropertyOption.IsNullable };

        /// <summary>
        /// Custom getter and setter for the available items of the property
        /// </summary>
        public List<PropertyItemDefinition> AvailableItems
        {
            get
            {
                if (_availableItems == null)
                {
                    // Do what you have to do to get available items from database... :(
                }
                return _availableItems;
            }
            set 
            {
                _availableItems = value;
            }
        }
    }
    public class PropertyItemDefinition
    {
        /// <summary>
        /// The key of the item 
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// the label of the item
        /// </summary>
        public string Label { get; set; }
    }
    public class EntityDefinition
    {
        /// <summary>
        /// The name of the entity
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The properties of the entity
        /// </summary>
        public List<PropertyDefinition> Properties { get; set; }
    }

    public class SQLQueryResult
    {
        /// <summary>
        /// A boolean whose value will be true if the query succeeded and false if it didn't
        /// </summary>
        public bool QuerySuccessful { get; set; }

        /// <summary>
        /// The error message if the query didn't succeed
        /// </summary>
        public string ErrorMessage { get; set; }

        /// <summary>
        /// An entity definition
        /// </summary>
        public EntityDefinition Entity { get; set; }

        /// <summary>
        /// A list to contain the data from an sql query
        /// </summary>
        public List<dynamic> Datas { get; set; }
    }
}
