using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Querier.Api.Models.Auth
{
    public partial class ApiUser : IdentityUser
    {
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string ThirdName { get; set; }
        public string LastName { get; set; }
        public string LanguageCode { get; set; }
        public string Phone { get; set; }
        public string Img { get; set; }
        public string DateFormat { get; set; }
        public virtual List<HAApiUserAttributes> HAApiUserAttributes { get; set; }
    }

    public partial class HAApiUserAttributes
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public virtual ApiUser User { get; set; }
        public int EntityAttributeId { get; set; }
        public virtual HAEntityAttribute EntityAttribute { get; set; }
    }
    public class HAEntityAttribute
    {
        public int Id { get; set; }
        public string Label { get; set; }
        public bool Nullable { get; set; }

        [NotMapped]
        public object? Value
        {
            get
            {
                if (this.StringAttribute != null)
                    return StringAttribute;
                if (this.IntAttribute != null)
                    return IntAttribute;
                if (this.DecimalAttribute != null)
                    return DecimalAttribute;
                if (this.DateTimeAttribute != null)
                    return DateTimeAttribute;
                return null;
            }
            set
            {
                if (value == null && Nullable)
                {
                    this.StringAttribute = null;
                    this.DateTimeAttribute = null;
                    this.DecimalAttribute = null;
                }
                else
                {
                    if (value.GetType() == typeof(string))
                        this.StringAttribute = Convert.ToString(value);
                    if (value.GetType() == typeof(int))
                        this.IntAttribute = Convert.ToInt32(value);
                    if (value.GetType() == typeof(decimal))
                        this.DecimalAttribute = Convert.ToDecimal(value);
                    if (value.GetType() == typeof(DateTime))
                        this.DateTimeAttribute = Convert.ToDateTime(value);
                }
                
            }
        }
        private string? StringAttribute { get; set; }
        private int? IntAttribute { get; set; }
        private decimal? DecimalAttribute { get; set; }
        private DateTime? DateTimeAttribute { get; set; }
        public virtual List<HAApiUserAttributes> HAApiUserAttributes { get; set; } = new List<HAApiUserAttributes>();

        public class HAEntityAttributeConfiguration : IEntityTypeConfiguration<HAEntityAttribute>
        {
            public void Configure(EntityTypeBuilder<HAEntityAttribute> builder)
            {
                builder.Property(p => p.StringAttribute);
                builder.Property(p => p.IntAttribute);
                builder.Property(p => p.DecimalAttribute);
                builder.Property(p => p.DateTimeAttribute);
            }
        }
    }
}