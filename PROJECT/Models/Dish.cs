public class Dish
{
    public int DishId { get; set; }
    public string Name { get; set; }
    public double Price { get; set; }
    public int RestaurantId { get; set; }

    public override string ToString() => $"{DishId}. {Name} ({Price:C})";

}
