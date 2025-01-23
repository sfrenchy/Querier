using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Querier.Api.Domain.Common.Attributes;

namespace $nameSpace$ {
    $procedureList: {procedure|
    /// <summary>
    /// Input Data Transfer Object (DTO) for the $procedure.CSName$ stored procedure
    /// </summary>
    /// <remarks>
    /// This class represents the input parameters for the $procedure.Name$ stored procedure.
    /// Each property maps to a parameter in the stored procedure.
    /// </remarks>
    [DtoFor(DtoType = DtoType.InputDto, StoredProcedure = "$procedure.Name$")]
    public class $procedure.CSName$InputDto {
        $procedure.Parameters: {parameter|
        /// <summary>
        /// Gets or sets the $parameter.CSName$ parameter value
        /// </summary>
        $if(parameter.Description)$/// <remarks>$parameter.Description$</remarks>$endif$
        [JsonPropertyName("$parameter.CSName$")]
        [JsonProperty("$parameter.CSName$", Required = Required.AllowNull, NullValueHandling = NullValueHandling.Include)]
        $if(parameter.IsRequired)$[Required(ErrorMessage = "The $parameter.CSName$ parameter is required.")]$endif$
        $if(parameter.MaxLength)$[MaxLength($parameter.MaxLength$, ErrorMessage = "The $parameter.CSName$ parameter cannot exceed $parameter.MaxLength$ characters.")]$endif$
        $if(parameter.MinLength)$[MinLength($parameter.MinLength$, ErrorMessage = "The $parameter.CSName$ parameter must be at least $parameter.MinLength$ characters.")]$endif$
        $if(parameter.RegexPattern)$[RegularExpression(@"$parameter.RegexPattern$", ErrorMessage = "The $parameter.CSName$ parameter format is invalid.")]$endif$
        $if(parameter.Range)$[Range($parameter.Range.Min$, $parameter.Range.Max$, ErrorMessage = "The $parameter.CSName$ parameter must be between $parameter.Range.Min$ and $parameter.Range.Max$.")]$endif$
        public $parameter.CSType$ $parameter.CSName$ { get; set; \}

        }$
    \}
    }$
\} 