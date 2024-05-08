using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Querier.Api.Models.Interfaces
{
    //This viewmodel is used in the startup to specify the attributes that an application needs for it's AspNetUsers at their creation
    public class HAEntityAttributeViewModel
    {
        public string Label { get; set; }
        public dynamic Value { get; set; }
        public bool Nullable { get; set; }
    }
}
