using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class PartnerProductPriceController : ControllerBase
    {
        private readonly PartnerProductPriceService _partnerProductPriceService;

        public PartnerProductPriceController(PartnerProductPriceService partnerProductPriceService)
        {
            _partnerProductPriceService = partnerProductPriceService;
        }

        // GET: api/PartnerProductPrice
        [HttpGet]
        public async Task<ActionResult<List<PartnerProductPrice>>> GetPartnerProductPrices()
        {
            try
            {
                var partnerProductPrices = await _partnerProductPriceService.GetActivePartnerProductPricesAsync();
                return Ok(partnerProductPrices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/PartnerProductPrice/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PartnerProductPrice>> GetPartnerProductPrice(int id)
        {
            try
            {
                var partnerProductPrice = await _partnerProductPriceService.GetPartnerProductPriceByIdAsync(id);
                return Ok(partnerProductPrice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/PartnerProductPrice/partner/5/product/10
        [HttpGet("partner/{partnerId}/product/{productId}")]
        public async Task<ActionResult<PartnerProductPrice>> GetPartnerProductPriceByPartnerAndProduct(int partnerId, int productId)
        {
            try
            {
                var partnerProductPrice = await _partnerProductPriceService.GetPartnerProductPriceByPartnerAndProductAsync(partnerId, productId);
                return Ok(partnerProductPrice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // POST: api/PartnerProductPrice
        [HttpPost]
        public async Task<ActionResult<PartnerProductPrice>> CreatePartnerProductPrice(PartnerProductPrice partnerProductPrice)
        {
            try
            {
                var createdPrice = await _partnerProductPriceService.CreatePartnerProductPriceAsync(partnerProductPrice);
                return CreatedAtAction(nameof(GetPartnerProductPrice), new { id = createdPrice.PartnerProductPriceId }, createdPrice);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // PUT: api/PartnerProductPrice/5
        [HttpPut("{id}")]
        public async Task<ActionResult<PartnerProductPrice>> UpdatePartnerProductPrice(int id, PartnerProductPrice partnerProductPrice)
        {
            if (id != partnerProductPrice.PartnerProductPriceId)
            {
                return BadRequest("PartnerProductPriceId mismatch.");
            }

            try
            {
                var updatedPrice = await _partnerProductPriceService.UpdatePartnerProductPriceAsync(id, partnerProductPrice);
                return Ok(updatedPrice);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // DELETE: api/PartnerProductPrice/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePartnerProductPrice(int id)
        {
            try
            {
                await _partnerProductPriceService.DeletePartnerProductPriceAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}