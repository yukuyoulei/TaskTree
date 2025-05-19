# TaskTree 任务管理系统

## 项目概述

TaskTree是一个现代化的任务管理系统，支持任务创建、分配、跟踪和关系管理。系统采用前后端分离架构，提供直观的用户界面和强大的API接口，适用于团队协作和项目管理场景。

## 技术栈

### 后端技术

- **框架**: ASP.NET Core 8.0
- **API风格**: RESTful API
- **数据库**: SQLite (使用Entity Framework Core作为ORM)
- **认证**: JWT (JSON Web Token)
- **API文档**: Swagger/OpenAPI
- **依赖注入**: 内置ASP.NET Core DI容器

### 前端技术

- **语言**: HTML5, CSS3, JavaScript (原生)
- **UI设计**: 响应式设计，自定义CSS
- **API通信**: Fetch API
- **状态管理**: 本地存储 (localStorage)

## 核心功能

- **用户管理**: 注册、登录、权限控制
- **任务管理**: 创建、编辑、删除、状态更新
- **任务分配**: 将任务分配给多个用户
- **任务关系**: 支持父子任务关系，构建任务树
- **优先级管理**: 任务优先级设置
- **截止日期**: 任务截止日期设置和提醒

## 项目结构

### 后端结构

```
backend/
├── Controllers/         # API控制器
├── Data/                # 数据访问层
├── Dtos/                # 数据传输对象
├── Models/              # 数据模型
├── Services/            # 业务逻辑服务
├── Program.cs           # 应用程序入口点和配置
└── appsettings.json     # 应用程序配置
```

### 前端结构

```
frontend/
├── css/                 # 样式文件
├── js/                  # JavaScript文件
│   ├── api.js           # API交互
│   ├── main.js          # 主逻辑
│   └── ui.js            # UI操作
├── index.html           # 首页
├── task-list.html       # 任务列表页
├── task-detail.html     # 任务详情页
└── user-management.html # 用户管理页
```

## 部署指南

### 后端部署

1. **环境要求**:
   - .NET 8.0 SDK或更高版本
   - SQLite数据库

2. **配置数据库**:
   - 数据库连接字符串在`appsettings.json`中配置
   - 初次运行会自动创建数据库和表结构

3. **配置JWT**:
   - 在`appsettings.json`中设置JWT密钥、发行者和受众

4. **运行应用**:
   ```bash
   cd backend
   dotnet restore
   dotnet build
   dotnet run
   ```

5. **API访问**:
   - API默认运行在`http://localhost:5038`
   - Swagger文档访问地址: `http://localhost:5038/swagger`

### 前端部署

1. **环境要求**:
   - 任何现代Web服务器(如Nginx, Apache)或简单的HTTP服务器

2. **配置API地址**:
   - 在`js/api.js`中设置`API_BASE_URL`为后端API地址

3. **部署静态文件**:
   - 将前端文件部署到Web服务器
   - 或使用简单的HTTP服务器运行:
     ```bash
     # 使用Python的简易HTTP服务器
     cd frontend
     python -m http.server 8080
     ```

4. **访问应用**:
   - 浏览器访问: `http://localhost:8080`

## 开发指南

### 后端开发

1. **添加新API端点**:
   - 在Controllers目录创建新控制器或扩展现有控制器
   - 使用适当的HTTP方法和路由

2. **数据模型扩展**:
   - 在Models目录添加新模型类
   - 在ApplicationDbContext中注册DbSet
   - 创建迁移并更新数据库:
     ```bash
     dotnet ef migrations add [MigrationName]
     dotnet ef database update
     ```

### 前端开发

1. **添加新页面**:
   - 创建新的HTML文件
   - 在导航栏中添加链接

2. **扩展API交互**:
   - 在`api.js`中添加新的API调用函数

3. **添加新功能**:
   - 在`main.js`中添加业务逻辑
   - 在`ui.js`中添加UI交互逻辑

## 安全注意事项

1. **生产环境配置**:
   - 使用强密钥生成JWT令牌
   - 启用HTTPS
   - 设置适当的CORS策略

2. **数据验证**:
   - 所有用户输入都应在前后端进行验证

3. **认证与授权**:
   - 确保敏感操作需要适当的权限
   - 定期刷新令牌

## 许可证

本项目采用MIT许可证。详见LICENSE文件。