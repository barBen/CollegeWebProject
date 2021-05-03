﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace DGN.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [DisplayName("Category name")]
        [Required]
        public string CategoryName { get; set; }

        [DisplayName("Articles related to this category")]
        public IList<Atricle> Articles { get; set; }
    }
}