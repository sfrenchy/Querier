using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;

namespace Querier.Api.Models.Datatable
{
    /// <summary>
    /// Représente une colonne
    /// </summary>
    public class DatatableColumn
    {
        /// <summary>
        /// Nom technique de la colonne dans la BDD
        /// </summary>
        public string Data { get; set; }
        /// <summary>
        /// Nom de la propriété dans le model "Result"
        /// </summary>
        public string ResultPropertyName { get; set; }
        /// <summary>
        /// Type de la colonne
        /// </summary>
        public string Type { get; set; }
    }
}