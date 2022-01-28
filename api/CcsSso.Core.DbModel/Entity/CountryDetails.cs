using CcsSso.Core.DbModel.Constants;
using CcsSso.DbModel.Entity;
using System;

namespace CcsSso.Core.DbModel.Entity
{
    public class CountryDetails : BaseEntity
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

    }
}
