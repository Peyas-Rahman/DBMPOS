using DBM.POS.Domain.Entities;
using DBM.POS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace DBM.POS.Infrastructure.Seed;

public static class POSDataSeeder
{
    public static async Task SeedAsync(
        POSDbContext context,
        IConfiguration configuration)
    {
        await context.Database.MigrateAsync();

        await SeedPermissionsAsync(context);
        await SeedRolesAsync(context);
        await SeedPlatformAdminAsync(context, configuration);
    }

    private static async Task SeedPermissionsAsync(
        POSDbContext context)
    {
        var permissions = new List<Permission>
        {
            new()
            {
                PermissionCode = "Dashboard.View",
                PermissionName = "View Dashboard",
                Module = "Dashboard",
                Description = "View dashboard"
            },

            new()
            {
                PermissionCode = "Product.View",
                PermissionName = "View Products",
                Module = "Product"
            },
            new()
            {
                PermissionCode = "Product.Create",
                PermissionName = "Create Product",
                Module = "Product"
            },
            new()
            {
                PermissionCode = "Product.Update",
                PermissionName = "Update Product",
                Module = "Product"
            },
            new()
            {
                PermissionCode = "Product.Delete",
                PermissionName = "Delete Product",
                Module = "Product"
            },

            new()
            {
                PermissionCode = "Sales.View",
                PermissionName = "View Sales",
                Module = "Sales"
            },
            new()
            {
                PermissionCode = "Sales.Create",
                PermissionName = "Create Sale",
                Module = "Sales"
            },
            new()
            {
                PermissionCode = "Sales.Return",
                PermissionName = "Sales Return",
                Module = "Sales"
            },

            new()
            {
                PermissionCode = "Purchase.View",
                PermissionName = "View Purchases",
                Module = "Purchase"
            },
            new()
            {
                PermissionCode = "Purchase.Create",
                PermissionName = "Create Purchase",
                Module = "Purchase"
            },
            new()
            {
                PermissionCode = "Purchase.Return",
                PermissionName = "Purchase Return",
                Module = "Purchase"
            },

            new()
            {
                PermissionCode = "Inventory.View",
                PermissionName = "View Inventory",
                Module = "Inventory"
            },
            new()
            {
                PermissionCode = "Inventory.Adjustment",
                PermissionName = "Inventory Adjustment",
                Module = "Inventory"
            },
            new()
            {
                PermissionCode = "Inventory.Transfer",
                PermissionName = "Inventory Transfer",
                Module = "Inventory"
            },

            new()
            {
                PermissionCode = "Customer.View",
                PermissionName = "View Customers",
                Module = "Customer"
            },
            new()
            {
                PermissionCode = "Customer.Create",
                PermissionName = "Create Customer",
                Module = "Customer"
            },
            new()
            {
                PermissionCode = "Customer.Update",
                PermissionName = "Update Customer",
                Module = "Customer"
            },

            new()
            {
                PermissionCode = "Supplier.View",
                PermissionName = "View Suppliers",
                Module = "Supplier"
            },
            new()
            {
                PermissionCode = "Supplier.Create",
                PermissionName = "Create Supplier",
                Module = "Supplier"
            },
            new()
            {
                PermissionCode = "Supplier.Update",
                PermissionName = "Update Supplier",
                Module = "Supplier"
            },

            new()
            {
                PermissionCode = "Reports.View",
                PermissionName = "View Reports",
                Module = "Reports"
            },
            new()
            {
                PermissionCode = "Reports.Export",
                PermissionName = "Export Reports",
                Module = "Reports"
            },

            new()
            {
                PermissionCode = "Users.View",
                PermissionName = "View Users",
                Module = "Users"
            },
            new()
            {
                PermissionCode = "Users.Create",
                PermissionName = "Create User",
                Module = "Users"
            },
            new()
            {
                PermissionCode = "Users.Update",
                PermissionName = "Update User",
                Module = "Users"
            },
            new()
            {
                PermissionCode = "Users.Delete",
                PermissionName = "Delete User",
                Module = "Users"
            },

            new()
            {
                PermissionCode = "Settings.View",
                PermissionName = "View Settings",
                Module = "Settings"
            },
            new()
            {
                PermissionCode = "Settings.Update",
                PermissionName = "Update Settings",
                Module = "Settings"
            }
        };

        foreach (var permission in permissions)
        {
            var exists = await context.Permissions
                .AnyAsync(x =>
                    x.PermissionCode == permission.PermissionCode);

            if (!exists)
            {
                context.Permissions.Add(permission);
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedRolesAsync(
        POSDbContext context)
    {
        var roles = new[]
        {
            new
            {
                Name = "Super Admin",
                Description = "Full system access"
            },
            new
            {
                Name = "Company Admin",
                Description = "Company level administration"
            },
            new
            {
                Name = "Branch Manager",
                Description = "Branch management access"
            },
            new
            {
                Name = "Sales Manager",
                Description = "Sales management access"
            },
            new
            {
                Name = "Cashier",
                Description = "POS sales and cashier operations"
            },
            new
            {
                Name = "Inventory Manager",
                Description = "Inventory and stock management"
            },
            new
            {
                Name = "Accountant",
                Description = "Accounting and financial operations"
            },
            new
            {
                Name = "Auditor",
                Description = "Read-only audit and reporting access"
            }
        };

        foreach (var roleInfo in roles)
        {
            var exists = await context.Roles
                .AnyAsync(x =>
                    x.CompanyId == null &&
                    x.RoleName == roleInfo.Name);

            if (!exists)
            {
                context.Roles.Add(new Role
                {
                    CompanyId = null,
                    RoleName = roleInfo.Name,
                    Description = roleInfo.Description,
                    IsSystemRole = true,
                    IsActive = true
                });
            }
        }

        await context.SaveChangesAsync();

        await SeedRolePermissionsAsync(context);
    }

    private static async Task SeedRolePermissionsAsync(
        POSDbContext context)
    {
        var allPermissions = await context.Permissions
            .Where(x => x.IsActive)
            .ToListAsync();

        var roles = await context.Roles
            .Where(x =>
                x.CompanyId == null &&
                x.IsSystemRole &&
                x.IsActive)
            .ToListAsync();

        foreach (var role in roles)
        {
            IEnumerable<Permission> permissionsForRole =
                role.RoleName switch
                {
                    "Super Admin" => allPermissions,

                    "Company Admin" => allPermissions
                        .Where(x =>
                            x.Module != "Settings" ||
                            x.PermissionCode == "Settings.View"),

                    "Branch Manager" => allPermissions
                        .Where(x =>
                            x.Module == "Dashboard" ||
                            x.Module == "Product" ||
                            x.Module == "Sales" ||
                            x.Module == "Purchase" ||
                            x.Module == "Inventory" ||
                            x.Module == "Customer" ||
                            x.Module == "Supplier" ||
                            x.Module == "Reports"),

                    "Sales Manager" => allPermissions
                        .Where(x =>
                            x.Module == "Dashboard" ||
                            x.Module == "Product" ||
                            x.Module == "Sales" ||
                            x.Module == "Customer" ||
                            x.Module == "Reports"),

                    "Cashier" => allPermissions
                        .Where(x =>
                            x.PermissionCode == "Dashboard.View" ||
                            x.PermissionCode == "Product.View" ||
                            x.PermissionCode == "Sales.View" ||
                            x.PermissionCode == "Sales.Create" ||
                            x.PermissionCode == "Sales.Return" ||
                            x.PermissionCode == "Customer.View" ||
                            x.PermissionCode == "Customer.Create"),

                    "Inventory Manager" => allPermissions
                        .Where(x =>
                            x.Module == "Dashboard" ||
                            x.Module == "Product" ||
                            x.Module == "Purchase" ||
                            x.Module == "Inventory" ||
                            x.Module == "Supplier" ||
                            x.Module == "Reports"),

                    "Accountant" => allPermissions
                        .Where(x =>
                            x.Module == "Dashboard" ||
                            x.Module == "Sales" ||
                            x.Module == "Purchase" ||
                            x.Module == "Reports"),

                    "Auditor" => allPermissions
                        .Where(x =>
                            x.PermissionCode.EndsWith(".View") ||
                            x.PermissionCode == "Reports.Export"),

                    _ => Enumerable.Empty<Permission>()
                };

            foreach (var permission in permissionsForRole)
            {
                var exists = await context.RolePermissions
                    .AnyAsync(x =>
                        x.RoleId == role.Id &&
                        x.PermissionId == permission.Id);

                if (!exists)
                {
                    context.RolePermissions.Add(
                        new RolePermission
                        {
                            RoleId = role.Id,
                            PermissionId = permission.Id,
                            IsActive = true
                        });
                }
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedPlatformAdminAsync(
        POSDbContext context,
        IConfiguration configuration)
    {
        var company = await context.Companies
            .FirstOrDefaultAsync(x =>
                x.CompanyCode == "DBMSOFT");

        if (company == null)
        {
            company = new Company
            {
                CompanyCode = "DBMSOFT",
                CompanyName = "DBM SOFT LTD.",
                BusinessType = "Software",
                CurrencyCode = "BDT",
                TimeZone = "Asia/Dhaka",
                IsActive = true
            };

            context.Companies.Add(company);

            await context.SaveChangesAsync();
        }

        var username = configuration["InitialAdmin:Username"]
            ?? throw new InvalidOperationException(
                "InitialAdmin Username is not configured.");

        var email = configuration["InitialAdmin:Email"]
            ?? throw new InvalidOperationException(
                "InitialAdmin Email is not configured.");

        var fullName = configuration["InitialAdmin:FullName"]
            ?? throw new InvalidOperationException(
                "InitialAdmin FullName is not configured.");

        var password = configuration["InitialAdmin:Password"]
            ?? throw new InvalidOperationException(
                "InitialAdmin Password is not configured.");

        var adminRole = await context.Roles
            .FirstOrDefaultAsync(x =>
                x.RoleName == "Super Admin" &&
                x.IsSystemRole &&
                x.CompanyId == null &&
                x.IsActive);

        if (adminRole == null)
        {
            throw new InvalidOperationException(
                "Super Admin role was not found.");
        }

        var user = await context.Users
            .FirstOrDefaultAsync(x =>
                x.CompanyId == company.Id &&
                x.Username == username);

        if (user == null)
        {
            user = new User
            {
                CompanyId = company.Id,
                Username = username,
                Email = email,
                FullName = fullName,
                IsActive = true
            };

            var passwordHasher = new PasswordHasher<User>();

            user.PasswordHash = passwordHasher.HashPassword(
                user,
                password);

            context.Users.Add(user);

            await context.SaveChangesAsync();
        }

        var defaultBranch = await context.Branches.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.BranchCode == "HO");
        if (defaultBranch == null)
        {
            defaultBranch = new Branch
            {
                CompanyId = company.Id, BranchCode = "HO", BranchName = "Head Office",
                IsHeadOffice = true, IsActive = true
            };
            context.Branches.Add(defaultBranch);
            await context.SaveChangesAsync();
        }

        var defaultWarehouse = await context.Warehouses.FirstOrDefaultAsync(x => x.CompanyId == company.Id && x.BranchId == defaultBranch.Id && x.WarehouseCode == "MAIN");
        if (defaultWarehouse == null)
        {
            context.Warehouses.Add(new Warehouse
            {
                CompanyId = company.Id, BranchId = defaultBranch.Id, WarehouseCode = "MAIN",
                WarehouseName = "Main Warehouse", IsDefault = true, IsActive = true
            });
            await context.SaveChangesAsync();
        }

        var userRoleExists = await context.UserRoles
            .AnyAsync(x =>
                x.UserId == user.Id &&
                x.RoleId == adminRole.Id &&
                x.BranchId == null);

        if (!userRoleExists)
        {
            context.UserRoles.Add(
                new UserRole
                {
                    UserId = user.Id,
                    RoleId = adminRole.Id,
                    BranchId = null,
                    IsActive = true
                });

            await context.SaveChangesAsync();
        }
    }
}