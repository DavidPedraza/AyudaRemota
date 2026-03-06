# RemoteCare App

A remote support application for helping elderly people with their Android devices from another Android device, with secure screen mirroring and device pairing via QR code.

## 📋 Overview

RemoteCare allows a **Support user** to monitor the screen of a **Senior user** remotely through a secure WebSocket connection initiated by scanning a QR code. Perfect for families who want to help aging relatives with technical issues without being physically present.

### Key Features

✅ **Secure Authentication** - JWT-based auth with refresh tokens
✅ **Device Pairing** - Simple QR code linking between devices
✅ **Real-time Screen Mirroring** - WebSocket-based live screen streaming
✅ **Multi-language Support** - Spanish, English, Catalan (designed for easy expansion)
✅ **Audit Logging** - Complete activity tracking for compliance
✅ **End-to-End Design** - Scalable architecture for future commercial release

---

## 🛠 Tech Stack

### Backend
- **Framework**: ASP.NET Core 8
- **Language**: C#
- **Database**: SQLite (dev), SQL Server (production)
- **ORM**: Entity Framework Core
- **Real-time**: SignalR with WebSockets
- **Auth**: JWT Bearer tokens

### Frontend
- **Mobile**: Android (Kotlin)
- **Senior App**: Screen capture & QR display
- **Support App**: QR scanner & screen viewer

### Infrastructure
- **Hosting**: Dondomino.es (or Render.com/Azure)
- **Database**: SQLite initially, SQL Server Express for production
- **SSL**: HTTPS/TLS 1.3 required

---

## 📁 Project Structure

```
remotecare/
├── src/
│   ├── RemoteCare.Api/                  # ASP.NET Core backend
│   │   ├── Controllers/                 # API endpoints
│   │   ├── Services/                    # Business logic
│   │   ├── Data/                        # EF Core & Migrations
│   │   ├── Models/                      # Domain entities
│   │   ├── Dtos/                        # API contracts
│   │   ├── Hubs/                        # SignalR hubs
│   │   ├── Middleware/                  # Custom middleware
│   │   ├── Localization/                # i18n resources
│   │   └── Program.cs                   # Entry point
│   └── RemoteCare.Common/               # Shared utilities (optional)
│
├── tests/
│   └── RemoteCare.Api.Tests/            # Unit & integration tests
│
├── android-senior/                      # Senior device app
│   └── app/src/main/
│       ├── java/com/remotecare/senior/
│       └── res/
│
├── android-support/                     # Support device app
│   └── app/src/main/
│       ├── java/com/remotecare/support/
│       └── res/
│
├── docs/                                # Documentation
│   ├── ARCHITECTURE.md                  # Architecture overview
│   ├── API_SPECIFICATION.md             # API endpoints
│   ├── DATABASE_SCHEMA.md               # Database design
│   ├── SECURITY_CONSIDERATIONS.md       # Security details
│   ├── DEVELOPMENT_GUIDE.md             # Dev setup
│   ├── i18n_IMPLEMENTATION.md           # Translation system
│   └── DEPLOYMENT_GUIDE.md              # Deployment guide
│
├── .gitignore
├── README.md                            # This file
├── CLAUDE.md                            # Claude Code instructions
├── RemoteCare.sln                       # Visual Studio solution
└── LICENSE
```

---

## 🚀 Quick Start

