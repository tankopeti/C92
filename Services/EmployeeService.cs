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

        // GET: All active employees
        public async Task<IEnumerable<Employees>> GetAllEmployeesAsync()
        {
            try
            {
                return await _context.Employees
                    .Where(e => e.IsActive)
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all active employees.");
                throw;
            }
        }

        // GET: Employee by ID (only active employees)
        public async Task<Employees?> GetEmployeeByIdAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Employee ID must be greater than zero.", nameof(id));

            try
            {
                return await _context.Employees
                    .Where(e => e.IsActive)
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

        // GET: Employee by ID including soft-deleted (for admin operations)
        public async Task<Employees?> GetEmployeeByIdIncludingDeletedAsync(int id)
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
                _logger.LogError(ex, "Error retrieving employee (including deleted) with ID {EmployeeId}.", id);
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
                    IsActive = true, // Explicitly set for clarity
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
                    .IgnoreQueryFilters() // Need this to find if it's soft-deleted
                    .FirstOrDefaultAsync(e => e.EmployeeId == updateDto.EmployeeId);

                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {updateDto.EmployeeId} not found.");

                // Don't allow updating if it's permanently deleted (IsActive = false AND some other flag)
                // For now, just allow updates to soft-deleted records
                if (!employee.IsActive)
                {
                    _logger.LogWarning("Updating soft-deleted employee {EmployeeId}. Consider restoring first.", updateDto.EmployeeId);
                }

                // Manual mapping (only update provided fields)
                employee.FirstName = updateDto.FirstName ?? employee.FirstName;
                employee.LastName = updateDto.LastName ?? employee.LastName;
                employee.Email = updateDto.Email ?? employee.Email;
                employee.Email2 = updateDto.Email2 ?? employee.Email2;
                employee.PhoneNumber = updateDto.PhoneNumber ?? employee.PhoneNumber;
                employee.PhoneNumber2 = updateDto.PhoneNumber2 ?? employee.PhoneNumber2;
                employee.DateOfBirth = updateDto.DateOfBirth ?? employee.DateOfBirth;
                employee.Address = updateDto.Address ?? employee.Address;
                employee.HireDate = updateDto.HireDate ?? employee.HireDate;
                employee.DepartmentId = updateDto.DepartmentId ?? employee.DepartmentId;
                employee.JobTitleId = updateDto.JobTitleId ?? employee.JobTitleId;
                employee.StatusId = updateDto.StatusId ?? employee.StatusId;
                employee.DefaultSiteId = updateDto.DefaultSiteId ?? employee.DefaultSiteId;
                employee.WorkingTime = updateDto.WorkingTime ?? employee.WorkingTime;
                employee.IsContracted = updateDto.IsContracted ?? employee.IsContracted;
                employee.FamilyData = updateDto.FamilyData ?? employee.FamilyData;
                employee.Comment1 = updateDto.Comment1 ?? employee.Comment1;
                employee.Comment2 = updateDto.Comment2 ?? employee.Comment2;
                employee.VacationDays = updateDto.VacationDays ?? employee.VacationDays;
                employee.FullVacationDays = updateDto.FullVacationDays ?? employee.FullVacationDays;
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

        // SOFT DELETE: Mark employee as inactive instead of removing from DB
        public async Task SoftDeleteEmployeeAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Employee ID must be greater than zero.", nameof(id));

            try
            {
                var employee = await _context.Employees
                    .IgnoreQueryFilters() // Need this to find soft-deleted employees
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {id} not found.");

                // Already soft-deleted? Just log and return
                if (!employee.IsActive)
                {
                    _logger.LogInformation("Employee with ID {EmployeeId} is already soft-deleted.", id);
                    return;
                }

                employee.IsActive = false;
                employee.UpdatedAt = DateTime.UtcNow;
                // Optional: Add DeletedAt = DateTime.UtcNow if you want to track when it was deleted

                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee soft-deleted with ID {EmployeeId}.", id);
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(ex, "Unexpected error while soft-deleting employee ID {Id}.", id);
                throw;
            }
        }

        // RESTORE: Bring back a soft-deleted employee
        public async Task RestoreEmployeeAsync(int id)
        {
            if (id <= 0)
                throw new ArgumentException("Employee ID must be greater than zero.", nameof(id));

            try
            {
                var employee = await _context.Employees
                    .IgnoreQueryFilters() // Needed to find soft-deleted records
                    .FirstOrDefaultAsync(e => e.EmployeeId == id);

                if (employee == null)
                    throw new KeyNotFoundException($"Employee with ID {id} not found.");

                // Already active? Just log and return
                if (employee.IsActive)
                {
                    _logger.LogInformation("Employee with ID {EmployeeId} is already active.", id);
                    return;
                }

                employee.IsActive = true;
                employee.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Employee restored with ID {EmployeeId}.", id);
            }
            catch (Exception ex) when (!(ex is KeyNotFoundException))
            {
                _logger.LogError(ex, "Unexpected error while restoring employee ID {Id}.", id);
                throw;
            }
        }

        // GET: All soft-deleted employees (for admin panel)
        public async Task<IEnumerable<Employees>> GetDeletedEmployeesAsync()
        {
            try
            {
                return await _context.Employees
                    .IgnoreQueryFilters()
                    .Where(e => !e.IsActive)
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deleted employees.");
                throw;
            }
        }

        // SEARCH: By name, email, or phone (only active employees)
        public async Task<IEnumerable<Employees>> SearchEmployeesAsync(string? searchTerm)
        {
            try
            {
                var query = _context.Employees
                    .Where(e => e.IsActive)
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

        // SEARCH: Including soft-deleted employees (for admin search)
        public async Task<IEnumerable<Employees>> SearchAllEmployeesAsync(string? searchTerm)
        {
            try
            {
                var query = _context.Employees
                    .IgnoreQueryFilters()
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
                _logger.LogError(ex, "Error during full employee search with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        // GET: Employees by department (only active)
        public async Task<IEnumerable<Employees>> GetEmployeesByDepartmentAsync(int departmentId)
        {
            if (departmentId <= 0)
                throw new ArgumentException("Department ID must be greater than zero.", nameof(departmentId));

            try
            {
                return await _context.Employees
                    .Where(e => e.IsActive)
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

        // GET: Active employees by status
        public async Task<IEnumerable<Employees>> GetEmployeesByStatusAsync(int statusId)
        {
            if (statusId <= 0)
                throw new ArgumentException("Status ID must be greater than zero.", nameof(statusId));

            try
            {
                return await _context.Employees
                    .Where(e => e.IsActive)
                    .Where(e => e.StatusId == statusId)
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving employees for Status ID {StatusId}.", statusId);
                throw;
            }
        }

        // CHECK: Does employee exist? (including soft-deleted)
        public async Task<bool> EmployeeExistsAsync(int id)
        {
            if (id <= 0)
                return false;

            try
            {
                return await _context.Employees
                    .IgnoreQueryFilters()
                    .AnyAsync(e => e.EmployeeId == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if employee {EmployeeId} exists.", id);
                throw;
            }
        }

        // COUNT: Total active employees
        public async Task<int> GetActiveEmployeesCountAsync()
        {
            try
            {
                return await _context.Employees
                    .Where(e => e.IsActive)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting active employees.");
                throw;
            }
        }

        // COUNT: Total deleted employees
        public async Task<int> GetDeletedEmployeesCountAsync()
        {
            try
            {
                return await _context.Employees
                    .IgnoreQueryFilters()
                    .Where(e => !e.IsActive)
                    .CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error counting deleted employees.");
                throw;
            }
        }
    }
}