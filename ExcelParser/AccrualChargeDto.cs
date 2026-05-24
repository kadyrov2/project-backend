namespace ExcelParser
{
    public class AccrualChargeDto
    {
        public int ApartmentNumber { get; set; }
        public string FullName { get; set; } = string.Empty;
        public double TotalArea { get; set; }

        // Показания счетчиков
        public double? HotWaterPrevious { get; set; }
        public double? HotWaterCurrent { get; set; }
        public double? HotWaterConsumption { get; set; }
        public double? ColdWaterPrevious { get; set; }
        public double? ColdWaterCurrent { get; set; }
        public double? ColdWaterConsumption { get; set; }

        // Начальное сальдо
        public decimal? BalanceStartDebit { get; set; }
        public decimal? BalanceStartCredit { get; set; }

        // Начисления по статьям
        public decimal? Maintenance { get; set; }
        public decimal? Sewage { get; set; }
        public decimal? Elevator { get; set; }
        public decimal? HotWater { get; set; }
        public decimal? HotWaterSoi { get; set; }
        public decimal? ColdWater { get; set; }
        public decimal? ColdWaterSoi { get; set; }
        public decimal? ElectricitySoi { get; set; }

        public decimal? TotalAccrued { get; set; }
        public decimal? PaidAmount { get; set; }
        public DateTime? PaymentDate { get; set; }
        public string? PaymentDateRaw { get; set; }

        public decimal? DebtWithoutPenalty { get; set; }
        public decimal? Penalty { get; set; }
        public decimal? CommissionReimbursement { get; set; }
        public decimal? TotalDebit { get; set; }
        public decimal? BalanceEndDebit { get; set; }
        public decimal? BalanceEndCredit { get; set; }

        // Вычисляемые свойства (только геттеры)
        public bool IsPaid => PaidAmount.HasValue && PaidAmount.Value >= (TotalAccrued ?? 0);
        public string StatusText => IsPaid ? "✅ Оплачено" : "⏳ Ожидает оплаты";
        public string StatusColor => IsPaid ? "Green" : "Orange";
        public decimal Debt => (TotalAccrued ?? 0) - (PaidAmount ?? 0);
    }
}
