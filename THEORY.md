

| Status              | Description |
|---------------------|----------------------------------------------------------------------------------------------------|
| 200 OK              | The request succeeded. The result meaning of "success" depends on the HTTP method:                                                                                        |
| 201 Created         | The request succeeded, and a new resource was created as a result. This is typically the response sent after POST requests, or some PUT requests. |
| 202 Accepted        | The request has been received but not yet acted upon. It is noncommittal, since there is no way in HTTP to later send an asynchronous response indicating the outcome of the request. It is intended for cases where another process or server handles the request, or for batch processing. |
| 204 No Content      | There is no content to send for this request, but the headers may be useful. The user agent may update its cached headers for this resource with the new ones.                                                                         |
| 400 Bad Request     | The server cannot or will not process the request due to something that is perceived to be a client error (e.g., malformed request syntax, invalid request message framing, or deceptive request routing). |
| 401 Unauthorized    |  Although the HTTP standard specifies "unauthorized", semantically this response means "unauthenticated". That is, the client must authenticate itself to get the requested response.  |
| 403 Forbidden | The client does not have access rights to the content; that is, it is unauthorized, so the server is refusing to give the requested resource. Unlike 401 Unauthorized, the client's identity is known to the server.  |
| 404 Not Found       | The server cannot find the requested resource. In the browser, this means the URL is not recognized. In an API, this can also mean that the endpoint is valid but the resource itself does not exist.|
| 405 Not Allowed     | The request method is known by the server but is not supported by the target resource. For example, an API may not allow calling DELETE to remove a resource.  |
| 500 Internal Server Error | Internal Server Error |

| Methods | Description                  |
|---------|------------------------------|
| GET     | Fetch resource, GET should only retrieve data and should not contain a request content.                |
| POST    | Create resource, submits an entity to the specified resource               |
| PUT     | Update resource, The PUT method replaces all current representations of the target resource with the request    |
| TRACE   | The TRACE method performs a message loop-back test along the path to the target resource. |                
| DELETE  | Delete resource                     |
| PATCH   | update resource, method applies partial modifications to a resource. |


A resource is the model that the REST API manage, for example a customer.

All resourse routes are defined ising the plural form.

| Method    | Route                        | Description                        | Status Code                        | Payload                         |
|-----------|------------------------------|------------------------------------|------------------------------------|---------------------------------|
| GET       | /customers                   | All customers                      | 200                                | Returns a collectio             |
| GET       | /customers/{id}              | Customer with id                   | 200, 404 (customer not found)      | Returns an object                       |
| GET       | /customers/{id}/orders       | All orders for customer with id    | 200, 404 (customer not found)          | Returns an object                       |
| GET       | /customers/{id}/orders       | All orders for customer with id    | 200, 404 (customer not found)          | Returns an object with a collection         |
| GET       | /customers/{id}/orders/{oid} | Order oid for customer with id     | 200, 404 (customer or order not found) | Returns an object with a colletion of one  |
| POST      | /customers                   | Add a customer using content       | 201, 202, 400 (content is invalid)      | 
| PUT       | /customers/{id}              | Updates a customer                 | 202, 204, 404 (not found)  |
| PUT       | /customers                   | Updates a collection of customers  | 405                       |
| DELETE    | /customers/{id}              | Delete customer in a collection    | 204     




|
