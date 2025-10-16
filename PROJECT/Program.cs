using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Program
{
    private static readonly OrderRepository _repo = new OrderRepository();
    private const int TEST_CLIENT_ID = 1;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;

        while (true)
        {
            Console.WriteLine("\n===================================");
            Console.WriteLine($"=== СЛУЖБА ДОСТАВКИ ЕДЫ | КЛИЕНТ ID: {TEST_CLIENT_ID} ===");
            Console.WriteLine("===================================");
            Console.WriteLine("Выберите действие:");
            Console.WriteLine("1. Оформить новый заказ");
            Console.WriteLine("2. Отследить мои заказы / Редактировать");
            Console.WriteLine("3. Посмотреть мой профиль");
            Console.WriteLine("4. Редактировать адрес доставки"); // НОВЫЙ ПУНКТ
            Console.WriteLine("0. Выход");
            Console.Write("Ваш выбор: ");

            string choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        ProcessNewOrder();
                        break;
                    case "2":
                        TrackOrders(null);
                        break;
                    case "3":
                        ViewProfile();
                        break;
                    case "4":
                        EditDeliveryAddress();
                        break;
                    case "0":
                        Console.WriteLine("До свидания!");
                        return;
                    default:
                        Console.WriteLine("Неверный выбор. Попробуйте снова.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\nКритическая ошибка: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    private static void EditDeliveryAddress()
    {
        var client = _repo.GetClient(TEST_CLIENT_ID);
        if (client == null)
        {
            Console.WriteLine("Ошибка: Профиль клиента не найден.");
            return;
        }

        Console.WriteLine("\n--- Редактирование адреса доставки ---");
        Console.WriteLine($"Текущий адрес: {client.FullAddress}");

        Console.Write($"Введите новую улицу (текущая: {client.Street}): ");
        string street = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(street)) street = client.Street;

        Console.Write($"Введите новый дом/корпус (текущий: {client.Building}): ");
        string building = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(building)) building = client.Building;

        Console.Write($"Введите новую квартиру (текущая: {client.Apartment ?? "нет"}, можно оставить пустым): ");
        string apartment = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(apartment)) apartment = null;


        if (string.IsNullOrWhiteSpace(street) || string.IsNullOrWhiteSpace(building))
        {
            Console.WriteLine("Улица и номер дома не могут быть пустыми. Обновление отменено.");
            return;
        }

        if (street == client.Street && building == client.Building && apartment == client.Apartment)
        {
            Console.WriteLine("Изменений не обнаружено.");
            return;
        }

        _repo.UpdateClientAddress(TEST_CLIENT_ID, street, building, apartment);

        var newClient = new Client { Street = street, Building = building, Apartment = apartment };

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine($"\n Адрес успешно обновлен!");
        Console.WriteLine($"Новый адрес: {newClient.FullAddress}");
        Console.ResetColor();
    }


    private static void ProcessNewOrder()
    {
        // 1. Получение адреса доставки из профиля
        var client = _repo.GetClient(TEST_CLIENT_ID);
        if (client == null)
        {
            Console.WriteLine("Ошибка: Профиль клиента не найден.");
            return;
        }
        string address = client.FullAddress;

        if (string.IsNullOrWhiteSpace(client.Street) || string.IsNullOrWhiteSpace(client.Building))
        {
            Console.WriteLine($"\n Ошибка: Адрес доставки в профиле ({address}) не полон (нет улицы или дома).");
            Console.WriteLine("Пожалуйста, сначала используйте меню '4. Редактировать адрес доставки'.");
            return;
        }

        Console.WriteLine($"\n--- Адрес доставки ---");
        Console.WriteLine($"Используется адрес из профиля: {address}");

        // 2. Выбор ресторана
        Console.WriteLine("\n--- Выбор ресторана ---");
        var restaurants = _repo.GetRestaurants();
        if (!restaurants.Any())
        {
            Console.WriteLine("В системе нет доступных ресторанов.");
            return;
        }

        foreach (var r in restaurants)
        {
            Console.WriteLine($"{r.RestaurantId}. {r.Name} ({r.Street}, {r.Building})");
        }

        int restaurantId = ReadIntInput("Введите ID ресторана: ");
        var selectedRestaurant = restaurants.FirstOrDefault(r => r.RestaurantId == restaurantId);

        if (selectedRestaurant == null)
        {
            Console.WriteLine("Неверный ID ресторана.");
            return;
        }

        // 3. Выбор блюд
        Console.WriteLine($"\n--- Меню: {selectedRestaurant.Name} ---");
        var menu = _repo.GetDishesByRestaurant(restaurantId);
        var orderItems = new List<OrderItem>();
        bool addingDishes = true;

        while (addingDishes)
        {
            Console.WriteLine("\nДобавление блюда:");
            foreach (var d in menu) Console.WriteLine(d);

            int dishId = ReadIntInput("Введите ID блюда для добавления (или 0 для завершения): ");
            if (dishId == 0)
            {
                addingDishes = false;
                continue;
            }

            var dish = menu.FirstOrDefault(d => d.DishId == dishId);
            if (dish == null)
            {
                Console.WriteLine("Неверный ID блюда.");
                continue;
            }

            int quantity = ReadIntInput($"Введите количество для '{dish.Name}': ");
            if (quantity <= 0)
            {
                Console.WriteLine("Количество должно быть больше 0.");
                continue;
            }

            orderItems.Add(new OrderItem { DishId = dish.DishId, Quantity = quantity, UnitPrice = dish.Price });
            Console.WriteLine($"Добавлено: {quantity} x {dish.Name}. Всего позиций в заказе: {orderItems.Count}");
        }

        // 4. Оформление заказа
        if (orderItems.Any())
        {
            double finalAmount = orderItems.Sum(i => i.Quantity * i.UnitPrice);
            Console.WriteLine($"\n--- Подтверждение заказа ---");
            Console.WriteLine($"Адрес: {address}");
            Console.WriteLine($"Сумма к оплате: {finalAmount:C}");

            Console.Write("Оформить заказ? (y/n): ");
            if (Console.ReadLine()?.ToLower() == "y")
            {
                int newOrderId = _repo.PlaceOrder(TEST_CLIENT_ID, restaurantId, address, orderItems);
                if (newOrderId > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"\n Заказ №{newOrderId} успешно оформлен! Текущий статус: Новый.");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine(" Не удалось оформить заказ.");
                }
            }
            else
            {
                Console.WriteLine("Заказ отменен пользователем.");
            }
        }
        else
        {
            Console.WriteLine("Заказ пуст. Оформление отменено.");
        }
    }


    private static void TrackOrders(string initialSearchTerm)
    {
        string searchTerm = initialSearchTerm;

        while (true)
        {
            Console.WriteLine("\n--- Управление Заказами ---");
            Console.WriteLine(searchTerm == null ? "Ваши последние заказы:" : $"Результаты поиска ('{searchTerm}'):");

            var orders = _repo.GetClientOrders(TEST_CLIENT_ID, searchTerm);

            if (!orders.Any())
            {
                Console.WriteLine(searchTerm == null
                    ? "У вас пока нет оформленных заказов."
                    : "Поиск не дал результатов.");
            }
            else
            {
                foreach (var order in orders)
                {
                    Console.WriteLine(order);
                }
            }

            Console.WriteLine("\nВыберите действие:");
            Console.WriteLine("S. Поиск (по ID, статусу или ресторану)");
            Console.WriteLine("E. Редактировать заказ (статус/адрес)");
            Console.WriteLine("D. Удалить заказ");
            Console.WriteLine("R. Сбросить поиск / Обновить список");
            Console.WriteLine("0. Назад в главное меню");
            Console.Write("Ваш выбор: ");

            string choice = Console.ReadLine()?.ToUpper();

            if (choice == "0") return;

            switch (choice)
            {
                case "S":
                    Console.Write("Введите ID заказа, статус или название ресторана для поиска: ");
                    searchTerm = Console.ReadLine();
                    break;
                case "E":
                    if (orders.Any()) EditOrderPrompt(orders);
                    searchTerm = null;
                    break;
                case "D":
                    if (orders.Any()) DeleteOrderPrompt(orders);
                    searchTerm = null;
                    break;
                case "R":
                    searchTerm = null;
                    break;
                default:
                    Console.WriteLine("Неверный выбор.");
                    break;
            }
        }
    }

    private static void EditOrderPrompt(List<OrderSummary> orders)
    {
        int orderId = ReadIntInput("\nВведите ID заказа для редактирования: ");
        var order = orders.FirstOrDefault(o => o.OrderId == orderId);

        if (order == null)
        {
            Console.WriteLine($"Заказ №{orderId} не найден в списке.");
            return;
        }

        if (order.StatusId != 1)
        {
            Console.WriteLine($" Редактирование невозможно. Заказ находится в статусе: {order.StatusName}.");
            return;
        }

        Console.WriteLine($"\nРедактирование заказа №{orderId}:");
        Console.WriteLine($"Текущий статус: {order.StatusName}");
        Console.WriteLine($"Текущий адрес: {order.DeliveryAddress}");

        Console.Write("Введите новый адрес доставки (оставьте пустым, чтобы не менять): ");
        string newAddress = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(newAddress)) newAddress = order.DeliveryAddress;

        // В рамках этой логики мы не меняем статус 'Новый' при редактировании
        int newStatusId = order.StatusId;

        Console.WriteLine("\n--- Подтверждение ---");
        Console.WriteLine($"Адрес: {newAddress}");
        Console.Write("Применить изменения? (y/n): ");

        if (Console.ReadLine()?.ToLower() == "y")
        {
            _repo.UpdateOrderDetails(orderId, newStatusId, newAddress);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n Заказ №{orderId} успешно обновлен!");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("Изменения отменены.");
        }
    }

    private static void DeleteOrderPrompt(List<OrderSummary> orders)
    {
        int orderId = ReadIntInput("\nВведите ID заказа для удаления: ");
        var order = orders.FirstOrDefault(o => o.OrderId == orderId);

        if (order == null)
        {
            Console.WriteLine($"Заказ №{orderId} не найден в списке.");
            return;
        }

        if (order.StatusId != 1 && order.StatusId != 5)
        {
            Console.WriteLine($" Удаление невозможно. Заказ находится в активной стадии: {order.StatusName}.");
            return;
        }

        Console.WriteLine($"\nВы действительно хотите удалить заказ №{orderId} ({order.StatusName}, {order.TotalAmount:C})?");
        Console.Write("Подтвердите удаление (y/n): ");

        if (Console.ReadLine()?.ToLower() == "y")
        {
            _repo.DeleteOrder(orderId);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n Заказ №{orderId} и все его позиции успешно удалены!");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("Удаление отменено.");
        }
    }

    // --- МЕТОД: Просмотр Профиля ---
    private static void ViewProfile()
    {
        var client = _repo.GetClient(TEST_CLIENT_ID);

        Console.WriteLine("\n--- Мой Профиль ---");
        if (client != null)
        {
            Console.WriteLine($"Никнейм: {client.Nickname}");
            Console.WriteLine($"Email: {client.Email}");
            Console.WriteLine($"Адрес доставки: {client.FullAddress}");
        }
        else
        {
            Console.WriteLine($"Клиент с ID {TEST_CLIENT_ID} не найден.");
        }
    }

    // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
    private static int ReadIntInput(string prompt)
    {
        Console.Write(prompt);
        if (int.TryParse(Console.ReadLine(), out int result))
        {
            return result;
        }
        return -1;
    }

}
