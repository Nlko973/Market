# Market - WPF Client Application

## 📌 Project Overview

**Market** is the client-side desktop application of a retail automation system developed as part of a diploma project for  
**Rybinsk Multidisciplinary College**.

The application is built using WPF and communicates with a backend system via REST API.

---

## 🧩 Client Responsibilities

The client application provides:

- user authentication and registration
- session management
- employee management
- product management
- category management
- sales processing
- reporting and statistics

---

## 🪟 Application Windows

- **MainWindow** — user login  
- **Registration** — store and owner registration  
- **ConfirmationRegistration** — registration confirmation  
- **ForgotPassword** — password recovery request  
- **ResetPassword** — password reset  
- **Owner** — employee management panel  
- **Main** — employee dashboard  
- **Products** — product management  
- **Categories** — category management  
- **Sales** — sales processing  
- **Info** — reports and statistics  

---

## 🧩 Service Classes

- **ApiClient** — communication with backend API  
- **InputValidator** — input validation  
- **SessionStorage** — local session storage  
- **BackendSettings** — server configuration  

---

## 🛠 Technologies

- C#
- WPF (.NET Framework 4.8)
- XAML
- REST API backend
- Entity Framework 6 / SQLite (server-side)

---

## 🌐 Architecture

**WPF Client → REST API → Backend → Database**

---

## 🚀 Getting Started

```bash
git clone https://github.com/Nlko973/Market.git
```

📎 Backend repository: https://github.com/Nlko973/Market-backend
