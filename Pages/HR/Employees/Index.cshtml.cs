using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Cloud9_2.Data;
using Cloud9_2.Models;

namespace Cloud9_2.Pages.HR.WorkGroup
{
    public class EmployeesModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EmployeesModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IList<Employees> Employees { get; set; } = new List<Employees>();
        public IList<JobTitle> JobTitles { get; set; } = new List<JobTitle>();
        public IList<EmploymentStatus> EmploymentStatuses { get; set; } = new List<EmploymentStatus>();

        [BindProperty]
        public EmployeesCreateDto EmployeeCreate { get; set; } = new EmployeesCreateDto();

        [BindProperty]
        public EmployeesUpdateDto EmployeeUpdate { get; set; } = new EmployeesUpdateDto();

        public async Task OnGetAsync()
        {
            Employees = await _context.Employees
                .Include(e => e.JobTitle)
                .Include(e => e.Status)
                .ToListAsync();
            JobTitles = await _context.JobTitles.ToListAsync();
            EmploymentStatuses = await _context.EmploymentStatuses.ToListAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (!ModelState.IsValid)
            {
                Employees = await _context.Employees
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .ToListAsync();
                JobTitles = await _context.JobTitles.ToListAsync();
                EmploymentStatuses = await _context.EmploymentStatuses.ToListAsync();
                return Page();
            }

            var employee = new Employees
            {
                FirstName = EmployeeCreate.FirstName,
                LastName = EmployeeCreate.LastName,
                Email = EmployeeCreate.Email,
                Email2 = EmployeeCreate.Email2,
                PhoneNumber = EmployeeCreate.PhoneNumber,
                PhoneNumber2 = EmployeeCreate.PhoneNumber2,
                DateOfBirth = EmployeeCreate.DateOfBirth,
                Address = EmployeeCreate.Address,
                HireDate = EmployeeCreate.HireDate,
                DepartmentId = EmployeeCreate.DepartmentId,
                JobTitleId = EmployeeCreate.JobTitleId,
                StatusId = EmployeeCreate.StatusId,
                DefaultSiteId = EmployeeCreate.DefaultSiteId,
                WorkingTime = EmployeeCreate.WorkingTime,
                IsContracted = EmployeeCreate.IsContracted,
                FamilyData = EmployeeCreate.FamilyData,
                Comment1 = EmployeeCreate.Comment1,
                Comment2 = EmployeeCreate.Comment2,
                CreatedAt = DateTime.UtcNow,
                VacationDays = EmployeeCreate.VacationDays,
                FullVacationDays = EmployeeCreate.FullVacationDays
            };

            _context.Employees.Add(employee);
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee created successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while creating the employee.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostUpdateAsync()
        {
            if (!ModelState.IsValid)
            {
                Employees = await _context.Employees
                    .Include(e => e.JobTitle)
                    .Include(e => e.Status)
                    .ToListAsync();
                JobTitles = await _context.JobTitles.ToListAsync();
                EmploymentStatuses = await _context.EmploymentStatuses.ToListAsync();
                return Page();
            }

            var employee = await _context.Employees.FindAsync(EmployeeUpdate.EmployeeId);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToPage();
            }

            employee.FirstName = EmployeeUpdate.FirstName;
            employee.LastName = EmployeeUpdate.LastName;
            employee.Email = EmployeeUpdate.Email;
            employee.Email2 = EmployeeUpdate.Email2;
            employee.PhoneNumber = EmployeeUpdate.PhoneNumber;
            employee.PhoneNumber2 = EmployeeUpdate.PhoneNumber2;
            employee.DateOfBirth = EmployeeUpdate.DateOfBirth;
            employee.Address = EmployeeUpdate.Address;
            employee.HireDate = EmployeeUpdate.HireDate;
            employee.DepartmentId = EmployeeUpdate.DepartmentId;
            employee.JobTitleId = EmployeeUpdate.JobTitleId;
            employee.StatusId = EmployeeUpdate.StatusId;
            employee.DefaultSiteId = EmployeeUpdate.DefaultSiteId;
            employee.WorkingTime = EmployeeUpdate.WorkingTime;
            employee.IsContracted = EmployeeUpdate.IsContracted;
            employee.FamilyData = EmployeeUpdate.FamilyData;
            employee.Comment1 = EmployeeUpdate.Comment1;
            employee.Comment2 = EmployeeUpdate.Comment2;
            employee.UpdatedAt = DateTime.UtcNow;
            employee.VacationDays = EmployeeUpdate.VacationDays;
            employee.FullVacationDays = EmployeeUpdate.FullVacationDays;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee updated successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while updating the employee.";
            }

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var employee = await _context.Employees.FindAsync(id);
            if (employee == null)
            {
                TempData["ErrorMessage"] = "Employee not found.";
                return RedirectToPage();
            }

            _context.Employees.Remove(employee);
            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Employee deleted successfully.";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the employee.";
            }

            return RedirectToPage();
        }
    }
}