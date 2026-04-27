using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using DotNetNuke.Web.Api;
using DotNetNuke.Data;
using Frestek.Vision.Modul.Models;
using Frestek.Vision.Modul.Helpers;
using Hotcakes.Commerce;
using Hotcakes.Commerce.Catalog;
using Hotcakes.Commerce.Urls;

namespace Frestek.Vision.Modul.Services
{
    [AllowAnonymous]
    public class VisionController : DnnApiController
    {
        // 1. Végpont: AI Színkereső és Hotcakes Integráció (Modul használja)
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetMatchesByColor(string hex)
        {
            try
            {
                int moduleId = (ActiveModule != null) ? ActiveModule.ModuleID : 0;

                if (string.IsNullOrEmpty(hex))
                    return Request.CreateResponse(HttpStatusCode.BadRequest, "Színkód megadása kötelező.");

                hex = HttpUtility.UrlDecode(hex);
                if (!hex.StartsWith("#")) hex = "#" + hex;
                Color targetColor = ColorTranslator.FromHtml(hex);

                using (IDataContext ctx = DataContext.Instance())
                {
                    var productRep = ctx.GetRepository<ProductColor>();
                    var products = productRep.Get(moduleId).ToList();

                    if (!products.Any())
                        return Request.CreateResponse(HttpStatusCode.NotFound, "Nincsenek termékek az adatbázisban.");

                    var matches = products.Select(p => new {
                        Product = p,
                        Distance = ColorMath.GetDistance(targetColor, ColorTranslator.FromHtml(p.HexCode))
                    })
                    .OrderBy(m => m.Distance).Take(3).ToList();

                    // Naplózás a VisionLog táblába
                    try
                    {
                        var logRep = ctx.GetRepository<VisionLog>();
                        var best = matches.First().Product;
                        logRep.Insert(new VisionLog
                        {
                            ModuleId = moduleId,
                            DetectedHex = hex,
                            RecommendedProductName = best.ProductName,
                            PriceHUF = best.PriceHUF,
                            ColorGroup = best.ColorGroup, // Színcsoport mentése a statisztikához
                            CreatedDate = DateTime.Now,
                            UserName = (UserInfo != null && !string.IsNullOrEmpty(UserInfo.Username)) ? UserInfo.Username : "Vendég"
                        });
                    }
                    catch { /* Ha a naplózás sikertelen, a termékajánlás még lefut */ }

                    // Hotcakes Integráció
                    var hccApp = new HotcakesApplication(HccRequestContext.Current);
                    var enriched = matches.Select(m => {
                        var hcProduct = hccApp.CatalogServices.Products.FindBySku(m.Product.ProductSKU);
                        string img = "/Portals/0/Hotcakes/Data/products/default.png";
                        string url = "#";

                        if (hcProduct != null)
                        {
                            if (!string.IsNullOrEmpty(hcProduct.ImageFileSmall))
                                img = $"/Portals/0/Hotcakes/Data/products/{hcProduct.Bvin}/{hcProduct.ImageFileSmall}";
                            url = HccUrlBuilder.RouteHccUrl(HccRoute.Product, new { slug = hcProduct.UrlSlug });
                        }

                        return new
                        {
                            ProductName = m.Product.ProductName,
                            ProductSKU = m.Product.ProductSKU,
                            PriceHUF = m.Product.PriceHUF,
                            HexCode = m.Product.HexCode,
                            ImageUrl = img,
                            ProductUrl = url
                        };
                    }).ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, enriched);
                }
            }
            catch (Exception ex) { return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString()); }
        }

        // 2. Végpont: Tranzakciók lekérdezése (A Dashboard használja)
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetLogs()
        {
            try
            {
                using (IDataContext ctx = DataContext.Instance())
                {
                    var rep = ctx.GetRepository<VisionLog>();
                    int moduleId = (ActiveModule != null) ? ActiveModule.ModuleID : 0;

                    var logs = rep.Get(moduleId)
                                  .OrderByDescending(l => l.CreatedDate)
                                  .ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, logs);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.Message);
            }
        }

        // 3. Végpont: Statisztikák lekérdezése (A Dashboard használja)
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage GetStats()
        {
            try
            {
                using (IDataContext ctx = DataContext.Instance())
                {
                    var rep = ctx.GetRepository<VisionLog>();
                    int moduleId = (ActiveModule != null) ? ActiveModule.ModuleID : 0;

                    var stats = rep.Get(moduleId)
                                .GroupBy(l => l.ColorGroup)
                                .Select(g => new ColorGroupStat
                                {
                                    Group = g.Key,
                                    Count = g.Count()
                                })
                                .ToList();

                    return Request.CreateResponse(HttpStatusCode.OK, stats);
                }
            }
            catch (Exception ex)
            {
                return Request.CreateResponse(HttpStatusCode.InternalServerError, ex.ToString());
            }
        }
    }
}