# TaskGenie-AI 🤖⚡

> AI-Powered Task Assignment System — Automatically analyze project requirements and assign the most suitable developers.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Frontend | Angular 17 |
| Backend | .NET 8 Web API |
| AI Service | Python 3.11 + FastAPI |
| Database | MongoDB 7 |
| AI/LLM | OpenAI GPT-4o (configurable) |

## Project Structure

```
TaskGenie-AI/
├── frontend/          # Angular 17 SPA
├── backend/
│   └── TaskGenieAPI/  # .NET 8 Web API
├── ai-service/        # Python FastAPI AI service
└── docker-compose.yml
```

## Quick Start

### Prerequisites
- Node.js 18+
- .NET 8 SDK
- Python 3.11+
- MongoDB 7 (local or Atlas)
- (Optional) OpenAI API Key

---

### 1. Start MongoDB
```bash
# Option A: Docker
docker run -d -p 27017:27017 --name taskgenie-mongo mongo:7

# Option B: Use MongoDB Atlas connection string in appsettings.json
```

### 2. Python AI Service
```bash
cd ai-service
python -m venv venv
venv\Scripts\activate       # Windows
pip install -r requirements.txt

# Configure (optional OpenAI key)
copy .env.example .env
# Edit .env and add your OPENAI_API_KEY

# Start
uvicorn main:app --reload --port 8000
# API docs: http://localhost:8000/docs
```

### 3. .NET Web API
```bash
cd backend/TaskGenieAPI
dotnet run
# API: http://localhost:5000
# Default Admin: admin@taskgenie.ai / Admin@123
```

### 4. Angular Frontend
```bash
cd frontend
npm install
npm start
# App: http://localhost:4200
```

---

## Features

### 🤖 AI Analysis
- Upload PDF, DOCX, or TXT requirement documents
- AI extracts and classifies tasks automatically
- Categories: Frontend, Backend, Full Stack, Testing, DevOps, Design
- Falls back to rule-based NLP if no OpenAI key configured

### 🎯 Smart Developer Matching
Weighted scoring algorithm:
- **40%** Skill Match
- **25%** Experience Level
- **20%** Availability
- **15%** Current Workload

### 📊 Dashboard
- Total Projects / Pending / Assigned / Completed Tasks
- Developer availability and workload overview
- Recent assignment activity
- Task category breakdown

### 🔐 Authentication
- JWT-based auth (24-hour tokens)
- Roles: **Admin** and **Project Manager**
- Default admin: `admin@taskgenie.ai` / `Admin@123`

### 📋 Assignment Management
- AI-assisted or manual assignment
- Project Manager approve/reassign
- Full assignment history with timeline
- Real-time workload adjustment

### 📈 Reports
- Task status/category analytics
- Developer workload summary
- AI adoption rate tracking
- Assignment history export (CSV)

---

## API Endpoints

### Auth
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/register` | Register user |
| POST | `/api/auth/login` | Login (returns JWT) |
| GET | `/api/auth/me` | Current user |

### Projects
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/projects` | List projects |
| POST | `/api/projects` | Create project |
| POST | `/api/projects/:id/upload-requirements` | Upload PDF/DOCX/TXT |
| POST | `/api/projects/:id/analyze` | Run AI analysis |

### Tasks
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/tasks` | List tasks (filterable) |
| POST | `/api/tasks/:id/recommend` | Get AI recommendations |
| PATCH | `/api/tasks/:id/status` | Update status |

### Assignments
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/assignments` | Create assignment |
| POST | `/api/assignments/:id/reassign` | Reassign task |

### Reports
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/reports/dashboard` | Dashboard KPIs |
| GET | `/api/reports/developer-workload` | Developer workload |
| GET | `/api/reports/ai-stats` | AI adoption metrics |

### Python AI Service
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/extract-text` | Extract text from file |
| POST | `/analyze-requirements` | AI task classification |
| POST | `/match-developers` | Score developers for task |
| GET | `/docs` | Swagger UI |

---

## Configuration

### OpenAI (Optional)
Edit `ai-service/.env`:
```env
OPENAI_API_KEY=sk-...
OPENAI_MODEL=gpt-4o
```
If not configured, the system uses rule-based NLP analysis automatically.

### MongoDB
Edit `backend/TaskGenieAPI/appsettings.json`:
```json
{
  "MongoDbSettings": {
    "ConnectionString": "mongodb://localhost:27017",
    "DatabaseName": "taskgenie"
  }
}
```

---

## Docker (All Services)
```bash
cp ai-service/.env.example ai-service/.env
# Add your OPENAI_API_KEY to .env
docker-compose up -d
```
