using System;

public class OrderSummary
{
    public int OrderId { get; set; }
    public double TotalAmount { get; set; }
    public string StatusName { get; set; }
    public int StatusId { get; set; }
    public DateTime DateOrder { get; set; }
    public string RestaurantName { get; set; }
    public string DeliveryAddress { get; set; }

    public override string ToString() =>
        $"Заказ №{OrderId} от {DateOrder:dd.MM.yyyy HH:mm} | Ресторан: {RestaurantName} | Сумма: {TotalAmount:C} | СТАТУС: {StatusName}";

}
