﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce_MVC_Core.Models.Admin;

namespace Ecommerce_MVC_Core.ViewModel.Public
{
    public class HomePage
    {
        public List<CategoryViewModel> Categories { get; set; }
        public List<ProductListViewModel> ProductList { get; set; }
        public List<ProductListViewModel> Laptops { get; set; }
        public List<ProductListViewModel> ClothList { get; set; }
        public List<BrandListViewModel> BrandList { get; set; }
        public List<ProductListViewModel> Phones { get; internal set; }
        public List<ProductListViewModel> Head_phones { get; internal set; }
        public List<ProductListViewModel> other { get; internal set; }
    }
}
