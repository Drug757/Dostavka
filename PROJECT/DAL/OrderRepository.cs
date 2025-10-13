using Microsoft.Data.Sqlite;
using System.Data;

public class OrderRepository
{
    private const string ConnectionString = "Data Source=FoodDelivery.db;";

    public List<Restaurant> GetRestaurants()
    {
        var restaurants = new List<Restaurant>();
        string sql = "SELECT Restaurant_Id, Name, Street, Building, Apartment FROM Restaurant";

        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    restaurants.Add(new Restaurant
                    {
                        RestaurantId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Street = reader.GetString(2),
                        Building = reader.GetString(3),
                        Apartment = reader.IsDBNull(4) ? null : reader.GetString(4)
                    });
                }
            }
        }
        return restaurants;
    }

    public List<Dish> GetDishesByRestaurant(int restaurantId)
    {
        var dishes = new List<Dish>();
        string sql = "SELECT Dish_Id, Name, Price FROM Dish WHERE Restaurant_Id = @RestaurantId";

        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@RestaurantId", restaurantId);
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    dishes.Add(new Dish
                    {
                        DishId = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Price = reader.GetDouble(2),
                        RestaurantId = restaurantId
                    });
                }
            }
        }
        return dishes;
    }

    public int PlaceOrder(int clientId, int restaurantId, string deliveryAddress, List<OrderItem> items)
    {
        if (!items.Any()) return -1;

        using (var connection = new SqliteConnection(ConnectionString))
        {
            connection.Open();
            using (var transaction = connection.BeginTransaction())
            {
                try
                {
                    double totalAmount = items.Sum(i => i.Quantity * i.UnitPrice);

                    string orderSql = @"
                        INSERT INTO ""Order"" (Client_Id, Restaurant_Id, Total_Amount, Delivery_Address)
                        VALUES (@Client_Id, @Restaurant_Id, @Total_Amount, @Delivery_Address);
                        SELECT last_insert_rowid();";

                    int orderId;
                    using (var command = new SqliteCommand(orderSql, connection, transaction))
                    {
                        command.Parameters.AddWithValue("@Client_Id", clientId);
                        command.Parameters.AddWithValue("@Restaurant_Id", restaurantId);
                        command.Parameters.AddWithValue("@Total_Amount", totalAmount);
                        command.Parameters.AddWithValue("@Delivery_Address", deliveryAddress);

                        orderId = Convert.ToInt32(command.ExecuteScalar());
                    }

                    string itemSql = @"
                        INSERT INTO OrderItem (Order_Id, Dish_Id, Quantity, Unit_Price)
                        VALUES (@Order_Id, @Dish_Id, @Quantity, @Unit_Price);";

                    foreach (var item in items)
                    {
                        using (var command = new SqliteCommand(itemSql, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Order_Id", orderId);
                            command.Parameters.AddWithValue("@Dish_Id", item.DishId);
                            command.Parameters.AddWithValue("@Quantity", item.Quantity);
                            command.Parameters.AddWithValue("@Unit_Price", item.UnitPrice);
                            command.ExecuteNonQuery();
                        }
                    }

                    transaction.Commit();
                    return orderId;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при оформлении заказа: {ex.Message}");
                    transaction.Rollback();
                    return -1;
                }
            }
        }
    }

    public Client GetClient(int clientId)
    {
        Client client = null;
        string sql = "SELECT Client_Id, Nickname, Email, Street, Building, Apartment FROM Client WHERE Client_Id = @ClientId";

        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@ClientId", clientId);
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                {
                    client = new Client
                    {
                        ClientId = reader.GetInt32(0),
                        Nickname = reader.GetString(1),
                        Email = reader.GetString(2),
                        Street = reader.GetString(3),
                        Building = reader.GetString(4),
                        Apartment = reader.IsDBNull(5) ? null : reader.GetString(5)
                    };
                }
            }
        }
        return client;
    }


    public List<OrderSummary> GetClientOrders(int clientId)
    {
        var orders = new List<OrderSummary>();
        string sql = @"
            SELECT 
                O.Order_Id, O.Total_Amount, OS.Status_Name, O.Date_Order, R.Name 
            FROM 
                ""Order"" O
            JOIN 
                OrderStatus OS ON O.Status_Id = OS.Status_Id
            JOIN 
                Restaurant R ON O.Restaurant_Id = R.Restaurant_Id
            WHERE 
                O.Client_Id = @ClientId
            ORDER BY 
                O.Date_Order DESC";

        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@ClientId", clientId);
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    orders.Add(new OrderSummary
                    {
                        OrderId = reader.GetInt32(0),
                        TotalAmount = reader.GetDouble(1),
                        StatusName = reader.GetString(2),
                        DateOrder = reader.GetDateTime(3),
                        RestaurantName = reader.GetString(4)
                    });
                }
            }
        }
        return orders;
    }

}
