namespace Models;
using System.ComponentModel.DataAnnotations;

public class Customer
{
    public int CustomerId { get; set; }

    [Required(ErrorMessage = "Họ không được để trống.")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Tên không được để trống.")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng chọn giới tính.")]
    public string? Gender { get; set; }

    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Số điện thoại không được để trống.")]
    [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
    [RegularExpression(@"^0\d{9,10}$", ErrorMessage = "Số điện thoại phải bắt đầu bằng 0 và có 10-11 chữ số")]
    public string? PhoneNumber { get; set; }

    public string? Address { get; set; }

    public string? Picture { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn ngày sinh")]
    public DateOnly? Dob { get; set; }

    [Required(ErrorMessage = "Tến đăng nhập không được để trống.")]
    public string? Username { get; set; }

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string? Password { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? DeletedBy { get; set; }

    public DateTime? DeletedAt { get; set; }

    public bool? IsDeleted { get; set; }

    public string? Status { get; set; }

    public DateTime? LastLogin { get; set; }

    public virtual ICollection<ApplyVoucher> ApplyVouchers { get; set; } = new List<ApplyVoucher>();

    public virtual ICollection<Cart> Carts { get; set; } = new List<Cart>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}