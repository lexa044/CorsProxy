var appConfig = {
    CrossDomain: true,
    ReverseProxyUri: 'http://localhost:51562/corsproxy/',
    AppBaseUri: 'http://localhost:51562/',
    ReverseProxyOnIEOnly: false //Setting this to false along with CrossDomain to true, will make this always use CorsProxy in case you want to test it not only on IE LT 10
};

angular.module('todoApp', [])
    .service('TodoService', function ($http) {
        var apiBase = "http://localhost:54160/todos/";

        var _getAll = function () {
            return $http.get(apiBase).then(function (results) {
                return results;
            });
        };

        var _add = function (todo) {
            return $http.post(apiBase, todo).then(function (results) {
                return results;
            });
        };
        var service = {
            getAll: _getAll,
            add: _add
        };

        return service;
    })
    .factory('httpCorsProxyInterceptor', function ($q, $templateCache) {

        var getIeVersion = function () {
            if (navigator.appName == "Microsoft Internet Explorer") {
                var ua = navigator.userAgent;
                var re = new RegExp("MSIE ([0-9]{1,}[.0-9]{0,})");
                if (re.exec(ua) != null) {
                    return parseInt(RegExp.$1);
                }
            } else {
                return false;
            }
        };

        var _request = function (config) {
            var attachReverseProxy = false;
            config.headers = config.headers || {};

            if (appConfig.CrossDomain && appConfig.ReverseProxyUri && config.url.indexOf(appConfig.AppBaseUri) == -1) {
                if (!$templateCache.get(config.url)) {
                    if (appConfig.ReverseProxyOnIEOnly) {
                        if (getIeVersion() && getIeVersion() < 10) {
                            attachReverseProxy = true;
                        }
                    } else {//Always ReverseProxy
                        attachReverseProxy = true;
                    }
                }
            }

            if (attachReverseProxy) {
                var url = config.url;
                var questionPos = url.indexOf('?');
                if (questionPos == -1) {
                    url += '?' + config.data;
                } else {
                    url += '&' + config.data;
                }
                config.headers['X-CorsProxy-Url'] = url;
                config.url = appConfig.ReverseProxyUri;
            }

            return config || $q.when(config);
        };

        var service = {
            request: _request
        };

        return service;
    })
  .controller('TodoListController', function (TodoService) {
      var todoList = this;
      todoList.todos = [];

      todoList.addTodo = function () {
          var newTodo = { Text: todoList.todoText, Done: false };
          todoList.todos.push(newTodo);
          todoList.todoText = '';
          TodoService.add(newTodo);
      };

      todoList.remaining = function () {
          var count = 0;
          angular.forEach(todoList.todos, function (todo) {
              count += todo.Done ? 0 : 1;
          });
          return count;
      };

      todoList.archive = function () {
          var oldTodos = todoList.todos;
          todoList.todos = [];
          angular.forEach(oldTodos, function (todo) {
              if (!todo.Done) todoList.todos.push(todo);
          });
      };

      TodoService.getAll().then(function (results) {
          todoList.todos = results.data;
      }, function (error) {
          alert("an error has occurred while processing your request");
      });
  }).config(function ($httpProvider) {
      $httpProvider.interceptors.push('httpCorsProxyInterceptor');
  });