using Microsoft.AspNetCore.Mvc;
using Models;
using Repository.Interface;
using Xanh_Dau.DTO;
using Xanh_Dau.Services;

namespace Xanh_Dau.Controllers;

public class AdminController : Controller
{
    private readonly IAdminRepository _adminRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly FileService _fileService;
    private readonly IOrderRepository _orderRepository;
    private readonly IProductImageRepository _productImageRepository;
    private readonly IProductRepository _productRepository;
    private readonly TokenService _tokenService;

    public AdminController(
        IAdminRepository adminRepository,
        TokenService tokenService,
        ICustomerRepository customerRepository,
        ICategoryRepository categoryRepository,
        IProductRepository productRepository,
        IProductImageRepository productImageRepository,
        IOrderRepository orderRepository,
        FileService fileService)
    {
        _adminRepository = adminRepository;
        _tokenService = tokenService;
        _customerRepository = customerRepository;
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
        _productImageRepository = productImageRepository;
        _orderRepository = orderRepository;
        _fileService = fileService;
    }

    private int getUserId()
    {
        var token = Request.Cookies["auth_token"];

        var userId = _tokenService.GetUserIdFromToken(token);

        return Convert.ToInt32(userId);
    }

    public async Task<IActionResult> Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Index(string username, string password)
    {
        var user = await _adminRepository.GetAdminByUsernameAsync(username);

        if (user == null || user.Password != password)
        {
            ViewBag.Error = "Invalid username or password";
            return View();
        }

        // Create token for the user
        var tokenString = await _tokenService.CreateTokenAdminAsync(Convert.ToInt32(user.AdminId));

        Console.WriteLine(tokenString);

        Response.Cookies.Append("auth_token", tokenString, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            Expires = DateTime.Now.AddHours(24),
            SameSite = SameSiteMode.Strict
        });

