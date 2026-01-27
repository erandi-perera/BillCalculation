using System;
using System.Web.Http;
using BillCalculation.DAL;
using BillCalculation.Models;

namespace BillCalculation.Controllers
{
    [RoutePrefix("api/billcalculation")]
    public class BillCalculationController : ApiController
    {
        private readonly BillCalculationDao _dao;

        public BillCalculationController()
        {
            _dao = new BillCalculationDao();
        }

        /// <summary>
        /// Get tariff blocks for a specific category and date
        /// Returns all tariff blocks with their rates
        /// </summary>
        /// <param name="category">Tariff category (default: 11 - Domestic)</param>
        /// <param name="effectiveDate">Effective date to check tariff (format: yyyy-MM-dd). Defaults to today.</param>
        /// <returns>List of tariff blocks with rates</returns>
        /// <example>
        /// GET /api/billcalculation/tariff?category=11&effectiveDate=2025-06-12
        /// </example>
        [HttpGet]
        [Route("tariff")]
        public IHttpActionResult GetTariffBlocks(int category = 11, string effectiveDate = null)
        {
            try
            {
                DateTime effDate;
                if (string.IsNullOrEmpty(effectiveDate))
                {
                    effDate = DateTime.Today;
                }
                else
                {
                    if (!DateTime.TryParse(effectiveDate, out effDate))
                    {
                        return BadRequest("Invalid date format. Use yyyy-MM-dd");
                    }
                }

                var tariffs = _dao.GetTariffBlocks(category, effDate);

                if (tariffs == null || tariffs.Count == 0)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    Category = category,
                    EffectiveDate = effDate.ToString("yyyy-MM-dd"),
                    TariffBlocks = tariffs
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetTariffBlocks: {ex.Message}");
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Calculate detailed bill with breakdown across multiple tariff periods
        /// This is the main endpoint that handles calculations across different tariff structures
        /// </summary>
        /// <param name="request">Bill calculation request with category, units, fromDate, and toDate</param>
        /// <returns>Detailed bill calculation with period-wise breakdown and block charges</returns>
        /// <example>
        /// POST /api/billcalculation/calculate
        /// Body: {
        ///   "category": 11,
        ///   "fullUnits": 630,
        ///   "fromDate": "2025-02-04",
        ///   "toDate": "2026-01-20"
        /// }
        /// </example>
        [HttpPost]
        [Route("calculate")]
        public IHttpActionResult CalculateBill([FromBody] BillCalculationRequest request)
        {
            try
            {
                if (request == null)
                {
                    return BadRequest("Request body is required");
                }

                if (request.FullUnits <= 0)
                {
                    return BadRequest("FullUnits must be greater than 0");
                }

                if (request.FromDate >= request.ToDate)
                {
                    return BadRequest("FromDate must be before ToDate");
                }

                var result = _dao.CalculateDetailedBill(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in CalculateBill: {ex.Message}");
                System.Diagnostics.Trace.WriteLine($"Stack Trace: {ex.StackTrace}");
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Get bill summary with totals only (no detailed breakdown)
        /// Useful for quick calculations or dashboard displays
        /// </summary>
        /// <param name="category">Tariff category (default: 11 - Domestic)</param>
        /// <param name="units">Total units consumed</param>
        /// <param name="fromDate">From date (format: yyyy-MM-dd)</param>
        /// <param name="toDate">To date (format: yyyy-MM-dd)</param>
        /// <returns>Bill summary with total charges</returns>
        /// <example>
        /// GET /api/billcalculation/summary?category=11&units=630&fromDate=2025-02-04&toDate=2026-01-20
        /// </example>
        [HttpGet]
        [Route("summary")]
        public IHttpActionResult GetBillSummary(
            int category = 11,
            decimal units = 0,
            string fromDate = null,
            string toDate = null)
        {
            try
            {
                if (units <= 0)
                {
                    return BadRequest("Units must be greater than 0");
                }

                if (string.IsNullOrEmpty(fromDate) || string.IsNullOrEmpty(toDate))
                {
                    return BadRequest("Both fromDate and toDate are required");
                }

                if (!DateTime.TryParse(fromDate, out DateTime from))
                {
                    return BadRequest("Invalid fromDate format. Use yyyy-MM-dd");
                }

                if (!DateTime.TryParse(toDate, out DateTime to))
                {
                    return BadRequest("Invalid toDate format. Use yyyy-MM-dd");
                }

                if (from >= to)
                {
                    return BadRequest("fromDate must be before toDate");
                }

                var result = _dao.GetBillSummary(category, units, from, to);

                return Ok(result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine($"Error in GetBillSummary: {ex.Message}");
                return InternalServerError(ex);
            }
        }
    }
}