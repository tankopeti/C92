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
    public class ProductPriceController : ControllerBase
    {
        private readonly ProductPriceService _productPriceService;

        public ProductPriceController(ProductPriceService productPriceService)
        {
            _productPriceService = productPriceService;
        }

        // GET: api/ProductPrice
        [HttpGet]
        public async Task<ActionResult<List<ProductPrice>>> GetProductPrices()
        {
            try
            {
                var productPrices = await _productPriceService.GetActiveProductPricesAsync();
                return Ok(productPrices);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // GET: api/ProductPrice/5
        [HttpGet("{id}")]
        public async Task<ActionResult<ProductPrice>> GetProductPrice(int id)
        {
            try
            {
                var productPrice = await _productPriceService.GetProductPriceByIdAsync(id);
                return Ok(productPrice);
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

        // GET: api/ProductPrice/5/volume-price?quantity=100
        [HttpGet("{id}/volume-price")]
        public async Task<ActionResult<decimal>> GetVolumePrice(int id, [FromQuery] int quantity)
        {
            try
            {
                if (quantity < 0)
                {
                    return BadRequest("Quantity must be non-negative.");
                }

                var volumePrice = await _productPriceService.GetVolumePriceAsync(id, quantity);
                return Ok(volumePrice);
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

        // POST: api/ProductPrice
        [HttpPost]
        public async Task<ActionResult<ProductPrice>> CreateProductPrice(ProductPrice productPrice)
        {
            try
            {
                var createdPrice = await _productPriceService.CreateProductPriceAsync(productPrice);
                return CreatedAtAction(nameof(GetProductPrice), new { id = createdPrice.ProductPriceId }, createdPrice);
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

        // PUT: api/ProductPrice/5
        [HttpPut("{id}")]
        public async Task<ActionResult<ProductPrice>> UpdateProductPrice(int id, ProductPrice productPrice)
        {
            if (id != productPrice.ProductPriceId)
            {
                return BadRequest("ProductPriceId mismatch.");
            }

            try
            {
                var updatedPrice = await _productPriceService.UpdateProductPriceAsync(id, productPrice);
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

        // DELETE: api/ProductPrice/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProductPrice(int id)
        {
            try
            {
                await _productPriceService.DeleteProductPriceAsync(id);
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