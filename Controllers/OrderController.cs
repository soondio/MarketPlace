﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata;
using WebApplicationLab2.Models1;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using System.ComponentModel;
using Microsoft.AspNetCore.Identity;
using Azure.Core.Pipeline;

namespace WebApplicationLab2.Controllers
{
    [Route("api/[controller]")]
    [EnableCors]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly CompClubWebContext _context;
        private readonly UserManager<User> _userManager;
        public OrdersController(CompClubWebContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/Orders
        [HttpGet]
        [Authorize(Roles = "admin")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrder()
        {
            var orders = await _context.Orders
                                        .Include(o => o.Client)
                                        .ToListAsync();

            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                TotalPrice = o.TotalPrice,
                ComputerId=o.ComputerId,
                StartTime = o.Date,
                EndTime = o.EndDate,
                Status=o.Status,
                Client = new ClientDto
                {
                    Id = o.Client.Id,
                    Name = o.Client.Name,
                    Email = o.Client.Email,
                }
            });

            return Ok(orderDtos);
        }

        // GET: api/Orders/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Order>> GetOrder(int id)
        {
            var Order = await _context.Orders.FindAsync(id);
            if (Order == null)
            {
                return NotFound();
            }
            return Order;
        }
        [HttpPut("/api/Orders/{id}/cancel")]
        public async Task<ActionResult<Order>> CancelOrder(int id)
        {
            // Получить заказ из базы данных
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // Если заказ уже отменен или выполнен, вернуть BadRequest
            if (order.Status =="выполнен" || order.Status=="отменён")
            {
                return BadRequest();
            }

            // Рассчитать количество денег, которые нужно вернуть на баланс клиента
            var refundAmount = order.TotalPrice;

            // Отменить заказ и вернуть деньги на баланс клиента
            order.Status = "отменён";
            order.EndDate = DateTime.UtcNow;
            var client = await _context.Clients.FindAsync(order.ClientId);
            client.Balance += refundAmount;

            // Сохранить изменения в базу данных
            await _context.SaveChangesAsync();

            return order;
        }

        [HttpGet("client/{id}")]
        public async Task<ActionResult<IEnumerable<OrderDto>>> GetOrdersByClientId(int id)
        {
            var clients = await _context.Clients.ToListAsync();
            User user = await GetCurrentUserAsync();
            if(user ==null)
            {
                return Unauthorized();
            }
            Client client = clients.FirstOrDefault(c=>c.Email==user.Email);
            if (client == null) { return NotFound(); }
            var orders = await _context.Orders
            .Include(o => o.Client)
            .Where(o => o.ClientId == id)
            .ToListAsync();
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                TotalPrice = o.TotalPrice,
                ComputerId = o.ComputerId,
                StartTime = o.Date,
                EndTime = o.EndDate,
                Status = o.Status,
                
            });
            return Ok(orderDtos);
        }


        [HttpPost]
        public async Task<ActionResult<Order>> CreateOrder(OrderDto orderDto)
        {
            // Проверяем, что такой клиент и компьютер существуют в базе данных
            var clientExists = await _context.Clients.AnyAsync(c => c.Id == orderDto.ClientId);
            var computerExists = await _context.Computers.AnyAsync(c => c.Id == orderDto.ComputerId);

            if (!clientExists || !computerExists)
            {
                return BadRequest("Клиент или компьютер не найден в базе данных");
            }

            // Получаем клиента по его id
            var client = await _context.Clients
                .Include(c => c.Orders) // загружаем заказы клиента
                .FirstOrDefaultAsync(c => c.Id == orderDto.ClientId);

            var computer = await _context.Computers.FindAsync(orderDto.ComputerId);

            // Рассчитываем количество часов и общую стоимость бронирования
            var hours = (decimal)orderDto.EndTime.Subtract(orderDto.StartTime).TotalHours;
            var bookingPrice = hours * computer.Priceperhour;

            if (client.Balance+client.Bonus < bookingPrice)
            {
                return BadRequest();
            }
            else
            {
                // Создаем новый заказ на основе переданных данных
                var order = new Order
                {
                    Client = client, // присваиваем клиента заказу
                    ComputerId = orderDto.ComputerId,
                    TotalPrice = bookingPrice, // общая стоимость заказа
                    Date = orderDto.StartTime.AddHours(3),
                    EndDate = orderDto.EndTime.AddHours(3),
                    Status = "оформлен"
                };

                // Вычитаем стоимость заказа из баланса клиента
                client.Bonus -= Convert.ToInt32(bookingPrice);
                if(client.Bonus<0)
                {
                    client.Balance += (decimal)client.Bonus;
                    client.Bonus = 0;
                }
                client.Bonus += Convert.ToInt32(bookingPrice * (decimal)0.1f);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                return CreatedAtAction("GetOrder", new { id = order.Id }, order);
            }

        }
        [HttpPut("{id}")]
        public async Task<IActionResult> PutOrder(int id, Order order)
        {
            if (id != order.Id)
            {
                return BadRequest();
            }
            _context.Entry(order).State = EntityState.Modified;
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!OrderExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return NoContent();
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }
        // DELETE: api/Orders/5
        [Authorize(Roles = "admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order= await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            _context.Orders.Remove(order);
            await _context.SaveChangesAsync();
            return NoContent();
        }
        private Task<User> GetCurrentUserAsync() => _userManager.GetUserAsync(HttpContext.User);
    }
    
}