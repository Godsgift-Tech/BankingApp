
# 🏦 BankingAPP Assessment

A secure **Bank Application** built with **ASP.NET Core, MediatR, FluentValidation, Entity Framework Core, and Redis**.
Supports **Customer** and **Admin** roles with JWT-based authentication, account management, and transaction processing.

## 🚀 Features

### 👤 **Customer**

* Register and login to obtain an **Access Token** (JWT).
* Create a new bank account (choose account type and currency).
* View account details by ID.
* Perform transactions:

  * **Deposit**
  * **Withdraw**
  * **Transfer** funds to another account.
* View transaction history for their account.

### 🛡 **Admin**

* View **all customer accounts**.
* Delete customer accounts.
* Generate and export transaction history to **PDF** or **Excel**.

## 🛠 Tech Stack

* **ASP.NET Core 7/8** – Web API framework
* **Entity Framework Core** – ORM for database access
* **MediatR** – CQRS and request/response handling
* **FluentValidation** – Request validation
* **Identity** – Authentication and role management
* **Redis** – Distributed caching
* **Serilog** – Logging
* **SQL Server** – Primary database

## 🔑 Authentication Flow

1. **Register** as a Customer via `/api/auth/register`.
2. **Login** to get a **JWT token** from `/api/auth/login`.
3. Use the token in the **Authorization header** for all subsequent API requests:

   Authorization: Bearer <your_token_here>

## 📡 API Endpoints

### **Auth**

| Method | Endpoint             | Description           | Roles  |
| ------ | -------------------- | --------------------- | ------ |
| POST   | `/api/auth/register` | Register new customer | Public |
| POST   | `/api/auth/login`    | Login & get JWT token | Public |

### **Accounts**

| Method | Endpoint              | Description          | Roles          |
| ------ | --------------------- | -------------------- | -------------- |
| GET    | `/api/account/{id}`   | Get account by ID    | Customer,Admin |
| GET    | `/api/account`        | Get all accounts     | Admin          |
| POST   | `/api/account/create` | Create a new account | Customer       |
| PUT    | `/api/account/{id}`   | Update account       | Customer,Admin |
| DELETE | `/api/account/{id}`   | Delete account       | Admin          |

### **Transactions**

| Method | Endpoint                       | Description                     | Roles          |
| ------ | ------------------------------ | ------------------------------- | -------------- |
| POST   | `/api/transaction/deposit`     | Deposit into an account         | Customer       |
| POST   | `/api/transaction/withdraw`    | Withdraw from an account        | Customer       |
| POST   | `/api/transaction/transfer`    | Transfer to another account     | Customer       |
| GET    | `/api/transaction/{accountId}` | Get account transaction history | Customer,Admin |

### **Export**

| Method | Endpoint                                            | Description                         | Roles |
| ------ | --------------------------------------------------- | ----------------------------------- | ----- |
| GET    | `/api/export/transactions/{accountId}?format=pdf`   | Export transaction history to PDF   | Admin |
| GET    | `/api/export/transactions/{accountId}?format=excel` | Export transaction history to Excel | Admin |


1. **Run the application**

bash
   dotnet run --project BankingAPP.API
  
2. Open **Swagger** at:

   https://localhost:7025/swagger
  
## 📌 Notes

* Admin and Customer roles are assigned during registration or manually via the database.
* All transaction operations are cached using Redis for better performance.
* MediatR is used for clean separation of commands, queries, and handlers.
* Validation is done using FluentValidation before request processing.

