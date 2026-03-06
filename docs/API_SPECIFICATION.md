# RemoteCare API Specification

## Overview

RESTful API built with ASP.NET Core 8, siguiendo REST conventions y JSON para request/response.

**Base URL**: `https://api.remotecare.com` (Development: `http://localhost:5000`)

**Authentication**: JWT Bearer token en header `Authorization: Bearer {token}`

**Content-Type**: `application/json`

---

## 1. Authentication Endpoints

### 1.1 Register User

**Endpoint**: `POST /api/auth/register`

**Description**: Registrar nuevo usuario (Senior o Support)

**Request**:
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!",
  "fullName": "John Doe",
  "role": "Senior"  // "Senior" | "Support" | "Admin"
}
```

**Validation**:
- Email: formato válido, unique
- Password: min 8 chars, 1 uppercase, 1 lowercase, 1 number, 1 special char
- FullName: min 3 chars, max 100
- Role: debe ser enum válido

**Response** (201 Created):
```json
{
  "id": 1,
  "email": "john@example.com",
  "fullName": "John Doe",
  "role": "Senior",
  "createdAt": "2024-03-05T10:30:00Z",
  "jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string_here",
  "expiresIn": 900
}
```

**Error Responses**:
- `400 Bad Request`: Validación fallida
  ```json
  {
    "errors": {
      "email": ["Email already exists"],
      "password": ["Password must contain uppercase"]
    }
  }
  ```
- `409 Conflict`: Email ya existe

**Status Codes**:
- 201: Éxito
- 400: Validación fallida
- 409: Email duplicado

---

### 1.2 Login

**Endpoint**: `POST /api/auth/login`

**Description**: Autenticar usuario

**Request**:
```json
{
  "email": "john@example.com",
  "password": "SecurePass123!"
}
```

**Response** (200 OK):
```json
{
  "id": 1,
  "email": "john@example.com",
  "fullName": "John Doe",
  "role": "Senior",
  "jwt": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "refresh_token_string_here",
  "expiresIn": 900,
  "devices": [
    {
      "id": 1,
      "deviceId": "uuid-1234",
      "deviceName": "Samsung Galaxy A10",
      "role": "Senior",
      "isActive": true,
      "pairedAt": "2024-03-01T08:00:00Z"
    }
  ]
}
```

**Error Responses**:
- `401 Unauthorized`: Email o password incorrecto

**Status Codes**:
- 200: Éxito
- 401: Credenciales inválidas
- 400: Request malformado

---

### 1.3 Refresh Token

**Endpoint**: `POST /api/auth/refresh`

**Description**: Obtener nuevo JWT usando refresh token

**Authentication**: Opcional (puede usar en header o body)

**Request**:
```json
{
  "refreshToken": "refresh_token_string_here"
}
```

**Response** (200 OK):
```json
{
  "jwt": "new_jwt_token_here",
  "refreshToken": "new_refresh_token_here",
  "expiresIn": 900
}
```

**Error Responses**:
- `401 Unauthorized`: Token inválido o expirado

---

### 1.4 Logout

**Endpoint**: `POST /api/auth/logout`

**Description**: Invalidar refresh token

**Authentication**: Required (JWT)

**Request**:
```json
{
  "refreshToken": "refresh_token_string_here"
}
```

**Response** (200 OK):
```json
{
  "message": "Logged out successfully"
}
```

---

## 2. Device Endpoints

### 2.1 Register Device

**Endpoint**: `POST /api/device/register`

**Description**: Registrar un nuevo dispositivo en el sistema

**Authentication**: Required (JWT)

**Request**:
```json
{
  "deviceId": "uuid-generated-by-android",
  "deviceName": "My Samsung Galaxy",
  "osVersion": "14",
  "role": "Senior"  // "Senior" | "Support"
}
```

**Response** (201 Created):
```json
{
  "id": 1,
  "deviceId": "uuid-generated-by-android",
  "deviceName": "My Samsung Galaxy",
  "userId": 1,
  "role": "Senior",
  "isActive": true,
  "registeredAt": "2024-03-05T10:30:00Z"
}
```

**Status Codes**:
- 201: Éxito
- 400: Device ya registrado
- 401: No autenticado

---

### 2.2 Generate QR Code (for Pairing)

**Endpoint**: `POST /api/device/generate-qr`

**Description**: Generar código QR único para emparejar dispositivos

**Authentication**: Required (JWT)

**Request**:
```json
{
  "deviceId": "uuid-generated-by-android"
}
```

**Response** (200 OK):
```json
{
  "pairingCode": "QR-ABC123DEF456",
  "qrData": "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...",
  "expiresIn": 300,
  "expiresAt": "2024-03-05T10:35:00Z"
}
```

**Details**:
- `pairingCode`: Código único válido 5 minutos
- `qrData`: Base64-encoded PNG image
- Almacenado en Redis o in-memory con TTL

**Status Codes**:
- 200: Éxito
- 401: No autenticado
- 404: Device no encontrado

---

### 2.3 Pair Devices (Scan QR)

**Endpoint**: `POST /api/device/pair`

**Description**: Emparejar dos dispositivos usando código QR

**Authentication**: Required (JWT)

**Request**:
```json
{
  "pairingCode": "QR-ABC123DEF456",
  "deviceId": "support-device-uuid"
}
```

**Response** (200 OK):
```json
{
  "sessionId": "session-uuid-1234",
  "seniorDevice": {
    "id": 1,
    "deviceName": "Samsung Galaxy A10",
    "userId": 5
  },
  "supportDevice": {
    "id": 2,
    "deviceName": "iPhone 14",
    "userId": 10
  },
  "status": "Active",
  "connectedAt": "2024-03-05T10:30:00Z",
  "wsUrl": "wss://api.remotecare.com/hubs/screen"
}
```

**Validation**:
- Pairing code válido y no expirado
- Pairing code no duplicado (una sola vez)
- Device owner != Senior device owner

**Error Responses**:
- `400 Bad Request`: Código inválido o expirado
- `409 Conflict`: Código ya usado
- `401 Unauthorized`: No autenticado

**Status Codes**:
- 200: Éxito
- 400: Código inválido
- 409: Código duplicado
- 401: No autenticado

---

### 2.4 Get My Devices

**Endpoint**: `GET /api/device/my-devices`

**Description**: Obtener todos los dispositivos del usuario actual

**Authentication**: Required (JWT)

**Query Parameters**:
- `role` (optional): Filter by "Senior" or "Support"
- `isActive` (optional): true/false

**Response** (200 OK):
```json
{
  "devices": [
    {
      "id": 1,
      "deviceId": "uuid-1234",
      "deviceName": "Samsung Galaxy A10",
      "role": "Senior",
      "isActive": true,
      "registeredAt": "2024-03-01T08:00:00Z",
      "lastSeen": "2024-03-05T10:25:00Z",
      "pairedWith": [
        {
          "deviceId": 2,
          "deviceName": "iPhone 14",
          "userId": 10,
          "userName": "Support User"
        }
      ]
    }
  ],
  "totalCount": 1
}
```

**Status Codes**:
- 200: Éxito
- 401: No autenticado

---

### 2.5 Delete Device

**Endpoint**: `DELETE /api/device/{deviceId}`

**Description**: Desregistrar un dispositivo

**Authentication**: Required (JWT)

**Permissions**: Solo owner o admin

**Response** (204 No Content):
```
(No content)
```

**Status Codes**:
- 204: Éxito
- 401: No autenticado
- 403: Sin permisos
- 404: Device no encontrado

---

## 3. Session Endpoints

### 3.1 Get Active Sessions

**Endpoint**: `GET /api/session/active`

**Description**: Obtener sesiones activas del dispositivo actual

**Authentication**: Required (JWT)

**Response** (200 OK):
```json
{
  "sessions": [
    {
      "id": 1,
      "sessionId": "session-uuid-1234",
      "seniorDeviceId": 1,
      "supportDeviceId": 2,
      "status": "Active",
      "startedAt": "2024-03-05T10:30:00Z",
      "endedAt": null,
      "frameRate": 15,
      "bytesTransferred": 5242880,
      "isEncrypted": true
    }
  ],
  "totalCount": 1
}
```

**Status Codes**:
- 200: Éxito
- 401: No autenticado

---

### 3.2 End Session

**Endpoint**: `POST /api/session/{sessionId}/end`

**Description**: Terminar una sesión de monitoreo

**Authentication**: Required (JWT)

**Permissions**: Participante de la sesión o admin

**Request**:
```json
{
  "reason": "User disconnect"  // optional
}
```

**Response** (200 OK):
```json
{
  "sessionId": "session-uuid-1234",
  "status": "Closed",
  "endedAt": "2024-03-05T10:45:00Z",
  "duration": 900,
  "stats": {
    "framesSent": 450,
    "bytesTransferred": 5242880,
    "avgLatency": 120
  }
}
```

**Status Codes**:
- 200: Éxito
- 401: No autenticado
- 403: Sin permisos
- 404: Session no encontrada

---

### 3.3 Get Session History

**Endpoint**: `GET /api/session/history`

**Description**: Obtener historial de sesiones del usuario

**Authentication**: Required (JWT)

**Query Parameters**:
- `page` (default: 1)
- `pageSize` (default: 20)
- `startDate` (ISO 8601)
- `endDate` (ISO 8601)

**Response** (200 OK):
```json
{
  "sessions": [
    {
      "id": 1,
      "sessionId": "session-uuid-1234",
      "seniorUser": {
        "id": 5,
        "fullName": "Maria Garcia"
      },
      "supportUser": {
        "id": 10,
        "fullName": "Juan Lopez"
      },
      "startedAt": "2024-03-05T10:30:00Z",
      "endedAt": "2024-03-05T10:45:00Z",
      "duration": 900,
      "status": "Completed",
      "stats": {
        "framesSent": 450,
        "bytesTransferred": 5242880,
        "avgLatency": 120
      }
    }
  ],
  "page": 1,
  "pageSize": 20,
  "totalCount": 45
}
```

**Status Codes**:
- 200: Éxito
- 401: No autenticado

---

## 4. SignalR Hub - Screen Streaming

### 4.1 Hub URL

```
wss://api.remotecare.com/hubs/screen?sessionId=session-uuid&token=jwt-token
```

### 4.2 Client to Server Methods

#### SendScreenFrame
Enviar frame de pantalla (desde dispositivo Senior)

**Parameters**:
```typescript
async SendScreenFrame(
  sessionId: string,
  frameData: string,  // Base64 encoded image
  timestamp: number
): Promise<void>
```

**Example** (C# client):
```csharp
await hubConnection.InvokeAsync("SendScreenFrame",
  sessionId,
  base64FrameData,
  DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
```

#### KeepAlive
Enviar heartbeat para mantener conexión activa

**Parameters**:
```typescript
async KeepAlive(sessionId: string): Promise<void>
```

---

### 4.3 Server to Client Methods

#### ReceiveScreenFrame
Recibir frame de pantalla (en dispositivo Support)

**Parameters**:
```typescript
ReceiveScreenFrame(
  frameData: string,    // Base64 encoded image
  timestamp: number,
  frameNumber: number
): void
```

**Example** (TypeScript/Kotlin):
```kotlin
hubConnection.on("ReceiveScreenFrame") { frameData: String, timestamp: Long, frameNumber: Int ->
    // Update UI with new frame
    updateScreenDisplay(frameData)
}
```

#### SessionEnded
Notificación cuando la sesión termina

**Parameters**:
```typescript
SessionEnded(
  sessionId: string,
  reason: string,  // "User disconnect", "Timeout", etc.
  stats: {
    duration: number,
    framesReceived: number,
    avgLatency: number
  }
): void
```

#### ErrorOccurred
Notificación de error

**Parameters**:
```typescript
ErrorOccurred(
  errorCode: string,
  message: string
): void
```

---

### 4.4 Connection Lifecycle

```
1. Client connects to WSS endpoint with sessionId + JWT
   ├── Backend validates JWT
   ├── Backend validates sessionId exists and user is participant
   ├── Backend adds connection to session group
   └── Client receives confirmation

2. Senior app starts sending frames
   ├── Capture screen every ~66ms (15 FPS)
   ├── Encode as JPEG or WebP
   ├── Convert to Base64
   └── Send via SendScreenFrame

3. Backend receives and broadcasts
   ├── Validate frame data
   ├── Broadcast to group (Support device)
   └── Track statistics

4. Support app receives and displays
   ├── Decode Base64
   ├── Display as image
   ├── Update at max 30 FPS
   └── Send optional ACKs

5. Either device sends close
   ├── Stop capturing/displaying
   ├── Notify peer
   ├── Close WebSocket
   └── Update session status
```

---

### 4.5 Error Handling

**Connection Errors**:
- Reconectar automáticamente (exponential backoff)
- Max 5 intentos de reconexión
- Después de 5 intentos, terminar sesión

**Frame Errors**:
- Ignorar frames malformados
- No romper la conexión
- Loguear para debugging

**Session Errors**:
- Si sesión termina en backend, notificar cliente
- Si dispositivo se desconecta, permitir reconexión

---

## 5. Admin Endpoints (opcional Phase 2)

### 5.1 Get All Users

**Endpoint**: `GET /api/admin/users`

**Authentication**: Required (JWT + Admin role)

**Query Parameters**:
- `page` (default: 1)
- `pageSize` (default: 50)
- `role` (filter)

**Response**: Listado paginado de usuarios

---

### 5.2 Get Audit Logs

**Endpoint**: `GET /api/admin/audit-logs`

**Authentication**: Required (JWT + Admin role)

**Query Parameters**:
- `userId` (filter)
- `action` (filter)
- `startDate`, `endDate`

**Response**: Eventos de auditoría

---

## 6. Error Response Format

**Standar error response**:

```json
{
  "error": "Validation error",
  "message": "Email already exists",
  "code": "VALIDATION_ERROR",
  "details": {
    "email": ["Email already exists"]
  },
  "timestamp": "2024-03-05T10:30:00Z"
}
```

**HTTP Status Codes**:
- `200 OK`: Éxito
- `201 Created`: Creación exitosa
- `204 No Content`: Éxito sin contenido
- `400 Bad Request`: Datos inválidos
- `401 Unauthorized`: No autenticado
- `403 Forbidden`: Sin permisos
- `404 Not Found`: Recurso no encontrado
- `409 Conflict`: Conflicto (ej: email duplicado)
- `429 Too Many Requests`: Rate limit excedido
- `500 Internal Server Error`: Error del servidor

---

## 7. Rate Limiting

**Por endpoint**:
- Auth (register, login): 5 requests/minuto por IP
- Device pairing: 10 requests/minuto por usuario
- Frame sending: Sin límite (conexión WebSocket)

**Headers de respuesta**:
```
X-RateLimit-Limit: 5
X-RateLimit-Remaining: 3
X-RateLimit-Reset: 1709615460
```

---

## 8. Data Models (DTOs)

### User DTO
```csharp
public class UserDto
{
    public int Id { get; set; }
    public string Email { get; set; }
    public string FullName { get; set; }
    public string Role { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### Device DTO
```csharp
public class DeviceDto
{
    public int Id { get; set; }
    public string DeviceId { get; set; }
    public string DeviceName { get; set; }
    public string Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime RegisteredAt { get; set; }
    public DateTime? LastSeen { get; set; }
}
```

### Session DTO
```csharp
public class SessionDto
{
    public int Id { get; set; }
    public string SessionId { get; set; }
    public int SeniorDeviceId { get; set; }
    public int SupportDeviceId { get; set; }
    public string Status { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
}
```

---

## 9. Testing the API

### Using curl

```bash
# Register
curl -X POST http://localhost:5000/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "fullName": "John Doe",
    "role": "Senior"
  }'

# Login
curl -X POST http://localhost:5000/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!"
  }'

# Register Device (with JWT)
curl -X POST http://localhost:5000/api/device/register \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer {jwt-token}" \
  -d '{
    "deviceId": "uuid-1234",
    "deviceName": "My Phone",
    "osVersion": "14",
    "role": "Senior"
  }'
```

### Using Postman
- Import collection desde `/docs/postman-collection.json`
- Configurar variables de entorno (base_url, token)
- Ejecutar requests

---

## 10. API Versioning (Future)

Cuando se necesite breaking changes:
- `/api/v1/auth/login` (deprecated)
- `/api/v2/auth/login` (new)

Mantener v1 durante transición de 6 meses.
