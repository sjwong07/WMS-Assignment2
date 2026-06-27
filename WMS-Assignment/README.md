# WMS Restaurant Ordering System

## Overview
A comprehensive in-dining restaurant ordering system built with ASP.NET Core MVC, Entity Framework Core, and SQL Server Express.

## Features

### Core Modules
1. **User Authentication & Authorization**
   - Role-based access (Admin, Staff, Customer)
   - Registration, Login, Logout
   - Password validation
   - Account lockout after failed attempts

2. **Menu Management**
   - CRUD operations for menu items
   - Category management
   - Search and filter
   - Availability toggle

3. **Order Management**
   - Create new orders
   - View order history
   - Order status tracking
   - Table assignment

4. **Table Management**
   - Table status tracking
   - Capacity management
   - Occupancy management

5. **Admin Dashboard**
   - Overview statistics
   - User management
   - Sales reports
   - Kitchen view

### Additional Features
- AJAX for real-time updates
- Client-side and server-side validation
- Responsive Bootstrap UI
- Toast notifications
- Export to CSV
- Print functionality
- Dark mode toggle
- Keyboard shortcuts
- Interactive menu selection

## Technologies Used
- ASP.NET Core MVC (.NET 10)
- Entity Framework Core
- SQL Server Express
- Bootstrap 5
- jQuery
- FontAwesome Icons

## Installation

### Prerequisites
- Visual Studio 2022 or later
- .NET 10 SDK
- SQL Server Express
- SQL Server Management Studio (optional)

### Setup Steps
1. Clone the repository
2. Open the solution in Visual Studio
3. Update the connection string in `appsettings.json`
4. Run migrations: `dotnet ef database update`
5. Run the application: `dotnet run`

### Default Login Credentials
- **Admin**: admin@restaurant.com / admin123
- **Staff**: staff@restaurant.com / staff123  
- **Customer**: customer@restaurant.com / customer123

## Project Structure