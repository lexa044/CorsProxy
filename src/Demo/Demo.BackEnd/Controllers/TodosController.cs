using Demo.BackEnd.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Demo.BackEnd.Controllers
{
    [EnableCors("http://localhost:51562", "*", "*")]
    public class TodosController : ApiController
    {
        private static readonly List<Todo> _todos = new List<Todo>();
        private static bool _singletonInitialized = false;

        public TodosController()
        {
            if (!_singletonInitialized)
            {
                _todos.Add(new Todo() { Text = "learn angular", Done = true });
                _todos.Add(new Todo() { Text = "build an angular app", Done = false });
                _singletonInitialized = true;
            }
        }

        // GET api/todos
        public IEnumerable<Todo> Get()
        {
            return _todos;
        }

        // GET api/todos/5
        public Todo Get(int id)
        {
            if (id >= 0 && id < _todos.Count)
                return _todos[id];

            return null;
        }

        // POST api/todos
        public void Post([FromBody]Todo todo)
        {
            _todos.Add(todo);
        }

        // PUT api/todos/5
        public void Put(int id, [FromBody]Todo todo)
        {
        }

        // DELETE api/todos/5
        public void Delete(int id)
        {
        }
    }
}
