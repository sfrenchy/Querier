﻿namespace Querier.Api.Models.Requests
{
    public class QUpdateUserEmailTemplateRequest
    {
        public int IdEmailTemplate { get; set; }
        public string NewNameEmailTemplate { get; set; }
        public string NewContentEmailTemplate { get; set; }
    }
}