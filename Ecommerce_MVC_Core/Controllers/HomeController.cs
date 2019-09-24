﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Ecommerce_MVC_Core.Code;
using Ecommerce_MVC_Core.Data;
using Microsoft.AspNetCore.Mvc;
using Ecommerce_MVC_Core.Models;
using Ecommerce_MVC_Core.Models.Admin;
using Ecommerce_MVC_Core.Repository;
using Ecommerce_MVC_Core.ViewModel;
using Ecommerce_MVC_Core.ViewModel.Public;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce_MVC_Core.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly UserManager<ApplicationUsers> _userManager;

        public HomeController(
            UserManager<ApplicationUsers> userManager,
            IUnitOfWork unitOfWork
        )
        {
            _userManager = userManager;
            _unitOfWork = unitOfWork;
        }

        //List<int> test=new List<int>();
        public IActionResult Index(int categoryId=0)
        {
            HomePage homePage = new HomePage();
            homePage.Categories = GetMainCategory();

            homePage.ProductList = new List<ProductListViewModel>();

            var ctgQuery = _unitOfWork.Repository<Category>().Query();

            var phon = ctgQuery.FirstOrDefault(x => x.Name.ToLower() == "Phones".ToLower());
            var Lap = ctgQuery.FirstOrDefault(x => x.Name.ToLower() == "Laptops".ToLower());
            var hed_phon = ctgQuery.FirstOrDefault(x => x.Name.ToLower() == "Head_phones".ToLower());
            var oth = ctgQuery.FirstOrDefault(x => x.Name.ToLower() == "others".ToLower());
            homePage.Phones = new List<ProductListViewModel>();
            homePage.Laptops = new List<ProductListViewModel>();
            homePage.Head_phones = new List<ProductListViewModel>();
            homePage.other = new List<ProductListViewModel>();
            if (phon != null)
            {
                homePage.Phones = GetAllProductList(ctgid: phon.Id, take: 8);
            }
            if (Lap != null)
            {
                homePage.Laptops = GetAllProductList(ctgid: Lap.Id, take: 8);
            }
            if (hed_phon != null)
            {
                homePage.Head_phones = GetAllProductList(ctgid: hed_phon.Id, take: 8);
            }
            if (oth != null)
            {
                homePage.other = GetAllProductList(ctgid: oth.Id, take: 8);
            }
            homePage.BrandList = GetAllBrand();

            return View(homePage);
        }

       

        public List<BrandListViewModel> GetAllBrand()
        {
            List<BrandListViewModel> brandList = new List<BrandListViewModel>();
            _unitOfWork.Repository<Brand>().GetAll().ToList().ForEach(x =>
            {
                BrandListViewModel brand = new BrandListViewModel
                {
                    Name = x.Name,
                    Id = x.Id,
                    Description = x.Description
                };
                brandList.Add(brand);
            });
            return brandList;
        }
        #region CatrgoryMenu

        public List<CategoryViewModel> GetMainCategory()
        {
            List<CategoryViewModel> categoryList = new List<CategoryViewModel>();
            _unitOfWork.Repository<Category>().GetAll().Where(x => x.CategoryId == null).ToList().ForEach(c =>
            {
                CategoryViewModel ctg = new CategoryViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    CategoryId = c.Id,

                };
                ctg.CategoryMenus = GetSubCategory(c.Id);
                categoryList.Add(ctg);
            });

            
            return categoryList;
        }

        public List<CategoryViewModel> GetSubCategory(int id)
        {
            List<CategoryViewModel> categoryList = new List<CategoryViewModel>();
            _unitOfWork.Repository<Category>().GetAll().Where(x => x.CategoryId == id).ToList().ForEach(c =>
            {
                CategoryViewModel ctg = new CategoryViewModel
                {
                    Name = c.Name,
                    CategoryId = c.CategoryId,
                    Id = c.Id

                };
                ctg.CategoryMenus = GetSubCategory(c.Id);
                categoryList.Add(ctg);
                //test.Add(c.Id);
            });
            return categoryList;
        }

        #endregion

        public IActionResult Brand()
        {
            List<BrandListViewModel> brandList = new List<BrandListViewModel>();
            _unitOfWork.Repository<Brand>().GetAll().ToList().ForEach(x =>
            {
                BrandListViewModel brand=new BrandListViewModel
                {
                    Name = x.Name,
                    Id = x.Id,
                    Description = x.Description
                };
                brandList.Add(brand);
            });

            return View(brandList);
        }

        public IActionResult Category()
        {
            List<CategoryListViewModel> categoryList = new List<CategoryListViewModel>();
            _unitOfWork.Repository<Category>().GetAllInclude(x=>x.Categoris,p=>p.Products).ToList().ForEach(c =>
            {
                CategoryListViewModel category = new CategoryListViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Description = c.Description,
                    CategoryId = c.CategoryId,
                    CategoryParentName = c.Categoris?.Name,
                    TotalProduct = c.Products?.Count ?? 0
                };
                categoryList.Add(category);
            });
            return View(categoryList);
        }

        public IActionResult Product(int Brand=0,int Category=0,string search="")
        {
            List<ProductListViewModel> products = new List<ProductListViewModel>();
            products = GetAllProductList();

            if (!String.IsNullOrEmpty(search))
            {
                search=search.ToLower();
                products = products.Where(x => x.Name.ToLower().Contains(search) || x.BrandName.ToLower().Contains(search) || x.CategoryName.ToLower().Contains(search)).ToList();

            }

            if (Brand>0)
            {
                products = products.Where(x => x.BrandId == Brand).ToList();
            }
            if (Category>0)
            {
                products = products.Where(x => x.CategoryId == Category).ToList();
            }


            return View(products);
        }

        public List<ProductListViewModel> GetAllProductList( int id=0,int take=1000,int ctgid=0)
        {
            List<ProductListViewModel> productList = new List<ProductListViewModel>();
            IQueryable<Product> dbProducts = _unitOfWork.Repository<Product>().Query()
                .Include(x => x.Brand)
                .Include(c => c.Category)
                .Include(u => u.Unit)
                .Include(pc => pc.ProductCommentses)
                .Include(pi => pi.ProductImages)
                .Include(ps => ps.ProductStocks);

            if (ctgid>0)
            {
                dbProducts = dbProducts.Where(x => x.CategoryId == ctgid ||x.Category.Id==ctgid);
            }

            dbProducts = dbProducts.OrderByDescending(x => x.AddedDate).Take(take);
            foreach (var b in dbProducts)
            {
                ProductListViewModel product = new ProductListViewModel
                {
                    Id = b.Id,
                    Name = b.Name,
                    Code = b.Code,
                    Tag = b.Tag,
                    CategoryId = b.CategoryId,
                    BrandId = b.BrandId,
                    UnitId = b.UnitId,
                    Description = b.Description,
                    Price = b.Price,
                    BrandName = b.Brand.Name,
                    CategoryName = b.Category.Name,
                    UnitName = b.Unit.Name,
                    Discount = b.Discount,
                    FinalPrice = (b.Price - ((b.Price * b.Discount) / 100)),
                    ProductComments = b.ProductCommentses.Count,
                    TotalImage = b.ProductImages.Count,


                };
                var prdctStocks = b.ProductStocks.FirstOrDefault(x => x.ProductId == b.Id);
                product.ProductStocks = prdctStocks != null
                    ? product.ProductStocks = prdctStocks.InQuantity - prdctStocks.OutQuantity
                    : product.ProductStocks = 0;

                var productImageList = b.ProductImages.Where(x => x.ProductId == b.Id).ToList();

                var productImage = productImageList.FirstOrDefault();
                if (productImage != null)
                {
                    product.ImageTitle = productImage.Title;
                    product.ImagePath = productImage.ImagePath;
                    if (productImageList.Count > 1)
                    {
                        product.SecondImagePath = productImageList.Skip(1).First().ImagePath;
                    }
                    else
                    {
                        product.SecondImagePath = product.ImagePath;
                    }
                }
                else
                {
                    product.ImagePath = "noproductimage.png";
                    product.SecondImagePath = "noproductimage.png";
                }



                productList.Add(product);
            }
            
            
            return productList;

        }


        public List<ProductListViewModel> GetNewAriveProduct()
        {
            List<ProductListViewModel> productsList = new List<ProductListViewModel>();

            
                _unitOfWork.Repository<Product>().Query().Include(x => x.Brand)
                    .Include(c => c.Category)
                    .Include(u => u.Unit)
                    .Include(pc => pc.ProductCommentses)
                    .Include(pi => pi.ProductImages)
                    .Include(ps => ps.ProductStocks).OrderByDescending(x=>x.AddedDate).Take(12).ToList().ForEach(b =>
                {
                    ProductListViewModel product = new ProductListViewModel
                    {
                        Id = b.Id,
                        Name = b.Name,
                        Code = b.Code,
                        Tag = b.Tag,
                        CategoryId = b.CategoryId,
                        BrandId = b.BrandId,
                        UnitId = b.UnitId,
                        Description = b.Description,
                        Price = b.Price,
                        BrandName = b.Brand.Name,
                        CategoryName = b.Category.Name,
                        UnitName = b.Unit.Name,
                        Discount = b.Discount,
                        FinalPrice = (b.Price - ((b.Price * b.Discount) / 100)),
                        ProductComments = b.ProductCommentses.Count(x => x.ProductId == b.Id),
                        TotalImage = b.ProductImages.Count(x => x.ProductId == b.Id)
                    };
                    var prdctStocks = b.ProductStocks.FirstOrDefault(x => x.ProductId == b.Id);
                    product.ProductStocks = prdctStocks != null ? product.ProductStocks = prdctStocks.InQuantity - prdctStocks.OutQuantity : product.ProductStocks = 0;
                    productsList.Add(product);
                });




            return productsList;
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            
            ViewData["Message"] ="Hello" ;

            return View();
        }

        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public async Task<IActionResult> QuickViewProduct(int product)
        {
            ProductListViewModel pList = new ProductListViewModel();
           Product pro= await _unitOfWork.Repository<Product>().GetSingleIncludeAsync(x=>x.Id== product,b=>b.Brand,c=>c.Category,u=>u.Unit,rc=>rc.ProductCommentses,pi=>pi.ProductImages);
            if (pro!=null)
            {

                pList.Id = pro.Id;
                pList.Name = pro.Name;
                pList.Code = pro.Code;
                pList.Tag = pro.Tag;
                pList.CategoryId = pro.CategoryId;
                pList.BrandId = pro.BrandId;
                pList.UnitId = pro.UnitId;
                pList.Description = pro.Description;
                pList.Price = pro.Price;
                pList.BrandName = pro.Brand.Name;
                pList.CategoryName = pro.Category.Name;
                pList.UnitName = pro.Unit.Name;
                pList.Discount = pro.Discount;
                pList.FinalPrice = (pro.Price - ((pro.Price * pro.Discount) / 100));
                pList.ProductComments =pro.ProductCommentses.Count(x => x.ProductId == pro.Id);
                pList.TotalImage = pro.ProductImages.Count(x => x.ProductId == pro.Id);


                

                var productImageList = pro.ProductImages.Where(x => x.ProductId == pro.Id).ToList();

                var productImage = productImageList.FirstOrDefault();
                if (productImage != null)
                {
                    pList.ImageTitle = productImage.Title;
                    pList.ImagePath = productImage.ImagePath;

                }
                
            }
            return PartialView("_QuickView", pList);
        }

        public async Task<IActionResult> ProductDetails(int product)
        {
            ProductListViewModel pList = new ProductListViewModel();
            Product pro = await _unitOfWork.Repository<Product>().GetSingleIncludeAsync(x => x.Id == product, b => b.Brand, c => c.Category, u => u.Unit, rc => rc.ProductCommentses, pi => pi.ProductImages); ;
            if (pro != null)
            {

                pList.Id = pro.Id;
                pList.Name = pro.Name;
                pList.Code = pro.Code;
                pList.Tag = pro.Tag;
                pList.CategoryId = pro.CategoryId;
                pList.BrandId = pro.BrandId;
                pList.UnitId = pro.UnitId;
                pList.Description = pro.Description;
                pList.Price = pro.Price;
                pList.BrandName = pro.Brand.Name;
                pList.CategoryName = pro.Category.Name;
                pList.UnitName = pro.Unit.Name;
                pList.Discount = pro.Discount;
                pList.FinalPrice = (pro.Price - ((pro.Price * pro.Discount) / 100));
                pList.ProductComments = pro.ProductCommentses.Count(x => x.ProductId == pro.Id);
                pList.TotalImage = pro.ProductImages.Count(x => x.ProductId == pro.Id);
                pList.ProductCommentsList = GetAllCommentsByProduct(pro.Id);

                pList.ImageList=new List<ProductImageListViewModel>();

                var productImageList = pro.ProductImages.Where(x => x.ProductId == pro.Id).ToList();

                var productImage = productImageList.FirstOrDefault();
                if (productImage != null)
                {
                    pList.ImageTitle = productImage.Title;
                    pList.ImagePath = productImage.ImagePath;
                    productImageList.ForEach(x =>
                    {
                        ProductImageListViewModel image=new ProductImageListViewModel
                        {
                            Id = x.Id,
                            ImagePath = x.ImagePath,
                            Title = x.Title,
                        };
                        pList.ImageList.Add(image);
                     });
                }

            }
            return View(pList);
        }

        public List<CommentsListViewModel> GetAllCommentsByProduct(int productId)
        {
            List<CommentsListViewModel> commentsList=new List<CommentsListViewModel>();
            _unitOfWork.Repository<ProductComments>().GetIncludeList(x => x.ProductId == productId,p=>p.Product).OrderByDescending(x=>x.AddedDate).ToList().ForEach(x =>
            {
                CommentsListViewModel comments=new CommentsListViewModel
                {
                    ProductId = x.ProductId,
                    Comment = x.Comment,
                    ProductName = x.Product.Name,
                    UserId = x.UserId
                };
                DateTime date = x.AddedDate;
                comments.AddedDate = TimeAgoCustom.TimeAgo(date);

                ApplicationUsers user = _userManager.FindByIdAsync(x.UserId).Result;
                comments.UserName = user.Name;
                commentsList.Add(comments);
            });

            return commentsList;
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> AddComment(string commentMessage,int pro)
        {
            if (commentMessage!=null)
            {
                ApplicationUsers user = _userManager.GetUserAsync(HttpContext.User).Result;
                ProductComments comments=new ProductComments();
                comments.Comment = commentMessage;
                comments.ProductId = pro;
                comments.AddedDate=DateTime.Now;
                comments.ModifiedDate=DateTime.Now;
                comments.UserId = user.Id;
               await _unitOfWork.Repository<ProductComments>().InsertAsync(comments);
               
            }
            return RedirectToAction("ProductDetails", new { product = pro });

        }

        #region ProductCart

        [HttpGet]
        public async Task<IActionResult> AddToCart(int product)
        {
            
            if (product>0)
            {
                Models.Admin.Product pro =await _unitOfWork.Repository<Product>().GetByIdAsync(product);
                if (pro!=null)
                {
                    AddToCartViewModel addTo=new AddToCartViewModel
                    {
                        ProductId = pro.Id,
                        ProductName = pro.Name,
                        FinalPrice = (pro.Price - ((pro.Price * pro.Discount) / 100)),
                        Quantity = 1
                    };
                    ViewBag.productId = product;
                    return PartialView("_AddtoCart", addTo);
                }
            }
            
            return PartialView("_AddtoCart");
        }

        [HttpPost]
        public IActionResult AddToCart(int prodcut, AddToCartViewModel model)
        {
            List<AddToCartViewModel> addToCart = new List<AddToCartViewModel>();
            List<AddToCartViewModel> addToCartList = HttpContext.Session.Get<List<AddToCartViewModel>>("CartItem");
            int itemInCart=0;
            var valu = HttpContext.Session.GetInt32("itemCount");
            if (valu != null)
            {
                itemInCart = valu.Value;
            }

            if (addToCartList == null)
                {
                    AddToCartViewModel cart = new AddToCartViewModel();
                    cart.ProductId = model.ProductId;
                    cart.FinalPrice = model.FinalPrice;
                    cart.ProductName = model.ProductName;
                    cart.Quantity = model.Quantity;
                    addToCart.Add(cart);
                    HttpContext.Session.Set<List<AddToCartViewModel>>("CartItem",addToCart);
                    itemInCart= 1;
                }
                else
                {
                     
                    if (addToCartList.Where(x => x.ProductId == model.ProductId).ToList().Count<1)
                    {
                    AddToCartViewModel cart = new AddToCartViewModel();
                        cart.ProductId = model.ProductId;
                        cart.FinalPrice = model.FinalPrice;
                        cart.ProductName = model.ProductName;
                        cart.Quantity = model.Quantity;
                        addToCartList.Add(cart);
                        HttpContext.Session.Set<List<AddToCartViewModel>>("CartItem", addToCartList);
                        itemInCart = HttpContext.Session.Get<List<AddToCartViewModel>>("CartItem").Count;
                    }
                }


            HttpContext.Session.SetInt32("itemCount",itemInCart);
            return RedirectToAction(nameof(Product));
        }



        [HttpGet]
        public IActionResult RemoveCartItem(int product)
        {
            return View();
        }
        #endregion
    }
}
