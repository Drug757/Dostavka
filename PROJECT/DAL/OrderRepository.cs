using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

public enum OrderSortField
{
    DateOrder,
    OrderId,
    TotalAmount
}

public class OrderRepository
{
    private const string ConnectionString = "Data Source=FoodDelivery.db;";

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

    public Dictionary<int, string> GetOrderStatuses()
    {
        var statuses = new Dictionary<int, string>();
        string sql = "SELECT Status_Id, Status_Name FROM OrderStatus ORDER BY Status_Id";

        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    statuses.Add(reader.GetInt32(0), reader.GetString(1));
                }
            }
        }
        return statuses;
    }

    public List<OrderSummary> GetClientOrders(
        int clientId,
        string searchTerm = null,
        OrderSortField sortField = OrderSortField.DateOrder,
        bool isAscending = false)
    {
        var orders = new List<OrderSummary>();
        string sql = @"
            SELECT 
                O.Order_Id, O.Client_Id, O.Total_Amount, OS.Status_Name, O.Date_Order, R.Name, O.Delivery_Address, O.Status_Id
            FROM 
                ""Order"" O
            JOIN 
                OrderStatus OS ON O.Status_Id = OS.Status_Id
            JOIN 
                Restaurant R ON O.Restaurant_Id = R.Restaurant_Id
            WHERE 
                O.Client_Id = @ClientId";

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            if (int.TryParse(searchTerm, out int orderId))
            {
                sql += " AND O.Order_Id = @OrderId";
            }
            else
            {
                sql += " AND (OS.Status_Name LIKE @SearchTerm OR R.Name LIKE @SearchTerm)";
            }
        }

        string fieldName = sortField switch
        {
            OrderSortField.OrderId => "O.Order_Id",
            OrderSortField.TotalAmount => "O.Total_Amount",
            OrderSortField.DateOrder => "O.Date_Order",
            _ => "O.Date_Order"
        };

        string direction = isAscending ? "ASC" : "DESC";

        sql += $" ORDER BY {fieldName} {direction}";

        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@ClientId", clientId);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                if (int.TryParse(searchTerm, out int orderId))
                {
                    command.Parameters.AddWithValue("@OrderId", orderId);
                }
                else
                {
                    command.Parameters.AddWithValue("@SearchTerm", $"%{searchTerm}%");
                }
            }

            connection.Open();
            using (var reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    orders.Add(new OrderSummary
                    {
                        OrderId = reader.GetInt32(0),
                        ClientId = reader.GetInt32(1),
                        TotalAmount = reader.GetDouble(2),
                        StatusName = reader.GetString(3),
                        DateOrder = reader.GetDateTime(4),
                        RestaurantName = reader.GetString(5),
                        DeliveryAddress = reader.GetString(6),
                        StatusId = reader.GetInt32(7)
                    });
                }
            }
        }
        return orders;
    }

    public void UpdateClientAddress(int clientId, string street, string building, string apartment)
    {
        string sql = "UPDATE Client SET Street = @Street, Building = @Building, Apartment = @Apartment WHERE Client_Id = @ClientId";
        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@Street", street);
            command.Parameters.AddWithValue("@Building", building);
            // Обработка Apartment: если пусто или null, сохраняем DBNull
            command.Parameters.AddWithValue("@Apartment", string.IsNullOrWhiteSpace(apartment) ? (object)DBNull.Value : apartment);
            command.Parameters.AddWithValue("@ClientId", clientId);
            connection.Open();
            command.ExecuteNonQuery();
        }
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
                        INSERT INTO ""Order"" (Client_Id, Status_Id, Restaurant_Id, Total_Amount, Delivery_Address, Date_Order)
                        VALUES (@Client_Id, 1, @Restaurant_Id, @Total_Amount, @Delivery_Address, DATETIME('now'));
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

    public void UpdateOrderDetails(int orderId, int newStatusId, string newAddress)
    {
        string sql = "UPDATE \"Order\" SET Status_Id = @StatusId, Delivery_Address = @Address WHERE Order_Id = @OrderId";
        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@StatusId", newStatusId);
            command.Parameters.AddWithValue("@Address", newAddress);
            command.Parameters.AddWithValue("@OrderId", orderId);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

    public void DeleteOrder(int orderId)
    {
        string sql = "DELETE FROM \"Order\" WHERE Order_Id = @OrderId";
        using (var connection = new SqliteConnection(ConnectionString))
        using (var command = new SqliteCommand(sql, connection))
        {
            command.Parameters.AddWithValue("@OrderId", orderId);
            connection.Open();
            command.ExecuteNonQuery();
        }
    }

}
