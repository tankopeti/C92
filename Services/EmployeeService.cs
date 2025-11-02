using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cloud9_2.Data;
using Cloud9_2.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cloud9_2.Services
{
    public class EmployeeService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmployeeService> _logger;

        public EmployeeService(
            ApplicationDbContext context,
            ILogger<EmployeeService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // GET: All employees
        public async Task<IEnumerable<Employees>> GetAllEmployeesAsync()
        {
            try
            {
                return await _context.Employees
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all employees.");
                throw;
            }
        }

        // GET: Employee by ID
        public async Task<Employees?> GetEmployeeByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Employee ID must be greater than zero.", nameof(id));

            try
            {
                return await _context.Employees
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employee with ID {EmployeeId}.", id);
                throw;
            }
        }

        // CREATE: From EmployeesCreateDto
        public async Task<Employees> CreateEmployeeAsync(EmployeesCreateDto createDto)
        {
            if (createDto == null)
                throw new ArgumentNullException(nameof(createDto));

            try
            {
                var employee = new Employees
                {
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    Email = createDto.Email,
                    Email2 = createDto.Email2,
                    PhoneNumber = createDto.PhoneNumber,
                    PhoneNumber2 = createDto.PhoneNumber2,
                    DateOfBirth = createDto.DateOfBirth,
                    Address = createDto.Address,
                    HireDate = createDto.HireDate,
                    DepartmentId = createDto.DepartmentId,
                    JobTitleId = createDto.JobTitleId,
                    StatusId = createDto.StatusId,
                    DefaultSiteId = createDto.DefaultSiteId,
                    WorkingTime = createDto.WorkingTime ?? 8.00m,
                    IsContracted = createDto.IsContracted ?? 0,
                    FamilyData = createDto.FamilyData,
                    Comment1 = createDto.Comment1,
                    Comment2 = createDto.Comment2,
                    VacationDays = createDto.VacationDays,
                    FullVacationDays = createDto.FullVacationDays,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Employees.Add(employee);
                await _context.SaveChangesAsync();

                // Reload navigation properties
                await _context.Entry(employee)
                    .Reference(e => e.JobTitle).LoadAsync();
                await _context.Entry(employee)
                    .Reference(e => e.Status).LoadAsync();

                _logger.LogInformation("Employee created with ID {EmployeeId}.", employee.EmployeeId);
                return employee;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while creating employee.");
                throw new InvalidOperationException("Failed to create employee due to database constraint.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while creating employee.");
                throw;
            }
        }

        // UPDATE: From EmployeesUpdateDto
        public async Task<Employees> UpdateEmployeeAsync(EmployeesUpdateDto updateDto)
        {
            if (updateDto == null)
                throw new ArgumentNullException(nameof(updateDto));

            if (updateDto.EmployeeId <= 0)
                throw new ArgumentException("Invalid Employee ID.", nameof(updateDto.EmployeeId));

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == updateDto.EmployeeId);

                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {updateDto.EmployeeId} not found.");

                // Manual mapping (only update provided fields)
                employee.FirstName = updateDto.FirstName;
                employee.LastName = updateDto.LastName;
                employee.Email = updateDto.Email;
                employee.Email2 = updateDto.Email2;
                employee.PhoneNumber = updateDto.PhoneNumber;
                employee.PhoneNumber2 = updateDto.PhoneNumber2;
                employee.DateOfBirth = updateDto.DateOfBirth;
                employee.Address = updateDto.Address;
                employee.HireDate = updateDto.HireDate;
                employee.DepartmentId = updateDto.DepartmentId;
                employee.JobTitleId = updateDto.JobTitleId;
                employee.StatusId = updateDto.StatusId;
                employee.DefaultSiteId = updateDto.DefaultSiteId;
                employee.WorkingTime = updateDto.WorkingTime;
                employee.IsContracted = updateDto.IsContracted;
                employee.FamilyData = updateDto.FamilyData;
                employee.Comment1 = updateDto.Comment1;
                employee.Comment2 = updateDto.Comment2;
                employee.VacationDays = updateDto.VacationDays;
                employee.FullVacationDays = updateDto.FullVacationDays;
                employee.UpdatedAt = DateTime.UtcNow;

                _context.Employees.Update(employee);
                await _context.SaveChangesAsync();

                // Reload navigation properties
                await _context.Entry(employee)
                    .Reference(e => e.JobTitle).LoadAsync();
                await _context.Entry(employee)
                    .Reference(e => e.Status).LoadAsync();

                _logger.LogInformation("Employee updated with ID {EmployeeId}.", employee.EmployeeId);
                return employee;
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while updating employee ID {EmployeeId}.", updateDto.EmployeeId);
                throw new InvalidOperationException("Failed to update employee due to database constraint.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while updating employee ID {EmployeeId}.", updateDto.EmployeeId);
                throw;
            }
        }

        // DELETE: Hard delete
        public async Task DeleteEmployeeAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Employee ID must be greater than zero.", nameof(id));

            try
            {
                var employee = await _context.Employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {id} not found.");

                _context.Employees.Remove(employee);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee deleted with ID {EmployeeId}.", id);
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "Database error while deleting employee ID {Id}.", id);
                throw new InvalidOperationException("Failed to delete employee due to referential integrity.", dbEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting employee ID {Id}.", id);
                throw;
            }
        }

        // SEARCH: By name, email, or phone
        public async Task<IEnumerable<Employees>> SearchEmployeesAsync(string? searchTerm)
        {
            try
            {
                var query = _context.Employees
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .AsNoTracking();

                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    searchTerm = searchTerm.Trim().ToLower();
                    query = query.Where(e =>
                        (e.FirstName != null && e.FirstName.ToLower().Contains(searchTerm)) ||
                        (e.LastName != null && e.LastName.ToLower().Contains(searchTerm)) ||
                        (e.Email != null && e.Email.ToLower().Contains(searchTerm)) ||
                        (e.PhoneNumber != null && e.PhoneNumber.Contains(searchTerm))
                    );
                }

                return await query.ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during employee search with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        // OPTIONAL: Get by department
        public async Task<IEnumerable<Employees>> GetEmployeesByDepartmentAsync(int departmentId)
        {
            try
            {
                return await _context.Employees
                    .Where(e => e.DepartmentId == departmentId)
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees for Department ID {DepartmentId}.", departmentId);
                throw;
            }
        }
    }
}