### Prerequisites
- **.NET SDK 8.0+**: [Download](https://dotnet.microsoft.com/download)
- **Git**: [Download](https://git-scm.com/)
- **Android Studio**: [Download](https://developer.android.com/studio)
- **Visual Studio Code**: [Download](https://code.visualstudio.com/)

### Setup Backend

```bash
# Clone repository
git clone https://github.com/yourusername/remotecare.git
cd remotecare

# Restore dependencies
dotnet restore

# Set up local database
cd src/RemoteCare.Api
dotnet ef database update

# Run development server
dotnet run

# API available at https://localhost:5001
# Swagger UI at https://localhost:5001/swagger
```

### Setup Android Apps

```bash
# Open Android Studio
# File → Open → android-senior (for senior app)
# File → Open → android-support (for support app)

# Build and run on emulator/device
```

---

## 📚 Documentation

Read documentation in this order:

1. **ARCHITECTURE.md** - Understand the overall design
2. **API_SPECIFICATION.md** - Learn all available endpoints
3. **DATABASE_SCHEMA.md** - See database structure
4. **SECURITY_CONSIDERATIONS.md** - Understand security measures
5. **DEVELOPMENT_GUIDE.md** - Start developing
6. **i18n_IMPLEMENTATION.md** - Add translations

---

## 🔐 Security

- ✅ JWT authentication with 15-min access tokens
- ✅ Bcrypt password hashing (12 rounds)
- ✅ TLS 1.3 for all communications
- ✅ Rate limiting on authentication endpoints
- ✅ Input validation and sanitization
- ✅ CORS configured restrictively
- ✅ Security headers (X-Frame-Options, CSP, etc.)
- ✅ Audit logging of all actions
- ✅ No sensitive data logged

See SECURITY_CONSIDERATIONS.md for detailed information.

---

## 🌐 API Endpoints (Summary)

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login
- `POST /api/auth/refresh` - Refresh JWT
- `POST /api/auth/logout` - Logout

### Devices
- `POST /api/device/register` - Register device
- `POST /api/device/generate-qr` - Generate pairing QR code
- `POST /api/device/pair` - Pair devices via QR
- `GET /api/device/my-devices` - Get user's devices
- `DELETE /api/device/{id}` - Delete device

### Sessions
- `GET /api/session/active` - Get active sessions
- `POST /api/session/{id}/end` - End session
- `GET /api/session/history` - Get session history

### WebSocket (SignalR)
- `wss://api.remotecare.com/hubs/screen` - Screen streaming hub

Full API documentation in API_SPECIFICATION.md

---

## 🎯 Development Workflow

### Creating a New Feature

1. **Create Entity** → Add C# model class
2. **Update DbContext** → Register in RemoteCareContext
3. **Create Migration** → `dotnet ef migrations add FeatureName`
4. **Create DTO** → Define API contracts
5. **Create Repository** → Implement data access
6. **Create Service** → Implement business logic
7. **Create Controller** → Expose REST endpoints
8. **Add Tests** → Write unit tests
9. **Test API** → Use REST client or Postman
10. **Commit** → Use conventional commits

See DEVELOPMENT_GUIDE.md for detailed walkthrough.

---

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run specific test
dotnet test --filter ClassName=AuthServiceTests

# Run with coverage
dotnet test /p:CollectCoverage=true
```

---

## 🌍 Internationalization

RemoteCare is designed for multiple languages:

- **Spanish (es)** - Default
- **English (en)** - Ready
- **Catalan (ca)** - Template prepared

Add new language in 3 steps:
1. Create `Localization/Resources/{lang}.json`
2. Create Android `values-{lang}/strings.xml`
3. Add language code to supported list

See i18n_IMPLEMENTATION.md for complete guide.

---

## 📦 Deployment

### Development
```bash
dotnet run
```

### Production (Dondomino.es or Render.com)
```bash
dotnet publish -c Release -o ./publish
```

See DEPLOYMENT_GUIDE.md for detailed deployment instructions.

---

## 🤝 Contributing

1. Create feature branch: `git checkout -b feature/my-feature`
2. Make changes and commit: `git commit -m "feat: Add feature"`
3. Push to remote: `git push origin feature/my-feature`
4. Create Pull Request
5. Request review and merge

### Commit Message Convention

```
feat: Add new feature
fix: Fix a bug
docs: Update documentation
style: Code formatting
refactor: Code reorganization
test: Add tests
chore: Maintenance
```

---

## 📊 Project Status

### Completed
- ✅ Architecture design
- ✅ API specification
- ✅ Database schema
- ✅ Security framework
- ✅ Localization system
- ✅ Documentation

### Sprint 1 (Planned)
- Authentication system (Register/Login)
- Device registration
- QR code generation
- Database setup

### Sprint 2 (Planned)
- Device pairing logic
- Session management
- WebSocket configuration

### Sprint 3 (Planned)
- Screen capture (Android)
- Real-time streaming
- UI implementation

### Sprint 4 (Planned)
- Optimization & testing
- Security hardening
- Documentation completion

---

## 🐛 Troubleshooting

### Database Issues
```bash
# Reset database
dotnet ef database drop
dotnet ef database update
```

### Port Already in Use
```bash
# Change port in appsettings.json
# or kill process
lsof -i :5001
kill -9 <PID>
```

### Dependencies Issue
```bash
# Clean and restore
dotnet clean
dotnet restore
dotnet build
```

See DEVELOPMENT_GUIDE.md for more troubleshooting.

---

## 📝 License

This project is licensed under the MIT License - see LICENSE file for details.

---

## 👥 Support

For questions or issues:
- Create GitHub issue
- Check documentation in `/docs` folder
- Review API_SPECIFICATION.md for endpoint details

---

## 🚀 Roadmap

**Phase 1 (MVP)** - Family use
- Core functionality
- Basic UI
- Security baseline

**Phase 2 (Enhancement)**
- Advanced features
- Performance optimization
- Additional languages

**Phase 3 (Commercial)**
- Admin dashboard
- Analytics
- Enterprise features
- Support system

---

## 📖 Quick Links

- [Architecture](docs/ARCHITECTURE.md)
- [API Specification](docs/API_SPECIFICATION.md)
- [Database Schema](docs/DATABASE_SCHEMA.md)
- [Security Guide](docs/SECURITY_CONSIDERATIONS.md)
- [Development Guide](docs/DEVELOPMENT_GUIDE.md)
- [Localization Guide](docs/i18n_IMPLEMENTATION.md)

---

**Last Updated**: March 5, 2024
**Version**: 0.1.0-alpha
