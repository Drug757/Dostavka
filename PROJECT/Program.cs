// Program.cs

public class Program
{
    private static readonly OrderRepository _repo = new OrderRepository();
    // Используем тестовый ID клиента, который вы создали в скрипте заполнения
    private const int TEST_CLIENT_ID = 1;

    public static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        // Главный цикл меню
        while (true)
        {
            Console.WriteLine("\n===================================");
            Console.WriteLine($"=== СЛУЖБА ДОСТАВКИ ЕДЫ | КЛИЕНТ ID: {TEST_CLIENT_ID} ===");
            Console.WriteLine("===================================");
            Console.WriteLine("Выберите действие:");
            Console.WriteLine("1. Оформить новый заказ");
            Console.WriteLine("2. Отследить мои заказы");
            Console.WriteLine("3. Посмотреть мой профиль");
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
                        TrackOrders();
                        break;
                    case "3":
                        ViewProfile();
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

    // --- МЕТОДЫ UI ---

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

    private static void TrackOrders()
    {
        Console.WriteLine("\n--- Отслеживание Заказов ---");
        var orders = _repo.GetClientOrders(TEST_CLIENT_ID);

        if (!orders.Any())
        {
            Console.WriteLine("У вас пока нет оформленных заказов.");
            return;
        }

        Console.WriteLine("Ваши последние заказы:");
        foreach (var order in orders)
        {
            Console.WriteLine(order);
        }

        int orderIdToTrack = ReadIntInput("\nВведите ID заказа для просмотра статуса (или 0 для возврата в меню): ");

        if (orderIdToTrack > 0)
        {
            var selectedOrder = orders.FirstOrDefault(o => o.OrderId == orderIdToTrack);

            if (selectedOrder != null)
            {
                Console.WriteLine("\n*** Обновление Статуса ***");
                Console.WriteLine($"Заказ №{selectedOrder.OrderId} | Ресторан: {selectedOrder.RestaurantName}");

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Текущий статус: {selectedOrder.StatusName}");
                Console.ResetColor();

                Console.WriteLine($"Сумма: {selectedOrder.TotalAmount:C}");
            }
            else
            {
                Console.WriteLine($"Заказ №{orderIdToTrack} не найден в вашем списке.");
            }
        }
    }

    // --- МЕТОД: Оформление нового заказа (логика из предыдущего ответа) ---
    private static void ProcessNewOrder()
    {
        Console.WriteLine("\n--- Адрес доставки ---");
        Console.Write("Введите полный адрес доставки: ");
        string address = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(address))
        {
            Console.WriteLine("Адрес не может быть пустым.");
            return;
        }

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
                    Console.WriteLine($"\n✅ Заказ №{newOrderId} успешно оформлен!");
                    Console.WriteLine($"Текущий статус: Новый.");
                    Console.ResetColor();
                }
                else
                {
                    Console.WriteLine("❌ Не удалось оформить заказ.");
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

    // Вспомогательный метод для ввода целых чисел
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