﻿using System.ComponentModel.DataAnnotations;

namespace Querier.Api.Domain.Entities.Auth
{
    public class RefreshTokenDto
    {
        [Required]
        public string Token { get; set; }

        [Required]
        public string RefreshToken { get; set; }
    }
}