        return RedirectToAction("DashBoard", "Admin");
    }

    public async Task<IActionResult> Dashboard()
    {
        var products = await _productRepository.GetAllProductsAsync();
        var customers = await _customerRepository.GetListCustomerAsync();
        var orders = await _orderRepository.GetOrders();
        var categories = await _categoryRepository.GetAllCategoriesAsync();

        var totalRevenue = orders
            .Where(o => o.Status == "completed")
            .Sum(o => o.TotalPrice);

        var totalOrders = orders
            .Count(o => o.Status != "cancelled");

        // Lấy dữ liệu người dùng đăng ký theo tháng
        var userRegistrations = customers
            .Where(c => c.CreatedAt.HasValue) // Lọc bỏ các giá trị null
            .GroupBy(c => new { Year = c.CreatedAt.Value.Year, Month = c.CreatedAt.Value.Month }) // Lấy giá trị thực tế
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new 
            { 
                Month = $"{g.Key.Year}-{g.Key.Month:D2}", // Định dạng YYYY-MM
                Count = g.Count() 
            })
            .ToList();


        var dashboardDTO = new DashboardDTO
        {
            ListProducts = products,
            ListCustomers = customers,
            ListOrders = orders,
            ListCategories = categories,
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            UserRegistrationMonths = userRegistrations.Select(u => u.Month).ToArray(),
            UserRegistrationCounts = userRegistrations.Select(u => u.Count).ToArray()
        };

        return View(dashboardDTO);
    }

    [HttpGet]
    public async Task<IActionResult> Customer(int page = 1, int pageSize = 6)
    {
        var (customers, totalCount) = await _customerRepository.GetPaginationCustomersAsync(page, pageSize);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var customerDto = new CustomerDTO
        {
            ListCustomer = customers,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };

        return View(customerDto);
    }

    public async Task<IActionResult> CustomerDetail(int customerId)
    {
        var customerDTO = new CustomerDTO();
        customerDTO.Customer = await _customerRepository.GetCustomerByIdAsync(customerId);
        return View(customerDTO);
    }

    public async Task<IActionResult> Manage(int page = 1, int pageSize = 6)
    {
        var (admins, totalCount) = await _adminRepository.GetPaginationAdminsAsync(page, pageSize);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        var adminsDto = new AdminDTO
        {
            ListAdmin = admins,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };

        return View(adminsDto);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAdmin(Admin admin)
    {
        if (!ModelState.IsValid) return View(admin);

        // Kiểm tra email đã tồn tại chưa
        var existingAdmin = await _adminRepository.FindAdminByEmailAsync(admin.Email);
        
        if (existingAdmin != null)
        {
            ModelState.AddModelError("Email", "Email đã được sử dụng. Vui lòng chọn email khác.");
            return View(admin);
        }

        var ad = await _adminRepository.CreateAdminAsync(admin);

        if (ad != null) return RedirectToAction("Manage", "Admin");

        return View(admin);
    }

    [HttpGet]
    public async Task<IActionResult> EditManage(int adminId)
    {
        var admin = await _adminRepository.GetAdminByIdAsync(adminId);
        if (admin == null)
        {
            return NotFound();
        }
        return View(admin); // Trả về View chỉnh sửa
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditManage(Admin admin) 
    {
        if (!ModelState.IsValid)
        {
            TempData["updateMess"] = "Dữ liệu không hợp lệ!";
            return View(admin); 
        }

        try
        {
            var ad = await _adminRepository.GetAdminByIdAsync(admin.AdminId);
            if (ad == null)
            {
                return NotFound();
            }

            ad.FirstName = admin.FirstName;
            ad.LastName = admin.LastName;
            ad.Email = admin.Email;
            ad.Role = admin.Role;
            ad.Status = admin.Status;
            ad.UpdatedAt = DateTime.Now;

            var updated = await _adminRepository.UpdateAdminAsync(ad);
            if (updated == null)
            {
                TempData["updateMess"] = "Cập nhật không thành công!";
                return View(admin);
            }

            TempData["updateDone"] = "Cập nhật thành công!";
            return RedirectToAction("Manage", "Admin");
        }
        catch (Exception)
        {
            TempData["updateMess"] = "Lỗi khi cập nhật!";
            return View(admin);
        }
    }


    public async Task<IActionResult> ManageDetail(int adminId)
    {
        var adminDTO = new AdminDTO();
        adminDTO.Admin = await _adminRepository.GetAdminByIdAsync(adminId);
        return View(adminDTO);
    }

    [HttpGet]
    public async Task<IActionResult> Product(int page = 1, int pageSize = 6)
    {
        var (products, totalCount) = await _productRepository
            .GetPaginationProductsAsync(page, pageSize)
            .ConfigureAwait(false);


        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var productDto = new ProductDTO
        {
            ListProducts = products,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };

        return View(productDto);
    }


    public async Task<IActionResult> ProductDetail(int productId)
    {
        var productDTO = new ProductDTO();
        productDTO.Product = await _productRepository.GetProductByIdAsync(productId);
        productDTO.ListProductImages = await _productImageRepository.GetAllProductImagesAsync();
        return View(productDTO);
    }

    [HttpGet]
    public async Task<IActionResult> CreateProduct()
    {
        var productDTO = new ProductDTO();
        var category = await _categoryRepository.GetAllCategoriesAsync();
        productDTO.ListCategories = category;
        return View(productDTO);
    }

    [HttpPost]
    public async Task<IActionResult> CreateProduct(ProductDTO productDto, List<IFormFile> images)
    {
        if (productDto == null)
        {
            Console.WriteLine("ProductDto is null");
            return View(productDto);
        }

        try
        {
            // Gắn thông tin tạo sản phẩm
            productDto.Product.CreatedAt = DateTime.Now;
            productDto.Product.CreatedBy = getUserId();

            // Lưu sản phẩm vào cơ sở dữ liệu
            var createdProduct = await _productRepository.CreateProductAsync(productDto.Product);

            if (createdProduct != null && images != null && images.Any())
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var productImages = new List<ProductImage>();

                foreach (var image in images)
                {
                    if (image == null || image.Length == 0)
                        continue;

                    var fileExtension = Path.GetExtension(image.FileName).ToLower();
                    if (!allowedExtensions.Contains(fileExtension))
                        continue;

                    // Upload hình ảnh và lấy URL
                    var imageUrl = await _fileService.UploadImageAsync(image);

                    // Tạo đối tượng ProductImage
                    productImages.Add(new ProductImage
                    {
                        ProductId = createdProduct.ProductId,
                        ImageUrl = imageUrl,
                        IsPrimary = productImages.Count == 0, // Đặt ảnh đầu tiên là ảnh chính
                        ArrangeOrder = productImages.Count + 1 // Sắp xếp theo thứ tự tải lên
                    });
                }

                // Lưu danh sách hình ảnh vào cơ sở dữ liệu
                if (productImages.Any()) await _productImageRepository.AddProductImagesAsync(productImages);
            }

            return RedirectToAction("Product", "Admin");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError("", $"Đã xảy ra lỗi: {ex.Message}");
            return View(productDto);
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditProduct(int id)
    {
        try
        {
            var product = await _productRepository.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            // Tạo DTO từ Product
            var productDTO = new ProductDTO
            {
                Product = product, // Đảm bảo Product trong DTO chứa dữ liệu hợp lệ
                ListCategories = await _categoryRepository.GetAllCategoriesAsync() // Load danh mục
            };

            return View(productDTO);
        }
        catch (Exception)
        {
            TempData["errorMess"] = "Lỗi khi tải sản phẩm!";
            return RedirectToAction("Product", "Admin");
        }
    }


    [HttpPost]
    public async Task<IActionResult> EditProduct(ProductDTO productDTO)
    {
        if (productDTO == null || productDTO.Product == null)
        {
            TempData["updateMess"] = "Dữ liệu không hợp lệ!";
            System.Console.WriteLine(1);
            return RedirectToAction("Product", "Admin");
        }

        var existingProduct = await _productRepository.GetProductByIdAsync(productDTO.Product.ProductId);
        if (existingProduct == null)
        {
            TempData["updateMess"] = "Sản phẩm không tồn tại!";
            return RedirectToAction("Product", "Admin");
        }

        existingProduct.Name = productDTO.Product.Name;
        existingProduct.Description = productDTO.Product.Description;
        existingProduct.Price = productDTO.Product.Price;
        existingProduct.StockQuantity = productDTO.Product.StockQuantity;
        existingProduct.CategoryId = productDTO.Product.CategoryId;
        existingProduct.UpdatedAt = DateTime.Now;

        await _productRepository.UpdateProductAsync(existingProduct);

        TempData["updateDone"] = "Cập nhật sản phẩm thành công!";
        return RedirectToAction("Product", "Admin");
    }

    [HttpGet]
    public async Task<IActionResult> Category(int page = 1, int pageSize = 6)
    {
        var (categories, totalCount) = await _categoryRepository.GetPaginationCategorysAsync(page, pageSize);

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var categoryDto = new CategoryDTO
        {
            ListCategories = categories,
            CurrentPage = page,
            TotalPages = totalPages,
            PageSize = pageSize
        };

        return View(categoryDto);
    }

    public async Task<IActionResult> CategoryDetail(int categoryId)
    {
        var categoryDTO = new CategoryDTO();
        categoryDTO.Category = await _categoryRepository.GetCategoryByIdAsync(categoryId);
        return View(categoryDTO);
    }


    [HttpPost]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        try
        {
            await _productRepository.DeleteProductAsync(id);
            return RedirectToAction("Product", "Admin");
        }
        catch (Exception e)
        {
            Console.WriteLine("Lỗi khi xóa sản phẩm! " + e.Message);
            return RedirectToAction("Product");
        }
    }


    [HttpPost]
    public async Task<IActionResult> Category(CategoryDTO categoryDto)
    {
        var category = await _categoryRepository.CreateCategoryAsync(categoryDto.Category);
        return RedirectToAction("Category", "Admin");
    }

    [HttpPost("Admin/DeleteCategory/{id}")]
    public async Task<IActionResult> DeleteCategory(int id)
    {
        try
        {
            await _categoryRepository.DeleteCategoryAsync(id);
            return RedirectToAction("Category", "Admin");
        }
        catch (Exception e)
        {
            Console.WriteLine("Lỗi khi xóa danh mục! " + e.Message);
            return RedirectToAction("Category");
        }
    }

    [HttpGet]
    public async Task<IActionResult> EditCategory(int id)
    {
        try
        {
            var category = await _categoryRepository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return NotFound();
            }

            return View(category);
        }
        catch (Exception)
        {
            TempData["errorMess"] = "Lỗi khi tải danh mục!";
            return RedirectToAction("Category", "Admin");
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditCategory(Category category)
    {
        if (!ModelState.IsValid)
        {
            TempData["updateMess"] = "Dữ liệu không hợp lệ!";
            return View(category);
        }

        try
        {
            var existingCategory = await _categoryRepository.GetCategoryByIdAsync(category.CategoryId);
            if (existingCategory == null)
            {
                return NotFound();
            }

            // Cập nhật thông tin danh mục
            existingCategory.Name = category.Name;
            existingCategory.Description = category.Description;
            existingCategory.UpdatedAt = DateTime.Now;

            var updated = await _categoryRepository.UpdateCategoryAsync(existingCategory);
            if (updated == null)
            {
                TempData["updateMess"] = "Cập nhật không thành công!";
                return View(category);
            }

            TempData["updateDone"] = "Cập nhật danh mục thành công!";
            return RedirectToAction("Category", "Admin");
        }
        catch (Exception)
        {
            TempData["updateMess"] = "Lỗi khi cập nhật danh mục!";
            return View(category);
        }
    }


    public async Task<IActionResult> Order()
    {
        var adminOrderDTO = new AdminOrderDTO();
        adminOrderDTO.ListOrder = await _orderRepository.GetOrders();
        adminOrderDTO.ListCustomers = await _customerRepository.GetListCustomerAsync();
        return View(adminOrderDTO);
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetail(int orderId, int customerID)
    {
        try
        {
            var customerId = customerID;
            var order = await _orderRepository.GetOrderByIdAsync(orderId);

            // Verify order belongs to current customer
            if (order == null || order.CustomerId != customerId) return NotFound("Không tìm thấy đơn hàng");

            return View(order);
        }
        catch (Exception ex)
        {
            return BadRequest($"Lỗi khi tải chi tiết đơn hàng: {ex.Message}");
        }
    }

    public IActionResult Analyst()
    {
        return View();
    }

    [HttpGet]
    public IActionResult Logout()
    {
        // Delete auth_token cookie
        Response.Cookies.Delete("auth_token");

        return RedirectToAction("Index");
    }


    private bool IsValidStatus(string status)
    {
        var validStatuses = new[] { "pending", "processed", "completed", "cancelled" };
        return validStatuses.Contains(status.ToLower());
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus([FromBody] UpdateOrderStatusDTO model)
    {
        try
        {
            var success = await _orderRepository.UpdateOrderStatusAsync(model.OrderId, model.Status);
            if (success) return Json(new { success = true });
            return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
        }
        catch (Exception ex)
        {
            return Json(new { success = false, message = ex.Message });
        }
    }
}