using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetNuke.Web.Api;
using Frestek.Vision.Modul.Models;
using DotNetNuke.Data;

namespace Frestek.Vision.Modul.Services
{
    // [SupportedModules("Frestek.Vision.Modul")]
    [AllowAnonymous] // Teszteléshez egyelőre engedélyezzük bárkinek
    public class VisionController : DnnApiController
    {
        // Végpont a valós idejű naplóhoz
        [HttpGet]
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

        // Végpont a 8 színcsoportos statisztikához
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