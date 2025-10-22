using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class TestRunner
{
    private readonly OrderRepository _repo;
    private const int TEST_CLIENT_ID = 1;
    private const int TEST_RESTAURANT_ID = 1;

    public TestRunner(OrderRepository repository)
    {
        _repo = repository;
    }

    public void RunAllTests()
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Clear();
        Console.WriteLine("===================================");
        Console.WriteLine("=== ЗАПУСК РЕАЛЬНЫХ ИНТЕГРАЦИОННЫХ ТЕСТОВ ===");
        Console.WriteLine("=== Клиент ID: 1, Ресторан ID: 1 ====");
        Console.WriteLine("===================================");

        TestCase_OrderCreationAndTotal();
        TestCase_FilterAndSortOrders();

        Console.WriteLine("\n=== ТЕСТЫ ЗАВЕРШЕНЫ ===");
    }

    // СЦЕНАРИЙ 1: ПРОЦЕСС СОЗДАНИЯ ЗАКАЗА И РАСЧЕТ СУММЫ (PlaceOrder)
    // Цель: Проверить, что заказ создается, а сумма рассчитывается верно.
    // Предусловия: Клиент 1, Ресторан 1, Блюда 1 и 2 существуют.
    public void TestCase_OrderCreationAndTotal()
    {
        Console.WriteLine("\n--- ТЕСТ 1: Создание Заказа и Расчет Суммы ---");

        const string deliveryAddress = "ул. Тест-Адрес, д. 1, кв. 1 (Т1)";

        var items = new List<OrderItem>
        {
            new OrderItem { DishId = 1, Quantity = 5, UnitPrice = 10.00 },
            new OrderItem { DishId = 2, Quantity = 1, UnitPrice = 20.00 }
        };
        const double expectedTotal = 70.00;
        int newOrderId = -1;

        try
        {
            newOrderId = _repo.PlaceOrder(TEST_CLIENT_ID, TEST_RESTAURANT_ID, deliveryAddress, items);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" 1.0: Критическая ошибка при PlaceOrder: {ex.Message}");
            Console.ResetColor();
            return;
        }

        if (newOrderId > 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" 1.1: Заказ успешно создан с ID: {newOrderId}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" 1.1: Ошибка создания заказа. ID недействителен.");
            Console.ResetColor();
            return;
        }

        var createdOrders = _repo.GetClientOrders(TEST_CLIENT_ID, newOrderId.ToString());
        var createdOrder = createdOrders.FirstOrDefault();

        if (createdOrder != null && createdOrder.TotalAmount == expectedTotal && createdOrder.DeliveryAddress == deliveryAddress)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" 1.2: Проверка данных заказа. Сумма {createdOrder.TotalAmount:C} и адрес верны.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" 1.2: Ошибка данных. Ожидалось {expectedTotal:C}, получено {createdOrder?.TotalAmount:C} | Адрес: {createdOrder?.DeliveryAddress}");
        }

        Console.ResetColor();
        if (newOrderId > 0)
        {
            _repo.DeleteOrder(newOrderId);
            Console.WriteLine($" Тестовый заказ №{newOrderId} удален.");
        }
    }

    // СЦЕНАРИЙ 2: ФИЛЬТРАЦИЯ И СОРТИРОВКА (GetClientOrders)
    // Цель: Проверить сортировку по сумме (DESC) и фильтрацию по ID.
    // Предусловия: У Клиента 1 должно быть несколько заказов с разными суммами.
    public void TestCase_FilterAndSortOrders()
    {
        Console.WriteLine("\n--- ТЕСТ 2: Сортировка по Сумме (DESC) и Фильтрация по ID ---");

        var orders = _repo.GetClientOrders(
            TEST_CLIENT_ID,
            searchTerm: null,
            sortField: OrderSortField.TotalAmount,
            isAscending: false);

        if (!orders.Any())
        {
            Console.WriteLine(" 2.0: Клиент 1 не имеет заказов. Проверьте тестовые данные в БД.");
            return;
        }

        bool isSortedCorrectly = true;
        for (int i = 0; i < orders.Count - 1; i++)
        {
            if (orders[i].TotalAmount < orders[i + 1].TotalAmount)
            {
                isSortedCorrectly = false;
                break;
            }
        }

        if (isSortedCorrectly)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" 2.1: Сортировка {orders.Count} заказов по сумме (DESC) прошла успешно.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(" 2.1: Ошибка сортировки. Элементы расположены не по убыванию суммы.");
        }

        var firstOrderId = orders.First().OrderId;
        var filteredOrders = _repo.GetClientOrders(
            TEST_CLIENT_ID,
            firstOrderId.ToString(),
            OrderSortField.OrderId,
            isAscending: true);

        bool isFilteredCorrectly = filteredOrders.Count == 1 && filteredOrders.First().OrderId == firstOrderId;

        if (isFilteredCorrectly)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($" 2.2: Фильтрация по ID заказа ({firstOrderId}) прошла успешно.");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($" 2.2: Ошибка фильтрации. Ожидался 1 заказ с ID {firstOrderId}, получено {filteredOrders.Count}.");
        }

        Console.ResetColor();
    }

}
