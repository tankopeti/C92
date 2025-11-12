using Microsoft.AspNetCore.Mvc;
using Cloud9_2.Models;
using Cloud9_2.Services;
using Cloud9_2.Data;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Cloud9_2.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly EmployeeService _employeeService;
        private readonly ILogger<EmployeeController> _logger;
        private readonly ApplicationDbContext _context; // Added

public EmployeeController(
            EmployeeService employeeService,
            ApplicationDbContext context,  // INJECT DbContext
            ILogger<EmployeeController> logger)
        {
            _employeeService = employeeService ?? throw new ArgumentNullException(nameof(employeeService));
            _context = context ?? throw new ArgumentNullException(nameof(context));  // REQUIRED
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: api/employee
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Employees>>> GetEmployees()
        {
            try
            {
                var employees = await _employeeService.GetAllEmployeesAsync();
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all employees.");
                return StatusCode(500, "Failed to retrieve employees.");
            }
        }

        // GET: api/employee/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Employees>> GetEmployee(int id)
        {
            try
            {
                var employee = await _employeeService.GetEmployeeByIdAsync(id);
                if (employee == null)
                    return NotFound($"Employee with ID {id} not found.");

                return Ok(employee);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee ID {Id}.", id);
                return StatusCode(500, "Failed to retrieve employee.");
            }
        }

        // POST: api/employee
        [HttpPost]
        public async Task<ActionResult<Employees>> CreateEmployee([FromBody] EmployeesCreateDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var employee = await _employeeService.CreateEmployeeAsync(createDto);
                return CreatedAtAction(nameof(GetEmployee), new { id = employee.EmployeeId }, employee);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating employee.");
                return StatusCode(500, "Failed to create employee.");
            }
        }

        // PUT: api/employee/5
        [HttpPut("{id}")]
        public async Task<ActionResult<Employees>> UpdateEmployee(int id, [FromBody] EmployeesUpdateDto updateDto)
        {
            if (id != updateDto.EmployeeId)
                return BadRequest("ID in URL must match ID in payload.");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var employee = await _employeeService.UpdateEmployeeAsync(updateDto);
                return Ok(employee);
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Employee with ID {id} not found.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating employee ID {Id}.", id);
                return StatusCode(500, "Failed to update employee.");
            }
        }

        // DELETE: api/employee/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            try
            {
                await _employeeService.DeleteEmployeeAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException)
            {
                return NotFound($"Employee with ID {id} not found.");
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting employee ID {Id}.", id);
                return StatusCode(500, "Failed to delete employee.");
            }
        }

        // SEARCH: api/employee/search?q=john
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Employees>>> Search([FromQuery] string? q)
        {
            try
            {
                var employees = await _employeeService.SearchEmployeesAsync(q);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching employees with term: {SearchTerm}", q);
                return StatusCode(500, "Search failed.");
            }
        }

        // TOMSELECT: api/employee/tomselect?q=john
        // Returns minimal anonymous objects: { value: "1", label: "John Doe (john@example.com)" }
        [HttpGet("tomselect")]
        public async Task<ActionResult<IEnumerable<object>>> TomSelect([FromQuery] string? q)
        {
            try
            {
                var employees = await _employeeService.SearchEmployeesAsync(q);

                var results = employees.Select(e => new
                {
                    value = e.EmployeeId.ToString(),
                    label = string.IsNullOrWhiteSpace(e.FirstName) && string.IsNullOrWhiteSpace(e.LastName)
                        ? e.Email ?? "Név nélküli munkatárs"
                        : $"{e.FirstName?.Trim()} {e.LastName?.Trim()} ({e.Email})".Trim()
                });

                return Ok(results);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in TomSelect endpoint with term: {SearchTerm}", q);
                return StatusCode(500, "TomSelect search failed.");
            }
        }

// NEW: GET: api/employee/select (for TomSelect)
[HttpGet("select")]
public async Task<IActionResult> GetEmployeesForSelect([FromQuery] string search = "")
        {
            try
            {
                var query = _context.Employees.AsQueryable();  // Use _context

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(e =>
                        (e.FirstName != null && e.FirstName.Contains(search)) ||
                        (e.LastName != null && e.LastName.Contains(search)) ||
                        (e.Email != null && e.Email.Contains(search))
                    );
                }

                var employees = await query
                    .OrderBy(e => e.LastName).ThenBy(e => e.FirstName)
                    .Select(e => new
                    {
                        id = e.EmployeeId,
                        text = string.IsNullOrWhiteSpace(e.FirstName + " " + e.LastName)
                               ? (e.Email ?? "Ismeretlen alkalmazott")
                               : $"{e.FirstName} {e.LastName}".Trim()
                    })
                    .Take(50)
                    .ToListAsync();

                _logger.LogInformation("Loaded {Count} employees for select", employees.Count);
                return Ok(employees);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading employees for select. Search: {Search}", search);
                return StatusCode(500, new { errors = new { General = new[] { "Failed to load employees: " + ex.Message } } });
            }
        }
    }
}