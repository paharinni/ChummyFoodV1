using ChummyFoodBack.Persistance.DAO;

namespace ChummyFoodBack.Feature.Payment;

public record PaymentAmount(VoucherDAO? VoucherUsed, double TotalAmount);