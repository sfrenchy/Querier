using System.Collections.Generic;

namespace Querier.Api.Models.Datatable
{
    /// <summary>
    /// Représente une liste de valeurs uniques d'un jeu de données 
    /// </summary>
    public class GetUniqueColumnValuesResponse
    {
        /// <summary>
        /// Obtient ou défini la liste des filtres actifs
        /// </summary>
        public Dictionary<string, string> ActiveFilters { get; set; }

        /// <summary>
        /// Obtient ou défini la liste des valeurs uniques disponibles par colonne ( key = colonne, value = liste des valeurs)
        /// </summary>
        public Dictionary<string, DatatableColumnAllValues> values { get; set; }
    }
}
