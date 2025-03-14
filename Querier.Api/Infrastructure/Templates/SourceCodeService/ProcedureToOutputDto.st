$if (model.HasOutput)$
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Text.Json.Serialization;
    using Newtonsoft.Json;
    using Querier.Api.Domain.Common.Attributes;

    namespace $model.RootNamespace$.Dtos.Procedure;

    /// <summary>
    /// Data Transfer Object (DTO) for the $model.CSName$ stored procedure results
    /// </summary>
    /// <remarks>
    /// This class represents the result data returned by the $model.Name$ stored procedure.
    /// Each property maps to a column in the result set.
    /// </remarks>
    [DtoFor(StoredProcedure = "$model.Name$", DtoType = DtoType.OutputDto)]
    public class $model.CSName$OutputDto {
        $model.OutputSet: {column|
        /// <summary>
        /// Gets or sets the $column.Name$ column value
        /// </summary>
        $if(column.Description)$/// <remarks>$column.Description$</remarks>$endif$
        [JsonPropertyName("$column.Name$")]
        [JsonProperty("$column.Name$", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include)]
        $if(column.IsRequired)$[Required(ErrorMessage = "The $column.Name$ field is required.")]$endif$
        $if(column.MaxLength)$[MaxLength($column.MaxLength$, ErrorMessage = "The $column.Name$ field cannot exceed $column.MaxLength$ characters.")]$endif$
        $if(column.MinLength)$[MinLength($column.MinLength$, ErrorMessage = "The $column.Name$ field must be at least $column.MinLength$ characters.")]$endif$
        $if(column.RegexPattern)$[RegularExpression(@"$column.RegexPattern$", ErrorMessage = "The $column.Name$ field format is invalid.")]$endif$
        $if(column.Range)$[Range($column.Range.Min$, $column.Range.Max$, ErrorMessage = "The $column.Name$ field must be between $column.Range.Min$ and $column.Range.Max$.")]$endif$
        $if(column.IsSummable)$[Summable]$endif$
        public $column.CSType$ $column.Name$ { get; set; \}
        }$
    \}
    
$endif$