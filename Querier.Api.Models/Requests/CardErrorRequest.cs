using System;
using Querier.Api.Models.UI;

namespace Querier.Api.Models.Requests
{
    [Serializable]
    public class CardErrorRequest
    {
        /// <summary>
        /// The card object in error state
        /// </summary>
        public QPageCard Card { get; set; }
        /// <summary>
        /// The .Net exception
        /// </summary>
        public Exception DotNetException { get; set; }
        /// <summary>
        /// The requested ViewName if any
        /// </summary>
        public string ViewName { get; set; }
        /// <summary>
        /// The card Identifier
        /// </summary>
        public int CardId { get; set; }
        /// <summary>
        /// A holder of more data about exception
        /// </summary>
        public dynamic More { get; set; }
    }
}
